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

		static void RenameNodeKey(MiniYamlNode node, string key)
		{
			var parts = node.Key.Split('@');
			node.Key = key;
			if (parts.Length > 1)
				node.Key += "@" + parts[1];
		}

		static void ConvertUpgradesToCondition(MiniYamlNode parent, MiniYamlNode node, string upgradesKey, string conditionKey)
		{
			var upgradesNode = node.Value.Nodes.FirstOrDefault(n => n.Key == upgradesKey);
			if (upgradesNode != null)
			{
				var conditions = FieldLoader.GetValue<string[]>("", upgradesNode.Value.Value);
				if (conditions.Length > 1)
					Console.WriteLine("Unable to automatically migrate {0}:{1} {2} to {3}. This must be corrected manually",
						parent.Key, node.Key, upgradesKey, conditionKey);
				else
					upgradesNode.Key = conditionKey;
			}
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

						RenameNodeKey(node, "WithDamageOverlay");
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
					if (depth == 1 && node.Key.StartsWith("WithSpriteRotorOverlay", StringComparison.Ordinal))
					{
						RenameNodeKey(node, "WithIdleOverlay");
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

							Console.WriteLine("PoisonedByTiberium: Weapon isn't converted. Copy out the appropriate");
							Console.WriteLine("weapon's Damage, ReloadDelay and DamageTypes to DamagedByTerrain's Damage,");
							Console.WriteLine("DamageInterval and DamageTypes, respectively, then remove the Weapon tag.");
						}
					}

					if (node.Key.Contains("DamagedWithoutFoundation"))
					{
						node.Key = node.Key.Replace("DamagedWithoutFoundation", "DamagedByTerrain");
						if (!node.Key.StartsWith("-"))
						{
							Console.WriteLine("DamagedWithoutFoundation: Weapon isn't converted. Copy out the appropriate");
							Console.WriteLine("weapon's Damage, ReloadDelay and DamageTypes to DamagedByTerrain's Damage,");
							Console.WriteLine("DamageInterval and DamageTypes, respectively, then remove the Weapon tag.");

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

				if (engineVersion < 20160717)
				{
					if (depth == 0)
					{
						var selectionDecorations = node.Value.Nodes.FirstOrDefault(n => n.Key == "SelectionDecorations");
						if (selectionDecorations != null)
							node.Value.Nodes.Add(selectionDecorations = new MiniYamlNode("WithSpriteControlGroup", ""));
					}
				}

				if (engineVersion < 20160818)
				{
					if (depth == 1 && node.Key.StartsWith("UpgradeOnDamage", StringComparison.Ordinal))
						RenameNodeKey(node, "UpgradeOnDamageState");
				}

				// DisplayTimer was replaced by DisplayTimerStances
				if (engineVersion < 20160820)
				{
					if (node.Key == "DisplayTimer")
					{
						node.Key = "DisplayTimerStances";

						if (node.Value.Value.ToLower() == "false")
							node.Value.Value = "None";
						else
							node.Value.Value = "Ally, Neutral, Enemy";
					}
				}

				if (engineVersion < 20160821)
				{
					// Shifted custom build time properties to Buildable
					if (depth == 0)
					{
						var cbtv = node.Value.Nodes.FirstOrDefault(n => n.Key == "CustomBuildTimeValue");
						if (cbtv != null)
						{
							var bi = node.Value.Nodes.FirstOrDefault(n => n.Key == "Buildable");

							if (bi == null)
								node.Value.Nodes.Add(bi = new MiniYamlNode("Buildable", ""));

							var value = cbtv.Value.Nodes.First(n => n.Key == "Value");
							value.Key = "BuildDuration";
							bi.Value.Nodes.Add(value);
							bi.Value.Nodes.Add(new MiniYamlNode("BuildDurationModifier", "40"));
						}

						node.Value.Nodes.RemoveAll(n => n.Key == "CustomBuildTimeValue");
						node.Value.Nodes.RemoveAll(n => n.Key == "-CustomBuildTimeValue");
					}

					// rename ProductionQueue.BuildSpeed
					if (node.Key == "BuildSpeed")
					{
						node.Key = "BuildDurationModifier";
						var oldValue = FieldLoader.GetValue<int>(node.Key, node.Value.Value);
						oldValue = oldValue * 100 / 40;
						node.Value.Value = oldValue.ToString();
					}
				}

				if (engineVersion < 20160826 && depth == 0)
				{
					// Removed debug visualization
					node.Value.Nodes.RemoveAll(n => n.Key == "PathfinderDebugOverlay");
				}

				// AlliedMissiles on JamsMissiles was changed from a boolean to a Stances field and renamed
				if (engineVersion < 20160827)
				{
					if (node.Key == "JamsMissiles")
					{
						var alliedMissiles = node.Value.Nodes.FirstOrDefault(n => n.Key == "AlliedMissiles");
						if (alliedMissiles != null)
						{
							alliedMissiles.Value.Value = FieldLoader.GetValue<bool>("AlliedMissiles", alliedMissiles.Value.Value) ? "Ally, Neutral, Enemy" : "Neutral, Enemy";
							alliedMissiles.Key = "DeflectionStances";
						}
					}
				}

				// Add a warning to add WithRearmAnimation to actors that might need it.
				// Update rule added during prep-1609 stable period, date needs fixing after release.
				if (engineVersion < 20160918 && depth == 2)
				{
					if (node.Key == "RearmBuildings")
						foreach (var host in node.Value.Value.Split(','))
							Console.WriteLine("Actor type `{0}` is denoted as a RearmBuilding. Consider adding the `WithRearmAnimation` trait to it.".F(host));
				}

				// Resource type properties were renamed, and support for tooltips added
				if (engineVersion < 20160925)
				{
					if (node.Key.StartsWith("ResourceType"))
					{
						var image = node.Value.Nodes.FirstOrDefault(n => n.Key == "Sequence");
						if (image != null)
							image.Key = "Image";

						var sequences = node.Value.Nodes.FirstOrDefault(n => n.Key == "Variants");
						if (sequences != null)
							sequences.Key = "Sequences";

						var name = node.Value.Nodes.FirstOrDefault(n => n.Key == "Name");
						if (name != null)
							node.Value.Nodes.Add(new MiniYamlNode("Type", name.Value.Value));
					}
				}

				// Renamed AttackSequence to DefaultAttackSequence in WithInfantryBody.
				if (engineVersion < 20161014)
				{
					if (node.Key == "WithInfantryBody")
					{
						var attackSequence = node.Value.Nodes.FirstOrDefault(n => n.Key == "AttackSequence");
						if (attackSequence != null)
							attackSequence.Key = "DefaultAttackSequence";
					}
				}

				// Move production description from Tooltip to Buildable
				if (engineVersion < 20161016)
				{
					var tooltipChild = node.Value.Nodes.FirstOrDefault(n => n.Key == "Tooltip" || n.Key == "DisguiseToolTip");
					if (tooltipChild != null)
					{
						var descNode = tooltipChild.Value.Nodes.FirstOrDefault(n => n.Key == "Description");
						if (descNode != null)
						{
							var buildableNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "Buildable");
							if (buildableNode == null)
								node.Value.Nodes.Add(buildableNode = new MiniYamlNode("Buildable", ""));

							buildableNode.Value.Nodes.Add(descNode);
							tooltipChild.Value.Nodes.Remove(descNode);
						}
					}
				}

				// Move production icon sequence from Tooltip to Buildable
				if (engineVersion < 20161022)
				{
					var tooltipChild = node.Value.Nodes.FirstOrDefault(n => n.Key == "Tooltip" || n.Key == "DisguiseToolTip");
					if (tooltipChild != null)
					{
						var iconNode = tooltipChild.Value.Nodes.FirstOrDefault(n => n.Key == "Icon");
						if (iconNode != null)
						{
							var buildableNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "Buildable");
							if (buildableNode == null)
								node.Value.Nodes.Add(buildableNode = new MiniYamlNode("Buildable", ""));

							buildableNode.Value.Nodes.Add(iconNode);
							tooltipChild.Value.Nodes.Remove(iconNode);
						}
					}
				}

				// Replaced upgrade consumers with conditions
				if (engineVersion < 20161117)
				{
					var upgradeTypesNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "UpgradeTypes");
					if (upgradeTypesNode != null)
					{
						var upgradeMinEnabledLevel = 0;
						var upgradeMaxEnabledLevel = int.MaxValue;
						var upgradeMaxAcceptedLevel = 1;
						var upgradeTypes = FieldLoader.GetValue<string[]>("", upgradeTypesNode.Value.Value);
						var minEnabledNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "UpgradeMinEnabledLevel");
						if (minEnabledNode != null)
							upgradeMinEnabledLevel = FieldLoader.GetValue<int>("", minEnabledNode.Value.Value);

						var maxEnabledNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "UpgradeMaxEnabledLevel");
						if (maxEnabledNode != null)
							upgradeMaxEnabledLevel = FieldLoader.GetValue<int>("", maxEnabledNode.Value.Value);

						var maxAcceptedNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "UpgradeMaxAcceptedLevel");
						if (maxAcceptedNode != null)
							upgradeMaxAcceptedLevel = FieldLoader.GetValue<int>("", maxAcceptedNode.Value.Value);

						var processed = false;
						if (upgradeTypes.Length == 1 && upgradeMinEnabledLevel == 0 && upgradeMaxEnabledLevel == 0 && upgradeMaxAcceptedLevel == 1)
						{
							node.Value.Nodes.Add(new MiniYamlNode("RequiresCondition", "!" + upgradeTypes.First()));
							processed = true;
						}
						else if (upgradeTypes.Length == 1 && upgradeMinEnabledLevel == 1 && upgradeMaxEnabledLevel == int.MaxValue && upgradeMaxAcceptedLevel == 1)
						{
							node.Value.Nodes.Add(new MiniYamlNode("RequiresCondition", upgradeTypes.First()));
							processed = true;
						}

						if (processed)
							node.Value.Nodes.RemoveAll(n => n.Key == "UpgradeTypes" || n.Key == "UpgradeMinEnabledLevel" ||
								n.Key == "UpgradeMaxEnabledLevel" || n.Key == "UpgradeMaxAcceptedLevel");
						else
							Console.WriteLine("Unable to automatically migrate {0}:{1} UpgradeTypes to RequiresCondition. This must be corrected manually", parent.Key, node.Key);
					}
				}

				if (engineVersion < 20161119)
				{
					// Migrated carryalls over to new conditions system
					ConvertUpgradesToCondition(parent, node, "CarryableUpgrades", "CarriedCondition");

					if (node.Key == "WithDecorationCarryable")
					{
						node.Key = "WithDecoration@CARRYALL";
						node.Value.Nodes.Add(new MiniYamlNode("RequiresCondition", "carryall-reserved"));
					}
				}

				if (engineVersion < 20161120)
				{
					if (node.Key.StartsWith("TimedUpgradeBar", StringComparison.Ordinal))
					{
						RenameNodeKey(node, "TimedConditionBar");
						ConvertUpgradesToCondition(parent, node, "Upgrade", "Condition");
					}

					if (node.Key.StartsWith("GrantUpgradePower", StringComparison.Ordinal))
					{
						Console.WriteLine("GrantUpgradePower Condition must be manually added to all target actor's ExternalConditions list.");
						RenameNodeKey(node, "GrantExternalConditionPower");
						ConvertUpgradesToCondition(parent, node, "Upgrades", "Condition");

						var soundNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "GrantUpgradeSound");
						if (soundNode != null)
							soundNode.Key = "OnFireSound";
						else
							node.Value.Nodes.Add(new MiniYamlNode("OnFireSound", "ironcur9.aud"));

						var sequenceNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "GrantUpgradeSequence");
						if (sequenceNode != null)
							sequenceNode.Key = "Sequence";
					}

					if (node.Key.StartsWith("GrantUpgradeCrateAction", StringComparison.Ordinal))
					{
						Console.WriteLine("GrantUpgradeCrateAction Condition must be manually added to all target actor's ExternalConditions list.");
						RenameNodeKey(node, "GrantExternalConditionCrateAction");
						ConvertUpgradesToCondition(parent, node, "Upgrades", "Condition");
					}
				}

				// Reworking bridge logic
				if (engineVersion < 20161210)
				{
					if (node.Key == "BridgeHut")
						RenameNodeKey(node, "LegacyBridgeHut");

					if (node.Key == "BridgeLayer")
						RenameNodeKey(node, "LegacyBridgeLayer");
				}

				// Removed WithBuildingExplosion
				if (engineVersion < 20161210)
				{
					if (node.Key == "WithBuildingExplosion")
					{
						node.Value.Nodes.Add(new MiniYamlNode("Type", "Footprint"));
						node.Value.Nodes.Add(new MiniYamlNode("Weapon", "UnitExplodeSmall"));
						node.Key = "Explodes";
						Console.WriteLine("The trait WithBuildingExplosion has been removed and superseded by additional 'Explodes' functionality.");
						Console.WriteLine("If you need a delayed building explosion, use 'Explodes' with 'Type: Footprint' and a cosmetic weapon with warhead delay.");
					}
				}

				if (engineVersion < 20161210)
				{
					// Migrated lua upgrades to conditions
					if (node.Key.StartsWith("ScriptUpgradesCache", StringComparison.Ordinal))
					{
						RenameNodeKey(node, "ExternalConditions");
						var conditions = node.Value.Nodes.FirstOrDefault(n => n.Key == "Upgrades");
						if (conditions != null)
							conditions.Key = "Conditions";
					}
				}

				if (engineVersion < 20161212)
				{
					if (node.Key.StartsWith("UpgradeActorsNear", StringComparison.Ordinal))
					{
						RenameNodeKey(node, "ProximityExternalCondition");
						ConvertUpgradesToCondition(parent, node, "Upgrades", "Condition");
					}

					if (node.Key == "Cargo")
						ConvertUpgradesToCondition(parent, node, "LoadingUpgrades", "LoadingCondition");

					if (node.Key == "Passenger" && node.Value.Nodes.Any(n => n.Key == "GrantUpgrades"))
					{
						Console.WriteLine("Passenger.GrantUpgrades support has been removed.");
						Console.WriteLine("Define passenger-conditions using Cargo.PassengerConditions on the transports instead.");
						node.Value.Nodes.RemoveAll(n => n.Key == "GrantUpgrades");
					}
				}

				if (engineVersion < 20161213)
				{
					if (node.Key == "Aircraft")
					{
						ConvertUpgradesToCondition(parent, node, "AirborneUpgrades", "AirborneCondition");
						ConvertUpgradesToCondition(parent, node, "CruisingUpgrades", "CruisingCondition");
					}

					if (node.Key.StartsWith("Cloak", StringComparison.Ordinal))
						ConvertUpgradesToCondition(parent, node, "WhileCloakedUpgrades", "CloakedCondition");

					if (node.Key == "Disguise")
					{
						ConvertUpgradesToCondition(parent, node, "Upgrades", "DisguisedCondition");
						if (!node.Value.Nodes.Any(n => n.Key == "DisguisedCondition"))
							node.Value.Nodes.Add(new MiniYamlNode("DisguisedCondition", "disguise"));
					}

					if (node.Key == "Parachutable")
					{
						ConvertUpgradesToCondition(parent, node, "ParachuteUpgrade", "ParachutingCondition");
						if (!node.Value.Nodes.Any(n => n.Key == "ParachutingCondition"))
							node.Value.Nodes.Add(new MiniYamlNode("ParachutingCondition", "parachute"));
					}

					if (node.Key == "PrimaryBuilding")
					{
						ConvertUpgradesToCondition(parent, node, "Upgrades", "PrimaryCondition");
						if (!node.Value.Nodes.Any(n => n.Key == "PrimaryCondition"))
							node.Value.Nodes.Add(new MiniYamlNode("PrimaryCondition", "primary"));
					}

					if (node.Key.StartsWith("UpgradeOnDamageState", StringComparison.Ordinal))
					{
						RenameNodeKey(node, "GrantConditionOnDamageState");
						ConvertUpgradesToCondition(parent, node, "Upgrades", "Condition");
					}

					if (node.Key.StartsWith("UpgradeOnMovement", StringComparison.Ordinal))
					{
						RenameNodeKey(node, "GrantConditionOnMovement");
						ConvertUpgradesToCondition(parent, node, "Upgrades", "Condition");
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

				// Rename LaserZap BeamDuration to just Duration
				if (engineVersion < 20161009)
				{
					if (node.Key == "BeamDuration")
						node.Key = "Duration";
				}

				// Rename Bullet Angle to LaunchAngle
				if (engineVersion < 20161016)
				{
					if (node.Key == "Angle")
						node.Key = "LaunchAngle";
				}

				if (engineVersion < 20161120)
				{
					if (node.Key.StartsWith("Warhead", StringComparison.Ordinal) && node.Value.Value == "GrantUpgrade")
					{
						node.Value.Value = "GrantExternalCondition";
						Console.WriteLine("GrantExternalCondition Condition must be manually added to all target actor's ExternalConditions list.");
						ConvertUpgradesToCondition(parent, node, "Upgrades", "Condition");
					}
				}

				UpgradeWeaponRules(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		static int RemapD2k106Sequence(int frame)
		{
			if (frame < 2518)
				return frame;
			if (frame < 3370)
				return frame + 248;
			if (frame < 4011)
				return frame + 253;
			if (frame < 4036)
				return frame + 261;
			return frame + 264;
		}

		internal static void UpgradeSequences(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				if (engineVersion < 20160730 && modData.Manifest.Id == "d2k" && depth == 2)
				{
					if (node.Key == "Start")
						node.Value.Value = RemapD2k106Sequence(FieldLoader.GetValue<int>("", node.Value.Value)).ToString();
					if (node.Key == "Frames")
						node.Value.Value = FieldLoader.GetValue<int[]>("", node.Value.Value)
							.Select(RemapD2k106Sequence).JoinWith(", ");
				}

				UpgradeSequences(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
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
				if (engineVersion < 20160619 && modData.Manifest.Id == "ra" && depth == 1)
				{
					var buildings = new List<string>() { "tsla", "gap", "agun", "apwr", "fapw" };
					if (buildings.Contains(parent.Value.Value) && node.Key == "Location")
						ModifyCPos(ref node.Value.Value, new CVec(0, 1));
				}

				// Fix TD building footprints to not use _ when it's not necessary
				if (engineVersion < 20160619 && modData.Manifest.Id == "cnc" && depth == 1)
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
