using S7.Net;

namespace ThuCuaThangMay.Comm
{
    public enum WriteCmdTypes { Bit, SInt, Int, DInt, Real, LongReal }

    public class WriteCmd
    {
        public int ByteAddr {  get; set; }
        public int BitAddr { get; set; }
        public WriteCmdTypes Type { get; set; }
        public object? Data { get; set; }

        public async Task<bool> WriteAsync(Plc plc, int db, int maxAddr)
        {
            if (Data == null) return false;

            byte[]? buf = null;
            if (Type == WriteCmdTypes.Bit)
            {
                if (ByteAddr < maxAddr)
                    await plc.WriteBitAsync(DataType.DataBlock, db, ByteAddr, BitAddr, (bool)Data);
            }
            else
            {
                switch (Type)
                {
                    case WriteCmdTypes.SInt:
                        buf = [(byte)Data];
                        break;
                    case WriteCmdTypes.Int:
                        buf = BitConverter.GetBytes((short)Data);
                        break;
                    case WriteCmdTypes.DInt:
                        buf = BitConverter.GetBytes((int)Data);
                        break;
                    case WriteCmdTypes.Real:
                        buf = BitConverter.GetBytes((float)Data);
                        break;
                    case WriteCmdTypes.LongReal:
                        buf = BitConverter.GetBytes((double)Data);
                        break;
                }
                if (buf != null)
                {
                    if (ByteAddr + buf.Length <= maxAddr)
                    {
                        if (BitConverter.IsLittleEndian) { buf = buf.Reverse().ToArray(); }
                        await WriteBytesAsync(plc, db, buf);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private async Task WriteBytesAsync(Plc plc, int db, byte[] buf)
        {
            await plc.WriteBytesAsync(DataType.DataBlock, db, ByteAddr, buf);
        }

        public void SetWriteBit(int byteAddr, int bitAddr, bool v)
        {
            ByteAddr = byteAddr;
            BitAddr = bitAddr;
            Data = v;
        }
    }
}
