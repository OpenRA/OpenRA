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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.FileFormats;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	class ImportLegacyTilesetCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--tileset-import";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		[Desc("FILENAME", "TEMPLATEEXTENSION", "[TILESETNAME]", "Convert a legacy tileset to the OpenRA format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			var file = new IniFile(File.Open(args[1], FileMode.Open));
			var extension = args[2];
			var tileSize = utility.ModData.Manifest.Get<MapGrid>().TileSize;

			var templateIndex = 0;

			var terrainTypes = new string[]
			{
				"Clear",
				"Clear", // Note: sometimes "Ice"
				"Ice",
				"Ice",
				"Ice",
				"Road", // TS defines this as "Tunnel", but we don't need this
				"Rail",
				"Impassable", // TS defines this as "Rock", but also uses it for buildings
				"Impassable",
				"Water",
				"Water", // TS defines this as "Beach", but uses it for water...?
				"Road",
				"DirtRoad", // TS defines this as "Road", but we may want different speeds
				"Clear",
				"Rough",
				"Cliff" // TS defines this as "Rock"
			};

			var metadata = new StringBuilder();
			metadata.AppendLine("General:");

			var name = args.Length > 3 ? args[3] : Path.GetFileNameWithoutExtension(args[2]);
			metadata.AppendLine($"\tName: {name}");
			metadata.AppendLine($"\tId: {name.ToUpperInvariant()}");
			metadata.AppendLine("\tHeightDebugColors: 00000080, 00004480, 00008880, 0000CC80, 0000FF80, 4400CC80," +
				" 88008880, CC004480, FF110080, FF550080, FF990080, FFDD0080, DDFF0080, 99FF0080, 55FF0080, 11FF0080");

			// Loop over template sets
			var data = new StringBuilder();
			data.AppendLine("Templates:");
			var definedCategories = new HashSet<string>();
			var usedCategories = new HashSet<string>();
			try
			{
				for (var tilesetGroupIndex = 0; ; tilesetGroupIndex++)
				{
					var section = file.GetSection($"TileSet{tilesetGroupIndex:D4}");

					var sectionCount = int.Parse(section.GetValue("TilesInSet", "1"));
					var sectionFilename = section.GetValue("FileName", "").ToLowerInvariant();
					var sectionCategory = section.GetValue("SetName", "");
					if (!string.IsNullOrEmpty(sectionCategory) && sectionFilename != "blank")
						definedCategories.Add(sectionCategory);

					// Loop over templates
					for (var i = 1; i <= sectionCount; i++, templateIndex++)
					{
						var templateFilename = $"{sectionFilename}{i:D2}.{extension}";
						if (!modData.DefaultFileSystem.Exists(templateFilename))
							continue;

						using (var s = modData.DefaultFileSystem.Open(templateFilename))
						{
							data.AppendLine($"\tTemplate@{templateIndex}:");
							data.AppendLine($"\t\tCategories: {sectionCategory}");
							usedCategories.Add(sectionCategory);

							data.AppendLine($"\t\tId: {templateIndex}");

							var images = new List<string>
							{
								$"{sectionFilename}{i:D2}.{extension}"
							};
							for (var v = 'a'; v <= 'z'; v++)
							{
								var variant = $"{sectionFilename}{i:D2}{v}.{extension}";
								if (modData.DefaultFileSystem.Exists(variant))
									images.Add(variant);
							}

							data.AppendLine($"\t\tImages: {images.JoinWith(", ")}");

							var templateWidth = s.ReadUInt32();
							var templateHeight = s.ReadUInt32();
							/* var tileWidth = */s.ReadInt32();
							/* var tileHeight = */s.ReadInt32();
							var offsets = new uint[templateWidth * templateHeight];
							for (var j = 0; j < offsets.Length; j++)
								offsets[j] = s.ReadUInt32();

							data.AppendLine($"\t\tSize: {templateWidth}, {templateHeight}");
							data.AppendLine("\t\tTiles:");

							for (var j = 0; j < offsets.Length; j++)
							{
								if (offsets[j] == 0)
									continue;

								s.Position = offsets[j] + 40;
								var height = s.ReadUInt8();
								var terrainType = s.ReadUInt8();
								var rampType = s.ReadUInt8();

								if (terrainType >= terrainTypes.Length)
									throw new InvalidDataException($"Unknown terrain type {terrainType} in {templateFilename}");

								data.AppendLine($"\t\t\t{j}: {terrainTypes[terrainType]}");
								if (height != 0)
									data.AppendLine($"\t\t\t\tHeight: {height}");

								if (rampType != 0)
									data.AppendLine($"\t\t\t\tRampType: {rampType}");

								data.AppendLine($"\t\t\t\tMinColor: {s.ReadUInt8():X2}{s.ReadUInt8():X2}{s.ReadUInt8():X2}");
								data.AppendLine($"\t\t\t\tMaxColor: {s.ReadUInt8():X2}{s.ReadUInt8():X2}{s.ReadUInt8():X2}");
								data.AppendLine($"\t\t\t\tZOffset: {(-tileSize.Height / 2.0f)}");
								data.AppendLine("\t\t\t\tZRamp: 0");
							}
						}
					}
				}
			}
			catch (InvalidOperationException)
			{
				// GetSection will throw when we run out of sections to import
			}

			var unusedCategories = definedCategories.Except(usedCategories);
			metadata.Append("\tEditorTemplateOrder: " + usedCategories.JoinWith(", ") + " # " + unusedCategories.JoinWith(", "));

			metadata.AppendLine();
			metadata.AppendLine("\tSheetSize: 2048");
			metadata.AppendLine("\tEnableDepth: true");
			metadata.AppendLine();

			metadata.AppendLine("Terrain:");
			terrainTypes = terrainTypes.Distinct().ToArray();
			foreach (var terrainType in terrainTypes)
			{
				metadata.AppendLine($"\tTerrainType@{terrainType}:");
				metadata.AppendLine($"\t\tType: {terrainType}");

				if (terrainType == "Water")
					metadata.AppendLine("\t\tTargetTypes: Water");
				else
					metadata.AppendLine("\t\tTargetTypes: Ground");

				// TODO guess Color from Low/HighRadarColor
				metadata.AppendLine("\t\tColor: 000000");
			}

			metadata.AppendLine();

			Console.Write(metadata.ToString());
			Console.Write(data.ToString());
		}
	}
}
