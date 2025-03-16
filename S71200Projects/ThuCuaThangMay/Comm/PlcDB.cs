using S7.Net;

namespace ThuCuaThangMay.Comm
{
    public abstract class PlcDB
    {
        public long T { get; set; }
        public bool IsParseData { get; protected set; }

        public abstract Task ReadAsync(Plc plc, long t0);

        public abstract Task WriteAsync(Plc plc, WriteCmd cmd);

        public float ParseFloat(byte[] buf, int offset)
        {
            byte[] data = new byte[4];
            if (BitConverter.IsLittleEndian)
            {
                data[0] = buf[offset + 3];
                data[1] = buf[offset + 2];
                data[2] = buf[offset + 1];
                data[3] = buf[offset];
            }
            else
            {
                data[0] = buf[offset];
                data[1] = buf[offset + 1];
                data[2] = buf[offset + 2];
                data[3] = buf[offset + 3];
            }
            return BitConverter.ToSingle(data, 0);
        }
    }
}
