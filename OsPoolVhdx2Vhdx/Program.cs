﻿using DiscUtils;
using DiscUtils.Partitions;
using DiscUtils.Streams;
using StorageSpace;

namespace OsPoolVhdx2Vhdx
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: OsPoolVhdx2Vhdx <Path to VHD(X) File with Storage Pool> <Output director for SPACEDisk.vhdx files>");
                return;
            }

            string VhdxPath = args[0];
            string OutputDirectory = args[1];

            if (!File.Exists(VhdxPath))
            {
                Console.WriteLine($"VHD(X) file does not exist: {VhdxPath}");
                return;
            }

            if (!Directory.Exists(OutputDirectory))
            {
                Console.WriteLine($"Output directory does not exist: {OutputDirectory}");
                return;
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
                        Console.WriteLine($"{((GuidPartitionInfo)partitionInfo).Name} {((GuidPartitionInfo)partitionInfo).Identity} {((GuidPartitionInfo)partitionInfo).GuidType} {((GuidPartitionInfo)partitionInfo).SectorCount * virtualDisk.SectorSize} StoragePool");

                        Stream storageSpacePartitionStream = partitionInfo.Open();

                        Pool storageSpace = new(storageSpacePartitionStream);

                        Dictionary<int, string> disks = storageSpace.GetDisks();

                        foreach (KeyValuePair<int, string> disk in disks.OrderBy(x => x.Key))
                        {
                            Console.WriteLine($"- {disk.Key}: {disk.Value} StorageSpace");
                        }
                        
                        foreach (KeyValuePair<int, string> disk in disks)
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

                            string vhdfile = Path.Combine(outputDirectory, $"{disk.Value}.vhdx");
                            Console.WriteLine($"Dumping {vhdfile}...");

                            long diskCapacity = space.Length;
                            using Stream fs = new FileStream(vhdfile, FileMode.CreateNew, FileAccess.ReadWrite);
                            using VirtualDisk outDisk = DiscUtils.Vhdx.Disk.InitializeDynamic(fs, Ownership.None, diskCapacity, Geometry.FromCapacity(diskCapacity, sectorSize));

                            StreamPump pump = new()
                            {
                                InputStream = space,
                                OutputStream = outDisk.Content,
                                SparseCopy = true,
                                SparseChunkSize = sectorSize,
                                BufferSize = sectorSize * 1024
                            };

                            long totalBytes = space.Length;

                            DateTime now = DateTime.Now;
                            pump.ProgressEvent += (o, e) => { ShowProgress((ulong)e.BytesRead, (ulong)totalBytes, now); };

                            Console.WriteLine("Dumping " + disk.Value);
                            pump.Run();
                            Console.WriteLine();

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

            TimeSpan remaining =
                TimeSpan.FromMilliseconds(timeSoFar.TotalMilliseconds / readBytes * (totalBytes - readBytes));

            double speed = Math.Round(readBytes / 1024L / 1024L / timeSoFar.TotalSeconds);

            Console.Write(
                $"\r{GetDismLikeProgBar((int)(readBytes * 100 / totalBytes))} {speed}MB/s {remaining:hh\\:mm\\:ss\\.f}");
        }

        private static string GetDismLikeProgBar(int percentage)
        {
            int eqsLength = (int)((double)percentage / 100 * 55);
            string bases = new string('=', eqsLength) + new string(' ', 55 - eqsLength);
            bases = bases.Insert(28, percentage + "%");
            if (percentage == 100)
            {
                bases = bases[1..];
            }
            else if (percentage < 10)
            {
                bases = bases.Insert(28, " ");
            }

            return $"[{bases}]";
        }
    }
}
