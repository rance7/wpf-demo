using System;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WpfAppDemo
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private ModbusTcpClient _modbusClient;

        [ObservableProperty]
        private string host = "127.0.0.1";

        [ObservableProperty]
        private string port = "502";

        [ObservableProperty]
        private bool isConnected = false;

        // 计算属性 - 不需要单独的绑定属性
        public bool IsNotConnected => !IsConnected;

        [ObservableProperty]
        private string temperature = "-- °C";

        [ObservableProperty]
        private string humidity = "-- %";

        [ObservableProperty]
        private string pressure = "-- hPa";

        [ObservableProperty]
        private string status = "未连接";

        [ObservableProperty]
        private SolidColorBrush statusForeground = Brushes.Orange;

        [ObservableProperty]
        private string lastUpdate = "--";

        [ObservableProperty]
        private string message = "";

        [ObservableProperty]
        private SolidColorBrush messageForeground = Brushes.Black;

        // 计算属性 - 根据连接状态自动推导，无需单独的绑定属性
        public bool ConnectButtonEnabled => !IsConnected;
        public bool DisconnectButtonEnabled => IsConnected;
        public bool ReadButtonEnabled => IsConnected;
        public bool HostTextBoxEnabled => !IsConnected;
        public bool PortTextBoxEnabled => !IsConnected;

        [RelayCommand]
        private async Task ConnectAsync()
        {
            try
            {
                if (!int.TryParse(Port, out int portNumber))
                {
                    Message = "端口号无效";
                    return;
                }

                _modbusClient = new ModbusTcpClient(Host, portNumber);
                bool connected = await _modbusClient.ConnectAsync();

                if (connected)
                {
                    IsConnected = true;
                    Status = "已连接";
                    StatusForeground = Brushes.Green;
                    Message = $"已连接到 {Host}:{Port}";
                }
                else
                {
                    Message = "连接失败";
                    Status = "连接失败";
                    StatusForeground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                Message = $"错误: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Disconnect()
        {
            try
            {
                if (_modbusClient != null)
                {
                    _modbusClient.Disconnect();
                }

                IsConnected = false;
                Status = "未连接";
                StatusForeground = Brushes.Orange;
                Message = "已断开连接";
            }
            catch (Exception ex)
            {
                Message = $"错误: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ReadAsync()
        {
            if (!IsConnected || _modbusClient == null)
            {
                Message = "未连接到设备";
                return;
            }

            try
            {
                Message = "正在读取数据...";

                // 读取3个寄存器 (温度、湿度、气压)
                // 从地址 0 开始，读取 3 个值
                ushort[] registers = await _modbusClient.ReadHoldingRegistersAsync(slaveId: 1, startAddress: 0, quantity: 3);

                if (registers != null && registers.Length >= 3)
                {
                    // 模拟数据处理 (实际应根据设备协议调整)
                    float temperature = registers[0] / 10.0f;
                    float humidity = registers[1] / 10.0f;
                    float pressure = registers[2] / 10.0f;

                    Temperature = $"{temperature:F1} °C";
                    Humidity = $"{humidity:F1} %";
                    Pressure = $"{pressure:F1} hPa";
                    LastUpdate = $"最后更新: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                    Message = "数据读取成功";
                    MessageForeground = Brushes.Green;
                }
                else
                {
                    Message = "读取数据失败";
                    MessageForeground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                Message = $"错误: {ex.Message}";
                MessageForeground = Brushes.Red;
            }
        }

        [RelayCommand]
        private void Clear()
        {
            Temperature = "-- °C";
            Humidity = "-- %";
            Pressure = "-- hPa";
            LastUpdate = "--";
            Message = "";
            MessageForeground = Brushes.Black;
        }
    }
}
