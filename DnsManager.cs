using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;

namespace ZeusDNSChanger
{
    public class DnsManager
    {
        public static class DnsServers
        {
            public static readonly (string Primary, string Secondary) ZeusFree = ("37.32.5.60", "");
            public static readonly (string Primary, string Secondary) ZeusPlus = ("37.32.5.34", "");
            public static readonly (string Primary, string Secondary) Google = ("8.8.8.8", "8.8.4.4");
            public static readonly (string Primary, string Secondary) Cloudflare = ("1.1.1.1", "1.0.0.1");
        }

        public static string GetActiveNetworkInterface()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .ToList();

                if (interfaces.Any())
                {
                    var activeInterface = interfaces.FirstOrDefault(ni =>
                        ni.GetIPProperties().GatewayAddresses.Count > 0);

                    if (activeInterface != null)
                        return activeInterface.Name;

                    return interfaces.First().Name;
                }

                return "No active network interface found";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static string[] GetCurrentDnsServers()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                foreach (var ni in interfaces)
                {
                    var ipProps = ni.GetIPProperties();
                    var dnsServers = ipProps.DnsAddresses
                        .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(ip => ip.ToString())
                        .ToArray();

                    if (dnsServers.Any())
                        return dnsServers;
                }

                return new[] { "No DNS servers found" };
            }
            catch (Exception ex)
            {
                return new[] { $"Error: {ex.Message}" };
            }
        }

        public static bool SetDnsServers(string primaryDns, string secondaryDns)
        {
            try
            {
                string interfaceName = GetActiveNetworkInterface();
                if (interfaceName.StartsWith("Error") || interfaceName.StartsWith("No active"))
                    return false;

                string command = $"interface ip set dns \"{interfaceName}\" static {primaryDns} primary";
                ExecuteCommand(command);

                if (!string.IsNullOrEmpty(secondaryDns))
                {
                    command = $"interface ip add dns \"{interfaceName}\" {secondaryDns} index=2";
                    ExecuteCommand(command);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ClearDnsServers()
        {
            try
            {
                string interfaceName = GetActiveNetworkInterface();
                if (interfaceName.StartsWith("Error") || interfaceName.StartsWith("No active"))
                    return false;

                string command = $"interface ip set dns \"{interfaceName}\" dhcp";
                ExecuteCommand(command);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool FlushDnsCache()
        {
            try
            {
                ExecuteCommand("ipconfig /flushdns");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string ExecuteCommand(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                if (command.Contains("ipconfig"))
                {
                    psi.FileName = "ipconfig";
                    psi.Arguments = "/flushdns";
                }

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                        return error;

                    return output;
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
