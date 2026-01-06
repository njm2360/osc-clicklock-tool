using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Rug.Osc;

namespace OscLockApp
{
    public partial class MainWindow : Window
    {
        private readonly string oscAddress = "/input/UseLeft";
        private bool _isRunning = false;
        private readonly OscSender _osc = null!;
        private readonly DispatcherTimer _timer = new();

        private record Config(string Host, int Port);

        private Config LoadConfig()
        {
            const string file = "config.json";

            try
            {
                if (!File.Exists(file))
                {
                    var defaultConfig = new Config("127.0.0.1", 9000);
                    File.WriteAllText(file, JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true }));
                    return defaultConfig;
                }

                var json = File.ReadAllText(file);
                var cfg = JsonSerializer.Deserialize<Config>(json);

                if (cfg == null)
                    throw new Exception("Config deserialize returned null");

                return cfg;
            }
            catch
            {
                var fallback = new Config("127.0.0.1", 9000);
                File.WriteAllText(file, JsonSerializer.Serialize(fallback, new JsonSerializerOptions { WriteIndented = true }));
                return fallback;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            var cfg = LoadConfig();

            try
            {
                var ip = IPAddress.Parse(cfg.Host);
                _osc = new OscSender(ip, 0, cfg.Port);
                _osc.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"OSC接続に失敗しました。\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            _timer.Interval = TimeSpan.FromSeconds(0.2);
            _timer.Tick += Timer_Tick;

            this.Closing += MainWindow_Closing;

            UpdateButton();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton.IsEnabled = false;

            if (_isRunning)
            {
                SendInt(1);
                _pendingValue = 0;
            }
            else
            {
                SendInt(0);
                _pendingValue = 1;
            }

            _timer.Stop();
            _timer.Start();
        }

        private int _pendingValue;

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            SendInt(_pendingValue);
            _isRunning = !_isRunning;
            UpdateButton();

            ToggleButton.IsEnabled = true;
        }

        private void UpdateButton()
        {
            if (_isRunning)
            {
                ToggleButton.Content = "停止";
                ToggleButton.Background = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                ToggleButton.Foreground = Brushes.White;
            }
            else
            {
                ToggleButton.Content = "開始";
                ToggleButton.Background = new SolidColorBrush(Color.FromRgb(144, 238, 144));
                ToggleButton.Foreground = Brushes.Black;
            }
        }

        private void SendInt(int value)
        {
            var msg = new OscMessage(oscAddress, value);
            _osc.Send(msg);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _osc.Dispose();
            }
            catch { }
        }
    }
}
