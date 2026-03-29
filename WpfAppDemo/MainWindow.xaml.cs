using System;
using System.Windows;
using System.Threading.Tasks;

namespace WpfAppDemo
{
    public partial class MainWindow : Window
    {
        private ModbusTcpClient _modbusClient;
        private DeviceData _deviceData;
        private bool _isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
            _deviceData = new DeviceData();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string host = HostTextBox.Text;
                if (!int.TryParse(PortTextBox.Text, out int port))
                {
                    MessageBlock.Text = "端口号无效";
                    return;
                }

                _modbusClient = new ModbusTcpClient(host, port);
                bool connected = await _modbusClient.ConnectAsync();

                if (connected)
                {
                    _isConnected = true;
                    ConnectButton.IsEnabled = false;
                    DisconnectButton.IsEnabled = true;
                    HostTextBox.IsEnabled = false;
                    PortTextBox.IsEnabled = false;
                    ReadButton.IsEnabled = true;
                    StatusBlock.Text = "已连接";
                    StatusBlock.Foreground = System.Windows.Media.Brushes.Green;
                    MessageBlock.Text = $"已连接到 {host}:{port}";
                }
                else
                {
                    MessageBlock.Text = "连接失败";
                    StatusBlock.Text = "连接失败";
                    StatusBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBlock.Text = $"错误: {ex.Message}";
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_modbusClient != null)
                {
                    _modbusClient.Disconnect();
                }

                _isConnected = false;
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                HostTextBox.IsEnabled = true;
                PortTextBox.IsEnabled = true;
                ReadButton.IsEnabled = false;
                StatusBlock.Text = "未连接";
                StatusBlock.Foreground = System.Windows.Media.Brushes.Orange;
                MessageBlock.Text = "已断开连接";
            }
            catch (Exception ex)
            {
                MessageBlock.Text = $"错误: {ex.Message}";
            }
        }

        private async void ReadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConnected || _modbusClient == null)
            {
                MessageBlock.Text = "未连接到设备";
                return;
            }

            try
            {
                ReadButton.IsEnabled = false;
                MessageBlock.Text = "正在读取数据...";

                // 读取3个寄存器 (温度、湿度、气压)
                // 从地址 0 开始，读取 3 个值
                ushort[] registers = await _modbusClient.ReadHoldingRegistersAsync(slaveId: 1, startAddress: 0, quantity: 3);

                if (registers != null && registers.Length >= 3)
                {
                    // 模拟数据处理 (实际应根据设备协议调整)
                    float temperature = registers[0] / 10.0f;
                    float humidity = registers[1] / 10.0f;
                    float pressure = registers[2] / 10.0f;

                    TemperatureBlock.Text = $"{temperature:F1} °C";
                    HumidityBlock.Text = $"{humidity:F1} %";
                    PressureBlock.Text = $"{pressure:F1} hPa";
                    _deviceData.LastUpdate = DateTime.Now;
                    LastUpdateBlock.Text = $"最后更新: {_deviceData.LastUpdate:yyyy-MM-dd HH:mm:ss}";

                    MessageBlock.Text = "数据读取成功";
                    MessageBlock.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    MessageBlock.Text = "读取数据失败";
                    MessageBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBlock.Text = $"错误: {ex.Message}";
                MessageBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                ReadButton.IsEnabled = _isConnected;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            TemperatureBlock.Text = "-- °C";
            HumidityBlock.Text = "-- %";
            PressureBlock.Text = "-- hPa";
            LastUpdateBlock.Text = "--";
            MessageBlock.Text = "";
            MessageBlock.Foreground = System.Windows.Media.Brushes.Black;
        }
    }
}
