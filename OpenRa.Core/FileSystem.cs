using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenRa.Core
{
	public static class FileSystem
	{
		static List<IPackage> packages = new List<IPackage>();

		public static void Mount(IPackage package)
		{
			packages.Add(package);
		}

		internal static Stream GetItem(string filename)
		{
			foreach (IPackage package in packages)
			{
				Stream s = package.GetItem(filename);
				if (s != null)
					return s;
			}

			return null;
		}
	}
}
