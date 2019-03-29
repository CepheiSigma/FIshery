using System;
using System.IO;
using System.Text;
using Fishery.Core.Data;
using Fishery.Core.Extension;

namespace Fishery.Core.System
{
    public class IOManager
    {
        private static string _rootPath = "";
        private static BaseStorage _defaultDataStorage;

        public static void SetRootPath(string path)
        {
            _rootPath = path.Replace("\\", "/");
            if (!_rootPath.EndsWith("/"))
                _rootPath += "/";
            if (!Directory.Exists(_rootPath))
                throw new DirectoryNotFoundException();
            if (!Directory.Exists(GetRootPath() + "/cache/"))
                Directory.CreateDirectory(GetRootPath() + "/cache/");
            if (!Directory.Exists(GetRootPath() + "/extensions/"))
                Directory.CreateDirectory(GetRootPath() + "/extensions/");
            if (!Directory.Exists(GetRootPath() + "/data/"))
                Directory.CreateDirectory(GetRootPath() + "/data/");
        }

        public static FileStream GetFileStream(IExtension extension, string fileName, bool operateCache = false,
            bool needTruncate = false)
        {
            string path = GetPhysicalPath(extension, fileName, operateCache);
            if (!File.Exists(path) && needTruncate)
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            return new FileStream(path,
                needTruncate ? (FileMode.Truncate | FileMode.OpenOrCreate) : FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        public static bool Exists(IExtension extension, string fileName, bool operateCache = false)
        {
            return File.Exists(GetPhysicalPath(extension, fileName, operateCache));
        }

        public static MemoryStream GetMemoryStream(byte[] content)
        {
            return new MemoryStream(content);
        }

        public static void Delete(IExtension extension, string fileName, bool operateCache = false)
        {
            string path = GetPhysicalPath(extension, fileName, operateCache);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static string GetPhysicalPath(IExtension extension, string fileName, bool operateCache = false)
        {
            if (fileName.StartsWith("@"))
            {
                string extensionName = fileName.Substring(1, fileName.IndexOf(":") - 1);
                operateCache =
                    fileName.Substring(fileName.IndexOf(":") + 1, fileName.IndexOf("|") - 2 - extensionName.Length) ==
                    "cache";
                extension = ExtensionManager.GetInstance().GetExtensionByName(extensionName);
                fileName = fileName.Replace($"@{extensionName}:{(operateCache ? "cache" : "data")}|", "");
            }

            if (extension == null)
                return "<>ExtensionNotFound";
            string path = operateCache
                ? GetExtensionCachePath(extension) + fileName
                : GetDataPath(extension) + fileName;
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException());
            return operateCache ? GetExtensionCachePath(extension) + fileName : GetDataPath(extension) + fileName;
        }

        public static string GetRelativePath(IExtension extension, string fileName, bool operateCache = false)
        {
            return $"@{extension.GetExtensionName()}:{(operateCache ? "cache" : "data")}|{fileName}";
        }

        public static string[] GetFileList(IExtension extension, string directory = "", bool autoCreate = false,
            bool operateCache = false)
        {
            string path = GetPhysicalPath(extension, directory, operateCache);
            if (Directory.Exists(path))
                return Directory.GetFiles(path);
            if (autoCreate)
            {
                Directory.CreateDirectory(path);
            }

            return new string[0];
        }

        public static string ReadFileString(IExtension extension, string fileName, bool operateCache = false)
        {
            string path = GetPhysicalPath(extension, fileName, operateCache);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            return null;
        }

        public static byte[] ReadFileBytes(IExtension extension, string fileName, bool operateCache = false)
        {
            string path = GetPhysicalPath(extension, fileName, operateCache);
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }

            return null;
        }

        public static string WriteFile(IExtension extension, string fileName, Stream stream, bool operateCache = false,
            bool overwriteFile = true, long seekPosition = 0)
        {
            return WriteFileDirection(extension, fileName, stream, operateCache, overwriteFile, seekPosition);
        }

        public static string WriteFileDirection(IExtension extension, string fileName, Stream stream, bool operateCache = false,
            bool overwriteFile = true, long seekPosition = 0, int seekOrigin = 0)
        {
            using (FileStream fileStream = GetFileStream(extension, fileName, operateCache, overwriteFile))
            {
                if (fileStream.CanSeek)
                    fileStream.Seek(seekPosition, (SeekOrigin) seekOrigin);
                if (stream.CanSeek)
                    stream.Seek(0, SeekOrigin.Begin);
                BinaryWriter writer = new BinaryWriter(fileStream);
                BinaryReader reader = new BinaryReader(stream);
                byte[] buffer = new byte[409600];
                int lastReadCount;
                do
                {
                    lastReadCount = reader.Read(buffer, 0, 409600);
                    writer.Write(buffer, 0, lastReadCount);
                } while (lastReadCount == 409600);

                reader.Close();
                writer.Close();
                return GetRelativePath(extension, fileName, operateCache);
            }
        }

        public static string WriteFile(IExtension extension, string fileName, byte[] content, bool operateCache = false)
        {
            using (MemoryStream memoryStream = new MemoryStream(content))
            {
                return WriteFile(extension, fileName, memoryStream, operateCache);
            }
        }

        public static string WriteFile(IExtension extension, string fileName, string content, bool operateCache = false)
        {
            using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                return WriteFile(extension, fileName, memoryStream, operateCache);
            }
        }


        public static string AppendFile(IExtension extension, string fileName, byte[] content,
            bool operateCache = false, long position = 0)
        {
            using (MemoryStream memoryStream = new MemoryStream(content))
            {
                return WriteFileDirection(extension, fileName, memoryStream, operateCache, false, position,
                    (int) SeekOrigin.End);
            }
        }

        public static string AppendFile(IExtension extension, string fileName, string content,
            bool operateCache = false, long position = 0)
        {
            using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                return WriteFileDirection(extension, fileName, memoryStream, operateCache, false, position,
                    (int) SeekOrigin.End);
            }
        }

        public static string GetRootPath()
        {
            if (!Directory.Exists(_rootPath))
                throw new DirectoryNotFoundException();
            return _rootPath;
        }

        public static string GetDataPath(IExtension extension)
        {
            string path = GetRootPath() + "/data/" + extension.GetExtensionName().Replace(" ", "_") + "/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static string GetExtensionCachePath(IExtension extension)
        {
            string path = GetRootPath() + "/cache/" + extension.GetExtensionName().Replace(" ", "_") + "/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static string GetExtensionPath()
        {
            string path = GetRootPath() + "/extensions/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static string GetExtensionMetaPath()
        {
            string path = GetRootPath() + "/extension_metas/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static string GetDependencePath()
        {
            string path = GetRootPath() + "/dependencies/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static void SetDefaultDataStorage(BaseStorage storage)
        {
            _defaultDataStorage = storage ?? throw new NullReferenceException();
        }

        public static BaseStorage GetDefaultDataStorage()
        {
            if (_defaultDataStorage == null)
                throw new NullReferenceException();
            return _defaultDataStorage;
        }

        public static void BroadcastShutdownCommand()
        {
            EventRouter.GetInstance().FireEvent("System_Shutdown", null, null);
        }
    }
}