using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using OpenRa.FileFormats;

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

		class Chunk { public int Index = 0; public int Count = 0; public string Data = ""; }

		public static void ReceiveChunk(string data)
		{
			var c = new Chunk();
			FieldLoader.Load(c, new MiniYaml(null, MiniYaml.FromString(data)));
			var bytes = Convert.FromBase64String(c.Data);
			content.Write(bytes, 0, bytes.Length);

			if (c.Index == c.Count - 1)
				EndDownload();
		}

		static void BeginDownload()
		{
			if (missingPackages.Count == 0)		// we're finished downloading resources!
			{
				currentPackage = null;
				return;
			}

			currentPackage = missingPackages[0];
			missingPackages.RemoveAt(0);

			content = new MemoryStream();

			Game.controller.AddOrder(
				new Order("RequestFile", null, null, int2.Zero, currentPackage) 
				{ IsImmediate = true });
		}

		static void EndDownload()
		{
			// commit this data to disk
			var parts = currentPackage.Split(':');
			File.WriteAllBytes(parts[0], content.ToArray());

			if (CalculateSHA1(parts[0]) != parts[1])
				throw new InvalidOperationException("Broken download");

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
