using System.Net.Sockets;

namespace WpfAppDemo
{
    public class ModbusTcpClient
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private readonly string _host;
        private readonly int _port;
        private ushort _transactionId = 0;

        public ModbusTcpClient(string host = "127.0.0.1", int port = 502)
        {
            _host = host;
            _port = port;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(_host, _port);
                _stream = _tcpClient.GetStream();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Disconnect()
        {
            _stream?.Close();
            _tcpClient?.Close();
        }

        public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort quantity)
        {
            try
            {
                byte[] request = BuildReadHoldingRegistersRequest(slaveId, startAddress, quantity);
                await _stream.WriteAsync(request, 0, request.Length);

                byte[] response = new byte[1024];
                int bytesRead = await _stream.ReadAsync(response, 0, response.Length);

                return ParseReadHoldingRegistersResponse(response, bytesRead);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> WriteSingleRegisterAsync(byte slaveId, ushort registerAddress, ushort value)
        {
            try
            {
                byte[] request = BuildWriteSingleRegisterRequest(slaveId, registerAddress, value);
                await _stream.WriteAsync(request, 0, request.Length);

                byte[] response = new byte[1024];
                int bytesRead = await _stream.ReadAsync(response, 0, response.Length);

                return ValidateWriteResponse(response, bytesRead);
            }
            catch
            {
                return false;
            }
        }

        private byte[] BuildReadHoldingRegistersRequest(byte slaveId, ushort startAddress, ushort quantity)
        {
            _transactionId++;
            byte[] request = new byte[12];

            // MBAP Header
            request[0] = (byte)(_transactionId >> 8);       // Transaction ID High
            request[1] = (byte)(_transactionId & 0xFF);     // Transaction ID Low
            request[2] = 0;                                  // Protocol ID High
            request[3] = 0;                                  // Protocol ID Low
            request[4] = 0;                                  // Length High
            request[5] = 6;                                  // Length Low
            request[6] = slaveId;                            // Slave ID

            // Function Code
            request[7] = 3;                                  // Read Holding Registers

            // Starting Address
            request[8] = (byte)(startAddress >> 8);
            request[9] = (byte)(startAddress & 0xFF);

            // Quantity
            request[10] = (byte)(quantity >> 8);
            request[11] = (byte)(quantity & 0xFF);

            return request;
        }

        private byte[] BuildWriteSingleRegisterRequest(byte slaveId, ushort registerAddress, ushort value)
        {
            _transactionId++;
            byte[] request = new byte[12];

            // MBAP Header
            request[0] = (byte)(_transactionId >> 8);
            request[1] = (byte)(_transactionId & 0xFF);
            request[2] = 0;
            request[3] = 0;
            request[4] = 0;
            request[5] = 6;
            request[6] = slaveId;

            // Function Code
            request[7] = 16;                                 // Write Single Register

            // Register Address
            request[8] = (byte)(registerAddress >> 8);
            request[9] = (byte)(registerAddress & 0xFF);

            // Value
            request[10] = (byte)(value >> 8);
            request[11] = (byte)(value & 0xFF);

            return request;
        }

        private ushort[] ParseReadHoldingRegistersResponse(byte[] response, int length)
        {
            if (length < 9)
                return null;

            byte byteCount = response[8];
            int registerCount = byteCount / 2;
            ushort[] registers = new ushort[registerCount];

            for (int i = 0; i < registerCount; i++)
            {
                registers[i] = (ushort)((response[9 + i * 2] << 8) | response[10 + i * 2]);
            }

            return registers;
        }

        private bool ValidateWriteResponse(byte[] response, int length)
        {
            if (length < 8)
                return false;

            // Check if response is valid (no exception)
            return response[7] == 16;
        }
    }
}
