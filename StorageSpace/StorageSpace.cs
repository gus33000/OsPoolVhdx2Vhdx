using StorageSpace.Data;
using StorageSpace.Data.Subtypes;

namespace StorageSpace
{
    public class StorageSpace
    {
        private readonly Stream Stream;
        private readonly long OriginalSeekPosition;

        public SPACEDB SPACEDB
        {
            get; private set;
        }

        public SDBC SDBC
        {
            get; private set;
        }

        public List<SDBB> SDBBs
        {
            get; private set;
        }

        public Dictionary<uint, byte[]> BlockEntries
        {
            get; private set;
        }

        public List<Block> Blocks
        {
            get; private set;
        }

        public List<StoragePool> SDBBStorageInformation
        {
            get; private set;
        }

        public List<PhysicalDisk> SDBBPhysicalDisks
        {
            get; private set;
        }

        public List<Volume> SDBBVolumes
        {
            get; private set;
        }

        public List<SlabAllocation> SDBBSlabAllocation
        {
            get; private set;
        }

        public StorageSpace(Stream Stream)
        {
            this.Stream = Stream;
            OriginalSeekPosition = Stream.Position;

            using BinaryReader reader = new(Stream);

            SPACEDB = SPACEDB.Parse(Stream);

            int SDBCOffset = 0x1000;

            Stream.Seek(OriginalSeekPosition + SDBCOffset, SeekOrigin.Begin);

            SDBC = SDBC.Parse(Stream);

            if (SDBC.StorageGUID != SPACEDB.StorageGUID)
            {
                throw new Exception("Invalid OSPool! SDBC is not for the given SpaceDB!");
            }

            Stream.Seek(OriginalSeekPosition + SDBCOffset + SDBC.SDBCLength, SeekOrigin.Begin);

            SDBBStorageInformation = [];
            SDBBPhysicalDisks = [];
            SDBBVolumes = [];
            SDBBSlabAllocation = [];

            BlockEntries = [];

            SDBBs = [];
            Blocks = [];

            for (uint j = 8; j < SDBC.SDBBLength; j++)
            {
                SDBB SDBB = SDBB.Parse(Stream);
                SDBBs.Add(SDBB);

                if (SDBB.ParentSDBBIndex == 0) // Empty Entry
                {
                    //throw new Exception("An entry exists which is empty, this is abnormal");
                    continue;
                }

                if (BlockEntries.TryGetValue(SDBB.ParentSDBBIndex, out byte[]? value))
                {
                    BlockEntries[SDBB.ParentSDBBIndex] = [.. value, .. SDBB.Data];
                }
                else
                {
                    BlockEntries[SDBB.ParentSDBBIndex] = SDBB.Data;
                }
            }

            for (uint j = 8; j < SDBC.SDBBLength; j++)
            {
                if (!BlockEntries.ContainsKey(j))
                {
                    continue;
                }

                byte[] BlockBuffer = BlockEntries[j];
                using MemoryStream BlockStream = new(BlockBuffer);

                Block Block = Block.Parse(BlockStream);
                Blocks.Add(Block);

                using MemoryStream dataStream = new(Block.Data);

                switch (Block.Type)
                {
                    case BlockType.StoragePool:
                        SDBBStorageInformation.Add(StoragePool.Parse(dataStream));
                        break;
                    case BlockType.PhysicalDisk:
                        SDBBPhysicalDisks.Add(PhysicalDisk.Parse(dataStream));
                        break;
                    case BlockType.Volume:
                        SDBBVolumes.Add(Volume.Parse(dataStream));
                        break;
                    case BlockType.SlabAllocation:
                        SDBBSlabAllocation.Add(SlabAllocation.Parse(dataStream));
                        break;
                    default:
                        throw new Exception($"Unknown Entry Type! {Block.Type}");
                }
            }

            Stream.Seek(OriginalSeekPosition, SeekOrigin.Begin);
        }

        public Dictionary<int, string> GetDisks()
        {
            Dictionary<int, string> disks = [];

            foreach (Volume SDBBVolume in SDBBVolumes)
            {
                disks.Add(SDBBVolume.VolumeNumber, SDBBVolume.VolumeName);
            }

            return disks;
        }

        public Space OpenDisk(int storeIndex)
        {
            return new Space(Stream, storeIndex, this, OriginalSeekPosition);
        }
    }
}