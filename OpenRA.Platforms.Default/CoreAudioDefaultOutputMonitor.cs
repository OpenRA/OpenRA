#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace OpenRA.Platforms.Default
{
	/// <summary>
	/// Subscribes to macOS default-output changes (for example AirPods connecting).
	/// CoreAudio calls back on its own thread, so this only raises a flag for the render thread.
	/// </summary>
	static class CoreAudioDefaultOutputMonitor
	{
		const string CoreAudio = "/System/Library/Frameworks/CoreAudio.framework/CoreAudio";
		const uint SystemObject = 1;

		[StructLayout(LayoutKind.Sequential)]
		struct PropertyAddress
		{
			public uint Selector;
			public uint Scope;
			public uint Element;
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate int PropertyListener(uint objectId, uint addressCount, IntPtr addresses, IntPtr clientData);

		[DllImport(CoreAudio)]
		static extern int AudioObjectAddPropertyListener(uint objectId, ref PropertyAddress address, PropertyListener listener, IntPtr clientData);

		// Static so the garbage collector can never free the delegate CoreAudio is holding.
		static readonly PropertyListener Listener = OnChanged;
		static int started;
		static int changed;

		static int OnChanged(uint objectId, uint addressCount, IntPtr addresses, IntPtr clientData)
		{
			Interlocked.Exchange(ref changed, 1);
			return 0;
		}

		public static void Start()
		{
			if (Interlocked.Exchange(ref started, 1) != 0 || !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				return;

			try
			{
				// 'dOut' on the 'glob' scope: kAudioHardwarePropertyDefaultOutputDevice, global, main element.
				var address = new PropertyAddress { Selector = 0x644F7574, Scope = 0x676C6F62, Element = 0 };
				var status = AudioObjectAddPropertyListener(SystemObject, ref address, Listener, IntPtr.Zero);
				Log.Write("sound", $"Default output listener registered with status {status}.");
			}
			catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
			{
				Log.Write("sound", $"Default output listener is unavailable: {e.Message}");
			}
		}

		public static bool TakeChanged()
		{
			return Interlocked.Exchange(ref changed, 0) != 0;
		}
	}
}
