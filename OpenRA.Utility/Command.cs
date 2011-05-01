#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.GameRules;
using OpenRA.FileFormats.Graphics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenRA.FileFormats;

namespace OpenRA.Utility
{
	static class Command
	{
		public static void ExtractZip(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}

			var zipFile = args[1];
			var dest = args[2];
			
			if (!File.Exists(zipFile))
			{
				Console.WriteLine("Error: Could not find {0}", zipFile);
				return;
			}
			
			List<string> extracted = new List<string>();
			try
			{
				new ZipInputStream(File.OpenRead(zipFile)).ExtractZip(dest, extracted);
			}
			catch (SharpZipBaseException)
			{
				foreach(var f in extracted)
					File.Delete(f);
				Console.WriteLine("Error: Corrupted archive");
				return;
			}
			Console.WriteLine("Status: Completed");
		}

		static void InstallPackages(string fromPath, string toPath,
			string[] filesToCopy, string[] filesToExtract, string packageToMount)
		{
			if (!Directory.Exists(toPath))
				Directory.CreateDirectory(toPath);

			Util.ExtractFromPackage(fromPath, packageToMount, filesToExtract, toPath);
			foreach (var file in filesToCopy)
			{
				if (!File.Exists(Path.Combine(fromPath, file)))
				{
					Console.WriteLine("Error: Could not find {0}", file);
					return;
				}

				Console.WriteLine("Status: Extracting {0}", file.ToLowerInvariant());
				File.Copy(
					Path.Combine(fromPath, file),
					Path.Combine(toPath, Path.GetFileName(file).ToLowerInvariant()), true);
			}

			Console.WriteLine("Status: Completed");
		}
		
		public static void InstallRAPackages(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}

			InstallPackages(args[1], args[2],
				new string[] { "INSTALL/REDALERT.MIX" },
				new string[] { "conquer.mix", "russian.mix", "allies.mix", "sounds.mix",
					"scores.mix", "snow.mix", "interior.mix", "temperat.mix" },
				"MAIN.MIX");
		}

		public static void InstallCncPackages(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}

			InstallPackages(args[1], args[2],
				new string[] { "CONQUER.MIX", "DESERT.MIX", "GENERAL.MIX", "SCORES.MIX",
					"SOUNDS.MIX", "TEMPERAT.MIX", "WINTER.MIX" },
				new string[] { "cclocal.mix", "speech.mix", "tempicnh.mix", "updatec.mix" },
				"INSTALL/SETUP.Z");
		}

        public static void DisplayFilepicker(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Error: Invalid syntax");
                return;
            }

            using (var dialog = new OpenFileDialog() { Title = args[1] })
                if (dialog.ShowDialog() == DialogResult.OK)
                    Console.WriteLine(dialog.FileName);
        }

		public static void Settings(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}
			
			var section = args[2].Split('.')[0];
			var field = args[2].Split('.')[1];
			string expandedPath = args[1].Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.Personal));
			var settings = new Settings(Path.Combine(expandedPath,"settings.yaml"), Arguments.Empty);
			var result = settings.Sections[section].GetType().GetField(field).GetValue(settings.Sections[section]);
			Console.WriteLine(result);
		}

        static void AuthenticateAndExecute(string cmd, string[] args)
        {
            for (var i = 1; i < args.Length; i++)
                cmd += " \"{0}\"".F(args[i]);
            Util.CallWithAdmin(cmd);
        }

        public static void AuthenticateAndExtractZip(string[] args) { AuthenticateAndExecute("--extract-zip-inner", args); }
		public static void AuthenticateAndInstallRAPackages(string[] args) { AuthenticateAndExecute( "--install-ra-packages-inner", args ); }
		public static void AuthenticateAndInstallCncPackages(string[] args) { AuthenticateAndExecute( "--install-cnc-packages-inner", args ); }

		public static void ConvertPngToShp(string[] args)
		{
			var src = args[1];
			var dest = Path.ChangeExtension(src, ".shp");
			var width = int.Parse(args[2]);

			var srcImage = PngLoader.Load(src);

			if (srcImage.Width % width != 0)
				throw new InvalidOperationException("Bogus width; not a whole number of frames");

			using (var destStream = File.Create(dest))
				ShpWriter.Write(destStream, width, srcImage.Height,
					srcImage.ToFrames(width));
		}

		static IEnumerable<byte[]> ToFrames(this Bitmap bitmap, int width)
		{
			for (var x = 0; x < bitmap.Width; x += width)
			{
				var data = bitmap.LockBits(new Rectangle(x, 0, width, bitmap.Height), ImageLockMode.ReadOnly,
					PixelFormat.Format8bppIndexed);

				var bytes = new byte[width * bitmap.Height];
				for (var i = 0; i < bitmap.Height; i++)
					Marshal.Copy(new IntPtr(data.Scan0.ToInt64() + i * data.Stride),
						bytes, i * width, width);

				bitmap.UnlockBits(data);

				yield return bytes;
			}
		}

		public static void ConvertShpToPng(string[] args)
		{
			var src = args[1];
			var dest = Path.ChangeExtension(src, ".png");

			var srcImage = ShpReader.Load(src);

			using (var bitmap = new Bitmap(srcImage.ImageCount * srcImage.Width, srcImage.Height, PixelFormat.Format8bppIndexed))
			{
				var x = 0;

				foreach (var frame in srcImage)
				{
					var data = bitmap.LockBits(new Rectangle(x, 0, srcImage.Width, srcImage.Height), ImageLockMode.WriteOnly,
						PixelFormat.Format8bppIndexed);

					for (var i = 0; i < bitmap.Height; i++)
						Marshal.Copy(frame.Image, i * srcImage.Width,
							new IntPtr(data.Scan0.ToInt64() + i * data.Stride), srcImage.Width);

					Marshal.Copy(frame.Image, 0, data.Scan0, frame.Image.Length);
					x += srcImage.Width;
				}

				bitmap.Save(dest);
			}
		}
	}
}
