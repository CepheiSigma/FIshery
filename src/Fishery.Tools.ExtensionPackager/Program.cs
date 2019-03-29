using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;


namespace Fishery.Tools.ExtensionPackager
{
    class Program
    {
        static readonly string rootPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        private static Dictionary<string, List<Installation>> _repoExtensionList;
        private static string parentPath = "";

        static void Main(string[] args)
        {
            if (!Directory.Exists($"{rootPath}/metas"))
                Directory.CreateDirectory($"{rootPath}/metas");
            if (!Directory.Exists($"{rootPath}/extensions"))
                Directory.CreateDirectory($"{rootPath}/extensions");
            _repoExtensionList = new Dictionary<string, List<Installation>>();
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "/repo.json"))
                _repoExtensionList = new Serializer().DeSerializeFromFile<Dictionary<string, List<Installation>>>(
                                         AppDomain.CurrentDomain.BaseDirectory + "/repo.json") ??
                                     new Dictionary<string, List<Installation>>();
            if (args.Length < 1 || !File.Exists(args[0]))
            {
                Console.WriteLine("file not specified!");
                Console.WriteLine("exiting...");
                Console.ReadKey();
                return;
            }

            PackageExtension(args[0]);
            Console.ReadKey();
        }

        static void PackageExtension(string path)
        {
            parentPath = Path.GetDirectoryName(path);
            AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs erArgs) =>
            {
                string strFileName = erArgs.Name.Split(',')[0];
                string dllPath = $"{parentPath}/{strFileName}.dll";
                return Assembly.LoadFile(dllPath);
            };
            Installation installation = new Installation()
            {
                FileList = new List<string>(),
                Dependencies = new List<Dependency>(),
                MetaDependencies = new List<MetaDependency>(),
                UpdateLog = new Stack<UpdateLog>()
            };
            bool isMeta = false;
            if (path.EndsWith(".Meta.dll"))
            {
                Assembly metaAssembly = Assembly.LoadFile(path);
                installation.Id = metaAssembly.GetName().Name;
                installation.FileList.Add($"extension_metas/{installation.Id}.dll");
                isMeta = true;
            }
            else
            {
                Assembly extensionAssembly = Assembly.LoadFile(path);
                bool hasValidExtension = false;
                foreach (Type t in extensionAssembly.GetTypes())
                {
                    if (t.GetInterface("IExtension") != null)
                    {
                        hasValidExtension = true;
                    }
                }

                if (!hasValidExtension)
                {
                    Console.WriteLine("valid extension not found!");
                    Console.WriteLine("exiting...");
                    Console.ReadKey();
                    return;
                }

                installation.Id = extensionAssembly.GetName().Name;

                AssemblyName[] referencedAssemblyList;
                referencedAssemblyList = extensionAssembly.GetReferencedAssemblies();

                List<MetaDependency> metaList = new List<MetaDependency>();
                List<Dependency> extensionList = new List<Dependency>();
                List<string> depList = new List<string>();

                foreach (var assemblyName in referencedAssemblyList)
                {
                    if (File.Exists($"{parentPath}/{assemblyName.Name}.dll") && assemblyName.Name != "Fishery.Core")
                    {
                        if (assemblyName.Name.EndsWith(".Meta"))
                        {
                            metaList.Add(new MetaDependency() {Name = assemblyName.Name});
                        }
                        else if (IsExtension($"{parentPath}/{assemblyName.Name}.dll"))
                        {
                            extensionList.Add(new Dependency() {Name = assemblyName.Name});
                        }
                        else
                        {
                            depList.Add(assemblyName.Name);
                        }
                    }
                }

                foreach (var dependency in metaList)
                {
                    dependency.VersionCode =
                        ReadInt($"Which version of the [Meta]{dependency.Name} is using?", "VersionCode");
                    installation.MetaDependencies.Add(dependency);
                }

                foreach (var dependency in extensionList)
                {
                    dependency.VersionCode =
                        ReadInt($"Which version of the [Extension]{dependency.Name} is using?", "VersionCode");
                    installation.Dependencies.Add(dependency);
                }

                foreach (var dep in depList)
                {
                    List<string> depReferenceList = ResolveDependencyReferenceList(dep);
                    foreach (var depReference in depReferenceList)
                    {
                        installation.FileList.Add($"dependencies/{installation.Id}/{depReference}.dll");
                    }
                }
            }

            Installation latestInstallation = GetExistPackageInfo(installation.Id);
            bool useExistInfo = false;
            if (latestInstallation != null)
            {
                useExistInfo =
                    ReadString("Find last version of the extension use exist info?", "[Y/y]", "Y").ToUpper() == "Y";
                if (latestInstallation.UpdateLog.Count > 4)
                    installation.UpdateLog = new Stack<UpdateLog>(latestInstallation.UpdateLog.Take(4));
                else
                    installation.UpdateLog = new Stack<UpdateLog>(latestInstallation.UpdateLog.Take(latestInstallation.UpdateLog.Count));
            }

            if (useExistInfo)
            {
                installation.Name = latestInstallation.Name;
                installation.Author = latestInstallation.Author;
                installation.Summary = latestInstallation.Summary;
            }
            else
            {
                installation.Name = ReadString("What's the friendly name of this extension", "Name");
                installation.Author = ReadString("What's your name", "Author");
                installation.Summary = ReadString("What's the description of the extension", "Summary");
            }

            installation.VersionCode = ReadInt("What's the version code of the extension", "VersionCode",
                latestInstallation != null ? (latestInstallation.VersionCode + 1).ToString() : "");
            installation.Version = ReadString("What's the version of the extension", "Version",
                ConvertVersionCodeToVersionString(installation.VersionCode));
            if (latestInstallation != null)
            {
                UpdateLog updateLog = new UpdateLog();
                if (installation.VersionCode <= latestInstallation.VersionCode)
                {
                    Console.WriteLine("The version what you entered was lower than latest!");
                    Console.ReadKey();
                    return;
                }

                updateLog.OriginalVersion = latestInstallation.Version;
                updateLog.OriginalVersionCode = latestInstallation.VersionCode;
                updateLog.TargetVersion = installation.Version;
                updateLog.TargetVersionCode = installation.VersionCode;
                updateLog.UpdateContent = ReadString("What has been updated?", "Content");
                installation.UpdateLog.Push(updateLog);
            }

            installation.EntryPoint = "";
            if (!isMeta)
            {
                installation.EntryPoint = "extensions/" + installation.Id + ".dll";
                installation.FileList.Add(installation.EntryPoint);
            }

            FileStream fileStream = new FileStream(
                $"{rootPath}/{(isMeta ? "metas" : "extensions")}/{installation.Id}.{installation.VersionCode}.zip",
                FileMode.OpenOrCreate);
            ZipOutputStream zipOutputStream = new ZipOutputStream(fileStream);
            ZipEntryFactory zipEntryFactory = new ZipEntryFactory();
            byte[] buffer = new byte[4096];
            foreach (var filePath in installation.FileList)
            {
                ZipEntry entry = zipEntryFactory.MakeFileEntry(filePath);
                zipOutputStream.PutNextEntry(entry);
                StreamUtils.Copy(
                    new FileStream($"{parentPath}/{Path.GetFileName(filePath)}", FileMode.Open, FileAccess.Read,
                        FileShare.Read), zipOutputStream, buffer);
                zipOutputStream.CloseEntry();
            }

            zipOutputStream.PutNextEntry(zipEntryFactory.MakeFileEntry("info.json"));
            StreamUtils.Copy(new MemoryStream(Encoding.UTF8.GetBytes(new Serializer().SerializeToString(installation))),
                zipOutputStream, buffer);
            zipOutputStream.CloseEntry();
            zipOutputStream.Close();
            fileStream.Close();
            if (latestInstallation == null)
                _repoExtensionList.Add(installation.Id, new List<Installation>() {installation});
            else
                _repoExtensionList[installation.Id].Add(installation);
            new Serializer().SerializeToFile(_repoExtensionList,
                AppDomain.CurrentDomain.BaseDirectory + "/repo.json");
            Console.WriteLine($"{installation.Id}'s package file has been saved");
        }

        static int ReadInt(string tips, string enterItemName, string defaultValue = "")
        {
            while (true)
            {
                try
                {
                    Console.WriteLine(tips);
                    Console.Write(enterItemName + ",default - " + defaultValue + ":");
                    string input = Console.ReadLine();
                    if (String.IsNullOrEmpty(input))
                        return Int32.Parse(defaultValue);
                    return Int32.Parse(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error:" + ex.Message);
                    continue;
                }
            }
        }

        static string ReadString(string tips, string enterItemName, string defaultValue = "")
        {
            while (true)
            {
                try
                {
                    Console.WriteLine(tips);
                    Console.Write(enterItemName + ",default - " + defaultValue + ":");
                    string content = Console.ReadLine();
                    if (String.IsNullOrEmpty(content))
                        content = defaultValue;
                    return content;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error:" + ex.Message);
                    continue;
                }
            }
        }

        static bool IsExtension(string fileName)
        {
            if (!File.Exists(fileName))
                return false;
            Assembly assembly = Assembly.LoadFile(fileName);
            foreach (Type t in assembly.GetTypes())
            {
                if (t.GetInterface("IExtension") != null)
                {
                    return true;
                }
            }

            return false;
        }

        static List<string> ResolveDependencyReferenceList(string name)
        {
            string dllPath = $"{parentPath}/{name}.dll";
            if (!File.Exists(dllPath))
                return new List<string>();
            Assembly assembly = Assembly.LoadFile(dllPath);
            List<string> referenceList = new List<string> {name};
            AssemblyName[] referencedAssemblyList = assembly.GetReferencedAssemblies();
            foreach (var assemblyName in referencedAssemblyList)
            {
                referenceList.AddRange(ResolveDependencyReferenceList(assemblyName.Name));
            }

            return referenceList;
        }

        static Installation GetExistPackageInfo(string id)
        {
            if (_repoExtensionList.ContainsKey(id))
            {
                Installation latestInstallation = _repoExtensionList[id].Last();
                return latestInstallation;
            }

            return null;
        }

        static string ConvertVersionCodeToVersionString(int versionCode)
        {
            string versionString = versionCode.ToString().PadLeft(3, '0');

            versionString = versionString.Substring(0, versionString.Length - 2) + "." +
                            versionString.Substring(versionString.Length - 2, 1) + "." +
                            versionString.Substring(versionString.Length - 1, 1);
            return versionString;
        }
    }
}