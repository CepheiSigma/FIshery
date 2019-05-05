using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Fishery.Core;
using Fishery.Core.Data;
using Fishery.Core.Extension;
using Fishery.Core.System;

namespace Fishery.App.Service
{
    class Program : SharedObject
    {
        static void Main(string[] args)
        {
            if (args.Length < 1 || !Directory.Exists(args[0]))
            {
                Console.WriteLine("Root path is not valid!");
                Console.WriteLine("exiting...");
                Console.ReadKey();
                return;
            }

            if (!AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                var entryPoint = Assembly.GetExecutingAssembly();
                var applicationName = entryPoint.GetName().Name;
                var setup = new AppDomainSetup();
                setup.ApplicationName = applicationName;
                setup.ShadowCopyFiles = "true"; 

                AppDomain domain = AppDomain.CreateDomain(
                    applicationName,
                    AppDomain.CurrentDomain.Evidence,
                    setup);

                try
                {
                    domain.ExecuteAssembly(entryPoint.Location, args);
                }
                finally
                {
                    AppDomain.Unload(domain);
                }
            }
            else
            {
                Console.WriteLine($"{Info.ProductName} v{Info.CoverVersion} at {Info.HostOperationSystem}");
                Console.WriteLine(
                    $"[Info - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}/System]:EventRouter has been loaded");
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                EventRouter.GetInstance();
                IOManager.SetRootPath(args[0]);
                IOManager.SetDefaultDataStorage(new FileDBStorage());
                bool isUseSeparateAppdomain = args.Length >= 3 && args[2] == "separated";
                ExtensionManager extensionManager = ExtensionManager.GetInstance(isUseSeparateAppdomain);
                Console.WriteLine(
                    $"[Info - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}/System]:Core has been loaded");
                Console.WriteLine(
                    $"[Info - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}/ExtensionManager]:Starting load other extension");
                extensionManager.InitializeExtension();
                Console.WriteLine(
                    $"[Info - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}/ExtensionManager]:Loading other extension finished");
                Clock clock = new Clock();
                Console.WriteLine(
                    $"[Info - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}/System]:SystemClock has been loaded");
                if (args.Length >= 2 && args[1] == "daemon")
                {
                    while (true)
                    {
                        Thread.Sleep(30000);
                    }
                }
                else
                {
                    while (true)
                    {
                        string command = Console.ReadLine().ToLower();
                        switch (command)
                        {
                            case "run":
                                Console.Write("Extension:");
                                string extensionName = Console.ReadLine();
                                Console.Write("Method:");
                                string method = Console.ReadLine();
                                Console.Write("Param(splited with ,):");
                                string[] param = Console.ReadLine().Split(',');
                                IExtension extension = ExtensionManager.GetInstance().GetExtensionByName(extensionName);
                                BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Instance |
                                                     BindingFlags.Public;
                                if (!(extension is SharedObject))
                                {
                                    Console.WriteLine("No such extension");
                                    continue;
                                }

                                (extension as SharedObject).Invoke(method, flags, param);
                                Console.WriteLine("Excuted");
                                break;
                            case "event":
                                Console.Write("EventName:");
                                string eventName = Console.ReadLine();
                                EventRouter.GetInstance().FireEvent(eventName, null, null);
                                break;
                            case "analyze":
                                Console.WriteLine(AppDomain.CurrentDomain.FriendlyName);
                                Console.WriteLine("\tAllocated Memory:" +
                                                  AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize / 1024 +
                                                  " KB");
                                Console.WriteLine("\t     Used Memory:" +
                                                  AppDomain.CurrentDomain.MonitoringSurvivedMemorySize / 1024 + " KB");
                                foreach (var extensionProxy in ExtensionManager.GetInstance().GetExtensionList())
                                {
                                    Console.WriteLine(extensionProxy.DomainName);
                                    Console.WriteLine("\tAllocated Memory:" +
                                                      extensionProxy.TotalAllocatedMemorySize / 1024 + " KB");
                                    Console.WriteLine("\t     Used Memory:" + extensionProxy.SurvivedMemorySize / 1024 +
                                                      " KB");
                                }
                                break;
                            case "exit":
                                IOManager.BroadcastShutdownCommand();
                                return;
                        }
                    }
                }
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            EventRouter.GetInstance().FireEvent("Error_Occurred", sender, ((Exception) e.ExceptionObject).ToString());
        }
    }
}