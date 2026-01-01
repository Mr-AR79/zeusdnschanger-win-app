# ⚡ Zeus DNS Changer

A professional Windows DNS management application with a beautiful dark-themed interface.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)
![.NET](https://img.shields.io/badge/.NET-6.0-purple.svg)

## 🎯 Features

- **Multiple DNS Providers**: Zeus Free, Zeus Plus, Google DNS, Cloudflare DNS
- **Zeus Plus Token System**: Automatic token registration with IP monitoring
- **Automatic IP Monitoring**: Detects IP changes and re-registers token automatically
- **Network Information Display**: Shows active network interface, IPv4 address, and current DNS
- **DNS Cache Management**: One-click DNS cache clearing
- **Dark Mode UI**: Beautiful gold (#FFD700) and dark-themed interface
- **Administrator Privileges**: Automatic elevation for DNS modifications

## 📋 Requirements

- Windows 10 or later
- .NET 10.0 Runtime
- Administrator privileges (automatic elevation)
- Visual Studio 2022 (for development)

## 🚀 How to Build

### Method 1: Visual Studio

1. Open Visual Studio 2022
2. Click `File` → `Open` → `Project/Solution`
3. Navigate to the folder and select `ZeusDNSChanger.csproj`
4. Press `F5` or click `Start` to build and run

### Method 2: Command Line

```bash
# Navigate to project directory
cd ZeusDNSChanger

# Restore packages
dotnet restore

# Build the project
dotnet build -c Release

# Run the application
dotnet run
```

## 📦 Project Structure

```
ZeusDNSChanger/
├── App.xaml                    # Application resources and styles
├── App.xaml.cs                 # Application code-behind
├── MainWindow.xaml             # Main UI design
├── MainWindow.xaml.cs          # Main window logic
├── DnsManager.cs               # DNS management functionality
├── NetworkHelper.cs            # Network and API communication
├── ZeusDNSChanger.csproj       # Project configuration
├── app.manifest                # Administrator elevation manifest
└── README.md                   # Documentation
```

## 🎮 How to Use

### Basic Usage

1. **Run as Administrator**: The application will request administrator privileges automatically
2. **Select DNS Server**: Choose from the dropdown menu:
   - Zeus Free
   - Zeus Plus (requires token)
   - Google DNS
   - Cloudflare DNS
3. **Click START**: Activates the selected DNS configuration
4. **Click STOP**: Reverts to automatic DNS settings

### Zeus Plus Configuration

1. Select "Zeus Plus" from the dropdown
2. Enter your Zeus Plus token in the text field
3. Set the update interval (1-60 minutes) using the slider
4. Click START to activate

**Features**:
- Automatic token registration with your IP
- Periodic IP monitoring
- Automatic re-registration if IP changes
- Token is locked when service is active

### DNS Addresses

The application uses the following DNS addresses:

- **Zeus Free**: 178.22.122.100, 185.51.200.2
- **Zeus Plus**: Dynamic (set after token registration)
- **Google DNS**: 8.8.8.8, 8.8.4.4
- **Cloudflare DNS**: 1.1.1.1, 1.0.0.1

### Clear DNS Cache

Click the "CLEAR" button in the DNS Cache section to flush all cached DNS records.

## 🔧 API Endpoints

The application communicates with the following APIs:

1. **Get Public IP**: `http://37.32.5.34:81`
   - Returns your current public IPv4 address

2. **Register Zeus Plus Token**: `http://37.32.5.34:82/tap-in?token={TOKEN}&ip={IP}`
   - Registers your token with your current IP
   - Automatically called when IP changes (Zeus Plus only)

## 🎨 UI Features

- **Gold Accent Color**: #FFD700 throughout the interface
- **Dark Theme**: Easy on the eyes with #1A1A1A background
- **Responsive Design**: Clean card-based layout
- **Real-time Updates**: Live network status monitoring
- **Modern Typography**: Clean, professional fonts

## ⚙️ Technical Details

### DNS Management

The application uses Windows `netsh` commands to modify DNS settings:

```bash
# Set DNS
netsh interface ip set dns "{Interface}" static {DNS} primary

# Clear DNS (set to DHCP)
netsh interface ip set dns "{Interface}" dhcp

# Flush DNS cache
ipconfig /flushdns
```

### IP Monitoring

For Zeus Plus users, the application:
1. Fetches current IP on startup
2. Starts a timer based on selected interval
3. Periodically checks IP address
4. Re-registers token if IP changes

## 🛡️ Security

- Requires administrator privileges for DNS modifications
- Token is stored in memory only (not saved to disk)
- HTTPS is not used for API calls (consider using VPN)

## ⚠️ Troubleshooting

### "Administrator Required" Error
- Right-click the executable and select "Run as administrator"
- Or configure the app.manifest to require administrator privileges

### DNS Not Changing
1. Verify you have administrator privileges
2. Check if your network interface is active
3. Try disabling any VPN or proxy services
4. Restart the application

### Zeus Plus Token Registration Fails
1. Verify your token is correct
2. Check your internet connection
3. Ensure the API server is accessible
4. Try refreshing your IP address

### IP Address Shows Error
1. Check your internet connection
2. Verify the IP API endpoint is accessible
3. Try clicking the refresh button (↻)

## 📝 Development Notes

### Key Classes

- **DnsManager**: Handles all DNS-related operations
- **NetworkHelper**: Manages API communication and IP retrieval
- **MainWindow**: Contains all UI logic and event handlers

### Future Enhancements

- [ ] Add DNS speed testing
- [ ] Support for custom DNS servers
- [ ] DNS-over-HTTPS (DoH) support
- [ ] System tray minimization
- [ ] Startup with Windows option
- [ ] Multi-language support
- [ ] Connection history log
- [ ] Export/Import settings

## 📄 License

This project is licensed under the MIT License.

## 👨‍💻 Author

Created with ⚡ by Zeus Team

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!

---

**Note**: This application modifies system network settings. Always ensure you have a backup DNS configuration and understand the implications of changing DNS servers.
