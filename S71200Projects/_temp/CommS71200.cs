using System;
using System.Net.NetworkInformation;
using System.Threading.Channels;
using S7.Net;

namespace ThuCuaThangMay.Comm
{
    public class CommS71200
    {
        public DBComm Db1 { get; set; } = new();
        public string? IPAddress { get; set; }

        public string? Message { get; set; }
        public bool IsConnected { get { return _plc != null ? _plc.IsConnected : false; } }

        public bool CanComm { get; set; }
        public bool IsComm { get; private set; }
        public bool CanRead { get; set; }
        public bool IsRead { get; private set; }
        public bool IsParseData { get; private set; }

        public int ReadTime {  get; set; }
        public int CycleTime { get; set; }

        private CancellationTokenSource _cancelWaitReconnect = new();

        private Plc? _plc;
        private long _t0;
        private long _read_t0;

        public CommS71200(string? addr = null) { IPAddress = addr; }

        public async void Communicate()
        {
            if (IsComm) return;
            
            CanComm = true;
            IsComm = true;
            while (CanComm)
            {
                await Connect();

                if (_plc != null && _plc.IsConnected)
                {
                    // Nếu connect thành công
                    while (CanComm && _plc.IsConnected)
                    {
                        if (CanRead)
                        {
                            IsRead = true;
                            while (CanRead)
                            {
                                _t0 = DateTime.Now.Ticks;

                                await Db1.ReadAsync(_plc);
                                ReadTime = (int)(DateTime.Now.Ticks - _t0);

                                int dt = (500000 - ReadTime) / 10000;              // Delay = 50ms = 50 * 10000 ticks
                                if (dt > 0) await Task.Delay(dt);

                                CycleTime = (int)(DateTime.Now.Ticks - _t0);
                            }
                            IsRead = false;
                        }
                        else
                        {
                            await Task.Delay(100);
                        }
                    }
                }
                else
                {
                    // Nếu ko connect được, đợi 30 thì connect lại
                    _cancelWaitReconnect.TryReset();
                    await Task.Delay(30000, _cancelWaitReconnect.Token);
                }
            }
            Disconnect();
            IsComm = false;
        }

        public void StartComm(string? addr = null)
        {
            if (addr != null) IPAddress = addr;

            if (IsComm)
            {
                _cancelWaitReconnect.Cancel();
            }
            else
            {
                Communicate();
            }
        }

        public void StopComm()
        {
            CanComm = false;
            CanRead = false;
        }

        private async Task Connect()
        {
            if (!string.IsNullOrEmpty(IPAddress))
            {
                _plc = new(CpuType.S71200, IPAddress, 0, 1);

                try
                {
                    // Open connection to PLC
                    await _plc.OpenAsync();
                    if (_plc.IsConnected)
                    {
                        Message = "Connected to PLC!";
                    }
                    else
                    {
                        Message = "Failed to connect to PLC.";
                    }
                }
                catch (Exception ex)
                {
                    Message = $"Error: {ex.Message}";
                }
            }
        }

        private void Disconnect() {
            if (_plc != null)
            {
                if (_plc.IsConnected)
                {
                    try
                    {
                        _plc.Close();
                    }
                    catch (Exception ex)
                    {
                        Message = $"Error: {ex.Message}";
                    }
                }
            }
        }

    }

    public class DBComm
    {
        private byte[] _buf = new byte[4];

        public bool IsStart { get; set; }           // DB1.DBX0.0
        public bool IsRunning { get; set; }         // DB1.DBX0.2
        public bool IsWeigh { get; set; }           // DB1.DBX0.3
        public int WeightDAC { get; set; }          // DB1.DBW2

        public bool IsParseData { get; private set; }

        public async Task ReadAsync(Plc plc)
        {
            await plc.ReadBytesAsync(_buf, DataType.DataBlock, 1, 0);

            IsParseData = true;
            IsStart = (_buf[0] & 1) == 1;
            IsRunning = (_buf[0] & 4) == 4;
            IsWeigh = (_buf[0] & 8) == 8;
            WeightDAC = _buf[2] << 8 | _buf[3];
            IsParseData = false;
        }
    } 
}
