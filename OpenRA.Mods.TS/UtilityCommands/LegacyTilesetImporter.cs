#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System;
using OpenRA.FileFormats;
using OpenRA.FileSystem;

namespace OpenRA.Mods.TS.UtilityCommands
{
	class ImportLegacyTilesetCommand : IUtilityCommand
	{
		public string Name { get { return "--tileset-import"; } }

		[Desc("FILENAME", "Convert a legacy tileset to the OpenRA format.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.modData = modData;

			GlobalFileSystem.LoadFromManifest(Game.modData.Manifest);

			var file = new IniFile(File.Open(args[1], FileMode.Open));

			var templateIndex = 0;
			var extension = "tem";

			var terrainTypes = new Dictionary<int, string>()
			{
				{  1, "Clear" }, // Desert sand(?)
				{  5, "Road" }, // Paved road
				{  6, "Rail" }, // Monorail track
				{  7, "Impassable" }, // Building
				{  9, "Water" }, // Deep water(?)
				{ 10, "Water" }, // Shallow water
				{ 11, "Road" }, // Paved road (again?)
				{ 12, "DirtRoad" }, // Dirt road
				{ 13, "Clear" }, // Regular clear terrain
				{ 14, "Rough" }, // Rough terrain (cracks etc)
				{ 15, "Cliff" }, // Cliffs
			};

			// Loop over template sets
			try
			{
				for (var tilesetGroupIndex = 0; ; tilesetGroupIndex++)
				{
					var section = file.GetSection("TileSet{0:D4}".F(tilesetGroupIndex));

					var sectionCount = int.Parse(section.GetValue("TilesInSet", "1"));
					var sectionFilename = section.GetValue("FileName", "");
					var sectionCategory = section.GetValue("SetName", "");

					// Loop over templates
					for (var i = 1; i <= sectionCount; i++, templateIndex++)
					{
						var templateFilename = "{0}{1:D2}.{2}".F(sectionFilename, i, extension);
						if (!GlobalFileSystem.Exists(templateFilename))
							continue;

						using (var s = GlobalFileSystem.Open(templateFilename))
						{
							Console.WriteLine("\tTemplate@{0}:", templateIndex);
							Console.WriteLine("\t\tCategory: {0}", sectionCategory);
							Console.WriteLine("\t\tId: {0}", templateIndex);
							Console.WriteLine("\t\tImage: {0}{1:D2}", sectionFilename, i);

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
								/* var height = */s.ReadUInt8();
								var terrainType = s.ReadUInt8();
								/* var rampType = */s.ReadUInt8();
								/* var height = */s.ReadUInt8();
								if (!terrainTypes.ContainsKey(terrainType))
									throw new InvalidDataException("Unknown terrain type {0} in {1}".F(terrainType, templateFilename));

								Console.WriteLine("\t\t\t{0}: {1}", j, terrainTypes[terrainType]);
								// Console.WriteLine("\t\t\t\tHeight: {0}", height);
								// Console.WriteLine("\t\t\t\tTerrainType: {0}", terrainType);
								// Console.WriteLine("\t\t\t\tRampType: {0}", rampType);
								// Console.WriteLine("\t\t\t\tLeftColor: {0},{1},{2}", s.ReadUInt8(), s.ReadUInt8(), s.ReadUInt8());
								// Console.WriteLine("\t\t\t\tRightColor: {0},{1},{2}", s.ReadUInt8(), s.ReadUInt8(), s.ReadUInt8());
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
