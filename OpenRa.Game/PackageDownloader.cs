using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa
{
	static class PackageDownloader
	{
		static string[] allPackages = { };
		static List<string> missingPackages = new List<string>();
		static string currentPackage = null;
		static MemoryStream content = null;

		public static string CurrentPackage { get { return currentPackage; } }
		public static int RemainingPackages { get { return missingPackages.Count; } }
		public static float Fraction { get; private set; }
		public static int DownloadedBytes { get { return (int)content.Length; } }

		public static bool SetPackageList(string[] packages)
		{
			if (!(allPackages.Except(packages).Any()
				|| packages.Except(allPackages).Any()))
				return false;

			allPackages = packages;
			missingPackages = allPackages.Where(p => !HavePackage(p)).ToList();

			if (currentPackage == null || !missingPackages.Contains(currentPackage))
				BeginDownload();
			else
				missingPackages.Remove(currentPackage);

			return true;
		}

		class Chunk { public int Index = 0; public int Count = 0; public string Data = ""; }

		public static void ReceiveChunk(string data)
		{
			var c = new Chunk();
			FieldLoader.Load(c, new MiniYaml(null, MiniYaml.FromString(data)));
			var bytes = Convert.FromBase64String(c.Data);
			content.Write(bytes, 0, bytes.Length);

			Fraction = (float)c.Index / c.Count;

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

			Game.chat.AddLine(Color.White, "Debug", "Requesting package: {0}".F(currentPackage));

			Game.controller.AddOrder(
				new Order("RequestFile", Game.LocalPlayer.PlayerActor, null, int2.Zero, currentPackage) { IsImmediate = true });

			Fraction = 0f;
		}

		static void EndDownload()
		{
			// commit this data to disk
			var parts = currentPackage.Split(':');
			File.WriteAllBytes(parts[0], content.ToArray());

			if (CalculateSHA1(parts[0]) != parts[1])
				throw new InvalidOperationException("Broken download");

			Game.chat.AddLine(Color.White, "Debug", "Finished receiving package: {0}".F(currentPackage));

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
			if (!File.Exists(parts[0]))
			{
				Game.chat.AddLine(Color.White, "Debug", "Missing package: {0}".F(p));
				return false;
			}

			if (CalculateSHA1(parts[0]) != parts[1])
			{
				Game.chat.AddLine(Color.White, "Debug", "Bad SHA1 for package; redownloading: {0}".F(p));
				return false;
			}

			Game.chat.AddLine(Color.White, "Debug", "Verified package: {0}".F(p));
			return true;
		}

		public static string CalculateSHA1(string filename)
		{
			using (var csp = SHA1.Create())
				return new string(csp.ComputeHash(File.ReadAllBytes(filename))
					.SelectMany(a => a.ToString("x2")).ToArray());
		}
	}
}
