using System;
using System.IO;

namespace OpenRA.Support
{
	public struct WindowsPreparedNativeDll : IDisposable
	{
		readonly string tempDirectory;

		/// <summary>
		/// On Windows only, prepares a native DLL to be loaded accounting for the process bitness. A native DLL with a
		/// '-x86' and '-x64' suffix for each bitness is expected in <see cref="Platform.GameDir"/>. The copy with the
		/// correct bitness will be copied to a new location and the suffix removed. You should then run code than will
		/// trigger the DLL to be loaded.
		/// </summary>
		/// <param name="file">Name of the native DLL to be prepared. E.g. "native.dll". The files "native-x86.dll" and
		/// "native-x64.dll" are expected in <see cref="Platform.GameDir"/>. The correct one will be copied to a
		/// location and renamed to "native.dll" so it can be loaded.</param>
		public WindowsPreparedNativeDll(string file)
		{
			if (Platform.CurrentPlatform != PlatformType.Windows)
			{
				tempDirectory = null;
				return;
			}

			// The AnyCPU configuration allows a managed binary to run in 32-bit on 32-bit OSes, and 64-bit on 64-bit
			// OSes. (If "Prefer 32-bit" is selected, it will run as 32-bit on 64-bit OSes as Windows supports this.)
			// This feature falls a bit flat when faced with unmanaged dependencies. Unmanaged binaries must be built
			// for a specific bitness, and there's no inbuilt feature to allow a process to choose the correct one at
			// runtime. Mono fairly elegantly solves this using "dllmap" but that only works if you run OpenRA via
			// mono. Instead, we provide this much less elegant workaround by copying a DLL with the correct bitness to
			// the name expected at runtime. We can't use different DLL names at compile time, as a constant value is
			// required.
			// We could copy the correct DLL locally, but this fails if OpenRA is installed in a directory protected by
			// UAC. Instead we are forced to create a temporary directory and copy the DLL there. We then need to add
			// the temporary directory to the PATH in order to convince the loader to look there when required.
			// This is dumb, but it works.
			tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempDirectory);
			var filename = Path.GetFileNameWithoutExtension(file);
			var ext = Path.GetExtension(file);
			var sourceFile = filename + (Environment.Is64BitProcess ? "-x64" : "-x86") + ext;
			File.Copy(Path.Combine(Platform.GameDir, sourceFile), Path.Combine(tempDirectory, file), true);
			var envPath = Environment.GetEnvironmentVariable("PATH");
			tempDirectory = ";" + tempDirectory;
			Environment.SetEnvironmentVariable("PATH", envPath + tempDirectory);
		}

		public void Dispose()
		{
			if (Platform.CurrentPlatform != PlatformType.Windows)
				return;

			// Once we've loaded the DLL, we can remove the temporary directory from the path.
			var path = Environment.GetEnvironmentVariable("PATH");
			path = path.Replace(tempDirectory, "");
			Environment.SetEnvironmentVariable("PATH", path);
		}
	}
}
