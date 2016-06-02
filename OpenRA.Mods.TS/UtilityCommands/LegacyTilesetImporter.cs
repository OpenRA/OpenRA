#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.FileFormats;

namespace OpenRA.Mods.TS.UtilityCommands
{
	class ImportLegacyTilesetCommand : IUtilityCommand
	{
		public string Name { get { return "--tileset-import"; } }

		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		[Desc("FILENAME", "TEMPLATEEXTENSION", "Convert a legacy tileset to the OpenRA format.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			var file = new IniFile(File.Open(args[1], FileMode.Open));
			var extension = args[2];
			var tileSize = modData.Manifest.Get<MapGrid>().TileSize;

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

			// Loop over template sets
			try
			{
				for (var tilesetGroupIndex = 0;; tilesetGroupIndex++)
				{
					var section = file.GetSection("TileSet{0:D4}".F(tilesetGroupIndex));

					var sectionCount = int.Parse(section.GetValue("TilesInSet", "1"));
					var sectionFilename = section.GetValue("FileName", "").ToLowerInvariant();
					var sectionCategory = section.GetValue("SetName", "");

					// Loop over templates
					for (var i = 1; i <= sectionCount; i++, templateIndex++)
					{
						var templateFilename = "{0}{1:D2}.{2}".F(sectionFilename, i, extension);
						if (!modData.DefaultFileSystem.Exists(templateFilename))
							continue;

						using (var s = modData.DefaultFileSystem.Open(templateFilename))
						{
							Console.WriteLine("\tTemplate@{0}:", templateIndex);
							Console.WriteLine("\t\tCategory: {0}", sectionCategory);
							Console.WriteLine("\t\tId: {0}", templateIndex);

							var images = new List<string>();

							images.Add("{0}{1:D2}.{2}".F(sectionFilename, i, extension));
							for (var v = 'a'; v <= 'z'; v++)
							{
								var variant = "{0}{1:D2}{2}.{3}".F(sectionFilename, i, v, extension);
								if (modData.DefaultFileSystem.Exists(variant))
									images.Add(variant);
							}

							Console.WriteLine("\t\tImages: {0}", images.JoinWith(", "));

							var templateWidth = s.ReadUInt32();
							var templateHeight = s.ReadUInt32();
							/* var tileWidth = */s.ReadInt32();
							/* var tileHeight = */s.ReadInt32();
							var offsets = new uint[templateWidth * templateHeight];
							for (var j = 0; j < offsets.Length; j++)
								offsets[j] = s.ReadUInt32();

							Console.WriteLine("\t\tSize: {0}, {1}", templateWidth, templateHeight);
							Console.WriteLine("\t\tTiles:");

							for (var j = 0; j < offsets.Length; j++)
							{
								if (offsets[j] == 0)
									continue;

								s.Position = offsets[j] + 40;
								var height = s.ReadUInt8();
								var terrainType = s.ReadUInt8();
								var rampType = s.ReadUInt8();

								if (terrainType >= terrainTypes.Length)
									throw new InvalidDataException("Unknown terrain type {0} in {1}".F(terrainType, templateFilename));

								Console.WriteLine("\t\t\t{0}: {1}", j, terrainTypes[terrainType]);
								if (height != 0)
									Console.WriteLine("\t\t\t\tHeight: {0}", height);

								if (rampType != 0)
									Console.WriteLine("\t\t\t\tRampType: {0}", rampType);

								Console.WriteLine("\t\t\t\tLeftColor: {0:X2}{1:X2}{2:X2}", s.ReadUInt8(), s.ReadUInt8(), s.ReadUInt8());
								Console.WriteLine("\t\t\t\tRightColor: {0:X2}{1:X2}{2:X2}", s.ReadUInt8(), s.ReadUInt8(), s.ReadUInt8());
								Console.WriteLine("\t\t\t\tZOffset: {0}", -tileSize.Height / 2.0f);
								Console.WriteLine("\t\t\t\tZRamp: 0");
							}
						}
					}
				}
			}
			catch (InvalidOperationException)
			{
				// GetSection will throw when we run out of sections to import
			}
		}
	}
}
