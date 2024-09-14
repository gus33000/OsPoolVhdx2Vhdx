namespace StorageSpace.Data.Subtypes
{
    public enum BlockType : byte
    {
        Unknown,
        StoragePool = 1,
        PhysicalDisk = 2,
        Volume = 3,
        SlabAllocation = 4
    }
}