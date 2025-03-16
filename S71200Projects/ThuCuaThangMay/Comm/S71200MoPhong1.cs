using S7.Net;

namespace ThuCuaThangMay.Comm
{
    public class S71200MoPhong1: CommS71200
    {
        private DBSim1Full _dbFull = new();
        private DBSim1 _db = new();

        public IDBSim1 Db { get; private set; }

        private Queue<WriteCmd> _cmds = new();

        public S71200MoPhong1(string? addr): base(addr) {
            Db = _db;
        }

        protected override async Task Comm(Plc plc, long t0)
        {
            await Db.ReadAsync(plc, t0);

            if (plc.IsConnected)
            {
                // Đọc thành công -> ghi lệnh
                if (_cmds.Count > 0)
                {
                    WriteCmd cmd = _cmds.Dequeue();
                    await Db.WriteAsync(plc, cmd);
                }
            }
        }

        public void SetStart(bool v)
        {
            var cmd = new WriteCmd()
            {
                ByteAddr = 0,
                BitAddr = 0,
                Data = v
            };
            _cmds.Enqueue(cmd);
        }
    }

    public abstract class IDBSim1: PlcDB
    {
        public bool IsStart { get; set; }           // DB6.DBX0.0
        public bool IsStop { get; set; }            // DB6.DBX0.1
        public bool IsForward { get; set; }         // DB6.DBX0.2
        public bool IsRunning { get; set; }         // DB6.DBX1.0

        public float Spd { get; protected set; }                   // DB6.DBD2
        public float Distance { get; protected set; }              // DB6.DBD6
    }

    public class DBSim1Full : IDBSim1
    {
        private byte[] _buf = new byte[10];

        public override async Task ReadAsync(Plc plc, long t0 = 0)
        {
            await plc.ReadBytesAsync(_buf, DataType.DataBlock, 6, 0);

            IsParseData = true;

            IsStart = (_buf[0] & 1) == 1;
            IsRunning = (_buf[0] & 4) == 4;
            IsForward = (_buf[0] & 8) != 8;
            Spd = ParseFloat(_buf, 2);
            Distance = ParseFloat(_buf, 6);

            T = DateTime.Now.Ticks - t0;
            IsParseData = false;
        }

        public override async Task WriteAsync(Plc plc, WriteCmd cmd)
        {
            await cmd.WriteAsync(plc, 6, 10);
        }
    }

    public class DBSim1 : IDBSim1
    {
        private byte[] _buf = new byte[2];

        public override async Task ReadAsync(Plc plc, long t0 = 0)
        {
            await plc.ReadBytesAsync(_buf, DataType.DataBlock, 6, 0);

            IsParseData = true;

            IsStart = (_buf[0] & 1) == 1;
            IsRunning = (_buf[0] & 4) == 4;
            IsForward = (_buf[0] & 8) != 8;

            T = DateTime.Now.Ticks - t0;
            IsParseData = false;
        }

        public override async Task WriteAsync(Plc plc, WriteCmd cmd)
        {
            await cmd.WriteAsync(plc, 6, 2);
        }
    }
}
