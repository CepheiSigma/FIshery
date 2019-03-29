using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Fishery.Core.Cron;
using Fishery.Core.Data;
using Fishery.Core.System;

namespace Fishery.Core.Extension
{
    public class ExtensionProxy : SharedObject
    {
        private Dictionary<string, Assembly> _loadedAssemblyList;
        private Dictionary<string, IExtension> _extensionList;
        private Dictionary<string, Assembly> _metaAssemblyList;
        private string _fileName;

        public void SetEnvironment(ExtensionManager extensionManager, EventRouter eventRouter,CronTab cronTab,BaseStorage storage,string rootPath)
        {
            ExtensionManager.GetInstance(extensionManager);
            EventRouter.GetInstance(eventRouter);
            //AppDomain.MonitoringIsEnabled = true;
            AppDomain.CurrentDomain.AssemblyResolve += _currentAppDomain_AssemblyResolve;
            _loadedAssemblyList = new Dictionary<string, Assembly>();
            _extensionList = new Dictionary<string, IExtension>();
            _metaAssemblyList = new Dictionary<string, Assembly>();
            IOManager.SetDefaultDataStorage(storage);
            IOManager.SetRootPath(rootPath);
            CronTab.GetInstance(cronTab);
        }

        private Assembly LoadAssembly(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            using (FileStream dllFileStream = File.OpenRead(filePath))
            {
                byte[] fileContent;
                fileContent = new byte[dllFileStream.Length];
                dllFileStream.Read(fileContent, 0, (int)dllFileStream.Length);
                return Assembly.Load(fileContent);
            }
        }

        private Assembly _currentAppDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (_metaAssemblyList.ContainsKey(args.Name))
                return _metaAssemblyList[args.Name];
            if (_loadedAssemblyList.ContainsKey(args.Name))
                return _loadedAssemblyList[args.Name];
            string strFileName = args.Name.Split(',')[0];
            string dllPath = $"{IOManager.GetDependencePath()}/{Path.GetFileNameWithoutExtension(_fileName)}/{strFileName}.dll";
            Assembly assembly;
            if (!File.Exists(dllPath))
            {
                dllPath = $"{IOManager.GetExtensionMetaPath()}/{strFileName}.dll";
                assembly = LoadAssembly(dllPath);
                if(assembly!=null)
                    _metaAssemblyList.Add(assembly.FullName,assembly);
            }
            else
            {
                assembly = LoadAssembly(dllPath);
                if(assembly!=null)
                    _loadedAssemblyList.Add(assembly.FullName,assembly);
            }
            return assembly;
        }

        public void LoadExtension(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            _fileName = fileName;

            Assembly assembly = LoadAssembly(fileName);
            if (!_loadedAssemblyList.ContainsKey(assembly.FullName))
                _loadedAssemblyList.Add(assembly.FullName, assembly);
            foreach (Type t in assembly.GetTypes())
            {
                if (t.GetInterface("IExtension") != null)
                {
                    IExtension extension = (IExtension) assembly.CreateInstance(t.FullName);
                    if (extension != null &&
                        ExtensionManager.GetInstance().GetExtensionByName(extension.GetExtensionName()) == null &&
                        !_extensionList.ContainsKey(extension.GetExtensionName()))
                    {
                        _extensionList.Add(extension.GetExtensionName(), extension);
                        EventRouter.GetInstance().FireEvent("Extension_Loaded", (object)extension, null);
                        Console.WriteLine(
                            $"[Info - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}/ExtensionManager]:{extension.GetExtensionName()} has been loaded");
                    }
                }
            }
        }

        public void Initialize()
        {
            foreach (var extension in _extensionList)
            {
                extension.Value.Initialize();
                EventRouter.GetInstance().FireEvent("Extension_Initialize", null,extension.Value.GetExtensionName());
            }
        }

        public void Uninitialize()
        {
            foreach (var extension in _extensionList)
            {
                extension.Value.Uninitialize();
            }
        }

        public long TotalAllocatedMemorySize => AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
        public long SurvivedMemorySize => AppDomain.CurrentDomain.MonitoringSurvivedMemorySize;
        public string DomainName => AppDomain.CurrentDomain.FriendlyName;

        public IExtension GetExtensionByName(string name)
        {
            if (_extensionList.ContainsKey(name))
                if (_extensionList[name] is SharedObject)
                    return _extensionList[name];
            return null;
        }

        public IExtension GetExtensionByType(Type t)
        {
            foreach (var extension in _extensionList)
            {
                if (extension.Value.GetType() == t && extension.Value is SharedObject)
                    return extension.Value;
            }

            return null;
        }

        public T GetExtensionByType<T>()
        {
            try
            {
                foreach (var extension in _extensionList)
                {
                    if (extension.Value is SharedObject && extension.Value.GetType() == typeof(T) ||
                        extension.Value.GetType().GetInterface(typeof(T).ToString()) != null ||
                        extension.Value.GetType().BaseType == typeof(T).BaseType)
                        return (T) extension.Value;
                }
            }
            catch (Exception)
            {
                return default(T);
            }

            return default(T);
        }

        public T GetExtensionNewInstanceByType<T>()
        {
            try
            {
                foreach (var extension in _extensionList)
                {
                    if (extension.Value is SharedObject && extension.Value.GetType() == typeof(T) ||
                        extension.Value.GetType().GetInterface(typeof(T).ToString()) != null ||
                        extension.Value.GetType().BaseType == typeof(T).BaseType)
                    {
                        T instance = (T) Activator.CreateInstance(extension.Value.GetType());
                        ((IExtension) instance).Initialize();
                        return instance;
                    }
                }
            }
            catch (Exception)
            {
                return default(T);
            }

            return default(T);
        }
    }
}