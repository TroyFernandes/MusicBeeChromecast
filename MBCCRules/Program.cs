using System;
using NetFwTypeLib;
using System.Security.Permissions;
using System.Linq;

namespace MBCCRules
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length != 1)
            {
                System.Console.WriteLine("Please enter a port number.");
                System.Console.WriteLine("usage: .exe [port] ");
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
                return;
            }

            string port = args[0];


            Console.WriteLine("Creating Firewall Rule");
            if (CreateFirewallRule(port))
            {
                Console.WriteLine("Created Firewall Rule (Name: Musicbee Chromecast)");
            }
            else
            {
                Console.WriteLine("Error creating firewall rule");
            }

            Console.WriteLine("Creating Access Rule");

            if (CreateAccessRule(port))
            {
                Console.WriteLine("Created access rule");
            }
            else
            {
                Console.WriteLine("Error creating access rule");
            }

            Console.WriteLine("Finished. Press any key to exit");
            Console.ReadLine();
        }

        private static bool CreateFirewallRule(string port)
        {
            try
            {
                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                INetFwRule firewallRule = firewallPolicy.Rules.OfType<INetFwRule>().Where(x => x.Name == "Musicbee Chromecast").FirstOrDefault();

                if (firewallRule == null)
                {
                    firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    firewallRule.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE;
                    firewallRule.Description = "Allow access within local network from port " + port;
                    firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                    firewallRule.Enabled = true;
                    firewallRule.InterfaceTypes = "All";
                    firewallRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
                    firewallRule.LocalPorts = port;
                    firewallRule.Name = "Musicbee Chromecast";
                    firewallPolicy.Rules.Add(firewallRule);
                }
                return true;
            }

            catch (Exception)
            {
                return false;
            }
        }

        private static bool CreateAccessRule(string port)
        {
            try
            {
                System.Diagnostics.Process.Start("netsh.exe", "http add urlacl url=http://*:" + port + "/ user=Everyone");

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
