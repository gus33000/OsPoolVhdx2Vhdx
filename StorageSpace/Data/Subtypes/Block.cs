namespace StorageSpace.Data.Subtypes
{
    public class Block
    {
        public BlockType Type
        {
            get; private set;
        } // 0, 1

        // Unknown, 1, 3

        public uint DataLength
        {
            get; private set;
        } // 4, 4

        public byte[] Data
        {
            get; private set;
        } // 8, 8 + DataLength

        private Block()
        {
        }

        public static Block Parse(Stream stream)
        {
            using BinaryReader reader = new(stream);

            BlockType EntryType = (BlockType)reader.ReadByte();
            stream.Seek(3, SeekOrigin.Current);
            uint DataLength = BitConverter.ToUInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
            byte[] Data = reader.ReadBytes((int)DataLength);

            Block Block = new()
            {
                Type = EntryType,
                DataLength = DataLength,
                Data = Data
            };

            return Block;
        }
    }
}
