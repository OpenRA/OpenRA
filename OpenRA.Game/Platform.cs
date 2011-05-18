using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using OpenRA.FileFormats;

namespace OpenRA
{
	enum PlatformType
	{
		Unknown,
		Windows,
		OSX,
		Linux
	}

	static class Platform
	{
		public static PlatformType CurrentPlatform
		{
			get
			{
				return currentPlatform.Value;
			}
		}

		static Lazy<PlatformType> currentPlatform = new Lazy<PlatformType>(GetCurrentPlatform);
		
		static PlatformType GetCurrentPlatform()
		{
				if (Environment.OSVersion.Platform == PlatformID.Win32NT) return PlatformType.Windows;
				
				try
				{
					var psi = new ProcessStartInfo("uname", "-s");
					psi.UseShellExecute = false;
					psi.RedirectStandardOutput = true;
					var p = Process.Start(psi);
					var kernelName = p.StandardOutput.ReadToEnd();
					if (kernelName.Contains("Linux") || kernelName.Contains("BSD"))
						return PlatformType.Linux;
					if (kernelName.Contains("Darwin"))
						return PlatformType.OSX;
				}
				catch {	}
				
				return PlatformType.Unknown;
		}
	}
}
