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

namespace OpenRA.Mods.Common.UtilityCommands
{
	class Rgba2Hex : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rgba2hex";

		static readonly char[] Comma = new char[] { ',' };

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			if (args.Length <= 1)
				return PrintUsage();

			var invalid = false;
			for (var i = 1; i < args.Length; i++)
			{
				var parts = args[i].Split(Comma, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length != 3 && parts.Length != 4)
				{
					invalid = true;
					Console.WriteLine("Invalid color (argument " + i + "): " + args[i]);
				}
				else
				{
					foreach (var part in parts)
					{
						if (!byte.TryParse(part, out _))
						{
							invalid = true;
							Console.WriteLine("Invalid component in color (argument " + i + "): [" + part + "]: " + args[i]);
						}
					}
				}
			}

			return !invalid || PrintUsage();
		}

		bool PrintUsage()
		{
			Console.WriteLine("");
			Console.WriteLine("Usage:");
			Console.WriteLine("\tOpenRA.Utility.exe [MOD] --rgba2hex r1,g1,b1");
			Console.WriteLine("\tOpenRA.Utility.exe [MOD] --rgba2hex r1,g1,b1,a1");
			Console.WriteLine("\tOpenRA.Utility.exe [MOD] --rgba2hex r1,g1,b1 r2,g2,b2,a2");
			Console.WriteLine("\tOpenRA.Utility.exe [MOD] --rgba2hex r1,g1,b1,a1 r2,g2,b2 ...");
			Console.WriteLine("");
			Console.WriteLine("\tNo spaces between the color components (red,green,blue[,alpha]).");
			Console.WriteLine("\tSpaces between colors for a list; each argument is a color.");
			Console.WriteLine("\tExtra commas are ignored.");
			Console.WriteLine("");
			Console.WriteLine("Where:");
			Console.WriteLine("\tr# is a red component value (0-255)");
			Console.WriteLine("\tg# is a green component value (0-255)");
			Console.WriteLine("\tb# is a blue component value (0-255)");
			Console.WriteLine("\ta# is an optional alpha component value (0-255)");

			Console.WriteLine("");
			return false;
		}

		[Desc("Convert r,g,b[,a] triples/quads into hex colors")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			for (var i = 1; i < args.Length;)
			{
				var parts = args[i].Split(Comma, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 3)
				{
					foreach (var c in parts)
						Console.Write(byte.Parse(c).ToString("X2"));
				}
				else
				{
					Console.Write(byte.Parse(parts[0]).ToString("X2"));
					Console.Write(byte.Parse(parts[1]).ToString("X2"));
					Console.Write(byte.Parse(parts[2]).ToString("X2"));
					var alpha = byte.Parse(parts[3]);
					if (alpha < 255)
						Console.Write(alpha.ToString("X2"));
				}

				if (++i != args.Length)
					Console.Write(", ");
				else
					Console.WriteLine();
			}
		}
	}

	class Argb2Hex : IUtilityCommand
	{
		string IUtilityCommand.Name => "--argb2hex";

		static readonly char[] Comma = new char[] { ',' };

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			if (args.Length <= 1)
				return PrintUsage();

			var invalid = false;
			for (var i = 1; i < args.Length; i++)
			{
				var parts = args[i].Split(Comma, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length != 3 && parts.Length != 4)
				{
					invalid = true;
					Console.WriteLine("Invalid color (argument " + i + "): " + args[i]);
				}
				else
				{
					foreach (var part in parts)
					{
						if (!byte.TryParse(part, out _))
						{
							invalid = true;
							Console.WriteLine("Invalid component in color (argument " + i + "): [" + part + "]: " + args[i]);
						}
					}
				}
			}

			return !invalid || PrintUsage();
		}

		bool PrintUsage()
		{
			Console.WriteLine("");
			Console.WriteLine("Usage:");
			Console.WriteLine("\tOpenRA.Utility.exe [MOD] --argb2hex a1,r1,g1,b1");
			Console.WriteLine("\tOpenRA.Utility.exe [MOD] --argb2hex r1,g1,b1");
			Console.WriteLine("\tOpenRA.Utility.exe [MOD] --argb2hex a1,r1,g1,b1 a2,r2,g2,b2");
			Console.WriteLine("\tOpenRA.Utility.exe [MOD] --argb2hex a1,r1,g1,b1, a2,r2,g2,b2");
			Console.WriteLine("\tOpenRA.Utility.exe [MOD] --argb2hex a1,r1,g1,b1 a2,r2,g2,b2 ...");
			Console.WriteLine("");
			Console.WriteLine("\tNo spaces between color components ([alpha,]red,green,blue).");
			Console.WriteLine("\tSpaces between colors for a list; each argument is a color.");
			Console.WriteLine("\tExtra commas are ignored; useful for pasting legacy color lists to the command line.");
			Console.WriteLine("");
			Console.WriteLine("Where:");
			Console.WriteLine("\ta# is an optional alpha component value (0-255)");
			Console.WriteLine("\tr# is a red component value (0-255)");
			Console.WriteLine("\tg# is a green component value (0-255)");
			Console.WriteLine("\tb# is a blue component value (0-255)");
			Console.WriteLine("\t[MOD] is any valid mod such as \"all\"");
			Console.WriteLine("");
			Console.WriteLine("Converting legacy color lists:");
			Console.WriteLine("\tType into command line: OpenRA.Utility.exe all --argb2hex ");
			Console.WriteLine("\tFollow with a space.");
			Console.WriteLine("\tCopy legacy color list and paste into command line");
			Console.WriteLine("\t1.) Copying from command line terminal:");
			Console.WriteLine("\t\tPress Enter in command line terminal.");
			Console.WriteLine("\t\tCopy hex color list from command line terminal.");
			Console.WriteLine("\t2.) Append to file");
			Console.WriteLine("\t\tSave any unsaved changes to file.");
			Console.WriteLine("\t\tEnter \">>\" into command line terminal without the quotes.");
			Console.WriteLine("\t\tEnter relative or absolute path follow by a \"/\" to file directory if it is not the current directory.");
			Console.WriteLine("\t\tEnter full filename with extension.");
			Console.WriteLine("\t\tPress Enter.");
			Console.WriteLine("\t\tOpen/reload file");
			Console.WriteLine("");
			Console.WriteLine("");
			return false;
		}

		[Desc("Convert a,r,g,b legacy colors into hex colors")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			for (var i = 1; i < args.Length;)
			{
				var parts = args[i].Split(Comma, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 3)
				{
					foreach (var c in parts)
						Console.Write(byte.Parse(c).ToString("X2"));
				}
				else
				{
					Console.Write(byte.Parse(parts[1]).ToString("X2"));
					Console.Write(byte.Parse(parts[2]).ToString("X2"));
					Console.Write(byte.Parse(parts[3]).ToString("X2"));
					var alpha = byte.Parse(parts[0]);
					if (alpha < 255)
						Console.Write(alpha.ToString("X2"));
				}

				if (++i != args.Length)
					Console.Write(", ");
				else
					Console.WriteLine();
			}
		}
	}
}
