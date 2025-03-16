using System;
using System.Net.NetworkInformation;
using S7.Net;

namespace ThuCuaThangMay.Comm
{
    public enum CommStates { Closed, Openning, Opened }

    public abstract class CommS71200
    {
        private Plc? _plc;
        private bool _newIPAddr = false;                    // Khi địa chỉ IP thay đổi
        private long _t0;                                   // Dùng tính ReadTime, RealCycleTime
        private long _read_t0;                              // Thời điểm bắt đầu chu kỳ đọc/ghi (ticks)
        private TaskCompletionSource<bool> taskCommCompleted = new();       // Dùng để báo đang kết nối

        public event EventHandler? StateChanged;
        private string? _ipAddr;
        /// <summary>
        /// Địa chỉ IP của PLC
        /// </summary>
        public string? IPAddress { get { return _ipAddr; } set { if (_ipAddr != value) { _ipAddr = value; _newIPAddr = true; } } }
        /// <summary>
        /// Trạng thái kết nối
        /// </summary>
        public CommStates State { get; private set; } = CommStates.Closed;
        
        /// <summary>
        /// Lưu các thông báo lỗi
        /// </summary>
        public string? Message { get; private set; }
        /// <summary>
        /// Chu kỳ đọc (ticks)
        /// </summary>
        public int CycleTime { get; set; } = 500000;
        /// <summary>
        /// Thời gian đợi đến khi bắt đầu kết nối (ms)<br/>
        /// Sử dụng: khi mất kết nối, ko muốn liên tục kết nối lại
        /// </summary>
        public double DelayConnect {  get; set; }

        /// <summary>
        /// Có thể đọc/ghi liên tục
        /// </summary>
        public bool CanComm {  get; set; }
        /// <summary>
        /// Đang liên tục đọc/ghi
        /// </summary>
        public bool IsCommunicating { get; private set; }
        /// <summary>
        /// Thời gian chờ phản hồi
        /// </summary>
        public int ReadTime {  get; private set; }
        /// <summary>
        /// Chu kỳ thực mỗi vòng lặp
        /// </summary>
        public int RealCycleTime { get; private set; }

        public CommS71200(string? addr = null) { IPAddress = addr; }

        /// <summary>
        /// Bắt đầu kết nối:<br/>
        /// - Kiểm tra kết nối hiện tại<br/>
        /// - Kiểm tra có thay đổi IP<br/>
        /// - Kiểm tra chờ để ko liên tục kết nối khi có lỗi<br/>
        /// Sử dụng: gọi trong vòng lặp timer
        /// </summary>
        /// <param name="delta">Chu kỳ vòng lặp (s)</param>
        public void Connect(double delta)
        {
            if (State != CommStates.Closed)
            {
                // Mất kết nối
                if (_plc != null) {
                    if (!_plc.IsConnected)
                    {
                        State = CommStates.Closed;
                        StateChanged?.Invoke(this, new EventArgs());
                        // Nghỉ ít nhất 10s mới có thể kết nối lại
                        DelayConnect = 10000;
                        return;
                    }
                }

                // Địa chỉ IP không đổi
                if (!_newIPAddr) return;
            }

            if (DelayConnect > 0)
            {
                DelayConnect -= delta;
                return;
            }

            if (_newIPAddr) Disconnect();

            ConnectAsync();
        }

        private async void ConnectAsync()
        {
            System.Diagnostics.Debug.WriteLine($"Ket noi den {IPAddress}");

            if (string.IsNullOrEmpty(IPAddress))
            {
                State = CommStates.Closed;
                StateChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            _newIPAddr = false;
            _plc = new(CpuType.S71200, IPAddress, 0, 1);

            State = CommStates.Openning;
            StateChanged?.Invoke(this, EventArgs.Empty);
            
            try
            {
                await _plc.OpenAsync();
            }
            catch (Exception ex) { 
                Message = $"Error: {ex.Message}";
                // Nghỉ ít nhất 10s mới có thể kết nối lại
                DelayConnect = 10000;
            }

            if (_plc.IsConnected)
            {
                State = CommStates.Opened;
            }
            else
            {
                State = CommStates.Closed;
            }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Dừng kết nối
        /// </summary>
        public async void Disconnect() { 
            if (_plc != null && _plc.IsConnected)
            {
                CanComm = false;
                await taskCommCompleted.Task;
                _plc.Close();
                State = CommStates.Closed;
                StateChanged?.Invoke(this, EventArgs.Empty);
                DelayConnect = 0;
            }
        }

        /// <summary>
        /// Bắt đầu chạy chu kỳ đọc/ghi liên tục
        /// </summary>
        public async void StartComm()
        {
            if (IsCommunicating) return;

            taskCommCompleted = new();
            CanComm = true;
            IsCommunicating = true;
            StateChanged?.Invoke(this, EventArgs.Empty);

            _read_t0 = DateTime.Now.Ticks;
            while (CanComm && _plc != null) {
                _t0 = DateTime.Now.Ticks;

                try
                {
                    await Comm(_plc, _read_t0);
                }
                catch (Exception ex)
                {
                    CanComm = false;
                    Message = $"Error: {ex.Message}";
                }
                
                ReadTime = (int)(DateTime.Now.Ticks - _t0);

                // Tạo trễ để có chu kỳ đọc 50 ms
                if (ReadTime < CycleTime) {
                    int delay = (CycleTime - ReadTime) / 10000;
                    await Task.Delay(delay);
                }

                RealCycleTime = (int)(DateTime.Now.Ticks - _t0);
            }
            
            IsCommunicating = false;
            StateChanged?.Invoke(this, EventArgs.Empty);

            taskCommCompleted.SetResult(true);
        }

        protected abstract Task Comm(Plc plc, long t0);
    }
}
