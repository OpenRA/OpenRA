using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenRa.Core
{
	public static class FileSystem
	{
		static List<IMountable> packages = new List<IMountable>();

		public static void Mount(IMountable package)
		{
			packages.Add(package);
		}

		internal static Stream GetItem(string filename)
		{
			foreach (IMountable package in packages)
			{
				Stream s = package.GetItem(filename);
				if (s != null)
					return s;
			}

			return null;
		}
	}
}
