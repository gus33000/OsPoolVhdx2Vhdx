using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Spaces.Diskstream
{
	public class Vhd : Disk
	{
		protected Vhd(IntPtr handle) : base(handle)
		{
		}

		public static List<Vhd> Open(List<string> vhdFilePaths, bool readOnly = false)
		{
			List<string> list = new(vhdFilePaths);
			list.Reverse();
			List<Vhd> list2 = new();
			bool flag = false;
			try
			{
				Vhd vhd = null;
				for (int i = 0; i < list.Count; i++)
				{
					bool readOnly2 = true;
					bool flag2 = i == list.Count - 1;
					if (flag2)
					{
						readOnly2 = readOnly;
					}
					vhd = Vhd.Open(list[i], readOnly2, vhd);
					list2.Add(vhd);
				}
				list2.Reverse();
				flag = true;
			}
			finally
			{
				bool flag3 = !flag;
				if (flag3)
				{
					foreach (Vhd vhd2 in list2)
					{
						vhd2.Dispose();
					}
				}
			}
			return list2;
		}

		public static Vhd Open(string vhdFilePath, bool readOnly, Vhd parent)
		{
			IntPtr parentVhd = Disk.InvalidHandleValue;
			bool flag = parent != null;
			if (flag)
			{
				parentVhd = parent.GetHandle();
			}
			IntPtr intPtr = Vhd.VhdHandleOpen(vhdFilePath, readOnly, parentVhd);
			bool flag2 = intPtr == Disk.InvalidHandleValue;
			if (flag2)
			{
				throw new Win32Exception();
			}
			return new Vhd(intPtr);
		}

		protected override void Dispose(bool disposing)
		{
			bool isDisposed = base.IsDisposed;
			if (!isDisposed)
			{
				Vhd.VhdHandleClose(base.GetHandle());
				base.IsDisposed = true;
			}
		}

		[DllImport("diskhandle.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr VhdHandleOpen(string vhdFilePath, bool readOnly, IntPtr parentVhd);

		[DllImport("diskhandle.dll", SetLastError = true)]
		private static extern bool VhdHandleClose(IntPtr vhdHandle);

		[DllImport("diskhandle.dll", SetLastError = true)]
		private static extern bool DiskHandleFlush(IntPtr diskHandle);
	}
}
