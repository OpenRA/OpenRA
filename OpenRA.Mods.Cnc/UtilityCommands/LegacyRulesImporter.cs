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
using OpenRA.Mods.Common.FileFormats;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	class LegacyRulesImporter : IUtilityCommand
	{
		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		string IUtilityCommand.Name => "--rules-import";

		IniFile rulesIni;
		IniFile artIni;

		[Desc("RULES.INI", "ART.INI", "Convert ART.INI and RULES.INI to the OpenRA rules definition format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			rulesIni = new IniFile(File.Open(args[1], FileMode.Open));
			artIni = new IniFile(File.Open(args[2], FileMode.Open));

			var buildings = rulesIni.GetSection("BuildingTypes").Select(b => b.Value).Distinct();
			Console.WriteLine("# Buildings");
			Console.WriteLine();
			ImportStructures(buildings);

			var terrainObjects = rulesIni.GetSection("TerrainTypes").Select(b => b.Value).Distinct();
			Console.WriteLine("# Terrain Objects");
			Console.WriteLine();
			ImportStructures(terrainObjects, true);
		}

		void ImportStructures(IEnumerable<string> structures, bool useTerrainPalette = false)
		{
			foreach (var building in structures)
			{
				var rulesSection = rulesIni.GetSection(building, allowFail: true);
				if (rulesSection == null)
					continue;

				Console.WriteLine(rulesSection.Name + ":");

				var name = rulesSection.GetValue("Name", string.Empty);
				if (!string.IsNullOrEmpty(name))
				{
					Console.WriteLine("\tTooltip:");
					Console.WriteLine("\t\tName: " + name);
				}

				var prerequisite = rulesSection.GetValue("Prerequisite", string.Empty);
				if (!string.IsNullOrEmpty(prerequisite))
				{
					Console.WriteLine("\tBuildable:");
					Console.WriteLine("\t\tPrerequisites: " + prerequisite.ToLowerInvariant());
				}

				var cost = rulesSection.GetValue("Cost", string.Empty);
				if (!string.IsNullOrEmpty(cost))
				{
					Console.WriteLine("\tValued:");
					Console.WriteLine("\t\tCost: " + cost);
				}

				var armor = rulesSection.GetValue("Armor", string.Empty);
				if (!string.IsNullOrEmpty(armor))
				{
					Console.WriteLine("\tArmor:");
					Console.WriteLine("\t\tType: " + armor);
				}

				var sight = rulesSection.GetValue("Sight", string.Empty);
				if (!string.IsNullOrEmpty(sight))
				{
					Console.WriteLine("\tRevealsShroud:");
					Console.WriteLine("\t\tRange: " + sight + "c0");
				}

				var strength = rulesSection.GetValue("Strength", string.Empty);
				if (!string.IsNullOrEmpty(strength))
				{
					Console.WriteLine("\tHealth:");
					Console.WriteLine("\t\tHP: " + strength);
				}

				var power = rulesSection.GetValue("Power", string.Empty);
				if (!string.IsNullOrEmpty(power))
				{
					Console.WriteLine("\tPower:");
					Console.WriteLine("\t\tAmount: " + power);
				}

				var captureable = rulesSection.GetValue("Capturable", string.Empty);
				if (!string.IsNullOrEmpty(captureable) && captureable == "true")
					Console.WriteLine("\tCapturable:");

				var crewed = rulesSection.GetValue("Crewed", string.Empty);
				if (!string.IsNullOrEmpty(crewed) && crewed == "yes")
					Console.WriteLine("\tSpawnActorsOnSell:");

				var deploysInto = rulesSection.GetValue("DeploysInto", string.Empty);
				if (!string.IsNullOrEmpty(deploysInto))
				{
					Console.WriteLine("\tTransforms:");
					Console.WriteLine("\t\tIntoActor: " + deploysInto);
				}

				var undeploysInto = rulesSection.GetValue("UndeploysInto", string.Empty);
				if (!string.IsNullOrEmpty(undeploysInto))
				{
					Console.WriteLine("\tTransforms:");
					Console.WriteLine("\t\tIntoActor: " + undeploysInto);
				}

				if (artIni.Sections.Any(s => s.Name == building.ToLowerInvariant()))
				{
					var artSection = artIni.GetSection(building);

					var foundation = artSection.GetValue("Foundation", string.Empty);
					if (!string.IsNullOrEmpty(foundation))
					{
						var dimensions = foundation.Split('x');
						if (dimensions.First() == "0" || dimensions.Last() == "0")
							Console.WriteLine("\tImmobile:\n \t\tOccupiesSpace: False");
						else
						{
							Console.WriteLine("\tBuilding:");

							var adjacent = rulesSection.GetValue("Adjacent", string.Empty);
							if (!string.IsNullOrEmpty(adjacent))
								Console.WriteLine("\t\tAdjacent: " + adjacent);

							Console.WriteLine("\t\tDimensions: " + dimensions.First() + "," + dimensions.Last());

							Console.Write("\t\tFootprint:");
							var width = 0;
							int.TryParse(dimensions.First(), out width);
							var height = 0;
							int.TryParse(dimensions.Last(), out height);
							for (var y = 0; y < height; y++)
							{
								Console.Write(" ");
								for (var x = 0; x < width; x++)
									Console.Write("x");
							}

							Console.WriteLine();
						}
					}

					var buildup = artSection.GetValue("Buildup", string.Empty);
					if (!string.IsNullOrEmpty(buildup) && buildup != "none")
						Console.WriteLine("\tWithMakeAnimation:");

					var terrainPalette = artSection.GetValue("TerrainPalette", string.Empty);
					if (!string.IsNullOrEmpty(terrainPalette))
						bool.TryParse(terrainPalette, out useTerrainPalette);

					var remapable = artSection.GetValue("Remapable", string.Empty);
					if (!string.IsNullOrEmpty(remapable) && remapable == "yes")
						useTerrainPalette = false;
				}

				var isAnimated = rulesSection.GetValue("IsAnimated", string.Empty);
				if (!string.IsNullOrEmpty(isAnimated) && isAnimated == "yes")
					useTerrainPalette = false;

				var invisibleInGame = rulesSection.GetValue("InvisibleInGame", string.Empty);
				if (!string.IsNullOrEmpty(invisibleInGame) && invisibleInGame == "yes")
					Console.WriteLine("\tRenderSpritesEditorOnly:");
				else
					Console.WriteLine("\tRenderSprites:");

				if (useTerrainPalette)
					Console.WriteLine("\t\tPalette: terrain");

				var image = rulesSection.GetValue("Image", string.Empty);
				if (!string.IsNullOrEmpty(image) && image != "none")
					Console.WriteLine("\t\tImage: " + image.ToLowerInvariant());

				Console.WriteLine("\tWithSpriteBody:");
				Console.WriteLine("\tAutoSelectionSize:");
				Console.WriteLine("\tBodyOrientation:\n\t\tUseClassicPerspectiveFudge: False\n\t\tQuantizedFacings: 1");

				Console.WriteLine("\tFrozenUnderFog:");

				Console.WriteLine();
			}
		}
	}
}
