# 🖥️ System Analyzer - Windows Performance Monitor

A comprehensive Windows system performance monitoring and analysis tool built with .NET 8.0. This tool provides real-time system monitoring, anomaly detection, and generates detailed performance reports in multiple formats.

## ✨ Features

- **🔍 Real-time Monitoring**: CPU, Memory, Disk, and Network usage
- **🚨 Anomaly Detection**: Identifies suspicious processes and system behavior
- **📊 Multiple Report Formats**: HTML, CSV, JSON, TXT
- **🎯 Interactive Console**: Real-time display with user commands
- **⚙️ Configurable Thresholds**: Custom CPU and memory warning levels
- **🧵 Thread-Safe**: Robust multi-threaded architecture
- **💾 Memory Efficient**: Smart memory management and cleanup
- **🔒 Security Analysis**: Process behavior analysis and threat detection

## 🚀 Quick Start

### Prerequisites

- Windows 10/11
- .NET 8.0 Runtime or SDK
- Administrator privileges (recommended for full system access)

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/SystemAnalyzer.git
   cd SystemAnalyzer
   ```

2. **Build the project:**
   ```bash
   dotnet build -c Release
   ```

3. **Run the analyzer:**
   ```bash
   dotnet run -- --help
   ```

### Basic Usage

```bash
# Monitor for 5 minutes with 30-second intervals
dotnet run -- -i 30 -d 5 -f html -o "performance_report.html"

# Silent mode with custom thresholds
dotnet run -- -i 10 -d 10 -f json -o "system_data.json" --silent --threshold-cpu 70

# Verbose monitoring with all formats
dotnet run -- -i 15 -d 3 -f csv -o "detailed_report.csv" --verbose
```

## 📖 Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--interval` | `-i` | Monitoring interval in seconds | 60 |
| `--duration` | `-d` | Analysis duration in minutes | Unlimited |
| `--output` | `-o` | Output file path | Auto-generated |
| `--format` | `-f` | Report format (html, csv, json, txt) | html |
| `--verbose` | `-v` | Enable verbose output | false |
| `--silent` | `-s` | Silent mode (no console output) | false |
| `--threshold-cpu` | `-t` | CPU warning threshold (%) | 80 |
| `--threshold-mem` | `-m` | Memory warning threshold (%) | 80 |
| `--help` | `-h` | Show help information | - |

## 🎮 Runtime Commands

While the analyzer is running, you can use these keyboard shortcuts:

- **Q** - Quit application
- **P** - Pause/Resume monitoring
- **S** - Show current statistics
- **C** - Clear console screen
- **H** - Show help

## 📊 Report Formats

### HTML Report
Rich, interactive web-based report with:
- Real-time performance charts
- Color-coded system health indicators
- Process analysis tables
- Network activity summary
- Anomaly detection results

### CSV Report
Structured data format for Excel analysis:
- System performance timeline
- Process resource consumption
- Network statistics
- Easy data manipulation and charting

### JSON Report
Machine-readable format for programmatic access:
- Complete system snapshots
- Structured performance metrics
- API integration friendly
- Custom analysis pipeline support

### TXT Report
Human-readable text report:
- Executive summary
- Performance recommendations
- System health assessment
- Ideal for documentation

## 🏗️ Architecture

### Clean Architecture Principles
- **Core Layer**: Business logic and domain models
- **Infrastructure Layer**: External dependencies (Windows APIs, file system)
- **Presentation Layer**: Console UI and report generation

### Design Patterns Used
- **Dependency Injection**: Loose coupling and testability
- **Factory Pattern**: Multiple export format support
- **Strategy Pattern**: Pluggable analysis algorithms
- **Facade Pattern**: Simplified complex subsystem access
- **Observer Pattern**: Real-time event handling

### Key Components

```
SystemAnalyzer/
├── 📁 Core/
│   ├── 📁 Interfaces/     # Contracts and abstractions
│   ├── 📁 Models/         # Domain models and DTOs
│   └── 📁 Services/       # Business logic implementation
├── 📁 Infrastructure/
│   ├── 📁 Monitors/       # System data collection
│   ├── 📁 Exporters/      # Report generation
│   ├── 📁 Security/       # Anomaly detection
│   └── 📁 UI/            # User interface
└── 📄 Program.cs         # Application entry point
```

## 🔒 Security Features

- **Process Anomaly Detection**: Identifies suspicious processes
- **Resource Abuse Detection**: Monitors excessive CPU/memory usage
- **Network Activity Analysis**: Tracks unusual network patterns
- **System Health Monitoring**: Overall security posture assessment

## 🛠️ Development

### Building from Source

1. **Install .NET 8.0 SDK**
2. **Clone and build:**
   ```bash
   git clone https://github.com/yourusername/SystemAnalyzer.git
   cd SystemAnalyzer
   dotnet restore
   dotnet build
   ```

### Running Tests
```bash
dotnet test
```

### Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 Performance Tips

- **Administrator Rights**: Run as administrator for complete system access
- **Monitoring Interval**: Use longer intervals (60s+) for extended monitoring
- **Memory Management**: The tool automatically manages memory for long-running sessions
- **Output Location**: Use SSD storage for faster report generation

## 🐛 Troubleshooting

### Common Issues

**"Access Denied" errors:**
- Run as Administrator
- Check Windows UAC settings

**High CPU usage:**
- Increase monitoring interval
- Use silent mode for background monitoring

**Performance counter errors:**
- Restart Windows Performance Toolkit service
- Run `lodctr /R` as Administrator

## 📊 Sample Output

```
╔══════════════════════════════════════════════════════════════╗
║              SYSTEM ANALYZER - PERFORMANCE MONITOR          ║
╚══════════════════════════════════════════════════════════════╝
📅 Start Time: 03.08.2025 10:30:00
⏱️  Interval: 30 seconds
⏰ Duration: 5 minutes
📄 Output: performance_report.html (HTML)
🚨 CPU Threshold: 80% | Memory Threshold: 80%

💻 CPU: 45.2%             🧠 RAM: 67.8%
💾 Disk: 12.1%            ⏰ Time: 10:30:30
🔢 Processes: 156         🌐 Connections: 42

🚀 Top CPU-Consuming Processes:
PID      Name                 CPU%     RAM(MB)    Threads
────────────────────────────────────────────────────────────────
4892     chrome               15.2     445        28
1234     code                 8.7      312        15
5678     SystemAnalyzer       2.1      45         8
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Acknowledgments

- Built with .NET 8.0 and Microsoft Extensions
- Uses Windows Management Instrumentation (WMI)
- Performance Counter API integration
- Modern async/await patterns

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/SystemAnalyzer/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/SystemAnalyzer/discussions)
- **Documentation**: [Wiki](https://github.com/yourusername/SystemAnalyzer/wiki)

---

⭐ **Star this repository if you find it helpful!**
