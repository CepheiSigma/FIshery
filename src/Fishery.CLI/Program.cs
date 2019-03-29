using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;
using Sapphire.Universal.Net;

namespace Fishery.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            string host = "http://127.0.0.1:8187";
            bool forced = false;
            string mode = "";
            Console.OutputEncoding = System.Text.Encoding.UTF8; 
            var option = new OptionSet () {
                { "h|host", "set host of fishery",
                    h => host = h },
                { "f|force", "",
                    f =>
                    {
                        Console.WriteLine("!!! all operations will be enforced, i hope you know what you are doing");
                        forced = true;
                    }
                },
                { "i|install", 
                    "",
                    m => { mode = "INSTALL"; }
                },
                { "<>", 
                    "",
                    expression =>
                    {
                        switch (mode)
                        {
                            case "INSTALL":
                                string[] items = expression.Split(new[]{':'}, 2);
                                InstallExtension(host,items[0],items[1]);
                                break;
                        }
                    }
                }
            };
            
            try {
                var r = option.Parse (args);
            }
            catch (OptionException e) {
                Console.Write ("Oooooops! ");
                Console.WriteLine (e.Message);
                Console.WriteLine (e.StackTrace);
            }

            Console.ReadLine();
        }

        static void InstallExtension(string host,string name,string versionCode)
        {
            Console.WriteLine($"{name} installing...");
            HttpClient client = new HttpClient();
            client.Method = HttpMethod.POST;
            client.Headers.Add("Content-Type","application/x-www-form-urlencoded");
            client.FormData.Add("name",new TextFormData(name));
            client.FormData.Add("version",new TextFormData(versionCode));
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
    }
}
