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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	static class UpgradeRules
	{
		public const int MinimumSupportedVersion = 20160508;

		internal static void TryUpdateColor(ref string value)
		{
			if (value.Length == 0)
				return;

			try
			{
				var parts = value.Split(',');
				if (parts.Length == 3)
					value = FieldSaver.FormatValue(Color.FromArgb(
						Exts.ParseIntegerInvariant(parts[0]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[1]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[2]).Clamp(0, 255)));
				else if (parts.Length == 4)
					value = FieldSaver.FormatValue(Color.FromArgb(
						Exts.ParseIntegerInvariant(parts[0]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[1]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[2]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[3]).Clamp(0, 255)));
			}
			catch { }
		}

		internal static void TryUpdateColors(ref string value)
		{
			if (value.Length == 0)
				return;

			try
			{
				var parts = value.Split(',');
				if (parts.Length % 4 != 0)
					return;

				var colors = new Color[parts.Length / 4];
				for (var i = 0; i < colors.Length; i++)
				{
					colors[i] = Color.FromArgb(
						Exts.ParseIntegerInvariant(parts[4 * i]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[4 * i + 1]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[4 * i + 2]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[4 * i + 3]).Clamp(0, 255));
				}

				value = FieldSaver.FormatValue(colors);
			}
			catch { }
		}

		internal static void TryUpdateHSLColor(ref string value)
		{
			if (value.Length == 0)
				return;

			try
			{
				var parts = value.Split(',');
				if (parts.Length == 3 || parts.Length == 4)
					value = FieldSaver.FormatValue(new HSLColor(
						(byte)Exts.ParseIntegerInvariant(parts[0]).Clamp(0, 255),
						(byte)Exts.ParseIntegerInvariant(parts[1]).Clamp(0, 255),
						(byte)Exts.ParseIntegerInvariant(parts[2]).Clamp(0, 255)));
			}
			catch { }
		}

		internal static void UpgradeActorRules(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			var addNodes = new List<MiniYamlNode>();

			foreach (var node in nodes)
			{
				if (engineVersion < 20160515)
				{
					// Use generic naming for building demolition using explosives.
					if (node.Key == "C4Demolition")
						node.Key = "Demolition";

					foreach (var n in node.Value.Nodes)
						if (n.Key == "C4Delay")
							n.Key = "DetonationDelay";
				}

				// WithSmoke was refactored to become more generic and Sequence/Image notation has been unified.
				if (engineVersion < 20160528)
				{
					if (depth == 1 && node.Key.StartsWith("WithSmoke"))
					{
						var s = node.Value.Nodes.FirstOrDefault(n => n.Key == "Sequence");
						if (s != null)
							s.Key = "Image";

						var parts = node.Key.Split('@');
						node.Key = "WithDamageOverlay";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}
				}

				if (engineVersion < 20160604 && node.Key.StartsWith("ProvidesTechPrerequisite"))
				{
					var name = node.Value.Nodes.First(n => n.Key == "Name");
					var id = name.Value.Value.ToLowerInvariant().Replace(" ", "");
					node.Value.Nodes.Add(new MiniYamlNode("Id", id));
				}

				if (engineVersion < 20160611)
				{
					// Deprecated WithSpriteRotorOverlay
					if (depth == 1 && node.Key.StartsWith("WithSpriteRotorOverlay"))
					{
						var parts = node.Key.Split('@');
						node.Key = "WithIdleOverlay";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];

						Console.WriteLine("The 'WithSpriteRotorOverlay' trait has been removed.");
						Console.WriteLine("Its functionality can be fully replicated with 'WithIdleOverlay' + upgrades.");
						Console.WriteLine("Look at the helicopters in our RA / C&C1  mods for implementation details.");
					}
				}

				// Map difficulty configuration was split to a generic trait
				if (engineVersion < 20160614 && node.Key.StartsWith("MapOptions"))
				{
					var difficultiesNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "Difficulties");
					if (difficultiesNode != null)
					{
						var difficulties = FieldLoader.GetValue<string[]>("Difficulties", difficultiesNode.Value.Value)
							.ToDictionary(d => d.Replace(" ", "").ToLowerInvariant(), d => d);
						node.Value.Nodes.Remove(difficultiesNode);

						var childNodes = new List<MiniYamlNode>()
						{
							new MiniYamlNode("ID", "difficulty"),
							new MiniYamlNode("Label", "Difficulty"),
							new MiniYamlNode("Values", new MiniYaml("", difficulties.Select(kv => new MiniYamlNode(kv.Key, kv.Value)).ToList()))
						};

						var difficultyNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "Difficulty");
						if (difficultyNode != null)
						{
							childNodes.Add(new MiniYamlNode("Default", difficultyNode.Value.Value.Replace(" ", "").ToLowerInvariant()));
							node.Value.Nodes.Remove(difficultyNode);
						}
						else
							childNodes.Add(new MiniYamlNode("Default", difficulties.Keys.First()));

						var lockedNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "DifficultyLocked");
						if (lockedNode != null)
						{
							childNodes.Add(new MiniYamlNode("Locked", lockedNode.Value.Value));
							node.Value.Nodes.Remove(lockedNode);
						}

						addNodes.Add(new MiniYamlNode("ScriptLobbyDropdown@difficulty", new MiniYaml("", childNodes)));
					}
				}

				if (engineVersion < 20160702)
				{
					if (node.Key.StartsWith("GivesExperience"))
					{
						var ff = "FriendlyFire";
						var ffNode = node.Value.Nodes.FirstOrDefault(n => n.Key == ff);
						if (ffNode != null)
						{
							var newStanceStr = "";
							if (FieldLoader.GetValue<bool>(ff, ffNode.Value.Value))
								newStanceStr = "Neutral, Enemy, Ally";
							else
								newStanceStr = "Neutral, Enemy";

							node.Value.Nodes.Add(new MiniYamlNode("ValidStances", newStanceStr));
						}

						node.Value.Nodes.Remove(ffNode);
					}
					else if (node.Key.StartsWith("GivesBounty"))
					{
						var stancesNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "Stances");
						if (stancesNode != null)
							stancesNode.Key = "ValidStances";
					}
				}

				if (engineVersion < 20160703)
				{
					if (node.Key.StartsWith("WithDecoration") || node.Key.StartsWith("WithRankDecoration") || node.Key.StartsWith("WithDecorationCarryable"))
					{
						var stancesNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "Stances");
						if (stancesNode != null)
							stancesNode.Key = "ValidStances";
					}
				}

				if (engineVersion < 20160704)
				{
					if (node.Key.Contains("PoisonedByTiberium"))
					{
						node.Key = node.Key.Replace("PoisonedByTiberium", "DamagedByTerrain");
						if (!node.Key.StartsWith("-"))
						{
							if (node.Value.Nodes.Any(a => a.Key == "Resources"))
								node.Value.Nodes.Where(n => n.Key == "Resources").Do(n => n.Key = "Terrain");
							else
								node.Value.Nodes.Add(new MiniYamlNode("Terrain", new MiniYaml("Tiberium, BlueTiberium")));

							if (!node.Value.Nodes.Any(a => a.Key == "Weapon"))
								node.Value.Nodes.Add(new MiniYamlNode("Weapon", new MiniYaml("Tiberium")));
						}
					}

					if (node.Key.Contains("DamagedWithoutFoundation"))
					{
						node.Key = node.Key.Replace("DamagedWithoutFoundation", "DamagedByTerrain");
						if (!node.Key.StartsWith("-"))
						{
							if (!node.Value.Nodes.Any(a => a.Key == "Weapon"))
								node.Value.Nodes.Add(new MiniYamlNode("Weapon", new MiniYaml("weathering")));

							Console.WriteLine("SafeTerrain isn't converted. Setup an inverted check using Terrain.");

							node.Value.Nodes.Add(new MiniYamlNode("StartOnThreshold", new MiniYaml("true")));
							if (!node.Value.Nodes.Any(a => a.Key == "DamageThreshold"))
								node.Value.Nodes.Add(new MiniYamlNode("DamageThreshold", new MiniYaml("50")));
						}
					}
				}

				// ParticleDensityFactor was converted from a float to an int
				if (engineVersion < 20160713 && node.Key == "WeatherOverlay")
				{
					var density = node.Value.Nodes.FirstOrDefault(n => n.Key == "ParticleDensityFactor");
					if (density != null)
					{
						var value = float.Parse(density.Value.Value, CultureInfo.InvariantCulture);
						value = (int)Math.Round(value * 10000, 0);
						density.Value.Value = value.ToString();
					}
				}

				UpgradeActorRules(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}

			foreach (var a in addNodes)
				nodes.Add(a);
		}

		internal static void UpgradeWeaponRules(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Refactor Missile RangeLimit from ticks to WDist
				if (engineVersion < 20160509)
				{
					var weapRange = node.Value.Nodes.FirstOrDefault(n => n.Key == "Range");
					var projectile = node.Value.Nodes.FirstOrDefault(n => n.Key == "Projectile");

					if (projectile != null && weapRange != null && projectile.Value.Value == "Missile")
					{
						var oldWDist = FieldLoader.GetValue<WDist>("Range", weapRange.Value.Value);
						var rangeLimitNode = projectile.Value.Nodes.FirstOrDefault(x => x.Key == "RangeLimit");

						// RangeLimit is now a WDist value, so for the conversion, we take weapon range and add 20% on top.
						// Overly complicated calculations using Range, Speed and the old RangeLimit value would be rather pointless,
						// because currently most mods have somewhat arbitrary, usually too high and in a few cases too low RangeLimits anyway.
						var newValue = oldWDist.Length * 120 / 100;
						var newCells = newValue / 1024;
						var newCellPart = newValue % 1024;

						if (rangeLimitNode != null)
							rangeLimitNode.Value.Value = newCells.ToString() + "c" + newCellPart.ToString();
						else
						{
							// Since the old default was 'unlimited', we're using weapon range * 1.2 for missiles not defining a custom RangeLimit as well
							projectile.Value.Nodes.Add(new MiniYamlNode("RangeLimit", newCells.ToString() + "c" + newCellPart.ToString()));
						}
					}
				}

				// Streamline some projectile property names and functionality
				if (engineVersion < 20160601)
				{
					if (node.Key == "Sequence")
						node.Key = "Sequences";

					if (node.Key == "TrailSequence")
						node.Key = "TrailSequences";

					if (node.Key == "Trail")
						node.Key = "TrailImage";

					if (node.Key == "Velocity")
						node.Key = "Speed";
				}

				UpgradeWeaponRules(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeTileset(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeTileset(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeCursors(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeCursors(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradePlayers(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradePlayers(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeChromeMetrics(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeChromeMetrics(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeChromeLayout(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeChromeLayout(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void ModifyCPos(ref string input, CVec vector)
		{
			var oldCPos = FieldLoader.GetValue<CPos>("(value)", input);
			var newCPos = oldCPos + vector;
			input = newCPos.ToString();
		}

		internal static void UpgradeActors(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Fix RA building footprints to not use _ when it's not necessary
				if (engineVersion < 20160619 && modData.Manifest.Mod.Id == "ra" && depth == 1)
				{
					var buildings = new List<string>() { "tsla", "gap", "agun", "apwr", "fapw" };
					if (buildings.Contains(parent.Value.Value) && node.Key == "Location")
						ModifyCPos(ref node.Value.Value, new CVec(0, 1));
				}

				// Fix TD building footprints to not use _ when it's not necessary
				if (engineVersion < 20160619 && modData.Manifest.Mod.Id == "cnc" && depth == 1)
				{
					var buildings = new List<string>() { "atwr", "obli", "tmpl", "weap", "hand" };
					if (buildings.Contains(parent.Value.Value) && node.Key == "Location")
						ModifyCPos(ref node.Value.Value, new CVec(0, 1));
				}

				UpgradeActors(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeMapFormat(ModData modData, IReadWritePackage package)
		{
			if (package == null)
				return;

			var yamlStream = package.GetStream("map.yaml");
			if (yamlStream == null)
				return;

			var yaml = new MiniYaml(null, MiniYaml.FromStream(yamlStream, package.Name));
			var nd = yaml.ToDictionary();
			var mapFormat = FieldLoader.GetValue<int>("MapFormat", nd["MapFormat"].Value);
			if (mapFormat < 6)
				throw new InvalidDataException("Map format {0} is not supported.\n File: {1}".F(mapFormat, package.Name));

			// Format 6 -> 7 combined the Selectable and UseAsShellmap flags into the Class enum
			if (mapFormat < 7)
			{
				MiniYaml useAsShellmap;
				if (nd.TryGetValue("UseAsShellmap", out useAsShellmap) && bool.Parse(useAsShellmap.Value))
					yaml.Nodes.Add(new MiniYamlNode("Visibility", new MiniYaml("Shellmap")));
				else if (nd["Type"].Value == "Mission" || nd["Type"].Value == "Campaign")
					yaml.Nodes.Add(new MiniYamlNode("Visibility", new MiniYaml("MissionSelector")));
			}

			// Format 7 -> 8 replaced normalized HSL triples with rgb(a) hex colors
			if (mapFormat < 8)
			{
				var players = yaml.Nodes.FirstOrDefault(n => n.Key == "Players");
				if (players != null)
				{
					bool noteHexColors = false;
					bool noteColorRamp = false;
					foreach (var player in players.Value.Nodes)
					{
						var colorRampNode = player.Value.Nodes.FirstOrDefault(n => n.Key == "ColorRamp");
						if (colorRampNode != null)
						{
							Color dummy;
							var parts = colorRampNode.Value.Value.Split(',');
							if (parts.Length == 3 || parts.Length == 4)
							{
								// Try to convert old normalized HSL value to a rgb hex color
								try
								{
									HSLColor color = new HSLColor(
										(byte)Exts.ParseIntegerInvariant(parts[0].Trim()).Clamp(0, 255),
										(byte)Exts.ParseIntegerInvariant(parts[1].Trim()).Clamp(0, 255),
										(byte)Exts.ParseIntegerInvariant(parts[2].Trim()).Clamp(0, 255));
									colorRampNode.Value.Value = FieldSaver.FormatValue(color);
									noteHexColors = true;
								}
								catch (Exception)
								{
									throw new InvalidDataException("Invalid ColorRamp value.\n File: " + package.Name);
								}
							}
							else if (parts.Length != 1 || !HSLColor.TryParseRGB(parts[0], out dummy))
								throw new InvalidDataException("Invalid ColorRamp value.\n File: " + package.Name);

							colorRampNode.Key = "Color";
							noteColorRamp = true;
						}
					}

					if (noteHexColors)
						Console.WriteLine("ColorRamp is now called Color and uses rgb(a) hex value - rrggbb[aa].");
					else if (noteColorRamp)
						Console.WriteLine("ColorRamp is now called Color.");
				}
			}

			// Format 8 -> 9 moved map options and videos from the map file itself to traits
			if (mapFormat < 9)
			{
				var rules = yaml.Nodes.FirstOrDefault(n => n.Key == "Rules");
				var worldNode = rules.Value.Nodes.FirstOrDefault(n => n.Key == "World");
				if (worldNode == null)
					worldNode = new MiniYamlNode("World", new MiniYaml("", new List<MiniYamlNode>()));

				var playerNode = rules.Value.Nodes.FirstOrDefault(n => n.Key == "Player");
				if (playerNode == null)
					playerNode = new MiniYamlNode("Player", new MiniYaml("", new List<MiniYamlNode>()));

				var visibilityNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Visibility");
				if (visibilityNode != null)
				{
					var visibility = FieldLoader.GetValue<MapVisibility>("Visibility", visibilityNode.Value.Value);
					if (visibility.HasFlag(MapVisibility.MissionSelector))
					{
						var missionData = new MiniYamlNode("MissionData", new MiniYaml("", new List<MiniYamlNode>()));
						worldNode.Value.Nodes.Add(missionData);

						var description = yaml.Nodes.FirstOrDefault(n => n.Key == "Description");
						if (description != null)
							missionData.Value.Nodes.Add(new MiniYamlNode("Briefing", description.Value.Value));

						var videos = yaml.Nodes.FirstOrDefault(n => n.Key == "Videos");
						if (videos != null && videos.Value.Nodes.Any())
						{
							var backgroundVideo = videos.Value.Nodes.FirstOrDefault(n => n.Key == "BackgroundInfo");
							if (backgroundVideo != null)
								missionData.Value.Nodes.Add(new MiniYamlNode("BackgroundVideo", backgroundVideo.Value.Value));

							var briefingVideo = videos.Value.Nodes.FirstOrDefault(n => n.Key == "Briefing");
							if (briefingVideo != null)
								missionData.Value.Nodes.Add(new MiniYamlNode("BriefingVideo", briefingVideo.Value.Value));

							var startVideo = videos.Value.Nodes.FirstOrDefault(n => n.Key == "GameStart");
							if (startVideo != null)
								missionData.Value.Nodes.Add(new MiniYamlNode("StartVideo", startVideo.Value.Value));

							var winVideo = videos.Value.Nodes.FirstOrDefault(n => n.Key == "GameWon");
							if (winVideo != null)
								missionData.Value.Nodes.Add(new MiniYamlNode("WinVideo", winVideo.Value.Value));

							var lossVideo = videos.Value.Nodes.FirstOrDefault(n => n.Key == "GameLost");
							if (lossVideo != null)
								missionData.Value.Nodes.Add(new MiniYamlNode("LossVideo", lossVideo.Value.Value));
						}
					}
				}

				var mapOptions = yaml.Nodes.FirstOrDefault(n => n.Key == "Options");
				if (mapOptions != null)
				{
					var cheats = mapOptions.Value.Nodes.FirstOrDefault(n => n.Key == "Cheats");
					if (cheats != null)
					{
						worldNode.Value.Nodes.Add(new MiniYamlNode("DeveloperMode", new MiniYaml("", new List<MiniYamlNode>()
						{
							new MiniYamlNode("Locked", "True"),
							new MiniYamlNode("Enabled", cheats.Value.Value)
						})));
					}

					var crates = mapOptions.Value.Nodes.FirstOrDefault(n => n.Key == "Crates");
					if (crates != null && !worldNode.Value.Nodes.Any(n => n.Key == "-CrateSpawner"))
					{
						if (!FieldLoader.GetValue<bool>("crates", crates.Value.Value))
							worldNode.Value.Nodes.Add(new MiniYamlNode("-CrateSpawner", new MiniYaml("")));
					}

					var creeps = mapOptions.Value.Nodes.FirstOrDefault(n => n.Key == "Creeps");
					if (creeps != null)
					{
						worldNode.Value.Nodes.Add(new MiniYamlNode("MapCreeps", new MiniYaml("", new List<MiniYamlNode>()
						{
							new MiniYamlNode("Locked", "True"),
							new MiniYamlNode("Enabled", creeps.Value.Value)
						})));
					}

					var fog = mapOptions.Value.Nodes.FirstOrDefault(n => n.Key == "Fog");
					var shroud = mapOptions.Value.Nodes.FirstOrDefault(n => n.Key == "Shroud");
					if (fog != null || shroud != null)
					{
						var shroudNode = new MiniYamlNode("Shroud", new MiniYaml("", new List<MiniYamlNode>()));
						playerNode.Value.Nodes.Add(shroudNode);

						if (fog != null)
						{
							shroudNode.Value.Nodes.Add(new MiniYamlNode("FogLocked", "True"));
							shroudNode.Value.Nodes.Add(new MiniYamlNode("FogEnabled", fog.Value.Value));
						}

						if (shroud != null)
						{
							var enabled = FieldLoader.GetValue<bool>("shroud", shroud.Value.Value);
							shroudNode.Value.Nodes.Add(new MiniYamlNode("ExploredMapLocked", "True"));
							shroudNode.Value.Nodes.Add(new MiniYamlNode("ExploredMapEnabled", FieldSaver.FormatValue(!enabled)));
						}
					}

					var allyBuildRadius = mapOptions.Value.Nodes.FirstOrDefault(n => n.Key == "AllyBuildRadius");
					if (allyBuildRadius != null)
					{
						worldNode.Value.Nodes.Add(new MiniYamlNode("MapBuildRadius", new MiniYaml("", new List<MiniYamlNode>()
						{
							new MiniYamlNode("AllyBuildRadiusLocked", "True"),
							new MiniYamlNode("AllyBuildRadiusEnabled", allyBuildRadius.Value.Value)
						})));
					}

					var startingCash = mapOptions.Value.Nodes.FirstOrDefault(n => n.Key == "StartingCash");
					if (startingCash != null)
					{
						playerNode.Value.Nodes.Add(new MiniYamlNode("PlayerResources", new MiniYaml("", new List<MiniYamlNode>()
						{
							new MiniYamlNode("DefaultCashLocked", "True"),
							new MiniYamlNode("DefaultCash", startingCash.Value.Value)
						})));
					}

					var startingUnits = mapOptions.Value.Nodes.FirstOrDefault(n => n.Key == "ConfigurableStartingUnits");
					if (startingUnits != null && !worldNode.Value.Nodes.Any(n => n.Key == "-SpawnMPUnits"))
					{
						worldNode.Value.Nodes.Add(new MiniYamlNode("SpawnMPUnits", new MiniYaml("", new List<MiniYamlNode>()
						{
							new MiniYamlNode("Locked", "True"),
						})));
					}

					var techLevel = mapOptions.Value.Nodes.FirstOrDefault(n => n.Key == "TechLevel");
					var difficulties = mapOptions.Value.Nodes.FirstOrDefault(n => n.Key == "Difficulties");
					var shortGame = mapOptions.Value.Nodes.FirstOrDefault(n => n.Key == "ShortGame");
					if (techLevel != null || difficulties != null || shortGame != null)
					{
						var optionsNode = new MiniYamlNode("MapOptions", new MiniYaml("", new List<MiniYamlNode>()));
						worldNode.Value.Nodes.Add(optionsNode);

						if (techLevel != null)
						{
							optionsNode.Value.Nodes.Add(new MiniYamlNode("TechLevelLocked", "True"));
							optionsNode.Value.Nodes.Add(new MiniYamlNode("TechLevel", techLevel.Value.Value));
						}

						if (difficulties != null)
							optionsNode.Value.Nodes.Add(new MiniYamlNode("Difficulties", difficulties.Value.Value));

						if (shortGame != null)
						{
							optionsNode.Value.Nodes.Add(new MiniYamlNode("ShortGameLocked", "True"));
							optionsNode.Value.Nodes.Add(new MiniYamlNode("ShortGameEnabled", shortGame.Value.Value));
						}
					}
				}

				if (worldNode.Value.Nodes.Any() && !rules.Value.Nodes.Contains(worldNode))
					rules.Value.Nodes.Add(worldNode);

				if (playerNode.Value.Nodes.Any() && !rules.Value.Nodes.Contains(playerNode))
					rules.Value.Nodes.Add(playerNode);
			}

			// Format 9 -> 10 moved smudges to SmudgeLayer, and uses map.png for all maps
			if (mapFormat < 10)
			{
				ExtractSmudges(yaml);
				if (package.Contains("map.png"))
					yaml.Nodes.Add(new MiniYamlNode("LockPreview", new MiniYaml("True")));
			}

			// Format 10 -> 11 replaced the single map type field with a list of categories
			if (mapFormat < 11)
			{
				var type = yaml.Nodes.First(n => n.Key == "Type");
				yaml.Nodes.Add(new MiniYamlNode("Categories", type.Value));
				yaml.Nodes.Remove(type);
			}

			if (mapFormat < Map.SupportedMapFormat)
			{
				yaml.Nodes.First(n => n.Key == "MapFormat").Value = new MiniYaml(Map.SupportedMapFormat.ToString());
				Console.WriteLine("Converted {0} to MapFormat {1}.", package.Name, Map.SupportedMapFormat);
			}

			package.Update("map.yaml", Encoding.UTF8.GetBytes(yaml.Nodes.WriteToString()));
		}

		static void ExtractSmudges(MiniYaml yaml)
		{
			var smudges = yaml.Nodes.FirstOrDefault(n => n.Key == "Smudges");
			if (smudges == null || !smudges.Value.Nodes.Any())
				return;

			var scorches = new List<MiniYamlNode>();
			var craters = new List<MiniYamlNode>();
			foreach (var s in smudges.Value.Nodes)
			{
				// loc=type,loc,depth
				var parts = s.Key.Split(' ');
				var value = "{0},{1}".F(parts[0], parts[2]);
				var node = new MiniYamlNode(parts[1], value);
				if (parts[0].StartsWith("sc"))
					scorches.Add(node);
				else if (parts[0].StartsWith("cr"))
					craters.Add(node);
			}

			var rulesNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Rules");
			if (rulesNode == null)
			{
				rulesNode = new MiniYamlNode("Rules", new MiniYaml("", new List<MiniYamlNode>()));
				yaml.Nodes.Add(rulesNode);
			}

			var worldNode = rulesNode.Value.Nodes.FirstOrDefault(n => n.Key == "World");
			if (worldNode == null)
			{
				worldNode = new MiniYamlNode("World", new MiniYaml("", new List<MiniYamlNode>()));
				rulesNode.Value.Nodes.Add(rulesNode);
			}

			if (scorches.Any())
			{
				var initialScorches = new MiniYamlNode("InitialSmudges", new MiniYaml("", scorches));
				var smudgeLayer = new MiniYamlNode("SmudgeLayer@SCORCH", new MiniYaml("", new List<MiniYamlNode>() { initialScorches }));
				worldNode.Value.Nodes.Add(smudgeLayer);
			}

			if (craters.Any())
			{
				var initialCraters = new MiniYamlNode("InitialSmudges", new MiniYaml("", craters));
				var smudgeLayer = new MiniYamlNode("SmudgeLayer@CRATER", new MiniYaml("", new List<MiniYamlNode>() { initialCraters }));
				worldNode.Value.Nodes.Add(smudgeLayer);
			}
		}
	}
}
