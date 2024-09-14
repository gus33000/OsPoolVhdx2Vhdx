using System.Text;

namespace StorageSpace.Data
{
    public class SPACEDB
    {
        public string Signature
        {
            get; private set;
        } // 0, 8

        public ushort StorageGUIDLocation
        {
            get; private set;
        } // 8, 2

        public ushort SPACEDBLength
        {
            get; private set;
        } // 10, 2

        public uint SPACEDBCRC32
        {
            get; private set;
        } // 12, 4

        // Unknown

        public Guid StorageGUID
        {
            get; private set;
        } // StorageGUIDLocation * 16, 16

        public Guid SPACEDBGUID
        {
            get; private set;
        } // StorageGUIDLocation * 16 + 16, 16

        // Unknown

        private SPACEDB()
        {
        }

        public static SPACEDB Parse(Stream stream)
        {
            long ogSeek = stream.Position;

            using BinaryReader reader = new(stream);

            byte[] SPACEDBSignature = reader.ReadBytes(8);
            string SPACEDBSignatureStr = Encoding.ASCII.GetString(SPACEDBSignature, 0, SPACEDBSignature.Length);

            if (SPACEDBSignatureStr != "SPACEDB ")
            {
                throw new Exception("Invalid OSPool!");
            }

            ushort SPACEDBStorageGUIDLocation = reader.ReadUInt16();
            SPACEDBStorageGUIDLocation = (ushort)((SPACEDBStorageGUIDLocation & 0xFF00) >> 8 | (SPACEDBStorageGUIDLocation & 0xFF) << 8);

            ushort SPACEDBLength = reader.ReadUInt16();
            SPACEDBLength = (ushort)((SPACEDBLength & 0xFF00) >> 8 | (SPACEDBLength & 0xFF) << 8);

            uint SPACEDBCRC32 = reader.ReadUInt32();

            stream.Seek(ogSeek + SPACEDBStorageGUIDLocation * 16, SeekOrigin.Begin);

            Guid StorageGUID = new(reader.ReadBytes(16));
            Guid SPACEDBGUID = new(reader.ReadBytes(16));

            SPACEDB SPACEDB = new()
            {
                Signature = SPACEDBSignatureStr,
                StorageGUIDLocation = SPACEDBStorageGUIDLocation,
                SPACEDBLength = SPACEDBLength,
                SPACEDBCRC32 = SPACEDBCRC32,
                StorageGUID = StorageGUID,
                SPACEDBGUID = SPACEDBGUID
            };

            return SPACEDB;
        }
    }
}
