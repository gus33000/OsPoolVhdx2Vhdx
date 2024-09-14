using System.Text;

namespace StorageSpace.Data
{
    public class SDBC
    {
        public string Signature
        {
            get; private set;
        } // 0, 8

        public ushort StorageGUIDLocation
        {
            get; private set;
        } // 8, 2

        public ushort SDBCLength
        {
            get; private set;
        } // 10, 2

        public uint SDBCCRC32
        {
            get; private set;
        } // 12, 4

        // Unknown

        public Guid StorageGUID
        {
            get; private set;
        } // StorageGUIDLocation * 16, 16

        // Unknown

        public uint SDBBStartPosition
        {
            get; private set;
        } // StorageGUIDLocation * 16 + 20

        public uint SDBBLength
        {
            get; private set;
        } // StorageGUIDLocation * 16 + 24

        // Unknown

        public ulong CommandSerialNumber0
        {
            get; private set;
        } // StorageGUIDLocation * 16 + 40

        public ulong CommandSerialNumber1
        {
            get; private set;
        } // StorageGUIDLocation * 16 + 48

        public DateTime SDBBModifiedTime
        {
            get; private set;
        } // StorageGUIDLocation * 16 + 56

        public uint SDBBCRC32
        {
            get; private set;
        } // StorageGUIDLocation * 16 + 60

        private SDBC()
        {
        }

        public static SDBC Parse(Stream stream)
        {
            long ogSeek = stream.Position;

            using BinaryReader reader = new(stream);

            byte[] SDBCSignature = reader.ReadBytes(8);
            string SDBCSignatureStr = Encoding.ASCII.GetString(SDBCSignature, 0, SDBCSignature.Length);

            if (SDBCSignatureStr != "SDBC    ")
            {
                throw new Exception("Invalid SDBC!");
            }

            ushort SDBCStorageGUIDLocation = reader.ReadUInt16();
            SDBCStorageGUIDLocation = (ushort)((SDBCStorageGUIDLocation & 0xFF00) >> 8 | (SDBCStorageGUIDLocation & 0xFF) << 8);

            ushort SDBCLength = reader.ReadUInt16();
            SDBCLength = (ushort)((SDBCLength & 0xFF00) >> 8 | (SDBCLength & 0xFF) << 8);

            uint SDBCCRC32 = reader.ReadUInt32();

            stream.Seek(ogSeek + SDBCStorageGUIDLocation * 16, SeekOrigin.Begin);

            Guid SDBCStorageGUID = new(reader.ReadBytes(16));

            stream.Seek(ogSeek + SDBCStorageGUIDLocation * 16 + 20, SeekOrigin.Begin);

            uint SDBBStartPosition = BitConverter.ToUInt32(reader.ReadBytes(4).Reverse().ToArray());
            uint SDBBLength = BitConverter.ToUInt32(reader.ReadBytes(4).Reverse().ToArray());

            stream.Seek(ogSeek + SDBCStorageGUIDLocation * 16 + 40, SeekOrigin.Begin);

            ulong CommandSerialNumber0 = reader.ReadUInt64();
            CommandSerialNumber0 = SwapBytes(CommandSerialNumber0);
            ulong CommandSerialNumber1 = reader.ReadUInt64();
            CommandSerialNumber1 = SwapBytes(CommandSerialNumber1);

            ulong LastModifiedTime = reader.ReadUInt64();
            LastModifiedTime = SwapBytes(LastModifiedTime);

            DateTime SDBBModifiedTime = DateTime.FromFileTime((long)LastModifiedTime);

            stream.Seek(ogSeek + SDBCStorageGUIDLocation * 16 + 60, SeekOrigin.Begin);

            uint SDBBCRC32 = reader.ReadUInt32();

            SDBC SDBC = new()
            {
                Signature = SDBCSignatureStr,
                StorageGUIDLocation = SDBCStorageGUIDLocation,
                SDBCLength = SDBCLength,
                SDBCCRC32 = SDBCCRC32,
                StorageGUID = SDBCStorageGUID,
                SDBBStartPosition = SDBBStartPosition,
                SDBBLength = SDBBLength,
                CommandSerialNumber0 = CommandSerialNumber0,
                CommandSerialNumber1 = CommandSerialNumber1,
                SDBBModifiedTime = SDBBModifiedTime,
                SDBBCRC32 = SDBBCRC32
            };

            return SDBC;
        }

        private static ulong SwapBytes(ulong x)
        {
            // swap adjacent 32-bit blocks
            x = x >> 32 | x << 32;
            // swap adjacent 16-bit blocks
            x = (x & 0xFFFF0000FFFF0000) >> 16 | (x & 0x0000FFFF0000FFFF) << 16;
            // swap adjacent 8-bit blocks
            return (x & 0xFF00FF00FF00FF00) >> 8 | (x & 0x00FF00FF00FF00FF) << 8;
        }
    }
}
