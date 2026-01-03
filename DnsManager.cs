using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

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

        private static string _cachedInterfaceName = null;
        private static DateTime _cacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

        public static string GetActiveNetworkInterface()
        {
            try
            {
                if (_cachedInterfaceName != null && DateTime.Now - _cacheTime < CacheDuration)
                    return _cachedInterfaceName;

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
                    {
                        _cachedInterfaceName = activeInterface.Name;
                        _cacheTime = DateTime.Now;
                        return _cachedInterfaceName;
                    }

                    _cachedInterfaceName = interfaces.First().Name;
                    _cacheTime = DateTime.Now;
                    return _cachedInterfaceName;
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

                if (!string.IsNullOrEmpty(secondaryDns))
                {
                    ExecuteCommandFast($"interface ip set dns \"{interfaceName}\" static {primaryDns} primary");
                    ExecuteCommandFireAndForget($"interface ip add dns \"{interfaceName}\" {secondaryDns} index=2");
                }
                else
                {
                    ExecuteCommandFast($"interface ip set dns \"{interfaceName}\" static {primaryDns} primary");
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

                ExecuteCommandFast($"interface ip set dns \"{interfaceName}\" dhcp");
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
                ExecuteCommandFireAndForget("ipconfig /flushdns");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ExecuteCommandFast(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = command,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit(1000);
                }
            }
            catch { }
        }

        private static void ExecuteCommandFireAndForget(string command)
        {
            try
            {
                string fileName = "netsh";
                string args = command;

                if (command.Contains("ipconfig"))
                {
                    fileName = "ipconfig";
                    args = "/flushdns";
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                Process.Start(psi);
            }
            catch { }
        }
    }
}
