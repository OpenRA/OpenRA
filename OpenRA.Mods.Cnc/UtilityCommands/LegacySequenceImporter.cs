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
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.FileFormats;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	class ImportLegacySequenceCommand : IUtilityCommand
	{
		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		string IUtilityCommand.Name => "--sequence-import";

		IniFile file;
		MapGrid grid;

		[Desc("FILENAME", "Convert ART.INI to the OpenRA sequence definition format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			grid = Game.ModData.Manifest.Get<MapGrid>();

			file = new IniFile(File.Open(args[1], FileMode.Open));

			foreach (var section in file.Sections)
			{
				var sequence = section.GetValue("Sequence", string.Empty);
				if (!string.IsNullOrEmpty(sequence))
					ImportInfantrySequences(section, sequence);

				var foundation = section.GetValue("Foundation", string.Empty);
				if (!string.IsNullOrEmpty(foundation))
					ImportBuildingSequences(section);
			}
		}

		void ImportBuildingSequences(IniSection section)
		{
			Console.WriteLine(section.Name + ":");

			Console.WriteLine("\tDefaults:");

			var foundation = section.GetValue("Foundation", string.Empty);
			if (!string.IsNullOrEmpty(foundation))
			{
				var size = foundation.Split('x');
				if (size.Length == 2)
				{
					var x = int.Parse(size[0]);
					var y = int.Parse(size[1]);

					var xOffset = (x - y) * grid.TileSize.Width / 4;
					var yOffset = (x + y) * grid.TileSize.Height / 4;
					Console.WriteLine("\t\tOffset: {0},{1}", -xOffset, -yOffset);
				}
			}

			var theater = section.GetValue("Theater", string.Empty);
			if (!string.IsNullOrEmpty(theater) && theater == "yes")
				Console.WriteLine("\t\tUseTilesetExtension: true");
			else
			{
				var toOverlay = section.GetValue("ToOverlay", string.Empty);
				if (!string.IsNullOrEmpty(toOverlay))
				{
					var overlaySection = file.GetSection(toOverlay);
					var overlayTheater = overlaySection.GetValue("Theater", string.Empty);
					if (!string.IsNullOrEmpty(overlayTheater) && overlayTheater == "yes")
						Console.WriteLine("\t\tUseTilesetExtension: true");
				}
			}

			var newTheater = section.GetValue("NewTheater", string.Empty);
			if (!string.IsNullOrEmpty(newTheater) && newTheater == "yes")
				Console.WriteLine("\t\tUseTilesetCode: true");

			var cameo = section.GetValue("Cameo", string.Empty);
			if (!string.IsNullOrEmpty(cameo))
			{
				Console.WriteLine("\ticon: " + cameo.ToLowerInvariant());
				Console.WriteLine("\t\tOffset: 0,0");
				Console.WriteLine("\t\tUseTilesetExtension: false");
				Console.WriteLine("\t\tUseTilesetCode: false");
			}

			Console.WriteLine("\tidle: ");

			var buildup = section.GetValue("Buildup", string.Empty);
			if (!string.IsNullOrEmpty(buildup) && buildup != "none")
			{
				Console.WriteLine("\tmake: " + buildup.ToLowerInvariant());
			}

			var bibShape = section.GetValue("BibShape", string.Empty);
			if (!string.IsNullOrEmpty(bibShape))
			{
				Console.WriteLine("\tbib: " + bibShape.ToLowerInvariant());
				Console.WriteLine("\t\tLength: 1");
				Console.WriteLine("\t\tZOffset: -1024");
			}

			Console.WriteLine();
		}

		void ImportInfantrySequences(IniSection section, string sequence)
		{
			Console.WriteLine(section.Name + ":");

			var image = section.GetValue("Image", string.Empty);
			if (!string.IsNullOrEmpty(image))
			{
				Console.WriteLine("\tDefaults: " + image.ToLowerInvariant());
				Console.WriteLine("\t\tTick: 80");
			}

			var cameo = section.GetValue("Cameo", string.Empty);
			if (!string.IsNullOrEmpty(cameo))
			{
				Console.WriteLine("\ticon: " + cameo.ToLowerInvariant());
				Console.WriteLine("\t\tOffset: 0,0");
				Console.WriteLine("\t\tUseTilesetExtension: false");
				Console.WriteLine("\t\tUseTilesetCode: false");
			}

			if (file.Sections.Any(s => s.Name == sequence.ToLowerInvariant()))
			{
				var sequenceSection = file.GetSection(sequence);
				var guard = sequenceSection.GetValue("Guard", string.Empty);
				if (!string.IsNullOrEmpty(guard))
				{
					Console.WriteLine("\tstand:");
					ConvertStartLengthFacings(guard);
				}

				var walk = sequenceSection.GetValue("Walk", string.Empty);
				if (!string.IsNullOrEmpty(walk))
				{
					Console.WriteLine("\trun:");
					ConvertStartLengthFacings(walk);
				}

				var prone = sequenceSection.GetValue("Prone", string.Empty);
				if (!string.IsNullOrEmpty(prone))
				{
					Console.WriteLine("\tprone-stand:");
					ConvertStartLengthFacings(prone);
				}

				var crawl = sequenceSection.GetValue("Crawl", string.Empty);
				if (!string.IsNullOrEmpty(crawl))
				{
					Console.WriteLine("\tprone-run:");
					ConvertStartLengthFacings(crawl);
				}

				var fireProne = sequenceSection.GetValue("FireProne", string.Empty);
				if (!string.IsNullOrEmpty(fireProne))
				{
					Console.WriteLine("\tprone-shoot:");
					ConvertStartLengthFacings(fireProne);
				}

				var fireUp = sequenceSection.GetValue("FireUp", string.Empty);
				if (!string.IsNullOrEmpty(fireUp))
				{
					Console.WriteLine("\tshoot:");
					ConvertStartLengthFacings(fireUp);
				}

				var idle1 = sequenceSection.GetValue("Idle1", string.Empty);
				if (!string.IsNullOrEmpty(idle1))
				{
					Console.WriteLine("\tidle1:");
					ConvertStartLengthFacings(idle1);
				}

				var idle2 = sequenceSection.GetValue("Idle2", string.Empty);
				if (!string.IsNullOrEmpty(idle2))
				{
					Console.WriteLine("\tidle2:");
					ConvertStartLengthFacings(idle2);
				}

				var die1 = sequenceSection.GetValue("Die1", string.Empty);
				if (!string.IsNullOrEmpty(die1))
				{
					Console.WriteLine("\tdie1:");
					ConvertStartLengthFacings(die1);
				}

				var die2 = sequenceSection.GetValue("Die2", string.Empty);
				if (!string.IsNullOrEmpty(die2))
				{
					Console.WriteLine("\tdie2:");
					ConvertStartLengthFacings(die2);
				}

				var die3 = sequenceSection.GetValue("Die3", string.Empty);
				if (!string.IsNullOrEmpty(die3))
				{
					Console.WriteLine("\tdie3:");
					ConvertStartLengthFacings(die3);
				}

				var die4 = sequenceSection.GetValue("Die4", string.Empty);
				if (!string.IsNullOrEmpty(die4))
				{
					Console.WriteLine("\tdie4:");
					ConvertStartLengthFacings(die4);
				}

				var die5 = sequenceSection.GetValue("Die5", string.Empty);
				if (!string.IsNullOrEmpty(die5))
				{
					Console.WriteLine("\tdie5:");
					ConvertStartLengthFacings(die5);
				}

				var cheer = sequenceSection.GetValue("Cheer", string.Empty);
				if (!string.IsNullOrEmpty(cheer))
				{
					Console.WriteLine("\tcheer:");
					ConvertStartLengthFacings(cheer);
				}

				var panic = sequenceSection.GetValue("Panic", string.Empty);
				if (!string.IsNullOrEmpty(panic))
				{
					Console.WriteLine("\tpanic-stand:");
					ConvertStartLengthFacings(panic);
					Console.WriteLine("\tpanic-run:");
					ConvertStartLengthFacings(panic);
				}
			}

			Console.WriteLine();
		}

		void ConvertStartLengthFacings(string input)
		{
			var splitting = input.Split(',');
			if (splitting.Length >= 3)
			{
				Console.WriteLine("\t\tStart: " + splitting[0]);
				Console.WriteLine("\t\tLength: " + splitting[1]);

				if (splitting.Length == 4)
					Console.WriteLine("\t\tFacings: 1");
				else
					Console.WriteLine("\t\tFacings: 8");

				int.TryParse(splitting[2], out var stride);
				int.TryParse(splitting[1], out var length);
				if (stride != 0 && stride != length)
					Console.WriteLine("\t\tStride: " + stride);
			}
		}
	}
}
