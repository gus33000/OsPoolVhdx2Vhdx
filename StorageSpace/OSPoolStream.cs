using StorageSpace.Data.Subtypes;

namespace StorageSpace
{
    public class Space : Stream
    {
        private readonly Stream Stream;
        private readonly long OriginalSeekPosition;
        private readonly long length;
        private readonly long blockSize = 0x100000;
        private readonly Dictionary<long, int> blockTable;
        private readonly List<SlabAllocation> slabAllocations = [];
        private readonly int TotalBlocks;

        private long currentPosition = 0;

        internal Space(Stream Stream, int storeIndex, StorageSpace storageSpace, long OriginalSeekPosition)
        {
            this.OriginalSeekPosition = OriginalSeekPosition;
            this.Stream = Stream;

            foreach (Volume volume in storageSpace.SDBBVolumes)
            {
                if (volume.VolumeNumber != storeIndex)
                {
                    continue;
                }

                TotalBlocks = volume.VolumeBlockNumber;
            }

            foreach (SlabAllocation slabAllocation in storageSpace.SDBBSlabAllocation)
            {
                if (slabAllocation.VolumeID != storeIndex)
                {
                    continue;
                }

                slabAllocations.Add(slabAllocation);
            }

            (length, blockTable) = BuildBlockTable();
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => length;

        private (long, Dictionary<long, int>) BuildBlockTable()
        {
            Dictionary<long, int> blockTable = [];

            long blockSize = 0x10000000;

            int maxVirtualDiskBlockNumber = 0;

            foreach (SlabAllocation slabAllocation in slabAllocations)
            {
                int virtualDiskBlockNumber = slabAllocation.VolumeBlockNumber;
                int physicalDiskBlockNumber = slabAllocation.PhysicalDiskBlockNumber;

                if (virtualDiskBlockNumber > maxVirtualDiskBlockNumber)
                {
                    maxVirtualDiskBlockNumber = virtualDiskBlockNumber;
                }

                blockTable.Add(virtualDiskBlockNumber, physicalDiskBlockNumber);
            }

            long totalBlocks = Math.Max(TotalBlocks, maxVirtualDiskBlockNumber);

            return (totalBlocks * blockSize, blockTable);
        }

        private int GetBlockDataIndex(long realBlockOffset)
        {
            if (blockTable.TryGetValue(realBlockOffset, out int value))
            {
                return value;
            }

            return -1; // Invalid
        }

        public override long Position
        {
            get => currentPosition;
            set
            {
                if (currentPosition < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                // Workaround for malformed MBRs
                /*if (currentPosition > Length)
                {
                    throw new EndOfStreamException();
                }*/

                currentPosition = value;
            }
        }

        public override void Flush()
        {
            // Nothing to do here
        }

        private byte[] ImageGetStoreDataBlock(int physicalDiskBlockNumber)
        {
            byte[] buffer = new byte[blockSize];

            long physicalDiskLocation = physicalDiskBlockNumber * blockSize + 0x2000 + 0x4000000;

            Stream.Seek(OriginalSeekPosition + physicalDiskLocation, SeekOrigin.Begin);
            Stream.Read(buffer, 0, buffer.Length);

            return buffer;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // Workaround for malformed MBRs
            if (Position >= Length)
            {
                return count;
            }

            long readBytes = count;

            if (Position + readBytes > Length)
            {
                readBytes = (int)(Length - Position);
            }

            byte[] readBuffer = new byte[readBytes];
            Array.Fill<byte>(readBuffer, 0);

            // Read the buffer from the FFU file.
            // First we have to figure out where do we land here.

            long overflowBlockStartByteCount = Position % blockSize;
            long overflowBlockEndByteCount = (Position + readBytes) % blockSize;

            long startBlockIndex = (Position - overflowBlockStartByteCount) / blockSize;
            long endBlockIndex = (Position + readBytes + (blockSize - overflowBlockEndByteCount)) / blockSize;

            byte[] allReadBlocks = new byte[(endBlockIndex - startBlockIndex + 1) * blockSize];

            for (long currentBlock = startBlockIndex; currentBlock < endBlockIndex; currentBlock++)
            {
                int virtualBlockIndex = GetBlockDataIndex(currentBlock);
                if (virtualBlockIndex != -1)
                {
                    byte[] block = ImageGetStoreDataBlock(virtualBlockIndex);
                    Array.Copy(block, 0, allReadBlocks, (int)((currentBlock - startBlockIndex) * blockSize), blockSize);
                }
            }

            Array.Copy(allReadBlocks, overflowBlockStartByteCount, buffer, offset, readBytes);

            Position += readBytes;

            if (Position == Length)
            {
                // Workaround for malformed MBRs
                //return 0;
            }

            return (int)readBytes;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        Position = offset;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        Position += offset;
                        break;
                    }
                case SeekOrigin.End:
                    {
                        Position = Length + offset;
                        break;
                    }
                default:
                    {
                        throw new ArgumentException(nameof(origin));
                    }
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Stream.Dispose();
        }
    }
}
