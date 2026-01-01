using System;
using System.Windows;
using System.Windows.Controls;

namespace ZeusDNSChanger
{
    public partial class SettingsWindow : Window
    {
        public string SelectedTheme { get; private set; }
        public string SelectedLanguage { get; private set; }
        public bool AutoRefreshIP { get; private set; }

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            UpdateUILanguage();
        }

        private void UpdateUILanguage()
        {
            this.Title = LanguageManager.GetString("Settings");
            UpdateTextBlocks();
        }

        private void UpdateTextBlocks()
        {
            HeaderText.Text = LanguageManager.GetString("Settings");
            ThemeText.Text = LanguageManager.GetString("Theme");
            ThemeDescText.Text = LanguageManager.GetString("ThemeDesc");
            DarkThemeItem.Content = LanguageManager.GetString("Dark");
            LightThemeItem.Content = LanguageManager.GetString("Light");
            LanguageText.Text = LanguageManager.GetString("Language");
            LanguageDescText.Text = LanguageManager.GetString("LanguageDesc");
            EnglishLangItem.Content = LanguageManager.GetString("English");
            PersianLangItem.Content = LanguageManager.GetString("Persian");
            AutoRefreshText.Text = LanguageManager.GetString("AutoRefreshIp");
            AutoRefreshDescText.Text = LanguageManager.GetString("AutoRefreshDesc");
            StartupText.Text = LanguageManager.GetString("StartupWithWindows");
            StartupDescText.Text = LanguageManager.GetString("StartupWithWindowsDesc");
            SaveButton.Content = LanguageManager.GetString("SaveSettings");
            if (LanguageManager.IsPersian())
            {
                this.FlowDirection = System.Windows.FlowDirection.RightToLeft;
            }
            else
            {
                this.FlowDirection = System.Windows.FlowDirection.LeftToRight;
            }
        }

        private void LoadSettings()
        {
            if (Properties.Settings.Default.Theme == "Light")
            {
                ThemeComboBox.SelectedIndex = 1;
            }
            else
            {
                ThemeComboBox.SelectedIndex = 0;
            }

            if (Properties.Settings.Default.Language == "فارسی")
            {
                LanguageComboBox.SelectedIndex = 1;
            }
            else
            {
                LanguageComboBox.SelectedIndex = 0;
            }

            AutoRefreshCheckBox.IsChecked = Properties.Settings.Default.AutoRefreshIP;
            StartupCheckBox.IsChecked = Properties.Settings.Default.StartupWithWindows;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                SelectedTheme = item.Tag.ToString();
                ApplyTheme(SelectedTheme);
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                SelectedLanguage = item.Content.ToString();
                LanguageManager.SetLanguage(SelectedLanguage);
                UpdateTextBlocks();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Theme = SelectedTheme ?? "Dark";
            Properties.Settings.Default.Language = SelectedLanguage ?? "English";
            Properties.Settings.Default.AutoRefreshIP = AutoRefreshCheckBox.IsChecked ?? true;
            Properties.Settings.Default.StartupWithWindows = StartupCheckBox.IsChecked ?? false;
            Properties.Settings.Default.Save();

            SetStartupWithWindows(StartupCheckBox.IsChecked ?? false);

            DialogResult = true;
            Close();
        }

        private void SetStartupWithWindows(bool enable)
        {
            try
            {
                string taskName = "ZeusDNSChanger";
                string appPath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                
                if (string.IsNullOrEmpty(appPath)) return;

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
                }
            }
            catch
            {
            }
        }

        private void ApplyTheme(string theme)
        {
            var app = Application.Current;
            var resources = app.Resources;

            if (theme == "Light")
            {
                resources["DarkBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(245, 245, 245));
                resources["DarkSurfaceBrush"] = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["DarkBorderBrush"] = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(200, 200, 200));
                resources["TextPrimaryBrush"] = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(33, 33, 33));
                resources["TextSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(100, 100, 100));
            }
            else
            {
                resources["DarkBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(26, 26, 26));
                resources["DarkSurfaceBrush"] = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(37, 37, 37));
                resources["DarkBorderBrush"] = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(51, 51, 51));
                resources["TextPrimaryBrush"] = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 255, 255));
                resources["TextSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(176, 176, 176));
            }
        }
    }
}
