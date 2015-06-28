/* SDL2# - C# Wrapper for SDL2
 *
 * Copyright (c) 2013-2014 Ethan Lee.
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Ethan "flibitijibibo" Lee <flibitijibibo@flibitijibibo.com>
 *
 */

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace SDL2
{
	internal unsafe class LPUtf8StrMarshaler : ICustomMarshaler
	{
		public const string LeaveAllocated = "LeaveAllocated";

		private static ICustomMarshaler
			_leaveAllocatedInstance = new LPUtf8StrMarshaler(true),
			_defaultInstance = new LPUtf8StrMarshaler(false);

		public static ICustomMarshaler GetInstance(string cookie)
		{
			switch (cookie)
			{
			case "LeaveAllocated":
				return _leaveAllocatedInstance;
			default:
				return _defaultInstance;
			}
		}

		private bool _leaveAllocated;

		public LPUtf8StrMarshaler(bool leaveAllocated)
		{
			_leaveAllocated = leaveAllocated;
		}

		public object MarshalNativeToManaged(IntPtr pNativeData)
		{
			if (pNativeData == IntPtr.Zero)
				return null;
			var ptr = (byte*)pNativeData;
			while (*ptr != 0)
			{
				ptr++;
			}
			var bytes = new byte[ptr - (byte*)pNativeData];
			Marshal.Copy(pNativeData, bytes, 0, bytes.Length);
			return Encoding.UTF8.GetString(bytes);
		}

		public IntPtr MarshalManagedToNative(object ManagedObj)
		{
			if (ManagedObj == null)
				return IntPtr.Zero;
			var str = ManagedObj as string;
			if (str == null)
			{
				throw new ArgumentException("ManagedObj must be a string.", "ManagedObj");
			}
			var bytes = Encoding.UTF8.GetBytes(str);
			var mem = Marshal.AllocHGlobal(bytes.Length + 1);
			Marshal.Copy(bytes, 0, mem, bytes.Length);
			((byte*)mem)[bytes.Length] = 0;
			return mem;
		}

		public void CleanUpManagedData(object ManagedObj)
		{
		}

		public void CleanUpNativeData(IntPtr pNativeData)
		{
			if (!_leaveAllocated)
			{
				Marshal.FreeHGlobal(pNativeData);
			}
		}

		public int GetNativeDataSize ()
		{
			return -1;
		}
	}
}
