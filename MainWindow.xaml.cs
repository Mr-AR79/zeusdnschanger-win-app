using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Security.Principal;
using System.Runtime.InteropServices;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using Timer = System.Timers.Timer;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;

namespace ZeusDNSChanger
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private bool isConnected = false;
        private string currentIp = "";
        private Timer ipCheckTimer;
        private Timer ipRefreshTimer;
        private string selectedDnsType = "Zeus Free";
        private (string Primary, string Secondary) currentDnsAddresses;
        private NotifyIcon notifyIcon;
        private System.Text.StringBuilder logBuilder = new System.Text.StringBuilder();
        private bool isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeNotifyIcon();
            Loaded += MainWindow_Loaded;
        }

        private void SetDarkTitleBar(bool isDark)
        {
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    int useImmersiveDarkMode = isDark ? 1 : 0;
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
                }
            }
            catch { }
        }

        private void InitializeNotifyIcon()
        {
            System.Drawing.Icon trayIcon;
            try
            {
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    trayIcon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    trayIcon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
            }
            catch
            {
                trayIcon = SystemIcons.Application;
            }

            notifyIcon = new NotifyIcon
            {
                Icon = trayIcon,
                Visible = false,
                Text = "Zeus DNS Changer"
            };

            notifyIcon.DoubleClick += (s, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
                notifyIcon.Visible = false;
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Renderer = new ToolStripProfessionalRenderer();
            
            var showItem = new ToolStripMenuItem("Show");
            showItem.Image = CreateShowIcon();
            showItem.Click += (s, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
                notifyIcon.Visible = false;
            };
            
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Image = CreateExitIcon();
            exitItem.Click += (s, e) =>
            {
                notifyIcon.Visible = false;
                Application.Current.Shutdown();
            };
            
            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);
            
            notifyIcon.ContextMenuStrip = contextMenu;
        }
        
        private System.Drawing.Bitmap CreateShowIcon()
        {
            var bmp = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.DodgerBlue, 1.5f))
                {
                    g.DrawRectangle(pen, 2, 4, 12, 10);
                    g.DrawLine(pen, 2, 7, 14, 7);
                }
            }
            return bmp;
        }
        
        private System.Drawing.Bitmap CreateExitIcon()
        {
            var bmp = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Crimson, 2))
                {
                    g.DrawLine(pen, 3, 3, 13, 13);
                    g.DrawLine(pen, 13, 3, 3, 13);
                }
            }
            return bmp;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(2000, "Zeus DNS Changer",
                LanguageManager.GetString("AppRunningInBackground"), ToolTipIcon.Info);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!IsAdministrator())
            {
                MessageBox.Show("This application requires administrator privileges to modify DNS settings.\n\nPlease run as administrator.",
                    "Administrator Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            LoadSettings();
            LoadCustomDnsServers();
            UpdateCurrentDnsAddresses();
            await LoadNetworkInformation();

            if (Properties.Settings.Default.AutoRefreshIP)
            {
                StartIpRefreshTimer();
            }
        }

        private bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        #region Navigation

        private void NavHome_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(HomePage);
            HighlightNavButton(NavHomeButton);
        }

        private void NavManage_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(ManagePage);
            HighlightNavButton(NavManageButton);
        }

        private void NavLogs_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(LogsPage);
            HighlightNavButton(NavLogsButton);
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(SettingsPageContent);
            HighlightNavButton(NavSettingsButton);
        }

        private void ShowPage(Grid page)
        {
            HomePage.Visibility = Visibility.Collapsed;
            ManagePage.Visibility = Visibility.Collapsed;
            LogsPage.Visibility = Visibility.Collapsed;
            SettingsPageContent.Visibility = Visibility.Collapsed;

            page.Visibility = Visibility.Visible;
        }

        private void HighlightNavButton(System.Windows.Controls.Button activeButton)
        {
            NavHomeButton.Background = System.Windows.Media.Brushes.Transparent;
            NavManageButton.Background = System.Windows.Media.Brushes.Transparent;
            NavLogsButton.Background = System.Windows.Media.Brushes.Transparent;
            NavSettingsButton.Background = System.Windows.Media.Brushes.Transparent;
            
            NavHomeButton.Foreground = TryFindResource("TextPrimaryBrush") as SolidColorBrush;
            NavManageButton.Foreground = TryFindResource("TextPrimaryBrush") as SolidColorBrush;
            NavLogsButton.Foreground = TryFindResource("TextPrimaryBrush") as SolidColorBrush;
            NavSettingsButton.Foreground = TryFindResource("TextPrimaryBrush") as SolidColorBrush;

            var goldBrush = TryFindResource("GoldBrush") as SolidColorBrush;
            if (goldBrush != null)
            {
                activeButton.Background = goldBrush;
                activeButton.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        #endregion

        #region Logging

        private void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";
            
            lock (logBuilder)
            {
                logBuilder.AppendLine(logEntry);
            }

            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (LogTextBlock != null)
                        {
                            lock (logBuilder)
                            {
                                LogTextBlock.Text = logBuilder.ToString();
                            }

                            LogTextBlock.ScrollToEnd();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AddLog error: {ex.Message}");
                    }
                }));
            }
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            lock (logBuilder)
            {
                logBuilder.Clear();
            }
            
            if (LogTextBlock != null)
            {
                LogTextBlock.Text = "No activity yet...";
            }
            
            AddLog("🗑️ Logs cleared");
        }

        private void CopyLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logs;
                lock (logBuilder)
                {
                    logs = logBuilder.ToString();
                }
                
                if (string.IsNullOrEmpty(logs))
                {
                    ShowCustomDialog(LanguageManager.GetString("Info"), LanguageManager.GetString("NoLogsToCopy"), DialogType.Info);
                    return;
                }

                System.Windows.Clipboard.SetText(logs);
                AddLog("📋 Logs copied to clipboard");
                ShowCustomDialog(LanguageManager.GetString("Success"), LanguageManager.GetString("LogsCopied"), DialogType.Success);
            }
            catch (Exception ex)
            {
                ShowCustomDialog(LanguageManager.GetString("Error"), $"{LanguageManager.GetString("Error")}: {ex.Message}", DialogType.Error);
            }
        }

        #endregion

        #region Settings

        private void LoadSettings()
        {
            LanguageManager.SetLanguage(Properties.Settings.Default.Language);
            var theme = Properties.Settings.Default.Theme;
            ApplyTheme(theme);

            if (ThemeComboBox != null)
            {
                ThemeComboBox.SelectedIndex = (theme == "Light") ? 1 : 0;
            }

            if (LanguageComboBox != null)
            {
                LanguageComboBox.SelectedIndex = (Properties.Settings.Default.Language == "فارسی") ? 1 : 0;
            }

            if (AutoRefreshCheckBox != null)
            {
                AutoRefreshCheckBox.IsChecked = Properties.Settings.Default.AutoRefreshIP;
            }

            if (StartupCheckBox != null)
            {
                StartupCheckBox.IsChecked = Properties.Settings.Default.StartupWithWindows;
            }

            if (TokenTextBox != null && !string.IsNullOrEmpty(Properties.Settings.Default.ZeusPlusToken))
            {
                TokenTextBox.Text = Properties.Settings.Default.ZeusPlusToken;
            }

            if (IntervalSlider != null)
            {
                IntervalSlider.Value = Properties.Settings.Default.IpRefreshInterval;
            }

            isInitialized = true;
            AddLog("✅ Settings loaded");
        }

        private void ApplyTheme(string theme)
        {
            var app = Application.Current;
            var resources = app.Resources;

            SolidColorBrush goldBrush, darkBackgroundBrush, darkSurfaceBrush, darkBorderBrush, textPrimaryBrush, textSecondaryBrush;
            bool isLightTheme = theme == "Light";

            goldBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 215, 0));

            if (isLightTheme)
            {
                darkBackgroundBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 242, 245));
                darkSurfaceBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                darkBorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 225));
                textPrimaryBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 35));
                textSecondaryBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 95, 105));
            }
            else
            {
                darkBackgroundBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
                darkSurfaceBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 37, 37));
                darkBorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 51, 51));
                textPrimaryBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                textSecondaryBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(176, 176, 176));
            }

            resources["GoldBrush"] = goldBrush;
            resources["DarkBackgroundBrush"] = darkBackgroundBrush;
            resources["DarkSurfaceBrush"] = darkSurfaceBrush;
            resources["DarkBorderBrush"] = darkBorderBrush;
            resources["TextPrimaryBrush"] = textPrimaryBrush;
            resources["TextSecondaryBrush"] = textSecondaryBrush;
            resources["SuccessBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
            resources["ErrorBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54));

            if (isLightTheme)
            {
                var gradientBrush = new LinearGradientBrush();
                gradientBrush.StartPoint = new System.Windows.Point(0, 0);
                gradientBrush.EndPoint = new System.Windows.Point(1, 1);
                gradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(245, 247, 250), 0));
                gradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(235, 238, 245), 0.5));
                gradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(230, 235, 242), 1));
                this.Background = gradientBrush;
            }
            else
            {
                this.Background = darkBackgroundBrush;
            }

            SetDarkTitleBar(!isLightTheme);
            UpdateThemeElements(goldBrush, darkBackgroundBrush, darkSurfaceBrush, darkBorderBrush, textPrimaryBrush, textSecondaryBrush, isLightTheme);
        }

        private void UpdateThemeElements(SolidColorBrush gold, SolidColorBrush background, SolidColorBrush surface, SolidColorBrush border, SolidColorBrush textPrimary, SolidColorBrush textSecondary, bool isLight)
        {
            var cardEffect = isLight ? new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                BlurRadius = 15,
                ShadowDepth = 2,
                Opacity = 0.1,
                Direction = 270
            } : null;

            if (TopBar != null)
            {
                TopBar.Background = isLight ? surface : background;
                TopBar.BorderBrush = border;
            }

            if (DnsSelectionCard != null)
            {
                DnsSelectionCard.Background = surface;
                DnsSelectionCard.BorderBrush = border;
                DnsSelectionCard.Effect = cardEffect;
            }
            
            if (TokenPanel != null)
            {
                TokenPanel.Background = isLight ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 250, 230)) : background;
                TokenPanel.BorderBrush = gold;
            }
            
            if (NetworkInfoCard != null)
            {
                NetworkInfoCard.Background = isLight ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250)) : background;
                NetworkInfoCard.BorderBrush = border;
            }
            if (Ipv4Text != null) Ipv4Text.Foreground = textPrimary;
            if (ActiveDnsText != null) ActiveDnsText.Foreground = textSecondary;
            if (IntervalLabel != null) IntervalLabel.Foreground = textPrimary;

            if (ManageDnsHeader != null)
            {
                ManageDnsHeader.Background = surface;
                ManageDnsHeader.BorderBrush = border;
            }
            
            if (ZeusFreeCard != null)
            {
                ZeusFreeCard.Background = surface;
                ZeusFreeCard.BorderBrush = border;
                ZeusFreeCard.Effect = cardEffect;
            }
            if (ZeusPlusCard != null)
            {
                ZeusPlusCard.Background = surface;
                ZeusPlusCard.BorderBrush = border;
                ZeusPlusCard.Effect = cardEffect;
            }
            if (GoogleDnsCard != null)
            {
                GoogleDnsCard.Background = surface;
                GoogleDnsCard.BorderBrush = border;
                GoogleDnsCard.Effect = cardEffect;
            }
            if (CloudflareDnsCard != null)
            {
                CloudflareDnsCard.Background = surface;
                CloudflareDnsCard.BorderBrush = border;
                CloudflareDnsCard.Effect = cardEffect;
            }

            if (AddDnsDialogBox != null)
            {
                AddDnsDialogBox.Background = surface;
                AddDnsDialogBox.BorderBrush = gold;
            }

            if (LogsCard != null)
            {
                LogsCard.Background = surface;
                LogsCard.BorderBrush = border;
                LogsCard.Effect = cardEffect;
            }
            if (LogsInnerCard != null)
            {
                LogsInnerCard.Background = isLight ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250)) : background;
                LogsInnerCard.BorderBrush = border;
            }

            if (SettingsCard != null)
            {
                SettingsCard.Background = surface;
                SettingsCard.BorderBrush = border;
                SettingsCard.Effect = cardEffect;
            }
            
            if (ThemeSettingBorder != null)
            {
                ThemeSettingBorder.Background = isLight ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250)) : background;
                ThemeSettingBorder.BorderBrush = border;
            }
            if (LanguageSettingBorder != null)
            {
                LanguageSettingBorder.Background = isLight ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250)) : background;
                LanguageSettingBorder.BorderBrush = border;
            }
            if (AutoRefreshSettingBorder != null)
            {
                AutoRefreshSettingBorder.Background = isLight ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250)) : background;
                AutoRefreshSettingBorder.BorderBrush = border;
            }
            if (StartupSettingBorder != null)
            {
                StartupSettingBorder.Background = isLight ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250)) : background;
                StartupSettingBorder.BorderBrush = border;
            }

            if (ThemeTitleText != null) ThemeTitleText.Foreground = textPrimary;
            if (ThemeDescText != null) ThemeDescText.Foreground = textSecondary;
            if (LanguageTitleText != null) LanguageTitleText.Foreground = textPrimary;
            if (LanguageDescText != null) LanguageDescText.Foreground = textSecondary;
            if (AutoRefreshTitleText != null) AutoRefreshTitleText.Foreground = textPrimary;
            if (AutoRefreshDescText != null) AutoRefreshDescText.Foreground = textSecondary;
            if (StartupTitleText != null) StartupTitleText.Foreground = textPrimary;
            if (StartupDescText != null) StartupDescText.Foreground = textSecondary;

            if (ToggleCircle != null && !isConnected)
            {
                ToggleCircle.Fill = isLight ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 225, 230)) : border;
            }

            if (CustomDialogBox != null)
            {
                CustomDialogBox.Background = surface;
                CustomDialogBox.BorderBrush = gold;
            }
            if (DialogTitle != null) DialogTitle.Foreground = textPrimary;
            if (DialogMessage != null) DialogMessage.Foreground = textSecondary;
            if (DialogIcon != null) DialogIcon.Foreground = gold;

            if (CustomDnsItemsPanel != null)
            {
                foreach (var child in CustomDnsItemsPanel.Children)
                {
                    if (child is Border itemBorder)
                    {
                        itemBorder.Background = surface;
                        itemBorder.BorderBrush = border;
                        itemBorder.Effect = cardEffect;
                        
                        if (itemBorder.Child is Grid grid)
                        {
                            foreach (var gridChild in grid.Children)
                            {
                                if (gridChild is StackPanel sp)
                                {
                                    foreach (var spChild in sp.Children)
                                    {
                                        if (spChild is TextBlock tb)
                                        {
                                            if (tb.FontWeight == FontWeights.SemiBold)
                                                tb.Foreground = textPrimary;
                                            else
                                                tb.Foreground = textSecondary;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (SelectDnsLabel != null) SelectDnsLabel.Foreground = textPrimary;
            if (BuiltInDnsLabel != null) BuiltInDnsLabel.Foreground = textSecondary;
            if (CustomDnsLabel != null) CustomDnsLabel.Foreground = textSecondary;

            if (NavHomeButton != null) NavHomeButton.Foreground = textPrimary;
            if (NavManageButton != null) NavManageButton.Foreground = textPrimary;
            if (NavLogsButton != null) NavLogsButton.Foreground = textPrimary;
            if (NavSettingsButton != null) NavSettingsButton.Foreground = textPrimary;
        }

        #endregion

        #region Network Information

        private async System.Threading.Tasks.Task LoadNetworkInformation()
        {
            try
            {
                AddLog("🔄 Loading network information...");

                currentIp = await NetworkHelper.GetPublicIpAsync();
                if (Ipv4Text != null)
                {
                    Ipv4Text.Text = currentIp;
                }

                var dnsServers = DnsManager.GetCurrentDnsServers();
                if (ActiveDnsText != null)
                {
                    ActiveDnsText.Text = "DNS: " + string.Join(", ", dnsServers);
                }
                
                AddLog($"✅ Network info loaded: IP {currentIp}");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Failed to load network info: {ex.Message}");
                MessageBox.Show($"Error loading network information: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region DNS Selection

        private void DnsServerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DnsServerComboBox == null || DnsServerComboBox.SelectedItem == null)
                return;

            var selectedItem = DnsServerComboBox.SelectedItem as ComboBoxItem;
            string selected = selectedItem?.Content?.ToString() ?? "";

            selectedDnsType = selected;

            if (TokenPanel != null)
            {
                TokenPanel.Visibility = (selected == "Zeus Plus") ? Visibility.Visible : Visibility.Collapsed;
            }

            UpdateCurrentDnsAddresses();
        }

        private void UpdateCurrentDnsAddresses()
        {
            switch (selectedDnsType)
            {
                case "Zeus Free":
                    currentDnsAddresses = DnsManager.DnsServers.ZeusFree;
                    break;
                case "Zeus Plus":
                    currentDnsAddresses = DnsManager.DnsServers.ZeusPlus;
                    break;
                case "Google DNS":
                    currentDnsAddresses = DnsManager.DnsServers.Google;
                    break;
                case "Cloudflare DNS":
                    currentDnsAddresses = DnsManager.DnsServers.Cloudflare;
                    break;
                default:
                    var customServers = CustomDnsManager.GetCustomServers();
                    var customServer = customServers.FirstOrDefault(s => s.Name == selectedDnsType);
                    if (customServer != null)
                    {
                        currentDnsAddresses = (customServer.PrimaryDns, customServer.SecondaryDns);
                    }
                    else
                    {
                        currentDnsAddresses = DnsManager.DnsServers.ZeusFree;
                    }
                    break;
            }
        }

        #endregion

        #region Toggle DNS

        private async void ToggleSwitch_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsAdministrator())
            {
                ShowCustomDialog("Error", "Administrator privileges required!", DialogType.Error);
                return;
            }

            if (!isConnected)
            {
                if (selectedDnsType == "Zeus Plus")
                {
                    await StartZeusPlus();
                }
                else
                {
                    await StartRegularDns();
                }
            }
            else
            {
                StopDns();
            }
        }

        private async System.Threading.Tasks.Task StartZeusPlus()
        {
            if (TokenTextBox == null)
                return;

            string token = TokenTextBox.Text.Trim();

            if (string.IsNullOrEmpty(token))
            {
                ShowCustomDialog("Error", "Please enter Zeus Plus token!", DialogType.Warning);
                AddLog("❌ No token provided");
                return;
            }

            if (currentIp.StartsWith("Error"))
            {
                ShowCustomDialog("Error", "Unable to get your IP address. Please check your internet connection.", DialogType.Error);
                AddLog("❌ Failed: Cannot get IP address");
                return;
            }

            AddLog("🔄 Connecting to Zeus Plus...");

            try
            {
                var result = await NetworkHelper.RegisterTokenAsync(token, currentIp);
                
                AddLog(result.RequestLog);

                if (result.Success)
                {
                    AddLog($"✅ Token registered with IP: {currentIp}");
                    
                    bool dnsSet = await System.Threading.Tasks.Task.Run(() =>
                        DnsManager.SetDnsServers(currentDnsAddresses.Primary, currentDnsAddresses.Secondary));

                    if (dnsSet)
                    {
                        isConnected = true;
                        UpdateConnectedState();
                        StartIpMonitoring();
                        
                        Properties.Settings.Default.ZeusPlusToken = token;
                        Properties.Settings.Default.Save();
                        
                        AddLog($"✅ Zeus Plus activated: {currentDnsAddresses.Primary}");
                        ShowCustomDialog("Success", "Zeus Plus activated successfully!", DialogType.Success);
                    }
                    else
                    {
                        AddLog("❌ Failed to set DNS servers");
                        ShowCustomDialog("Error", "Failed to set DNS servers. Please check administrator privileges.", DialogType.Error);
                    }
                }
                else
                {
                    AddLog($"❌ Token registration failed: {result.Message}");
                    ShowCustomDialog("Error", $"Token registration failed:\n{result.Message}", DialogType.Error);
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error: {ex.Message}");
                ShowCustomDialog("Error", $"Error: {ex.Message}", DialogType.Error);
            }
        }

        private async System.Threading.Tasks.Task StartRegularDns()
        {
            AddLog($"🔄 Connecting to {selectedDnsType}...");
            
            try
            {
                if (StatusText != null)
                {
                    StatusText.Text = "...";
                }

                bool success = await System.Threading.Tasks.Task.Run(() =>
                    DnsManager.SetDnsServers(currentDnsAddresses.Primary, currentDnsAddresses.Secondary));

                if (success)
                {
                    isConnected = true;
                    UpdateConnectedState();
                    AddLog($"✅ {selectedDnsType} activated: {currentDnsAddresses.Primary}");
                    ShowCustomDialog("Success", $"{selectedDnsType} activated successfully!", DialogType.Success);
                }
                else
                {
                    UpdateDisconnectedState();
                    AddLog("❌ Failed to set DNS servers");
                    ShowCustomDialog("Error", "Failed to set DNS servers. Please check administrator privileges.", DialogType.Error);
                }
            }
            catch (Exception ex)
            {
                UpdateDisconnectedState();
                AddLog($"❌ Error: {ex.Message}");
                ShowCustomDialog("Error", $"Error: {ex.Message}", DialogType.Error);
            }
        }

        private void StopDns()
        {
            StopIpMonitoring();

            AddLog("🔄 Disconnecting DNS...");

            try
            {
                if (ActiveDnsText != null)
                {
                    ActiveDnsText.Text = "DNS: Automatic (DHCP)";
                }

                bool success = DnsManager.ClearDnsServers();

                if (success)
                {
                    isConnected = false;
                    UpdateDisconnectedState();
                    AddLog("✅ DNS disconnected, back to automatic");
                    ShowCustomDialog("Success", "DNS settings cleared successfully!", DialogType.Success);
                }
                else
                {
                    AddLog("❌ Failed to clear DNS settings");
                    ShowCustomDialog("Error", "Failed to clear DNS settings.", DialogType.Error);
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error: {ex.Message}");
                ShowCustomDialog("Error", $"Error: {ex.Message}", DialogType.Error);
            }
        }

        private void UpdateConnectedState()
        {
            if (StatusText == null || ToggleCircle == null || ToggleIcon == null)
                return;

            StatusText.Text = "ON";
            StatusText.Foreground = System.Windows.Media.Brushes.White;

            var successBrush = TryFindResource("SuccessBrush") as SolidColorBrush;
            if (successBrush != null)
            {
                ToggleCircle.Fill = successBrush;
            }

            ToggleIcon.Text = "\uE7E8";

            if (DnsServerComboBox != null)
                DnsServerComboBox.IsEnabled = false;

            if (TokenTextBox != null)
                TokenTextBox.IsEnabled = false;

            if (IntervalSlider != null)
                IntervalSlider.IsEnabled = false;

            if (ActiveDnsText != null)
            {
                ActiveDnsText.Text = $"DNS: {currentDnsAddresses.Primary}" + 
                    (string.IsNullOrEmpty(currentDnsAddresses.Secondary) ? "" : $", {currentDnsAddresses.Secondary}");
            }

            if (Properties.Settings.Default.AutoRefreshIP)
            {
                StartIpRefreshTimer();
            }
        }

        private void UpdateDisconnectedState()
        {
            if (StatusText == null || ToggleCircle == null || ToggleIcon == null)
                return;

            StatusText.Text = "OFF";

            var textSecondaryBrush = TryFindResource("TextSecondaryBrush") as SolidColorBrush;
            if (textSecondaryBrush != null)
            {
                StatusText.Foreground = textSecondaryBrush;
            }

            var borderBrush = TryFindResource("DarkBorderBrush") as SolidColorBrush;
            if (borderBrush != null)
            {
                ToggleCircle.Fill = borderBrush;
            }

            ToggleIcon.Text = "\uE7E8";

            if (DnsServerComboBox != null)
                DnsServerComboBox.IsEnabled = true;

            if (TokenTextBox != null)
                TokenTextBox.IsEnabled = true;

            if (IntervalSlider != null)
                IntervalSlider.IsEnabled = true;

            StopIpRefreshTimer();
            AddLog("⏹️ IP auto-refresh stopped");
        }

        #endregion

        #region IP Monitoring

        private void StartIpMonitoring()
        {
            if (selectedDnsType != "Zeus Plus" || IntervalSlider == null)
                return;

            if (ipCheckTimer != null)
            {
                ipCheckTimer.Stop();
                ipCheckTimer.Dispose();
            }

            try
            {
                double intervalMinutes = IntervalSlider.Value;
                ipCheckTimer = new Timer(intervalMinutes * 60 * 1000);
                ipCheckTimer.Elapsed += async (sender, e) => await CheckAndUpdateIp();
                ipCheckTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting IP monitoring: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopIpMonitoring()
        {
            if (ipCheckTimer != null)
            {
                ipCheckTimer.Stop();
                ipCheckTimer.Dispose();
                ipCheckTimer = null;
            }
        }

        private void IntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isInitialized)
                return;
                
            if (Math.Abs(e.OldValue - e.NewValue) < 0.1)
                return;

            double intervalMinutes = e.NewValue;
            
            Properties.Settings.Default.IpRefreshInterval = (int)intervalMinutes;
            Properties.Settings.Default.Save();
            
            if (ipCheckTimer != null && isConnected && selectedDnsType == "Zeus Plus")
            {
                AddLog($"⏱️ Interval changed to {intervalMinutes:0} minutes");
                
                ipCheckTimer.Stop();
                ipCheckTimer.Dispose();
                
                try
                {
                    ipCheckTimer = new Timer(intervalMinutes * 60 * 1000);
                    ipCheckTimer.Elapsed += async (s, args) => await CheckAndUpdateIp();
                    ipCheckTimer.Start();
                }
                catch (Exception ex)
                {
                    AddLog($"❌ Error updating timer: {ex.Message}");
                }
            }
            
            if (ipRefreshTimer != null)
            {
                AddLog($"⏱️ IP refresh interval changed to {intervalMinutes:0} minutes");
                
                ipRefreshTimer.Stop();
                ipRefreshTimer.Dispose();
                ipRefreshTimer = null;
                
                try
                {
                    ipRefreshTimer = new Timer(intervalMinutes * 60 * 1000);
                    ipRefreshTimer.Elapsed += async (s, args) => await RefreshIpDisplay();
                    ipRefreshTimer.Start();
                }
                catch (Exception ex)
                {
                    AddLog($"❌ Error updating refresh timer: {ex.Message}");
                }
            }
        }

        private async System.Threading.Tasks.Task CheckAndUpdateIp()
        {
            try
            {
                string newIp = await NetworkHelper.GetPublicIpAsync();

                if (newIp != currentIp && !newIp.StartsWith("Error"))
                {
                    string token = "";
                    string oldIp = currentIp;

                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (TokenTextBox != null)
                            token = TokenTextBox.Text.Trim();

                        currentIp = newIp;

                        if (Ipv4Text != null)
                            Ipv4Text.Text = newIp;
                        
                        AddLog($"⚠️ IP changed: {oldIp} → {newIp}");
                    });

                    if (string.IsNullOrEmpty(token))
                        return;

                    var result = await NetworkHelper.RegisterTokenAsync(token, newIp);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        
                        if (result.Success)
                        {
                            AddLog($"✅ Token re-registered with new IP");
                            ShowCustomDialog(LanguageManager.GetString("Success"), 
                                $"{LanguageManager.GetString("IpChangedSuccess")}\n\n{oldIp} → {newIp}", 
                                DialogType.Success);
                        }
                        else
                        {
                            AddLog($"❌ Failed to re-register token: {result.Message}");
                            ShowCustomDialog(LanguageManager.GetString("Warning"), 
                                $"{LanguageManager.GetString("IpChangedFailed")}\n\n{oldIp} → {newIp}\n\n{result.Message}", 
                                DialogType.Warning);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error checking IP: {ex.Message}");
            }
        }

        private void StartIpRefreshTimer()
        {
            if (ipRefreshTimer != null)
                return;

            try
            {
                double intervalMinutes = 1;
                if (IntervalSlider != null)
                {
                    intervalMinutes = IntervalSlider.Value;
                }
                
                ipRefreshTimer = new Timer(intervalMinutes * 60 * 1000);
                ipRefreshTimer.Elapsed += async (sender, e) => await RefreshIpDisplay();
                ipRefreshTimer.Start();
                
                AddLog($"⏱️ IP refresh timer started ({intervalMinutes:0} min interval)");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting IP refresh: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopIpRefreshTimer()
        {
            if (ipRefreshTimer != null)
            {
                ipRefreshTimer.Stop();
                ipRefreshTimer.Dispose();
                ipRefreshTimer = null;
            }
        }

        private async System.Threading.Tasks.Task RefreshIpDisplay()
        {
            try
            {
                AddLog("🔍 Checking IP address...");
                string newIp = await NetworkHelper.GetPublicIpAsync();

                AddLog($"📡 Current IP: {currentIp}, New IP: {newIp}");

                if (!newIp.StartsWith("Error"))
                {
                    if (currentIp != newIp)
                    {
                        string oldIp = currentIp;
                        string token = "";

                        await Dispatcher.InvokeAsync(() =>
                        {
                            currentIp = newIp;

                            if (Ipv4Text != null)
                                Ipv4Text.Text = newIp;

                            if (selectedDnsType == "Zeus Plus" && TokenTextBox != null)
                            {
                                token = TokenTextBox.Text.Trim();
                            }

                            AddLog($"🔄 IP changed detected: {oldIp} → {newIp}");
                        });

                        if (selectedDnsType == "Zeus Plus" && !string.IsNullOrEmpty(token) && isConnected)
                        {
                            AddLog("🔄 Re-registering token with new IP...");
                            var result = await NetworkHelper.RegisterTokenAsync(token, newIp);

                            await Dispatcher.InvokeAsync(() =>
                            {

                                if (result.Success)
                                {
                                    AddLog($"✅ Token re-registered successfully with new IP: {newIp}");
                                    ShowCustomDialog(LanguageManager.GetString("Success"), 
                                        $"{LanguageManager.GetString("IpChangedSuccess")}\n\n{oldIp} → {newIp}", 
                                        DialogType.Success);
                                }
                                else
                                {
                                    AddLog($"❌ Failed to re-register token: {result.Message}");
                                    ShowCustomDialog(LanguageManager.GetString("Warning"), 
                                        $"{LanguageManager.GetString("IpChangedFailed")}\n\n{oldIp} → {newIp}\n\n{result.Message}", 
                                        DialogType.Warning);
                                }
                            });
                        }
                        else
                        {
                            AddLog($"ℹ️ IP changed but no re-registration needed (DNS: {selectedDnsType}, Connected: {isConnected})");
                        }
                    }
                    else
                    {
                        AddLog("✅ IP unchanged");
                    }
                }
                else
                {
                    AddLog($"❌ Error getting IP: {newIp}");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error refreshing IP: {ex.Message}");
            }
        }

        #endregion

        #region Clear Cache

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdministrator())
            {
                ShowCustomDialog(LanguageManager.GetString("Error"), LanguageManager.GetString("AdminRequired"), DialogType.Error);
                AddLog("❌ Clear cache failed: No admin privileges");
                return;
            }

            AddLog("🔄 Clearing DNS cache...");
            bool success = DnsManager.FlushDnsCache();

            if (success)
            {
                AddLog("✅ DNS cache cleared");
                ShowCustomDialog(LanguageManager.GetString("Success"), LanguageManager.GetString("CacheClearedSuccess"), DialogType.Success);
            }
            else
            {
                AddLog("❌ Failed to clear DNS cache");
                ShowCustomDialog(LanguageManager.GetString("Error"), LanguageManager.GetString("CacheClearFailed"), DialogType.Error);
            }
        }

        #endregion

        #region Settings Page

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string theme = item.Tag.ToString();
                ApplyTheme(theme);
                AddLog($"⚙️ Theme changed to {theme}");
                Properties.Settings.Default.Theme = theme;
                Properties.Settings.Default.Save();
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                string language = item.Content.ToString();
                LanguageManager.SetLanguage(language);
                ApplyLanguage();
                AddLog($"⚙️ Language changed to {language}");
            }
        }

        private void ApplyLanguage()
        {
            bool isPersian = LanguageManager.GetCurrentLanguage() == "فارسی";
            
            if (NavHomeButton != null) NavHomeButton.ToolTip = LanguageManager.GetString("Home");
            if (NavManageButton != null) NavManageButton.ToolTip = LanguageManager.GetString("ManageDns");
            if (NavLogsButton != null) NavLogsButton.ToolTip = LanguageManager.GetString("ActivityLogs");
            if (NavSettingsButton != null) NavSettingsButton.ToolTip = LanguageManager.GetString("Settings");
            if (SelectDnsLabel != null) SelectDnsLabel.Content = LanguageManager.GetString("SelectDnsServer");
            if (TokenLabel != null) TokenLabel.Content = LanguageManager.GetString("ZeusPlusToken");
            if (IntervalLabel != null) IntervalLabel.Content = LanguageManager.GetString("UpdateInterval");
            if (ClearCacheButton != null) ClearCacheButton.Content = LanguageManager.GetString("ClearDnsCache");
            if (StatusText != null && !isConnected) StatusText.Text = LanguageManager.GetString("OFF");
            if (StatusText != null && isConnected) StatusText.Text = LanguageManager.GetString("ON");
            if (ManageDnsTitle != null) ManageDnsTitle.Text = LanguageManager.GetString("ManageDnsServers");
            if (BuiltInDnsLabel != null) BuiltInDnsLabel.Text = LanguageManager.GetString("BuiltInDns");
            if (CustomDnsLabel != null) CustomDnsLabel.Text = LanguageManager.GetString("CustomDns");
            if (AddDnsDialogTitle != null) AddDnsDialogTitle.Text = LanguageManager.GetString("AddCustomDns");
            if (DnsNameLabel != null) DnsNameLabel.Content = LanguageManager.GetString("DnsName");
            if (PrimaryDnsLabel != null) PrimaryDnsLabel.Content = LanguageManager.GetString("PrimaryDns");
            if (SecondaryDnsLabel != null) SecondaryDnsLabel.Content = LanguageManager.GetString("SecondaryDns");
            if (CancelAddDnsButton != null) CancelAddDnsButton.Content = LanguageManager.GetString("Cancel");
            if (AddDnsButton != null) AddDnsButton.Content = LanguageManager.GetString("Add");
            if (LogsTitle != null) LogsTitle.Text = LanguageManager.GetString("ActivityLogs");
            if (CopyLogsButton != null) CopyLogsButton.Content = LanguageManager.GetString("CopyLogs");
            if (ClearLogsButton != null) ClearLogsButton.Content = LanguageManager.GetString("ClearLogs");
            if (SettingsTitle != null) SettingsTitle.Text = LanguageManager.GetString("Settings");
            if (ThemeTitleText != null) ThemeTitleText.Text = LanguageManager.GetString("Theme");
            if (ThemeDescText != null) ThemeDescText.Text = LanguageManager.GetString("ThemeDesc");
            if (LanguageTitleText != null) LanguageTitleText.Text = LanguageManager.GetString("Language");
            if (LanguageDescText != null) LanguageDescText.Text = LanguageManager.GetString("LanguageDesc");
            if (AutoRefreshTitleText != null) AutoRefreshTitleText.Text = LanguageManager.GetString("AutoRefreshIp");
            if (AutoRefreshDescText != null) AutoRefreshDescText.Text = LanguageManager.GetString("AutoRefreshDesc");
            if (StartupTitleText != null) StartupTitleText.Text = LanguageManager.GetString("StartupWithWindows");
            if (StartupDescText != null) StartupDescText.Text = LanguageManager.GetString("StartupWithWindowsDesc");
            if (SaveSettingsButton != null) SaveSettingsButton.Content = LanguageManager.GetString("SaveSettings");

            if (ThemeComboBox != null)
            {
                foreach (ComboBoxItem item in ThemeComboBox.Items)
                {
                    if (item.Tag?.ToString() == "Dark")
                        item.Content = LanguageManager.GetString("Dark");
                    else if (item.Tag?.ToString() == "Light")
                        item.Content = LanguageManager.GetString("Light");
                }
            }

            if (LanguageComboBox != null)
            {
                foreach (ComboBoxItem item in LanguageComboBox.Items)
                {
                    if (item.Tag?.ToString() == "English")
                        item.Content = LanguageManager.GetString("English");
                    else if (item.Tag?.ToString() == "Persian")
                        item.Content = LanguageManager.GetString("Persian");
                }
            }

            if (DialogOkButton != null) DialogOkButton.Content = LanguageManager.GetString("OK");
            if (DialogCancelButton != null) DialogCancelButton.Content = LanguageManager.GetString("Cancel");

            LoadCustomDnsServers();
            RefreshDnsComboBox();

            if (isPersian)
            {
                this.FlowDirection = System.Windows.FlowDirection.RightToLeft;
                if (LogTextBlock != null) LogTextBlock.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                
                var persianFont = TryFindResource("PersianFont") as FontFamily;
                if (persianFont != null)
                {
                    this.FontFamily = persianFont;
                }
            }
            else
            {
                this.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                
                var englishFont = TryFindResource("EnglishFont") as FontFamily;
                if (englishFont != null)
                {
                    this.FontFamily = englishFont;
                }
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem themeItem)
            {
                Properties.Settings.Default.Theme = themeItem.Tag?.ToString() ?? themeItem.Content.ToString();
            }

            if (LanguageComboBox.SelectedItem is ComboBoxItem langItem)
            {
                string langTag = langItem.Tag?.ToString();
                if (langTag == "English")
                    Properties.Settings.Default.Language = "English";
                else if (langTag == "Persian")
                    Properties.Settings.Default.Language = "فارسی";
                else
                    Properties.Settings.Default.Language = langItem.Content.ToString();
            }

            bool autoRefresh = AutoRefreshCheckBox.IsChecked ?? true;
            if (Properties.Settings.Default.AutoRefreshIP != autoRefresh)
            {
                Properties.Settings.Default.AutoRefreshIP = autoRefresh;
                AddLog(autoRefresh ? "✅ Auto-refresh IP enabled" : "✅ Auto-refresh IP disabled");
            }

            bool startup = StartupCheckBox.IsChecked ?? false;
            if (Properties.Settings.Default.StartupWithWindows != startup)
            {
                Properties.Settings.Default.StartupWithWindows = startup;
                SetStartupWithWindows(startup);
                AddLog(startup ? "✅ Added to Windows startup" : "✅ Removed from Windows startup");
            }

            if (IntervalSlider != null)
            {
                Properties.Settings.Default.IpRefreshInterval = (int)IntervalSlider.Value;
            }

            Properties.Settings.Default.Save();

            AddLog("✅ Settings saved");
            ShowCustomDialog(LanguageManager.GetString("Success"), LanguageManager.GetString("SettingsSaved"), DialogType.Success);

            if (Properties.Settings.Default.AutoRefreshIP && isConnected)
            {
                StopIpRefreshTimer();
                StartIpRefreshTimer();
            }
            else if (!Properties.Settings.Default.AutoRefreshIP)
            {
                StopIpRefreshTimer();
            }
        }

        private void SetStartupWithWindows(bool enable)
        {
            try
            {
                string taskName = "ZeusDNSChanger";
                string appPath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                
                if (string.IsNullOrEmpty(appPath))
                {
                    AddLog("❌ Could not determine application path");
                    return;
                }

                var deleteProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = $"/Delete /TN \"{taskName}\" /F",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                deleteProcess.Start();
                deleteProcess.WaitForExit();

                if (enable)
                {
                    var createProcess = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "schtasks.exe",
                            Arguments = $"/Create /TN \"{taskName}\" /TR \"\\\"{appPath}\\\"\" /SC ONLOGON /RL HIGHEST /F",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };
                    createProcess.Start();
                    createProcess.WaitForExit();
                    
                    if (createProcess.ExitCode == 0)
                    {
                        AddLog("✅ Added to Windows startup (Task Scheduler)");
                    }
                    else
                    {
                        string error = createProcess.StandardError.ReadToEnd();
                        AddLog($"❌ Failed to add startup task: {error}");
                    }
                }
                else
                {
                    AddLog("✅ Removed from Windows startup");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Startup setting error: {ex.Message}");
            }
        }

        #endregion

        #region Manage DNS

        private void LoadCustomDnsServers()
        {
            CustomDnsManager.LoadFromSettings();
            RefreshCustomDnsList();
            RefreshDnsComboBox();
        }

        private void RefreshCustomDnsList()
        {
            if (CustomDnsItemsPanel == null)
                return;

            CustomDnsItemsPanel.Children.Clear();

            var customServers = CustomDnsManager.GetCustomServers();

            foreach (var server in customServers)
            {
                var border = new Border
                {
                    Background = TryFindResource("DarkSurfaceBrush") as SolidColorBrush,
                    BorderBrush = TryFindResource("DarkBorderBrush") as SolidColorBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var stackPanel = new StackPanel();
                var nameText = new TextBlock
                {
                    Text = server.Name,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TryFindResource("TextPrimaryBrush") as SolidColorBrush
                };
                stackPanel.Children.Add(nameText);

                var dnsText = new TextBlock
                {
                    Text = $"Primary: {server.PrimaryDns} | Secondary: {server.SecondaryDns}",
                    FontSize = 12,
                    Foreground = TryFindResource("TextSecondaryBrush") as SolidColorBrush,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                stackPanel.Children.Add(dnsText);

                Grid.SetColumn(stackPanel, 0);
                grid.Children.Add(stackPanel);

                var deleteButton = new System.Windows.Controls.Button
                {
                    Content = LanguageManager.GetString("Delete"),
                    Width = 80,
                    Height = 35,
                    Tag = server.Name
                };
                deleteButton.Click += DeleteCustomDns_Click;
                Grid.SetColumn(deleteButton, 1);
                grid.Children.Add(deleteButton);

                border.Child = grid;
                CustomDnsItemsPanel.Children.Add(border);
            }
        }

        private void RefreshDnsComboBox()
        {
            if (DnsServerComboBox == null)
                return;

            string currentSelection = "";
            if (DnsServerComboBox.SelectedItem is ComboBoxItem item)
            {
                currentSelection = item.Content.ToString();
            }

            DnsServerComboBox.Items.Clear();

            DnsServerComboBox.Items.Add(new ComboBoxItem { Content = "Zeus Free" });
            DnsServerComboBox.Items.Add(new ComboBoxItem { Content = "Zeus Plus" });
            DnsServerComboBox.Items.Add(new ComboBoxItem { Content = "Google DNS" });
            DnsServerComboBox.Items.Add(new ComboBoxItem { Content = "Cloudflare DNS" });

            var customServers = CustomDnsManager.GetCustomServers();
            foreach (var server in customServers)
            {
                DnsServerComboBox.Items.Add(new ComboBoxItem { Content = server.Name });
            }

            if (!string.IsNullOrEmpty(currentSelection))
            {
                foreach (ComboBoxItem comboItem in DnsServerComboBox.Items)
                {
                    if (comboItem.Content.ToString() == currentSelection)
                    {
                        DnsServerComboBox.SelectedItem = comboItem;
                        break;
                    }
                }
            }

            if (DnsServerComboBox.SelectedItem == null && DnsServerComboBox.Items.Count > 0)
            {
                DnsServerComboBox.SelectedIndex = 0;
            }
        }

        private void FabAddDns_Click(object sender, RoutedEventArgs e)
        {
            if (AddDnsDialog != null)
            {
                DnsNameTextBox.Text = "";
                PrimaryDnsTextBox.Text = "";
                SecondaryDnsTextBox.Text = "";
                UpdateHintVisibility();
                AddDnsDialog.Visibility = Visibility.Visible;
            }
        }

        private void UpdateHintVisibility()
        {
            if (DnsNameHint != null)
                DnsNameHint.Visibility = string.IsNullOrEmpty(DnsNameTextBox?.Text) ? Visibility.Visible : Visibility.Collapsed;
            if (PrimaryDnsHint != null)
                PrimaryDnsHint.Visibility = string.IsNullOrEmpty(PrimaryDnsTextBox?.Text) ? Visibility.Visible : Visibility.Collapsed;
            if (SecondaryDnsHint != null)
                SecondaryDnsHint.Visibility = string.IsNullOrEmpty(SecondaryDnsTextBox?.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DnsNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DnsNameHint != null)
                DnsNameHint.Visibility = string.IsNullOrEmpty(DnsNameTextBox?.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PrimaryDnsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PrimaryDnsHint != null)
                PrimaryDnsHint.Visibility = string.IsNullOrEmpty(PrimaryDnsTextBox?.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SecondaryDnsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SecondaryDnsHint != null)
                SecondaryDnsHint.Visibility = string.IsNullOrEmpty(SecondaryDnsTextBox?.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool IsValidIpAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;
            
            string[] parts = ip.Trim().Split('.');
            if (parts.Length != 4)
                return false;
            
            foreach (string part in parts)
            {
                if (!int.TryParse(part, out int num))
                    return false;
                if (num < 0 || num > 255)
                    return false;
                if (part.Length > 1 && part.StartsWith("0"))
                    return false;
            }
            
            return true;
        }

        private void CancelAddDns_Click(object sender, RoutedEventArgs e)
        {
            if (AddDnsDialog != null)
            {
                AddDnsDialog.Visibility = Visibility.Collapsed;
            }
        }

        private void AddDnsConfirm_Click(object sender, RoutedEventArgs e)
        {
            string name = DnsNameTextBox?.Text.Trim() ?? "";
            string primary = PrimaryDnsTextBox?.Text.Trim() ?? "";
            string secondary = SecondaryDnsTextBox?.Text.Trim() ?? "";

            if (string.IsNullOrEmpty(name))
            {
                ShowCustomDialog(LanguageManager.GetString("Error"), LanguageManager.GetString("EnterDnsName"), DialogType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(primary))
            {
                ShowCustomDialog(LanguageManager.GetString("Error"), LanguageManager.GetString("EnterPrimaryDns"), DialogType.Warning);
                return;
            }

            if (!IsValidIpAddress(primary))
            {
                ShowCustomDialog(LanguageManager.GetString("Error"), LanguageManager.GetString("InvalidPrimaryIp"), DialogType.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(secondary) && !IsValidIpAddress(secondary))
            {
                ShowCustomDialog(LanguageManager.GetString("Error"), LanguageManager.GetString("InvalidSecondaryIp"), DialogType.Warning);
                return;
            }

            var existingServers = CustomDnsManager.GetCustomServers();
            if (existingServers.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                ShowCustomDialog(LanguageManager.GetString("Error"), LanguageManager.GetString("DnsNameExists"), DialogType.Warning);
                return;
            }

            var newServer = new CustomDnsServer(name, primary, secondary);
            CustomDnsManager.AddServer(newServer);

            AddLog($"✅ Custom DNS added: {name}");

            RefreshCustomDnsList();
            RefreshDnsComboBox();

            if (AddDnsDialog != null)
            {
                AddDnsDialog.Visibility = Visibility.Collapsed;
            }

            ShowCustomDialog(LanguageManager.GetString("Success"), LanguageManager.GetString("DnsAddedSuccess"), DialogType.Success);
        }

        private string pendingDeleteDnsName = null;

        private void DeleteCustomDns_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string name)
            {
                pendingDeleteDnsName = name;
                ShowCustomDialog(LanguageManager.GetString("ConfirmDelete"), $"{LanguageManager.GetString("DnsDeleteConfirm")} '{name}'?", DialogType.Confirm, (confirmed) =>
                {
                    if (confirmed && !string.IsNullOrEmpty(pendingDeleteDnsName))
                    {
                        CustomDnsManager.RemoveServer(pendingDeleteDnsName);
                        AddLog($"🗑️ Custom DNS deleted: {pendingDeleteDnsName}");

                        RefreshCustomDnsList();
                        RefreshDnsComboBox();

                        ShowCustomDialog(LanguageManager.GetString("Success"), LanguageManager.GetString("DnsDeletedSuccess"), DialogType.Success);
                        pendingDeleteDnsName = null;
                    }
                });
            }
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            StopIpMonitoring();
            StopIpRefreshTimer();
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
            base.OnClosed(e);
        }

        #region Custom Dialog

        private Action<bool> dialogCallback;

        public enum DialogType
        {
            Info,
            Success,
            Warning,
            Error,
            Confirm
        }

        private void ShowCustomDialog(string title, string message, DialogType type, Action<bool> callback = null)
        {
            dialogCallback = callback;

            if (DialogTitle != null) DialogTitle.Text = title;
            if (DialogMessage != null) DialogMessage.Text = message;

            string icon = type switch
            {
                DialogType.Success => "\uE73E",
                DialogType.Warning => "\uE7BA",
                DialogType.Error => "\uE711",
                DialogType.Confirm => "\uE897",
                _ => "\uE946"
            };

            if (DialogIcon != null)
            {
                DialogIcon.Text = icon;
                DialogIcon.Foreground = type switch
                {
                    DialogType.Success => TryFindResource("SuccessBrush") as SolidColorBrush ?? Brushes.Green,
                    DialogType.Error => TryFindResource("ErrorBrush") as SolidColorBrush ?? Brushes.Red,
                    DialogType.Warning => new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 152, 0)),
                    _ => TryFindResource("GoldBrush") as SolidColorBrush ?? Brushes.Gold
                };
            }

            if (DialogCancelButton != null)
            {
                DialogCancelButton.Visibility = type == DialogType.Confirm ? Visibility.Visible : Visibility.Collapsed;
            }

            if (CustomMessageDialog != null)
            {
                CustomMessageDialog.Visibility = Visibility.Visible;
            }
        }

        private void DialogOk_Click(object sender, RoutedEventArgs e)
        {
            if (CustomMessageDialog != null)
            {
                CustomMessageDialog.Visibility = Visibility.Collapsed;
            }
            dialogCallback?.Invoke(true);
            dialogCallback = null;
        }

        private void DialogCancel_Click(object sender, RoutedEventArgs e)
        {
            if (CustomMessageDialog != null)
            {
                CustomMessageDialog.Visibility = Visibility.Collapsed;
            }
            dialogCallback?.Invoke(false);
            dialogCallback = null;
        }

        #endregion
    }
}
