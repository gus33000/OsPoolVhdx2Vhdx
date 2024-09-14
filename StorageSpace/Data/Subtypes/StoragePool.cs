namespace StorageSpace.Data.Subtypes
{
    public class StoragePool
    {
        public Guid StorageGUID
        {
            get;
            private set;
        }

        public string StorageName
        {
            get;
            private set;
        }

        public byte ProvisioningTypeDefault
        {
            get;
            private set;
        }

        private StoragePool()
        {
        }

        public static StoragePool Parse(Stream stream)
        {
            using BinaryReader reader = new(stream);

            byte dataLength = reader.ReadByte();
            byte[] data = reader.ReadBytes(dataLength);

            dataLength = reader.ReadByte();
            byte[] CommandSerialNumber = reader.ReadBytes(dataLength);

            Guid StorageGUID = new(reader.ReadBytes(16));

            ushort StorageNameLength = reader.ReadUInt16();
            StorageNameLength = (ushort)((StorageNameLength & 0xFF00) >> 8 | (StorageNameLength & 0xFF) << 8);

            byte[] StorageNameBuffer = new byte[StorageNameLength * 2];
            for (int i = 0; i < StorageNameLength; i++)
            {
                byte low = reader.ReadByte();
                byte high = reader.ReadByte();

                StorageNameBuffer[i * 2] = high;
                StorageNameBuffer[(i * 2) + 1] = low;
            }

            string StorageName = System.Text.Encoding.Unicode.GetString(StorageNameBuffer).Replace("\0", "");

            stream.Seek(10, SeekOrigin.Current);

            byte DataValue = reader.ReadByte();

            byte ProvisioningTypeDefault = reader.ReadByte();

            // Note, this is also broken, review one day

            /*dataLength = reader.ReadByte();
            byte[] DataValue2 = reader.ReadBytes(dataLength);

            stream.Seek(4, SeekOrigin.Current);

            dataLength = reader.ReadByte();
            byte[] DataValue3 = reader.ReadBytes(dataLength);

            stream.Seek(2, SeekOrigin.Current);

            dataLength = reader.ReadByte();
            byte[] DataValue4 = reader.ReadBytes(dataLength);

            stream.Seek(6, SeekOrigin.Current);

            dataLength = reader.ReadByte();
            byte[] DataValue5 = reader.ReadBytes(dataLength);

            stream.Seek(8, SeekOrigin.Current);

            dataLength = reader.ReadByte();
            byte[] DataValue6 = reader.ReadBytes(dataLength);

            stream.Seek(6, SeekOrigin.Current);

            dataLength = reader.ReadByte();
            byte[] DataValue7 = reader.ReadBytes(dataLength);

            dataLength = reader.ReadByte();
            byte[] DataValue8 = reader.ReadBytes(dataLength);*/

            StoragePool storagePool = new()
            {
                StorageGUID = StorageGUID,
                StorageName = StorageName,
                ProvisioningTypeDefault = ProvisioningTypeDefault
            };

            return storagePool;
        }
    }
}