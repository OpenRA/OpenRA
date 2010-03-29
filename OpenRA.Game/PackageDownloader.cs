#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using OpenRA.FileFormats;

namespace OpenRA
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

			Game.Debug("Requesting package: {0}".F(currentPackage));

			Game.IssueOrder(
				new Order("RequestFile", null, currentPackage) { IsImmediate = true });

			Fraction = 0f;
		}

		static void EndDownload()
		{
			// commit this data to disk
			var parts = currentPackage.Split(':');
			File.WriteAllBytes(parts[0], content.ToArray());

			if (CalculateSHA1(parts[0]) != parts[1])
				throw new InvalidOperationException("Broken download");

			Game.Debug("Finished receiving package: {0}".F(currentPackage));

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
				Game.Debug("Missing package: {0}".F(p));
				return false;
			}

			if (CalculateSHA1(parts[0]) != parts[1])
			{
				Game.Debug("Bad SHA1 for package; redownloading: {0}".F(p));
				return false;
			}

			Game.Debug("Verified package: {0}".F(p));
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
