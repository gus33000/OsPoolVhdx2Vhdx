using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Spaces.Diskstream
{
	public class Pool : IDisposable
	{
		protected Pool(IntPtr handle)
		{
			bool flag = handle == IntPtr.Zero || handle == Pool.InvalidHandleValue;
			if (flag)
			{
				throw new ArgumentException("The handle is invalid.");
			}
			Handle = handle;
			IsDisposed = false;
		}

		~Pool()
		{
			Dispose(false);
		}

		public Space[] Spaces
		{
			get
			{
				int num = (int)Pool.PoolHandleGetNumberOfSpaceHandles(GetHandle());
				bool flag = num == 0;
				Space[] result;
				if (flag)
				{
					result = null;
				}
				else
				{
					IntPtr[] array = new IntPtr[num];
					Pool.PoolHandleGetSpaceHandles(GetHandle(), (uint)num, array);
					Space[] array2 = new Space[num];
					for (int i = 0; i < num; i++)
					{
						array2[i] = Space.Open(array[i]);
					}
					result = array2;
				}
				return result;
			}
		}

		protected IntPtr Handle { get; set; }

		protected bool IsDisposed { get; set; }

		public static Pool Open(Disk disk)
		{
			return Pool.Open(new Disk[]
			{
				disk
			});
		}

		public static Pool Open(Disk[] disks)
		{
			bool flag = disks == null;
			if (flag)
			{
				throw new ArgumentNullException("disks");
			}
			bool flag2 = disks.Length < 1;
			if (flag2)
			{
				throw new ArgumentException("At least one disk is required.");
			}
			IntPtr[] array = new IntPtr[disks.Length];
			for (int i = 0; i < disks.Length; i++)
			{
				array[i] = disks[i].GetHandle();
			}
			IntPtr intPtr = Pool.PoolHandleOpen((uint)array.Length, array);
			bool flag3 = intPtr == Pool.InvalidHandleValue;
			if (flag3)
			{
				throw new Win32Exception();
			}
			return new Pool(intPtr);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			bool isDisposed = IsDisposed;
			if (!isDisposed)
			{
				Pool.PoolHandleClose(GetHandle());
				IsDisposed = true;
			}
		}

		[DllImport("diskhandle.dll", SetLastError = true)]
		private static extern IntPtr PoolHandleOpen(uint numberOfDiskHandles, IntPtr[] diskHandles);

		[DllImport("diskhandle.dll")]
		private static extern bool PoolHandleClose(IntPtr poolHandle);

		[DllImport("diskhandle.dll")]
		private static extern uint PoolHandleGetNumberOfSpaceHandles(IntPtr poolHandle);

		[DllImport("diskhandle.dll")]
		private static extern void PoolHandleGetSpaceHandles(IntPtr poolHandle, uint numberOfSpaceHandles, IntPtr[] spaceHandles);

		private IntPtr GetHandle()
		{
			bool isDisposed = IsDisposed;
			if (isDisposed)
			{
				throw new ObjectDisposedException("Handle");
			}
			return Handle;
		}

		protected static readonly IntPtr InvalidHandleValue = new(-1);
	}
}
