using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Spaces.Diskstream
{
    public abstract class Disk : Stream
    {
        protected Disk(IntPtr handle)
        {
            bool flag = handle == IntPtr.Zero || handle == Disk.InvalidHandleValue;
            if (flag)
            {
                throw new ArgumentException("The handle is invalid.");
            }
            Handle = handle;
            IsDisposed = false;
            Position = 0L;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => (long)Disk.DiskHandleGetLength(GetHandle());

        public long Cylinders
        {
            get
            {
                long num = Length / BytesPerSector;
                long num2 = TracksPerCylinder * SectorsPerTrack;
                return num / num2;
            }
        }

        public int TracksPerCylinder => 255;

        public int SectorsPerTrack => 63;

        public int BytesPerSector => (int)Disk.DiskHandleGetBytesPerSector(GetHandle());

        public override long Position
        {
            get => position;
            set
            {
                bool flag = position < 0L;
                if (flag)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                bool flag2 = position > Length;
                if (flag2)
                {
                    throw new EndOfStreamException();
                }
                position = value;
            }
        }

        protected bool IsDisposed
        {
            get; set;
        }

        protected IntPtr Handle
        {
            get; set;
        }

        public override void Flush()
        {
            bool flag = Disk.DiskHandleFlush(GetHandle());
            bool flag2 = !flag;
            if (flag2)
            {
                throw new Win32Exception();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            bool flag = buffer == null;
            if (flag)
            {
                throw new ArgumentNullException("buffer");
            }
            bool flag2 = offset + count > buffer.Length;
            if (flag2)
            {
                throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            }
            bool flag3 = offset < 0;
            if (flag3)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            bool flag4 = count < 0;
            if (flag4)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            bool flag5 = Disk.DiskHandleRead(GetHandle(), buffer, (uint)buffer.Length, (uint)offset, (ulong)Position, (uint)count);
            bool flag6 = !flag5;
            if (flag6)
            {
                throw new Win32Exception();
            }
            Position += count;
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            bool flag = buffer == null;
            if (flag)
            {
                throw new ArgumentNullException("buffer");
            }
            bool flag2 = offset + count > buffer.Length;
            if (flag2)
            {
                throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            }
            bool flag3 = offset < 0;
            if (flag3)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            bool flag4 = count < 0;
            if (flag4)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            bool flag5 = Disk.DiskHandleWrite(GetHandle(), buffer, (uint)buffer.Length, (uint)offset, (ulong)Position, (uint)count);
            bool flag6 = !flag5;
            if (flag6)
            {
                throw new Win32Exception();
            }
            Position += count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        internal IntPtr GetHandle()
        {
            bool isDisposed = IsDisposed;
            return isDisposed ? throw new ObjectDisposedException("Handle") : Handle;
        }

        [DllImport("diskhandle.dll")]
        private static extern ulong DiskHandleGetLength(IntPtr diskHandle);

        [DllImport("diskhandle.dll")]
        private static extern uint DiskHandleGetBytesPerSector(IntPtr diskHandle);

        [DllImport("diskhandle.dll", SetLastError = true)]
        private static extern bool DiskHandleFlush(IntPtr diskHandle);

        [DllImport("diskhandle.dll", SetLastError = true)]
        private static extern bool DiskHandleRead(IntPtr diskHandle, byte[] buffer, uint length, uint offset, ulong position, uint count);

        [DllImport("diskhandle.dll", SetLastError = true)]
        private static extern bool DiskHandleWrite(IntPtr diskHandle, byte[] buffer, uint length, uint offset, ulong position, uint count);

        protected static readonly IntPtr InvalidHandleValue = new(-1);

        private long position;
    }
}
