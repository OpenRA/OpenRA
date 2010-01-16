using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;
using System.IO;
using System.Security.Cryptography;

namespace OpenRa.Game
{
	static class PackageDownloader
	{
		static string[] allPackages = { };
		static List<string> missingPackages = new List<string>();
		static string currentPackage = null;
		static MemoryStream content = null;

		public static void SetPackageList(string[] packages)
		{
			allPackages = packages;
			missingPackages = allPackages.Where(p => !HavePackage(p)).ToList();

			if (currentPackage == null || !missingPackages.Contains(currentPackage))
				BeginDownload();
		}

		public static void ReceiveChunk(string data)
		{
			
		}

		static void BeginDownload()
		{
			if (missingPackages.Count == 0)
			{	// we're finished downloading resources!
				currentPackage = null;
				return;
			}

			currentPackage = missingPackages[0];
			missingPackages.RemoveAt(0);

			content = new MemoryStream();
		}

		static void EndDownload()
		{
			File.WriteAllBytes(currentPackage.Split(':')[0], content.ToArray());
			currentPackage = null;

			BeginDownload();
		}

		public static bool IsIdle()
		{
			return currentPackage == null 
				&& missingPackages.Count == 0;
		}

		static bool HavePackage(string p)
		{
			var parts = p.Split(':');
			return File.Exists(parts[0]) && CalculateSHA1(parts[0]) == parts[1];
		}

		public static string CalculateSHA1(string filename)
		{
			using (var csp = SHA1.Create())
				return new string(csp.ComputeHash(File.ReadAllBytes(filename))
					.SelectMany(a => a.ToString("x2")).ToArray());
		}
	}
}
