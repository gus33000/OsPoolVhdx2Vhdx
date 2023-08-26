using DiscUtils;
using DiscUtils.Streams;
using Microsoft.Spaces.Diskstream;
using System.ComponentModel;

namespace OsPoolVhdx2Vhdx
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: OsPoolVhdx2Vhdx <Path to FFU File> <Output director for SPACEDisk.vhdx files>");
                return;
            }

            string VhdxPath = args[0];
            string OutputDirectory = args[1];

            if (!File.Exists(VhdxPath))
            {
                Console.WriteLine($"VHDX file does not exist: {VhdxPath}");
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
            using Disk virtualDisk = Vhd.Open(vhdx, true, null);

                try
                {
                    using Pool pool = Pool.Open(virtualDisk);
                    foreach (Space space in pool.Spaces)
                    {
                        string vhdfile = Path.Combine(outputDirectory, $"{space.Name}.vhdx");
                        Console.WriteLine($"Dumping {vhdfile}...");

                        long diskCapacity = space.Length;
                        using Stream fs = new FileStream(vhdfile, FileMode.CreateNew, FileAccess.ReadWrite);
                        using VirtualDisk outDisk = DiscUtils.Vhdx.Disk.InitializeDynamic(fs, Ownership.None, diskCapacity, logicalSectorSize: space.BytesPerSector);;

                        StreamPump pump = new()
                        {
                            InputStream = space,
                            OutputStream = outDisk.Content,
                            SparseCopy = true,
                            SparseChunkSize = space.BytesPerSector,
                            BufferSize = space.BytesPerSector * 1024
                        };

                        long totalBytes = space.Length;

                        DateTime now = DateTime.Now;
                        pump.ProgressEvent += (o, e) => { ShowProgress((ulong)e.BytesRead, (ulong)totalBytes, now); };

                        Console.WriteLine("Dumping " + space.Name);
                        pump.Run();
                        Console.WriteLine();

                        space.Dispose();
                    }
                }
                catch (Win32Exception ex)
                {
                    if (ex.NativeErrorCode != 1168)
                    {
                        throw;
                    }

                    Console.WriteLine("VHDX file contains no recognized OSPool partition");
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
