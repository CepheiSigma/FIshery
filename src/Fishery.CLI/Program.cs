using System;
using System.Net;
using System.Text;
using NDesk.Options;
using Sapphire.Windows.Net;

namespace Fishery.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            string host = "http://127.0.0.1:8187";
            bool forced = false;
            string mode = "";
            string version = "0";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var option = new OptionSet()
            {
                {
                    "h|help", "help",
                    h => Console.WriteLine("usage: Fishery.CLI.exe [-a Fishery-API-Address] [-f Force-Install] [-v Version] [command|-i <ExtensionId>:<Version/Optional>] ")
                },
                {
                    "a|api", "set host of fishery",
                    apiHost => host = apiHost
                },
                {
                    "f|force", "",
                    f =>
                    {
                        Console.WriteLine("!!! all operations will be enforced, i hope you know what you are doing");
                        forced = true;
                    }
                },
                {
                    "i|install",
                    "",
                    m => { mode = "INSTALL"; }
                },
                {
                    "u|update",
                    "",
                    m => { mode = "UPDATE"; }
                },
                {
                    "r|remove",
                    "",
                    m => { mode = "REMOVE"; }
                },
                {
                    "v|version", "",
                    v => { Console.WriteLine("Fishery Command Line Interface v0.0.2"); }
                },
                {
                    "<>",
                    "",
                    expression =>
                    {
                        switch (mode)
                        {
                            case "INSTALL":
                                string[] items = expression.Split(new[] {':'}, 2);
                                if (items.Length < 2)
                                    InstallExtension(host, items[0], "", forced);
                                else
                                    InstallExtension(host, items[0], items[1], forced);
                                break;
                            case "UPDATE":
                                UpateExtension(host,expression);
                                break;
                            case "REMOVE":
                                RemoveExtension(host,expression);
                                break;
                        }
                    }
                }
            };

            try
            {
                var r = option.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("Oooooops! ");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        static void InstallExtension(string host, string name, string versionCode, bool forced = false)
        {
            Console.WriteLine($"{name} installing...");
            HttpClient client = new HttpClient();
            client.Method = HttpMethod.POST;
            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            client.FormData.Add("name", new TextFormData(name));
            if(versionCode != "")
                client.FormData.Add("version", new TextFormData(versionCode));
            client.FormData.Add("force", new TextFormData(forced ? "1" : "0"));
            var response = client.SendRequest(host + "/modern-extension-installer/extensions");
            if (response.Status >= HttpStatusCode.OK && response.Status < HttpStatusCode.BadRequest)
            {
                Console.WriteLine($"[{response.Status}]{name} installed!");
            }
            else
            {
                Console.WriteLine($"[{response.Status}]install {name} failed!");
                Console.WriteLine($"{Encoding.UTF8.GetString(response.Content)}");
            }
        }

        static void UpateExtension(string host, string name)
        {
            Console.WriteLine($"{name} updating...");
            HttpClient client = new HttpClient();
            client.Method = HttpMethod.POST;
            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            var response = client.SendRequest(host + "/modern-extension-installer/extensions/"+ name+"/update");
            if (response.Status >= HttpStatusCode.OK && response.Status < HttpStatusCode.BadRequest)
            {
                Console.WriteLine($"[{response.Status}]{name} updated!");
            }
            else
            {
                Console.WriteLine($"[{response.Status}]update {name} failed!");
                Console.WriteLine($"{Encoding.UTF8.GetString(response.Content)}");
            }
        }

        static void RemoveExtension(string host, string name)
        {
            Console.WriteLine($"{name} uninstalling...");
            HttpClient client = new HttpClient();
            client.Method = HttpMethod.DELETE;
            var response = client.SendRequest(host + "/modern-extension-installer/extensions/" + name);
            if (response.Status >= HttpStatusCode.OK && response.Status < HttpStatusCode.BadRequest)
            {
                Console.WriteLine($"[{response.Status}]{name} updated!");
            }
            else
            {
                Console.WriteLine($"[{response.Status}]update {name} failed!");
                Console.WriteLine($"{Encoding.UTF8.GetString(response.Content)}");
            }
        }
    }
}