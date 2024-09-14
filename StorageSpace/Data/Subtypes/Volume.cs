using System.Text;

namespace StorageSpace.Data.Subtypes
{
    public class Volume
    {
        public int VolumeNumber
        {
            get;
            private set;
        }

        public int CommandSerialNumber
        {
            get;
            private set;
        }

        public Guid VolumeGUID
        {
            get;
            private set;
        }

        public string VolumeName
        {
            get;
            private set;
        }

        public string VolumeDescription
        {
            get;
            private set;
        }

        public int VolumeBlockNumber
        {
            get;
            private set;
        }

        public byte ProvisioningType
        {
            get;
            private set;
        }

        public byte ResiliencySettingName
        {
            get;
            private set;
        }

        public byte NumberOfCopies
        {
            get;
            private set;
        }

        public byte NumberOfClusters
        {
            get;
            private set;
        }

        private Volume()
        {
        }

        public static Volume Parse(Stream stream)
        {
            using BinaryReader reader = new(stream);

            byte dataLength = reader.ReadByte();
            byte[] VolumeNumber = reader.ReadBytes(dataLength);

            dataLength = reader.ReadByte();
            byte[] CommandSerialNumber = reader.ReadBytes(dataLength);

            Guid VolumeGUID = new(reader.ReadBytes(16));

            ushort VolumeNameLength = reader.ReadUInt16();
            VolumeNameLength = (ushort)((VolumeNameLength & 0xFF00) >> 8 | (VolumeNameLength & 0xFF) << 8);

            byte[] VolumeNameBuffer = new byte[VolumeNameLength * 2];
            for (int i = 0; i < VolumeNameLength; i++)
            {
                byte low = reader.ReadByte();
                byte high = reader.ReadByte();

                VolumeNameBuffer[i * 2] = high;
                VolumeNameBuffer[(i * 2) + 1] = low;
            }

            string VolumeName = Encoding.Unicode.GetString(VolumeNameBuffer).Replace("\0", "");

            ushort VolumeDescriptionLength = reader.ReadUInt16();
            VolumeDescriptionLength = (ushort)((VolumeDescriptionLength & 0xFF00) >> 8 | (VolumeDescriptionLength & 0xFF) << 8);

            byte[] VolumeDescriptionBuffer = new byte[VolumeDescriptionLength * 2];
            for (int i = 0; i < VolumeDescriptionLength; i++)
            {
                byte low = reader.ReadByte();
                byte high = reader.ReadByte();

                VolumeDescriptionBuffer[i * 2] = high;
                VolumeDescriptionBuffer[(i * 2) + 1] = low;
            }

            string VolumeDescription = Encoding.Unicode.GetString(VolumeDescriptionBuffer).Replace("\0", "");

            stream.Seek(3, SeekOrigin.Current);

            dataLength = reader.ReadByte();
            byte[] VolumeBlockNumber = reader.ReadBytes(dataLength);

            int ParsedVolumeBlockNumber = 0;
            dataLength -= 3;
            ParsedVolumeBlockNumber = BigEndianToInt(VolumeBlockNumber.Take(dataLength).ToArray()) / 0x10;

            if (ParsedVolumeBlockNumber == 0)
            {
                dataLength += 3;
                ParsedVolumeBlockNumber = BigEndianToInt(VolumeBlockNumber.Take(dataLength).ToArray());
            }

            dataLength = reader.ReadByte();
            byte[] DataValue2 = reader.ReadBytes(dataLength);

            byte ProvisioningType = reader.ReadByte();

            stream.Seek(9, SeekOrigin.Current);

            byte ResiliencySettingName = reader.ReadByte();

            dataLength = reader.ReadByte();
            byte[] DataValue3 = reader.ReadBytes(dataLength);

            stream.Seek(1, SeekOrigin.Current);

            byte NumberOfCopies = reader.ReadByte();

            stream.Seek(3, SeekOrigin.Current);

            byte NumberOfClusters = reader.ReadByte();

            // Unknown

            Volume volume = new()
            {
                VolumeNumber = BigEndianToInt(VolumeNumber),
                CommandSerialNumber = BigEndianToInt(CommandSerialNumber),
                VolumeGUID = VolumeGUID,
                VolumeName = VolumeName,
                VolumeDescription = VolumeDescription,
                VolumeBlockNumber = ParsedVolumeBlockNumber,
                ProvisioningType = ProvisioningType,
                ResiliencySettingName = ResiliencySettingName,
                NumberOfCopies = NumberOfCopies,
                NumberOfClusters = NumberOfClusters
            };

            return volume;
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
    }
}
