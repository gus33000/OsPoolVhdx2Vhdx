using DiscUtils;
using DiscUtils.Partitions;
using DiscUtils.Streams;
using StorageSpace;

namespace OsPoolVhdx2Vhdx
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Logging.Log("\nVHDX Containing a Storage Space OS Pool To Individual Storage Space VHDx(s) tool\nVersion: 1.0.0.0\n");

            if (args.Length != 2)
            {
                Logging.Log("Usage: OsPoolVhdx2Vhdx <Path to VHD(X) File with Storage Pool> <Output director for SPACEDisk.vhdx files>");
                return;
            }

            string VhdxPath = args[0];
            string OutputDirectory = args[1];

            if (!File.Exists(VhdxPath))
            {
                Logging.Log($"VHD(X) file does not exist: {VhdxPath}");
                return;
            }

            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }

            DumpSpaces(VhdxPath, OutputDirectory);
        }

        public static void DumpSpaces(string vhdx, string outputDirectory)
        {
            VirtualDisk virtualDisk;
            if (vhdx.EndsWith(".vhd", StringComparison.InvariantCultureIgnoreCase))
            {
                virtualDisk = new DiscUtils.Vhd.Disk(vhdx, FileAccess.Read);
            }
            else
            {
                virtualDisk = new DiscUtils.Vhdx.Disk(vhdx, FileAccess.Read);
            }

            PartitionTable partitionTable = virtualDisk.Partitions;

            if (partitionTable != null)
            {
                foreach (PartitionInfo partitionInfo in partitionTable.Partitions)
                {
                    if (partitionInfo.GuidType == new Guid("E75CAF8F-F680-4CEE-AFA3-B001E56EFC2D"))
                    {
                        Logging.Log($"{((GuidPartitionInfo)partitionInfo).Name} {((GuidPartitionInfo)partitionInfo).Identity} {((GuidPartitionInfo)partitionInfo).GuidType} {((GuidPartitionInfo)partitionInfo).SectorCount * virtualDisk.SectorSize} StoragePool");

                        Stream storageSpacePartitionStream = partitionInfo.Open();

                        Pool storageSpace = new(storageSpacePartitionStream);

                        Dictionary<long, string> disks = storageSpace.GetDisks();

                        foreach (KeyValuePair<long, string> disk in disks.OrderBy(x => x.Key).Skip(1))
                        {
                            using Space space = storageSpace.OpenDisk(disk.Key);

                            Logging.Log($"- {disk.Key}: {disk.Value} ({space.Length}B / {space.Length / 1024 / 1024}MB / {space.Length / 1024 / 1024 / 1024}GB) StorageSpace");
                        }

                        foreach (KeyValuePair<long, string> disk in disks.OrderBy(x => x.Key).Skip(1))
                        {
                            using Space space = storageSpace.OpenDisk(disk.Key);

                            // Default is 4096
                            int sectorSize = 4096;

                            if (space.Length > 4096 * 2)
                            {
                                BinaryReader reader = new(space);

                                space.Seek(512, SeekOrigin.Begin);
                                byte[] header1 = reader.ReadBytes(8);

                                space.Seek(4096, SeekOrigin.Begin);
                                byte[] header2 = reader.ReadBytes(8);

                                string header1str = System.Text.Encoding.ASCII.GetString(header1);
                                string header2str = System.Text.Encoding.ASCII.GetString(header2);

                                if (header1str == "EFI PART")
                                {
                                    sectorSize = 512;
                                }
                                else if (header2str == "EFI PART")
                                {
                                    sectorSize = 4096;
                                }
                                else if (space.Length % 512 == 0 && space.Length % 4096 != 0)
                                {
                                    sectorSize = 512;
                                }

                                space.Seek(0, SeekOrigin.Begin);
                            }
                            else
                            {
                                if (space.Length % 512 == 0 && space.Length % 4096 != 0)
                                {
                                    sectorSize = 512;
                                }
                            }

                            Logging.Log();

                            string vhdFile = Path.Combine(outputDirectory, $"{disk.Value}.vhdx");
                            Logging.Log($"Dumping {vhdFile}...");

                            long diskCapacity = space.Length;
                            using Stream fs = new FileStream(vhdFile, FileMode.CreateNew, FileAccess.ReadWrite);
                            using VirtualDisk outDisk = DiscUtils.Vhdx.Disk.InitializeDynamic(fs, Ownership.None, diskCapacity, Geometry.FromCapacity(diskCapacity, sectorSize));

                            DateTime now = DateTime.Now;
                            Action<ulong, ulong> progressCallback = (ulong readBytes, ulong totalBytes) =>
                            {
                                ShowProgress(readBytes, totalBytes, now);
                            };

                            Logging.Log($"Dumping {disk.Value}");
                            space.CopyTo(outDisk.Content, progressCallback);
                            Logging.Log();

                            space.Dispose();
                        }
                    }
                }
            }
        }

        protected static void ShowProgress(ulong readBytes, ulong totalBytes, DateTime startTime)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            TimeSpan remaining = readBytes != 0 ?
                TimeSpan.FromMilliseconds(timeSoFar.TotalMilliseconds / readBytes * (totalBytes - readBytes)) : TimeSpan.MaxValue;

            double speed = Math.Round(readBytes / 1024L / 1024L / timeSoFar.TotalSeconds);

            uint percentage = (uint)(readBytes * 100 / totalBytes);

            Logging.Log($"{Logging.GetDISMLikeProgressBar(percentage)} {speed}MB/s {remaining:hh\\:mm\\:ss\\.f}", returnLine: false);
        }
    }
}
