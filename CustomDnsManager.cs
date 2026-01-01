using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeusDNSChanger
{
    [Serializable]
    public class CustomDnsServer
    {
        public string Name { get; set; }
        public string PrimaryDns { get; set; }
        public string SecondaryDns { get; set; }

        public CustomDnsServer()
        {
        }

        public CustomDnsServer(string name, string primary, string secondary)
        {
            Name = name;
            PrimaryDns = primary;
            SecondaryDns = secondary;
        }
    }

    public static class CustomDnsManager
    {
        private static List<CustomDnsServer> customServers = new List<CustomDnsServer>();

        public static List<CustomDnsServer> GetCustomServers()
        {
            return new List<CustomDnsServer>(customServers);
        }

        public static void AddServer(CustomDnsServer server)
        {
            customServers.Add(server);
            SaveToSettings();
        }

        public static void RemoveServer(string name)
        {
            customServers.RemoveAll(s => s.Name == name);
            SaveToSettings();
        }

        public static void LoadFromSettings()
        {
            try
            {
                string json = Properties.Settings.Default.CustomDnsServers;
                if (!string.IsNullOrEmpty(json))
                {
                    customServers = System.Text.Json.JsonSerializer.Deserialize<List<CustomDnsServer>>(json) ?? new List<CustomDnsServer>();
                }
            }
            catch
            {
                customServers = new List<CustomDnsServer>();
            }
        }

        private static void SaveToSettings()
        {
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(customServers);
                Properties.Settings.Default.CustomDnsServers = json;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving custom DNS servers: {ex.Message}");
            }
        }
    }
}
