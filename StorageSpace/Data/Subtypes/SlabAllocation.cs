namespace StorageSpace.Data.Subtypes
{
    public class SlabAllocation
    {
        public int VolumeID
        {
            get;
            private set;
        }

        public int VolumeBlockNumber
        {
            get;
            private set;
        }

        public int ParitySequenceNumber
        {
            get;
            private set;
        }

        public int MirrorSequenceNumber
        {
            get;
            private set;
        }

        public int PhysicalDiskID
        {
            get;
            private set;
        }

        public int PhysicalDiskBlockNumber
        {
            get;
            private set;
        }

        private static int BigEndianToInt(byte[] buf)
        {
            int val = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                val *= 0x100;
                val += buf[i];
            }
            return val;
        }
        private SlabAllocation()
        {
        }

        public static SlabAllocation Parse(Stream stream)
        {
            using BinaryReader reader = new(stream);

            byte dataLength = reader.ReadByte();
            byte[] DataValue1 = reader.ReadBytes(dataLength);

            dataLength = reader.ReadByte();
            byte[] DataValue2 = reader.ReadBytes(dataLength);

            dataLength = reader.ReadByte();
            byte[] DataValue3 = reader.ReadBytes(dataLength);

            dataLength = reader.ReadByte();
            byte[] DataValue4 = reader.ReadBytes(dataLength);

            dataLength = reader.ReadByte();
            byte[] DataValue5 = reader.ReadBytes(dataLength);

            dataLength = reader.ReadByte();
            int VolumeID = BigEndianToInt(reader.ReadBytes(dataLength));

            dataLength = reader.ReadByte();
            int VolumeBlockNumber = BigEndianToInt(reader.ReadBytes(dataLength));

            dataLength = reader.ReadByte();
            int ParitySequenceNumber = BigEndianToInt(reader.ReadBytes(dataLength));

            dataLength = reader.ReadByte();
            int MirrorSequenceNumber = BigEndianToInt(reader.ReadBytes(dataLength));

            dataLength = reader.ReadByte();
            byte[] DataValue6 = reader.ReadBytes(dataLength);

            dataLength = reader.ReadByte();
            int PhysicalDiskID = BigEndianToInt(reader.ReadBytes(dataLength));

            dataLength = reader.ReadByte();
            int PhysicalDiskBlockNumber = BigEndianToInt(reader.ReadBytes(dataLength));

            SlabAllocation slabAllocation = new()
            {
                VolumeID = VolumeID,
                VolumeBlockNumber = VolumeBlockNumber,
                ParitySequenceNumber = ParitySequenceNumber,
                MirrorSequenceNumber = MirrorSequenceNumber,
                PhysicalDiskID = PhysicalDiskID,
                PhysicalDiskBlockNumber = PhysicalDiskBlockNumber
            };

            return slabAllocation;
        }
    }
}
