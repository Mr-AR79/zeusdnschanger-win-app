using System.Collections.Generic;

namespace ZeusDNSChanger
{
    public static class LanguageManager
    {
        private static string currentLanguage = "English";
        
        private static Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "English", new Dictionary<string, string>
                {
                    // Navigation
                    {"Home", "Home"},
                    {"ManageDns", "Manage DNS"},
                    {"ActivityLogs", "Activity Logs"},
                    {"Settings", "Settings"},
                    
                    // Main Window
                    {"AppTitle", "Zeus DNS Changer"},
                    {"AppSubtitle", "Secure and Fast DNS Management"},
                    {"SelectDnsServer", "Select DNS Server"},
                    {"ZeusPlusToken", "Zeus Plus Token"},
                    {"UpdateInterval", "Update Interval (minutes)"},
                    {"ClearDnsCache", "Clear DNS Cache"},
                    {"ON", "ON"},
                    {"OFF", "OFF"},
                    
                    // Manage DNS
                    {"ManageDnsServers", "Manage DNS Servers"},
                    {"BuiltInDns", "Built-in DNS Servers"},
                    {"CustomDns", "Custom DNS Servers"},
                    {"BuiltIn", "Built-in"},
                    {"AddCustomDns", "Add Custom DNS Server"},
                    {"DnsName", "DNS Name"},
                    {"PrimaryDns", "Primary DNS"},
                    {"SecondaryDns", "Secondary DNS (Optional)"},
                    {"Add", "Add"},
                    {"Cancel", "Cancel"},
                    {"Delete", "Delete"},
                    
                    // Settings
                    {"Theme", "Theme"},
                    {"ThemeDesc", "Switch between Dark and Light mode"},
                    {"Dark", "Dark"},
                    {"Light", "Light"},
                    {"Language", "Language"},
                    {"LanguageDesc", "Change application language"},
                    {"English", "English"},
                    {"Persian", "فارسی"},
                    {"AutoRefreshIp", "Auto-refresh IP"},
                    {"AutoRefreshDesc", "Automatically check and update IP when connected"},
                    {"StartupWithWindows", "Start with Windows"},
                    {"StartupWithWindowsDesc", "Launch application when Windows starts"},
                    {"SaveSettings", "Save Settings"},
                    
                    // Logs
                    {"CopyLogs", "Copy Logs"},
                    {"ClearLogs", "Clear Logs"},
                    {"NoActivity", "No activity yet..."},
                    
                    // Messages
                    {"Success", "Success"},
                    {"Error", "Error"},
                    {"Warning", "Warning"},
                    {"Confirm", "Confirm"},
                    {"Info", "Info"},
                    {"OK", "OK"},
                    {"Yes", "Yes"},
                    {"No", "No"},
                    {"ConfirmDelete", "Confirm Delete"},
                    
                    // Dialog Messages
                    {"AdminRequired", "Administrator privileges required!"},
                    {"EnterDnsName", "Please enter a DNS name!"},
                    {"EnterPrimaryDns", "Please enter a primary DNS!"},
                    {"InvalidPrimaryIp", "Primary DNS is not a valid IP address!\nExample: 8.8.8.8"},
                    {"InvalidSecondaryIp", "Secondary DNS is not a valid IP address!\nExample: 8.8.4.4"},
                    {"DnsNameExists", "A DNS server with this name already exists!"},
                    {"DnsAddedSuccess", "DNS server added successfully!"},
                    {"DnsDeleteConfirm", "Are you sure you want to delete"},
                    {"DnsDeletedSuccess", "DNS server deleted!"},
                    {"SettingsSaved", "Settings saved successfully!"},
                    {"CacheClearedSuccess", "DNS cache cleared successfully!"},
                    {"CacheClearFailed", "Failed to clear DNS cache."},
                    {"NoLogsToCopy", "No logs to copy!"},
                    {"LogsCopied", "Logs copied to clipboard!"},
                    {"EnterToken", "Please enter Zeus Plus token!"},
                    {"CannotGetIp", "Unable to get your IP address. Please check your internet connection."},
                    {"ZeusPlusActivated", "Zeus Plus activated successfully!"},
                    {"DnsSetFailed", "Failed to set DNS servers. Please check administrator privileges."},
                    {"TokenRegFailed", "Token registration failed:"},
                    {"DnsActivatedSuccess", "activated successfully!"},
                    {"DnsDisconnected", "DNS settings cleared successfully!"},
                    {"DnsClearFailed", "Failed to clear DNS settings."},
                    {"AppRunningInBackground", "Application is running in background"},
                    {"IpChangedSuccess", "IP changed and token re-registered successfully!"},
                    {"IpChangedFailed", "IP changed but token registration failed!"},
                }
            },
            {
                "فارسی", new Dictionary<string, string>
                {
                    // Navigation
                    {"Home", "خانه"},
                    {"ManageDns", "مدیریت DNS"},
                    {"ActivityLogs", "گزارش فعالیت"},
                    {"Settings", "تنظیمات"},
                    
                    // Main Window
                    {"AppTitle", "تغییر‌دهنده DNS زئوس"},
                    {"AppSubtitle", "مدیریت امن و سریع DNS"},
                    {"SelectDnsServer", "انتخاب سرور DNS"},
                    {"ZeusPlusToken", "توکن زئوس پلاس"},
                    {"UpdateInterval", "فاصله به‌روزرسانی (دقیقه)"},
                    {"ClearDnsCache", "پاکسازی کش DNS"},
                    {"ON", "روشن"},
                    {"OFF", "خاموش"},
                    
                    // Manage DNS
                    {"ManageDnsServers", "مدیریت سرورهای DNS"},
                    {"BuiltInDns", "سرورهای DNS پیش‌فرض"},
                    {"CustomDns", "سرورهای DNS سفارشی"},
                    {"BuiltIn", "پیش‌فرض"},
                    {"AddCustomDns", "افزودن DNS سفارشی"},
                    {"DnsName", "نام DNS"},
                    {"PrimaryDns", "DNS اصلی"},
                    {"SecondaryDns", "DNS ثانویه (اختیاری)"},
                    {"Add", "افزودن"},
                    {"Cancel", "انصراف"},
                    {"Delete", "حذف"},
                    
                    // Settings
                    {"Theme", "تم"},
                    {"ThemeDesc", "تغییر بین حالت تاریک و روشن"},
                    {"Dark", "تاریک"},
                    {"Light", "روشن"},
                    {"Language", "زبان"},
                    {"LanguageDesc", "تغییر زبان برنامه"},
                    {"English", "English"},
                    {"Persian", "فارسی"},
                    {"AutoRefreshIp", "به‌روزرسانی خودکار IP"},
                    {"AutoRefreshDesc", "بررسی و به‌روزرسانی خودکار IP هنگام اتصال"},
                    {"StartupWithWindows", "اجرا با ویندوز"},
                    {"StartupWithWindowsDesc", "اجرای خودکار برنامه با روشن شدن ویندوز"},
                    {"SaveSettings", "ذخیره تنظیمات"},
                    
                    // Logs
                    {"CopyLogs", "کپی گزارش"},
                    {"ClearLogs", "پاک کردن گزارش"},
                    {"NoActivity", "هنوز فعالیتی ثبت نشده..."},
                    
                    // Messages
                    {"Success", "موفقیت"},
                    {"Error", "خطا"},
                    {"Warning", "هشدار"},
                    {"Confirm", "تأیید"},
                    {"Info", "اطلاعات"},
                    {"OK", "تأیید"},
                    {"Yes", "بله"},
                    {"No", "خیر"},
                    {"ConfirmDelete", "تأیید حذف"},
                    
                    // Dialog Messages
                    {"AdminRequired", "نیاز به دسترسی مدیر!"},
                    {"EnterDnsName", "لطفاً نام DNS را وارد کنید!"},
                    {"EnterPrimaryDns", "لطفاً DNS اصلی را وارد کنید!"},
                    {"InvalidPrimaryIp", "DNS اصلی یک آدرس IP معتبر نیست!\nمثال: 8.8.8.8"},
                    {"InvalidSecondaryIp", "DNS ثانویه یک آدرس IP معتبر نیست!\nمثال: 8.8.4.4"},
                    {"DnsNameExists", "سرور DNS با این نام قبلاً وجود دارد!"},
                    {"DnsAddedSuccess", "سرور DNS با موفقیت اضافه شد!"},
                    {"DnsDeleteConfirm", "آیا از حذف مطمئن هستید؟"},
                    {"DnsDeletedSuccess", "سرور DNS حذف شد!"},
                    {"SettingsSaved", "تنظیمات با موفقیت ذخیره شد!"},
                    {"CacheClearedSuccess", "کش DNS با موفقیت پاک شد!"},
                    {"CacheClearFailed", "پاکسازی کش DNS ناموفق بود."},
                    {"NoLogsToCopy", "گزارشی برای کپی وجود ندارد!"},
                    {"LogsCopied", "گزارش کپی شد!"},
                    {"EnterToken", "لطفاً توکن زئوس پلاس را وارد کنید!"},
                    {"CannotGetIp", "امکان دریافت آدرس IP وجود ندارد. اتصال اینترنت را بررسی کنید."},
                    {"ZeusPlusActivated", "زئوس پلاس با موفقیت فعال شد!"},
                    {"DnsSetFailed", "تنظیم DNS ناموفق بود. دسترسی مدیر را بررسی کنید."},
                    {"TokenRegFailed", "ثبت توکن ناموفق بود:"},
                    {"DnsActivatedSuccess", "با موفقیت فعال شد!"},
                    {"DnsDisconnected", "تنظیمات DNS با موفقیت پاک شد!"},
                    {"DnsClearFailed", "پاکسازی تنظیمات DNS ناموفق بود."},
                    {"AppRunningInBackground", "برنامه در پس‌زمینه در حال اجراست"},
                    {"IpChangedSuccess", "آی‌پی تغییر کرد و توکن با موفقیت ثبت شد!"},
                    {"IpChangedFailed", "آی‌پی تغییر کرد اما ثبت توکن ناموفق بود!"},
                }
            }
        };
        
        public static void SetLanguage(string language)
        {
            if (translations.ContainsKey(language))
            {
                currentLanguage = language;
            }
        }
        
        public static string GetString(string key)
        {
            if (translations.ContainsKey(currentLanguage) && 
                translations[currentLanguage].ContainsKey(key))
            {
                return translations[currentLanguage][key];
            }
            return key;
        }
        
        public static string GetCurrentLanguage()
        {
            return currentLanguage;
        }

        public static bool IsPersian()
        {
            return currentLanguage == "فارسی";
        }
    }
}
