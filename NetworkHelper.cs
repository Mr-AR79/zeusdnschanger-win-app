using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZeusDNSChanger
{
    public class NetworkHelper
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<string> GetPublicIpAsync()
        {
            try
            {
                string response = await httpClient.GetStringAsync("http://37.32.5.34:81");
                return response.Trim();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static async Task<(bool Success, string Message, string RequestLog)> RegisterTokenAsync(string token, string ip, int maxRetries = 3)
        {
            var logBuilder = new System.Text.StringBuilder();
            string url = $"http://37.32.5.34:82/tap-in?token={token}&ip={ip}";
            
            logBuilder.AppendLine($"🔄 Max retries: {maxRetries}");
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    logBuilder.AppendLine($"⏳ Attempt {attempt}/{maxRetries}...");
                    
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    
                    logBuilder.AppendLine($"📥 Status Code: {(int)response.StatusCode} {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        logBuilder.AppendLine($"✅ Response: {content}");
                        return (true, content, logBuilder.ToString());
                    }
                    else
                    {
                        if (attempt < maxRetries)
                        {
                            logBuilder.AppendLine($"⚠️ Failed, waiting 2s before retry...");
                            await Task.Delay(2000);
                            continue;
                        }
                        logBuilder.AppendLine($"❌ All attempts failed");
                        return (false, $"Failed with status code: {response.StatusCode}", logBuilder.ToString());
                    }
                }
                catch (Exception ex)
                {
                    logBuilder.AppendLine($"❌ Exception: {ex.Message}");
                    if (attempt < maxRetries)
                    {
                        logBuilder.AppendLine($"⚠️ Retrying in 2 seconds...");
                        await Task.Delay(2000);
                        continue;
                    }
                    return (false, $"Error after {maxRetries} attempts: {ex.Message}", logBuilder.ToString());
                }
            }
            return (false, "Unknown error", logBuilder.ToString());
        }
    }
}
