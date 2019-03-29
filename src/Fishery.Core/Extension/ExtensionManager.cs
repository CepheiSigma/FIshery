using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Policy;
using Fishery.Core.Cron;
using Fishery.Core.System;

namespace Fishery.Core.Extension
{
    public class ExtensionDomain
    {
        public ExtensionProxy Proxy;
        public AppDomain Domain;
        public string AssemblyName;
        public bool IsAbandoned;
    }

    public class ExtensionManager : SharedObject
    {
        private Dictionary<string, Assembly> _metaAssemblyList;
        private List<ExtensionDomain> _extensionDomainList;
        private static ExtensionManager _instance;
        private readonly bool _isUseSeparateAppdomain;
        public bool IsUseSeparateAppDomain => _isUseSeparateAppdomain;

        private ExtensionManager(bool isUseSeparateAppdomain = false)
        {
            _instance = this;
            _isUseSeparateAppdomain = isUseSeparateAppdomain;
            _metaAssemblyList = new Dictionary<string, Assembly>();
            _extensionDomainList = new List<ExtensionDomain>();
            AppDomain.CurrentDomain.AssemblyResolve += _currentAppDomain_AssemblyResolve;
        }

        private Assembly LoadAssembly(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            using (FileStream dllFileStream = File.OpenRead(filePath))
            {
                byte[] fileContent;
                fileContent = new byte[dllFileStream.Length];
                dllFileStream.Read(fileContent, 0, (int) dllFileStream.Length);
                return Assembly.Load(fileContent);
            }
        }

        private Assembly _currentAppDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (_metaAssemblyList.ContainsKey(args.Name))
                return _metaAssemblyList[args.Name];
            string strFileName = args.Name.Split(',')[0];
            string dllPath = $"{IOManager.GetExtensionMetaPath()}/{strFileName}.dll";
            Assembly assembly = LoadAssembly(dllPath);
            if(assembly!=null)
                _metaAssemblyList.Add(assembly.FullName, assembly);
            return assembly;
        }

        public void LoadMeta()
        {
            foreach (string fileName in Directory.GetFiles(IOManager.GetExtensionMetaPath()))
            {
                if (fileName.EndsWith(".dll"))
                {
                    Assembly assembly = LoadAssembly(fileName);
                    if (!_metaAssemblyList.ContainsKey(assembly.FullName))
                    {
                        _metaAssemblyList.Add(assembly.FullName, assembly);
                    }
                }
            }
        }

        public void InitializeExtension()
        {
            if (Directory.Exists(IOManager.GetExtensionMetaPath()))
                LoadMeta();
            if (Directory.Exists(IOManager.GetExtensionPath()))
            {
                foreach (var file in Directory.GetFiles(IOManager.GetExtensionPath()))
                {
                    LoadExtension(file);
                }
            }

            foreach (var domain in _extensionDomainList)
            {
                domain.Proxy.Initialize();
            }
        }

        public void LoadExtension(string fileName, bool autoInitialize = false)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            string assemblyName = Path.GetFileName(fileName);

            if (!_isUseSeparateAppdomain && Array.Find(AppDomain.CurrentDomain.GetAssemblies(),
                    assembly => assembly.GetName().Name == Path.GetFileNameWithoutExtension(fileName)) != null)
            {
                Console.WriteLine(
                    $"[Warning - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}/ExtensionManager]:An existing extension has been loaded in default appdomain.");
                return;
            }

            ExtensionDomain existExtensionDomain = _extensionDomainList.Find(ed => ed.AssemblyName == assemblyName);
            if (existExtensionDomain != null && _isUseSeparateAppdomain)
            {
                existExtensionDomain.Proxy.Uninitialize();
                _extensionDomainList.Remove(existExtensionDomain);
            }

            ExtensionProxy proxy;
            AppDomain ownerDomain;
            if (_isUseSeparateAppdomain)
            {
                Evidence evidence = new Evidence();
                AppDomainSetup appDomainSetup = new AppDomainSetup();
                appDomainSetup.ShadowCopyFiles = "true";
                appDomainSetup.ApplicationBase = Environment.CurrentDirectory;
                appDomainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
                ownerDomain = AppDomain.CreateDomain(fileName, evidence, appDomainSetup);

                Type type = typeof(ExtensionProxy);
                proxy =
                    (ExtensionProxy) ownerDomain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
            }
            else
            {
                ownerDomain = AppDomain.CurrentDomain;
                proxy = new ExtensionProxy();
            }

            proxy.SetEnvironment(ExtensionManager.GetInstance(), EventRouter.GetInstance(), CronTab.GetInstance(),
                IOManager.GetDefaultDataStorage(), IOManager.GetRootPath());
            proxy.LoadExtension(fileName);
            _extensionDomainList.Add(new ExtensionDomain()
            {
                Domain = ownerDomain,
                Proxy = proxy,
                AssemblyName = assemblyName,
                IsAbandoned = false
            });
            if (autoInitialize)
                proxy.Initialize();
            if (existExtensionDomain != null && _isUseSeparateAppdomain)
                AppDomain.Unload(existExtensionDomain.Domain);
        }

        public void UnloadExtension(string fileName, bool autoInitialize = false)
        {
            string assemblyName = Path.GetFileName(fileName);
            ExtensionDomain existExtensionDomain = _extensionDomainList.Find(ed => ed.AssemblyName == assemblyName);
            if (existExtensionDomain == null)
                return;
            existExtensionDomain.Proxy.Uninitialize();
            if (!_isUseSeparateAppdomain)
            {
                existExtensionDomain.IsAbandoned = true;
                Console.WriteLine(
                    $"[Warning - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}/ExtensionManager]:You can not unload any extension from default appdomain,you may need to enable useSeparateAppdomain to unload extension at runtime.");
            }
            else
            {
                _extensionDomainList.Remove(existExtensionDomain);
                AppDomain.Unload(existExtensionDomain.Domain);
            }
        }

        public IExtension GetExtensionByName(string name)
        {
            foreach (var domain in _extensionDomainList)
            {
                IExtension extension = domain.Proxy.GetExtensionByName(name);
                if (extension != null)
                    return extension;
            }

            return null;
        }


        public long TotalAllocatedMemorySize => AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
        public long SurvivedMemorySize => AppDomain.CurrentDomain.MonitoringSurvivedMemorySize;

        public IExtension GetExtensionByType(Type t)
        {
            foreach (var domain in _extensionDomainList)
            {
                IExtension extension = domain.Proxy.GetExtensionByType(t);
                if (extension != null)
                    return extension;
            }

            return null;
        }

        public T GetExtensionByType<T>()
        {
            foreach (var domain in _extensionDomainList)
            {
                if(domain.IsAbandoned)
                    continue;
                T extension = domain.Proxy.GetExtensionByType<T>();
                if (extension != null)
                    return extension;
            }

            return default(T);
        }

        public T GetExtensionNewInstanceByType<T>()
        {
            foreach (var domain in _extensionDomainList)
            {
                if(domain.IsAbandoned)
                    continue;
                T extension = domain.Proxy.GetExtensionNewInstanceByType<T>();
                if (extension != null)
                    return extension;
            }

            return default(T);
        }

        public List<ExtensionProxy> GetExtensionList()
        {
            List<ExtensionProxy> extensionList = new List<ExtensionProxy>();
            foreach (var extensionDomain in _extensionDomainList)
            {
                extensionList.Add(extensionDomain.Proxy);
            }

            return extensionList;
        }

        public static ExtensionManager GetInstance(ExtensionManager initialExtensionManager = null)
        {
            return _instance = _instance ?? initialExtensionManager ?? new ExtensionManager();
        }

        public static ExtensionManager GetInstance(bool isUseSeparateAppdomain,ExtensionManager initialExtensionManager = null)
        {
            return _instance = _instance ?? initialExtensionManager ?? new ExtensionManager(isUseSeparateAppdomain);
        }
    }
}