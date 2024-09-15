using System;
using System.Runtime.InteropServices;

namespace Microsoft.Spaces.Diskstream
{
    public class Space : Disk
    {
        protected Space(IntPtr handle) : base(handle)
        {
        }

        public string Name => Marshal.PtrToStringUni(Space.SpaceHandleGetName(base.GetHandle()));

        internal static Space Open(IntPtr handle)
        {
            return new Space(handle);
        }

        protected override void Dispose(bool disposing)
        {
            bool isDisposed = base.IsDisposed;
            if (!isDisposed)
            {
                base.IsDisposed = true;
            }
        }

        [DllImport("diskhandle.dll")]
        private static extern IntPtr SpaceHandleGetName(IntPtr spaceHandle);
    }
}
