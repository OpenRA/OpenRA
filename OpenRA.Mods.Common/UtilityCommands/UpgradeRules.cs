#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	static class UpgradeRules
	{
		internal static void ConvertFloatToRange(ref string input)
		{
			var value = float.Parse(input);
			var cells = (int)value;
			var subcells = (int)(1024 * value) - 1024 * cells;

			input = "{0}c{1}".F(cells, subcells);
		}

		internal static void ConvertFloatArrayToPercentArray(ref string input)
		{
			input = string.Join(", ", input.Split(',')
				.Select(s => ((int)Math.Round(FieldLoader.GetValue<float>("(float value)", s) * 100)).ToString()));
		}

		internal static void ConvertFloatToIntPercentage(ref string input)
		{
			var value = float.Parse(input, CultureInfo.InvariantCulture);

			if (value <= 1)
				value = (int)Math.Round(value * 100, 0);
			else
				value = (int)Math.Round(value, 0);

			input = value.ToString();
		}

		internal static void ConvertPxToRange(ref string input)
		{
			ConvertPxToRange(ref input, 1, 1);
		}

		internal static void ConvertPxToRange(ref string input, int scaleMult, int scaleDiv)
		{
			var value = Exts.ParseIntegerInvariant(input);
			var ts = Game.ModData.Manifest.Get<MapGrid>().TileSize;
			var world = value * 1024 * scaleMult / (scaleDiv * ts.Height);
			var cells = world / 1024;
			var subcells = world - 1024 * cells;

			input = cells != 0 ? "{0}c{1}".F(cells, subcells) : subcells.ToString();
		}

		internal static void ConvertAngle(ref string input)
		{
			var value = float.Parse(input);
			input = WAngle.ArcTan((int)(value * 4 * 1024), 1024).ToString();
		}

		internal static void ConvertInt2ToWVec(ref string input)
		{
			var offset = FieldLoader.GetValue<int2>("(value)", input);
			var ts = Game.ModData.Manifest.Get<MapGrid>().TileSize;
			var world = new WVec(offset.X * 1024 / ts.Width, offset.Y * 1024 / ts.Height, 0);
			input = world.ToString();
		}

		internal static void RenameDamageTypes(MiniYamlNode damageTypes)
		{
			var mod = Game.ModData.Manifest.Mod.Id;
			if (mod == "cnc" || mod == "ra")
			{
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType1", "DefaultDeath");
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType2", "BulletDeath");
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType3", "SmallExplosionDeath");
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType4", "ExplosionDeath");
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType5", "FireDeath");
			}

			if (mod == "cnc")
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType6", "TiberiumDeath");

			if (mod == "ra")
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType6", "ElectricityDeath");

			if (mod == "d2k")
			{
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType1", "ExplosionDeath");
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType2", "SoundDeath");
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType3", "SmallExplosionDeath");
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType4", "BulletDeath");
			}

			if (mod == "ts")
			{
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType1", "BulletDeath");
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType2", "SmallExplosionDeath");
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType3", "ExplosionDeath");
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType5", "FireDeath");
				damageTypes.Value.Value = damageTypes.Value.Value.Replace("DeathType6", "EnergyDeath");
			}
		}

		internal static string RenameD2kActors(string name)
		{
			switch (name)
			{
				case "rifle": return "light_inf";
				case "bazooka": return "trooper";
				case "stealthraider": return "stealth_raider";
				case "combata": return "combat_tank_a";
				case "combath": return "combat_tank_h";
				case "combato": return "combat_tank_o";
				case "siegetank": return "siege_tank";
				case "missiletank": return "missile_tank";
				case "sonictank": return "sonic_tank";
				case "devast": return "devastator";
				case "deviatortank": return "deviator";
				case "orni": return "ornithopter";

				case "combata.starport": return "combat_tank_a.starport";
				case "combath.starport": return "combat_tank_h.starport";
				case "combato.starport": return "combat_tank_o.starport";
				case "siegetank.starport": return "siege_tank.starport";
				case "missiletank.starport": return "missile_tank.starport";

				case "conyard": return "construction_yard";
				case "power": return "wind_trap";
				case "light": return "light_factory";
				case "heavy": return "heavy_factory";
				case "guntower": return "medium_gun_turret";
				case "rockettower": return "large_gun_turret";
				case "research": return "research_centre";
				case "repair": return "repair_pad";
				case "radar": return "outpost";
				case "hightech": return "high_tech_factory";

				default: return name;
			}
		}

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

		internal static void UpgradeActorRules(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			var parentKey = parent != null ? parent.Key.Split('@').First() : null;

			foreach (var node in nodes)
			{
				// Weapon definitions were converted to world coordinates
				if (engineVersion < 20131226)
				{
					if (depth == 2 && parentKey == "Exit" && node.Key == "SpawnOffset")
						ConvertInt2ToWVec(ref node.Value.Value);

					if (depth == 2 && (parentKey == "Aircraft" || parentKey == "Helicopter" || parentKey == "Plane"))
					{
						if (node.Key == "CruiseAltitude")
							ConvertPxToRange(ref node.Value.Value);

						if (node.Key == "Speed")
							ConvertPxToRange(ref node.Value.Value, 7, 32);
					}

					if (depth == 2 && parentKey == "Mobile" && node.Key == "Speed")
						ConvertPxToRange(ref node.Value.Value, 1, 3);

					if (depth == 2 && parentKey == "Health" && node.Key == "Radius")
						ConvertPxToRange(ref node.Value.Value);
				}

				// CrateDrop was replaced with CrateSpawner
				if (engineVersion < 20131231)
				{
					if (depth == 1 && parentKey == "World")
					{
						if (node.Key == "CrateDrop")
							node.Key = "CrateSpawner";

						if (node.Key == "-CrateDrop")
							node.Key = "-CrateSpawner";
					}
				}

				// AttackTesla was replaced with AttackCharge
				if (engineVersion < 20140307)
				{
					if (depth == 1)
					{
						if (node.Key == "AttackTesla")
							node.Key = "AttackCharge";

						if (node.Key == "-AttackTesla")
							node.Key = "-AttackCharge";
					}
				}

				// AttackMove was generalized to support all movable actor types
				if (engineVersion < 20140116)
				{
					if (depth == 1 && node.Key == "AttackMove")
						node.Value.Nodes.RemoveAll(n => n.Key == "JustMove");
				}

				// UnloadFacing was removed from Cargo
				if (engineVersion < 20140212)
				{
					if (depth == 1 && node.Key == "Cargo")
						node.Value.Nodes.RemoveAll(n => n.Key == "UnloadFacing");
				}

				// RevealShroud was updated to use world units.
				if (engineVersion < 20140220)
				{
					if (depth == 2 && parentKey == "RevealsShroud" && node.Key == "Range")
						ConvertFloatToRange(ref node.Value.Value);

					if (depth == 2 && parentKey == "CreatesShroud" && node.Key == "Range")
						ConvertFloatToRange(ref node.Value.Value);
				}

				// Waypoint was renamed to Immobile
				if (engineVersion < 20140312)
				{
					if (depth == 1 && node.Key == "Waypoint")
						node.Key = "Immobile";
				}

				// Spy was renamed to Disguise
				if (engineVersion < 20140314)
				{
					if (depth == 1 && node.Key == "Spy")
						node.Key = "Disguise";

					if (depth == 1 && node.Key == "SpyToolTip")
						node.Key = "DisguiseToolTip";

					if (depth == 1 && node.Key == "RenderSpy")
						node.Key = "RenderDisguise";
				}

				// IOccupySpace was removed from Mine
				if (engineVersion < 20140320)
				{
					if (depth == 0 && node.Value.Nodes.Any(n => n.Key == "Mine"))
						node.Value.Nodes.Add(new MiniYamlNode("Immobile", new MiniYaml("", new List<MiniYamlNode>() { new MiniYamlNode("OccupiesSpace", "true") })));
					else
						foreach (var i in nodes.Where(n => n.Key == "Immobile"))
							if (!i.Value.Nodes.Any(n => n.Key == "OccupiesSpace"))
								i.Value.Nodes.Add(new MiniYamlNode("OccupiesSpace", "false"));
				}

				// Armaments and muzzleflashes were reworked to support garrisoning
				if (engineVersion < 20140321)
				{
					if (depth == 0)
					{
						var muzzles = node.Value.Nodes.Where(n => n.Key.StartsWith("WithMuzzleFlash"));
						var armaments = node.Value.Nodes.Where(n => n.Key.StartsWith("Armament"));

						// Shift muzzle flash definitions to Armament
						foreach (var m in muzzles)
						{
							var muzzleArmNode = m.Value.Nodes.SingleOrDefault(n => n.Key == "Armament");
							var muzzleSequenceNode = m.Value.Nodes.SingleOrDefault(n => n.Key == "Sequence");
							var muzzleSplitFacingsNode = m.Value.Nodes.SingleOrDefault(n => n.Key == "SplitFacings");
							var muzzleFacingsCountNode = m.Value.Nodes.SingleOrDefault(n => n.Key == "FacingCount");

							var muzzleArmName = muzzleArmNode != null ? muzzleArmNode.Value.Value.Trim() : "primary";
							var muzzleSequence = muzzleSequenceNode != null ? muzzleSequenceNode.Value.Value.Trim() : "muzzle";
							var muzzleSplitFacings = muzzleSplitFacingsNode != null ? FieldLoader.GetValue<bool>("SplitFacings", muzzleSplitFacingsNode.Value.Value) : false;
							var muzzleFacingsCount = muzzleFacingsCountNode != null ? FieldLoader.GetValue<int>("FacingsCount", muzzleFacingsCountNode.Value.Value) : 8;

							foreach (var a in armaments)
							{
								var armNameNode = m.Value.Nodes.SingleOrDefault(n => n.Key == "Name");
								var armName = armNameNode != null ? armNameNode.Value.Value.Trim() : "primary";

								if (muzzleArmName == armName)
								{
									a.Value.Nodes.Add(new MiniYamlNode("MuzzleSequence", muzzleSequence));
									if (muzzleSplitFacings)
										a.Value.Nodes.Add(new MiniYamlNode("MuzzleSplitFacings", muzzleFacingsCount.ToString()));
								}
							}
						}

						foreach (var m in muzzles.ToList().Skip(1))
							node.Value.Nodes.Remove(m);
					}

					// Remove all but the first muzzle flash definition
					if (depth == 1 && node.Key.StartsWith("WithMuzzleFlash"))
					{
						node.Key = "WithMuzzleFlash";
						node.Value.Nodes.RemoveAll(n => n.Key == "Armament");
						node.Value.Nodes.RemoveAll(n => n.Key == "Sequence");
					}
				}

				// "disabled" palette overlay has been moved into it's own DisabledOverlay trait
				if (engineVersion < 20140305)
				{
					if (node.Value.Nodes.Any(n => n.Key.StartsWith("RequiresPower"))
						&& !node.Value.Nodes.Any(n => n.Key.StartsWith("DisabledOverlay")))
						node.Value.Nodes.Add(new MiniYamlNode("DisabledOverlay", new MiniYaml("")));
				}

				// ChronoshiftDeploy was replaced with PortableChrono
				if (engineVersion < 20140321)
				{
					if (depth == 1 && node.Key == "ChronoshiftDeploy")
						node.Key = "PortableChrono";

					if (depth == 2 && parentKey == "PortableChrono" && node.Key == "JumpDistance")
						node.Key = "MaxDistance";
				}

				// Added new Lua API
				if (engineVersion < 20140421)
				{
					if (depth == 0 && node.Value.Nodes.Any(n => n.Key == "LuaScriptEvents"))
						node.Value.Nodes.Add(new MiniYamlNode("ScriptTriggers", ""));
				}

				if (engineVersion < 20140517)
				{
					if (depth == 0)
						node.Value.Nodes.RemoveAll(n => n.Key == "TeslaInstantKills");
				}

				if (engineVersion < 20140615)
				{
					if (depth == 1 && node.Key == "StoresOre")
						node.Key = "StoresResources";
				}

				// make animation is now its own trait
				if (engineVersion < 20140621)
				{
					if (depth == 1 && node.Key.StartsWith("RenderBuilding"))
						node.Value.Nodes.RemoveAll(n => n.Key == "HasMakeAnimation");

					if (node.Value.Nodes.Any(n => n.Key.StartsWith("RenderBuilding"))
						&& !node.Value.Nodes.Any(n => n.Key == "RenderBuildingWall")
						&& !node.Value.Nodes.Any(n => n.Key == "WithMakeAnimation"))
						node.Value.Nodes.Add(new MiniYamlNode("WithMakeAnimation", new MiniYaml("")));
				}

				// ParachuteAttachment was merged into Parachutable
				if (engineVersion < 20140701)
				{
					if (depth == 1 && node.Key == "ParachuteAttachment")
					{
						node.Key = "Parachutable";

						foreach (var subnode in node.Value.Nodes)
							if (subnode.Key == "Offset")
								subnode.Key = "ParachuteOffset";
					}

					if (depth == 2 && node.Key == "ParachuteSprite")
						node.Key = "ParachuteSequence";
				}

				// SonarPulsePower was implemented as a generic SpawnActorPower
				if (engineVersion < 20140703)
				{
					if (depth == 1 && node.Key == "SonarPulsePower")
						node.Key = "SpawnActorPower";
				}

				if (engineVersion < 20140707)
				{
					// SpyPlanePower was removed (use AirstrikePower instead)
					if (depth == 1 && node.Key == "SpyPlanePower")
					{
						node.Key = "AirstrikePower";

						var revealTime = 6 * 25;
						var revealNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "RevealTime");
						if (revealNode != null)
						{
							revealTime = int.Parse(revealNode.Value.Value) * 25;
							node.Value.Nodes.Remove(revealNode);
						}

						node.Value.Nodes.Add(new MiniYamlNode("CameraActor", new MiniYaml("camera")));
						node.Value.Nodes.Add(new MiniYamlNode("CameraRemoveDelay", new MiniYaml(revealTime.ToString())));
						node.Value.Nodes.Add(new MiniYamlNode("UnitType", new MiniYaml("u2")));
					}

					if (depth == 2 && node.Key == "LZRange" && parentKey == "ParaDrop")
					{
						node.Key = "DropRange";
						ConvertFloatToRange(ref node.Value.Value);
					}
				}

				// GiveUnitCrateAction and GiveMcvCrateAction were updated to allow multiple units
				if (engineVersion < 20140723)
				{
					if (depth == 2 && !string.IsNullOrEmpty(parentKey))
					{
						if (parentKey.Contains("GiveMcvCrateAction"))
							if (node.Key == "Unit")
								node.Key = "Units";

						if (parentKey.Contains("GiveUnitCrateAction"))
							if (node.Key == "Unit")
								node.Key = "Units";
					}
				}

				// Power from Building was moved out into Power and ScalePowerWithHealth traits
				if (engineVersion < 20140823)
				{
					if (depth == 0)
					{
						var actorTraits = node.Value.Nodes;
						var building = actorTraits.FirstOrDefault(t => t.Key == "Building");
						if (building != null)
						{
							var buildingFields = building.Value.Nodes;
							var power = buildingFields.FirstOrDefault(n => n.Key == "Power");
							if (power != null)
							{
								buildingFields.Remove(power);

								var powerFields = new List<MiniYamlNode> { new MiniYamlNode("Amount", power.Value) };
								actorTraits.Add(new MiniYamlNode("Power", new MiniYaml("", powerFields)));

								if (FieldLoader.GetValue<int>("Power", power.Value.Value) > 0)
									actorTraits.Add(new MiniYamlNode("ScaleWithHealth", ""));
							}
						}
					}
				}

				if (engineVersion < 20140803)
				{
					// ContainsCrate was removed (use LeavesHusk instead)
					if (depth == 1 && node.Key == "ContainsCrate")
					{
						node.Key = "LeavesHusk";
						node.Value.Nodes.Add(new MiniYamlNode("HuskActor", new MiniYaml("crate")));
					}
				}

				if (engineVersion < 20140806)
				{
					// remove ConquestVictoryConditions when StrategicVictoryConditions is set
					if (depth == 0 && node.Key == "Player" && node.Value.Nodes.Any(n => n.Key == "StrategicVictoryConditions"))
						node.Value.Nodes.Add(new MiniYamlNode("-ConquestVictoryConditions", ""));

					// the objectives panel trait and its properties have been renamed
					if (depth == 1 && node.Key == "ConquestObjectivesPanel")
					{
						node.Key = "ObjectivesPanel";
						node.Value.Nodes.RemoveAll(_ => true);
						node.Value.Nodes.Add(new MiniYamlNode("PanelName", new MiniYaml("SKIRMISH_STATS")));
					}
				}

				// Veterancy was changed to use the upgrades system
				if (engineVersion < 20140807)
				{
					if (depth == 0 && node.Value.Nodes.Any(n => n.Key.StartsWith("GainsExperience")))
						node.Value.Nodes.Add(new MiniYamlNode("GainsStatUpgrades", new MiniYaml("")));

					if (depth == 1 && node.Key == "-CloakCrateAction")
						node.Key = "-UnitUpgradeCrateAction@cloak";

					if (depth == 1 && node.Key == "CloakCrateAction")
					{
						node.Key = "UnitUpgradeCrateAction@cloak";
						node.Value.Nodes.Add(new MiniYamlNode("Upgrades", new MiniYaml("cloak")));
					}

					if (depth == 2 && node.Key == "RequiresCrate" && parentKey == "Cloak")
					{
						node.Key = "RequiresUpgrade";
						node.Value.Value = "cloak";
					}
				}

				// Modifiers were changed to integer percentages
				if (engineVersion < 20140812)
				{
					if (depth == 2 && node.Key == "ClosedDamageMultiplier" && parentKey == "AttackPopupTurreted")
						ConvertFloatArrayToPercentArray(ref node.Value.Value);

					if (depth == 2 && node.Key == "ArmorModifier" && parentKey == "GainsStatUpgrades")
						ConvertFloatArrayToPercentArray(ref node.Value.Value);

					if (depth == 2 && node.Key == "FullyLoadedSpeed" && parentKey == "Harvester")
						ConvertFloatArrayToPercentArray(ref node.Value.Value);

					if (depth == 2 && node.Key == "PanicSpeedModifier" && parentKey == "ScaredyCat")
						ConvertFloatArrayToPercentArray(ref node.Value.Value);

					if (depth == 2 && node.Key == "ProneSpeed" && parentKey == "TakeCover")
					{
						node.Key = "SpeedModifier";
						ConvertFloatArrayToPercentArray(ref node.Value.Value);
					}

					if (depth == 2 && node.Key == "SpeedModifier" && parentKey == "GainsStatUpgrades")
						ConvertFloatArrayToPercentArray(ref node.Value.Value);

					if (depth == 2 && node.Key == "FirepowerModifier" && parentKey == "GainsStatUpgrades")
						ConvertFloatArrayToPercentArray(ref node.Value.Value);
				}

				// RemoveImmediately was replaced with RemoveOnConditions
				if (engineVersion < 20140821)
				{
					if (depth == 1)
					{
						if (node.Key == "RemoveImmediately")
							node.Key = "RemoveOnConditions";

						if (node.Key == "-RemoveImmediately")
							node.Key = "-RemoveOnConditions";
					}
				}

				if (engineVersion < 20140823)
				{
					if (depth == 2 && node.Key == "ArmorUpgrade" && parentKey == "GainsStatUpgrades")
						node.Key = "DamageUpgrade";

					if (depth == 2 && node.Key == "ArmorModifier" && parentKey == "GainsStatUpgrades")
					{
						node.Key = "DamageModifier";
						node.Value.Value = string.Join(", ", node.Value.Value.Split(',')
							.Select(s => ((int)(100 * 100 / float.Parse(s))).ToString()));
					}

					if (depth == 3 && parentKey == "Upgrades")
						node.Value.Value = node.Value.Value.Replace("armor", "damage");
				}

				// RenderInfantryProne and RenderInfantryPanic was merged into RenderInfantry
				if (engineVersion < 20140824)
				{
					var renderInfantryRemoval = node.Value.Nodes.FirstOrDefault(n => n.Key == "-RenderInfantry");
					if (depth == 0 && renderInfantryRemoval != null && !node.Value.Nodes.Any(n => n.Key == "RenderDisguise"))
						node.Value.Nodes.Remove(renderInfantryRemoval);

					if (depth == 1 && (node.Key == "RenderInfantryProne" || node.Key == "RenderInfantryPanic"))
						node.Key = "RenderInfantry";
				}

				// InfDeath was renamed to DeathType
				if (engineVersion < 20140830)
				{
					if (depth == 2 && parentKey.StartsWith("DeathSounds") && node.Key == "InfDeaths")
						node.Key = "DeathTypes";

					if (depth == 2 && parentKey == "SpawnViceroid" && node.Key == "InfDeath")
						node.Key = "DeathType";

					if (depth == 2 && parentKey == "Explodes" && node.Key == "InfDeath")
						node.Key = "DeathType";
				}

				// SellSounds from Building was moved into Sellable
				if (engineVersion < 20140904)
				{
					if (depth == 0)
					{
						var actorTraits = node.Value.Nodes;
						var building = actorTraits.FirstOrDefault(t => t.Key == "Building");
						if (building != null)
						{
							var buildingFields = building.Value.Nodes;
							var sellSounds = buildingFields.FirstOrDefault(n => n.Key == "SellSounds");
							if (sellSounds != null)
							{
								buildingFields.Remove(sellSounds);
								var sellable = actorTraits.FirstOrDefault(t => t.Key == "Sellable");
								if (sellable != null)
									sellable.Value.Nodes.Add(sellSounds);
								else
								{
									Console.WriteLine("Warning: Adding Sellable trait to {0} in {1}".F(node.Key, node.Location.Filename));
									actorTraits.Add(new MiniYamlNode("Sellable", new MiniYaml("", new List<MiniYamlNode> { sellSounds })));
								}
							}
						}
					}
				}

				// DuplicateUnitCrateAction was tidied up
				if (engineVersion < 20140912)
				{
					if (depth == 2 && node.Key == "MaxDuplicatesWorth" && parentKey == "DuplicateUnitCrateAction")
						node.Key = "MaxDuplicateValue";

					if (depth == 2 && node.Key == "ValidDuplicateTypes" && parentKey == "DuplicateUnitCrateAction")
						node.Key = "ValidTargets";
				}

				// Added WithDeathAnimation
				if (engineVersion < 20140913)
				{
					var spawnsCorpseRemoval = node.Value.Nodes.FirstOrDefault(n => n.Key == "SpawnsCorpse");

					if (depth == 0 && node.Value.Nodes.Any(n => n.Key.StartsWith("RenderInfantry")) && spawnsCorpseRemoval == null)
						node.Value.Nodes.Add(new MiniYamlNode("WithDeathAnimation", new MiniYaml("")));

					if (depth == 2 && node.Key == "SpawnsCorpse" && parentKey == "RenderInfantry")
						node.Value.Nodes.Remove(spawnsCorpseRemoval);

					// CrushableInfantry renamed to Crushable
					if (depth == 1)
					{
						if (node.Key == "CrushableInfantry")
							node.Key = "Crushable";

						if (node.Key == "-CrushableInfantry")
							node.Key = "-Crushable";
					}
				}

				// Replaced Wall with Crushable + BlocksBullets
				if (engineVersion < 20140914)
				{
					if (depth == 0)
					{
						var actorTraits = node.Value.Nodes;
						var wall = actorTraits.FirstOrDefault(t => t.Key == "Wall");
						if (wall != null)
							node.Value.Nodes.Add(new MiniYamlNode("BlocksBullets", new MiniYaml("")));

						var blocksBullets = actorTraits.FirstOrDefault(t => t.Key == "BlocksBullets");
						if (depth == 1 && node.Key == "Wall" && blocksBullets != null)
							node.Key = "Crushable";
					}
				}

				if (engineVersion < 20140927)
				{
					if (depth == 0)
						node.Value.Nodes.RemoveAll(n => n.Key == "SelfHealingTech");

					if (depth == 2 && node.Key == "RequiresTech" && parentKey.StartsWith("SelfHealing"))
					{
						node.Key = "RequiresUpgrade";
						node.Value.Value = "selfhealing-needs-reconfiguration";
					}
				}

				if (engineVersion < 20141001)
				{
					// Routed unit upgrades via the UnitUpgradeManager trait
					if (depth == 0 && node.Value.Nodes.Any(n => n.Key.StartsWith("GainsStatUpgrades")))
						node.Value.Nodes.Add(new MiniYamlNode("UnitUpgradeManager", new MiniYaml("")));

					// Replaced IronCurtainPower -> GrantUpgradePower
					if (depth == 1 && node.Key == "IronCurtainPower")
					{
						node.Key = "GrantUpgradePower@IRONCURTAIN";
						node.Value.Nodes.Add(new MiniYamlNode("Upgrades", "invulnerability"));

						var durationNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "Duration");
						if (durationNode != null)
							durationNode.Value.Value = (int.Parse(durationNode.Value.Value) * 25).ToString();
						else
							node.Value.Nodes.Add(new MiniYamlNode("Duration", "600"));

						var soundNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "IronCurtainSound");
						if (soundNode != null)
							soundNode.Key = "GrantUpgradeSound";
					}

					if (depth == 0 && node.Value.Nodes.Any(n => n.Key.StartsWith("IronCurtainable")))
					{
						node.Value.Nodes.RemoveAll(n => n.Key.StartsWith("IronCurtainable"));

						var overlayKeys = new List<MiniYamlNode>();
						overlayKeys.Add(new MiniYamlNode("RequiresUpgrade", "invulnerability"));
						node.Value.Nodes.Add(new MiniYamlNode("UpgradeOverlay@IRONCURTAIN", new MiniYaml("", overlayKeys)));

						var invulnKeys = new List<MiniYamlNode>();
						invulnKeys.Add(new MiniYamlNode("RequiresUpgrade", "invulnerability"));
						node.Value.Nodes.Add(new MiniYamlNode("InvulnerabilityUpgrade@IRONCURTAIN", new MiniYaml("", invulnKeys)));

						var barKeys = new List<MiniYamlNode>();
						barKeys.Add(new MiniYamlNode("Upgrade", "invulnerability"));
						node.Value.Nodes.Add(new MiniYamlNode("TimedUpgradeBar", new MiniYaml("", barKeys)));

						if (!node.Value.Nodes.Any(n => n.Key.StartsWith("UnitUpgradeManager")))
							node.Value.Nodes.Add(new MiniYamlNode("UnitUpgradeManager", new MiniYaml("")));
					}

					if (depth == 1 && node.Key == "-IronCurtainable")
						node.Key = "-InvulnerabilityUpgrade@IRONCURTAIN";

					// Replaced RemoveOnConditions with KillsSelf
					if (depth == 1 && node.Key == "RemoveOnConditions")
					{
						node.Key = "KillsSelf";
						node.Value.Nodes.Add(new MiniYamlNode("RemoveInstead", new MiniYaml("true")));
					}

					if (depth == 1 && node.Key.StartsWith("UnitUpgradeCrateAction"))
					{
						var parts = node.Key.Split('@');
						node.Key = "GrantUpgradeCrateAction";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					if (depth == 1 && node.Key.StartsWith("-UnitUpgradeCrateAction"))
					{
						var parts = node.Key.Split('@');
						node.Key = "-GrantUpgradeCrateAction";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}
				}

				if (engineVersion < 20141002)
				{
					if (node.Key == "AlignWhenIdle" && parentKey == "Turreted")
					{
						node.Key = "RealignDelay";
						node.Value.Value = "0";
					}
				}

				// Replaced BelowUnits with per sequence ZOffsets
				if (engineVersion < 20141030)
				{
					if (depth == 0)
					{
						node.Value.Nodes.RemoveAll(n => n.Key == "BelowUnits");
						node.Value.Nodes.RemoveAll(n => n.Key == "-BelowUnits");
					}
				}

				if (engineVersion < 20141121)
				{
					if (depth == 1)
					{
						if (node.Value.Nodes.Exists(n => n.Key == "RestrictedByUpgrade"))
						{
							node.Value.Nodes.Add(new MiniYamlNode("UpgradeMaxEnabledLevel", "0"));
							node.Value.Nodes.Add(new MiniYamlNode("UpgradeMaxAcceptedLevel", "1"));
						}
						else if (node.Value.Nodes.Exists(n => n.Key == "RequiresUpgrade"))
							node.Value.Nodes.Add(new MiniYamlNode("UpgradeMinEnabledLevel", "1"));

						if (node.Key.StartsWith("DisableUpgrade") && !node.Value.Nodes.Any(n => n.Key == "RequiresUpgrade" || n.Key == "UpgradeTypes"))
							node.Value.Nodes.Add(new MiniYamlNode("UpgradeTypes", "disable"));

						if (node.Key.StartsWith("InvulnerabilityUpgrade") && !node.Value.Nodes.Any(n => n.Key == "RequiresUpgrade" || n.Key == "UpgradeTypes"))
							node.Value.Nodes.Add(new MiniYamlNode("UpgradeTypes", "invulnerability"));
					}
					else if (depth == 2)
					{
						if (node.Key == "RequiresUpgrade" || node.Key == "RestrictedByUpgrade")
							node.Key = "UpgradeTypes";
						else if (node.Key == "-RequiresUpgrade" || node.Key == "-RestrictedByUpgrade")
							node.Key = "-UpgradeTypes";
					}
				}

				// Adjust MustBeDestroyed for short games
				if (engineVersion < 20141218)
					if (depth == 1 && node.Key == "MustBeDestroyed")
						node.Value.Nodes.Add(new MiniYamlNode("RequiredForShortGame", "true"));

				if (engineVersion < 20150125)
				{
					// Remove PlayMusicOnMapLoad
					if (depth == 0 && node.Value.Nodes.Exists(n => n.Key == "PlayMusicOnMapLoad"))
					{
						node.Value.Nodes.RemoveAll(n => n.Key == "PlayMusicOnMapLoad");
						Console.WriteLine("The 'PlayMusicOnMapLoad' trait has been removed.");
						Console.WriteLine("Please use the Lua API function 'PlayMusic' instead.");
						Console.WriteLine("See http://wiki.openra.net/Lua-API for details.");
					}

					// Remove TiberiumRefinery and OreRefinery
					if (node.Key == "TiberiumRefinery" || node.Key == "OreRefinery")
						node.Key = "Refinery";
				}

				// Append an 's' as the fields were changed from string to string[]
				if (engineVersion < 20150311)
				{
					if (depth == 2 && parentKey == "SoundOnDamageTransition")
					{
						if (node.Key == "DamagedSound")
							node.Key = "DamagedSounds";
						else if (node.Key == "DestroyedSound")
							node.Key = "DestroyedSounds";
					}
				}

				if (engineVersion < 20150321)
				{
					// Note: These rules are set up to do approximately the right thing for maps, but
					// mods need additional manual tweaks. This is the best we can do without having
					// much smarter rules parsing, because we currently can't reason about inherited traits.
					if (depth == 0)
					{
						var childKeys = new[] { "MinIdleWaitTicks", "MaxIdleWaitTicks", "MoveAnimation", "AttackAnimation", "IdleAnimations", "StandAnimations" };

						var ri = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderInfantry"));
						if (ri != null)
						{
							ri.Key = "WithInfantryBody";

							var rsNodes = ri.Value.Nodes.Where(n => !childKeys.Contains(n.Key)).ToList();
							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));

							ri.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));
						}

						var rri = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderInfantry"));
						if (rri != null)
							rri.Key = "-WithInfantryBody";

						var rdi = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderDisguise"));
						if (rdi != null)
						{
							rdi.Key = "WithDisguisingInfantryBody";

							var rsNodes = rdi.Value.Nodes.Where(n => !childKeys.Contains(n.Key)).ToList();
							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));

							rdi.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));
						}

						var rrdi = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderDisguise"));
						if (rrdi != null)
							rrdi.Key = "-WithDisguisingInfantryBody";
					}

					if (depth == 2 && node.Key == "MoveAnimation")
						node.Key = "MoveSequence";

					if (depth == 2 && node.Key == "AttackAnimation")
						node.Key = "AttackSequence";

					if (depth == 2 && node.Key == "IdleAnimations")
						node.Key = "IdleSequences";

					if (depth == 2 && node.Key == "StandAnimations")
						node.Key = "StandSequences";
				}

				if (engineVersion < 20150323)
				{
					// Moved Reloads functionality to LimitedAmmo and refactored the latter into AmmoPool
					if (depth == 0)
					{
						var actorTraits = node.Value.Nodes;
						var limitedAmmo = actorTraits.FirstOrDefault(l => l.Key == "LimitedAmmo");
						var reloads = actorTraits.FirstOrDefault(r => r.Key == "Reloads");

						if (reloads != null)
						{
							var reloadsFields = reloads.Value.Nodes;
							var limitedAmmoFields = limitedAmmo.Value.Nodes;
							var count = reloadsFields.FirstOrDefault(c => c.Key == "Count");
							var period = reloadsFields.FirstOrDefault(p => p.Key == "Period");
							var resets = reloadsFields.FirstOrDefault(res => res.Key == "ResetOnFire");

							var reloadsCount = count != null ? FieldLoader.GetValue<int>("Count", count.Value.Value) : -1;
							var reloadsPeriod = period != null ? FieldLoader.GetValue<int>("Period", period.Value.Value) : 50;
							var reloadsResetOnFire = resets != null ? FieldLoader.GetValue<bool>("ResetOnFire", resets.Value.Value) : false;

							limitedAmmoFields.Add(new MiniYamlNode("SelfReloads", "true"));
							limitedAmmoFields.Add(new MiniYamlNode("ReloadCount", reloadsCount.ToString()));
							limitedAmmoFields.Add(new MiniYamlNode("SelfReloadTicks", reloadsPeriod.ToString()));
							limitedAmmoFields.Add(new MiniYamlNode("ResetOnFire", reloadsResetOnFire.ToString()));

							node.Value.Nodes.RemoveAll(n => n.Key == "Reloads");
							node.Value.Nodes.RemoveAll(n => n.Key == "-Reloads");
						}
					}

					// Moved RearmSound from Minelayer to LimitedAmmo/AmmoPool
					if (depth == 0)
					{
						var actorTraits = node.Value.Nodes;
						var limitedAmmo = actorTraits.FirstOrDefault(la => la.Key == "LimitedAmmo");
						var minelayer = actorTraits.FirstOrDefault(ml => ml.Key == "Minelayer");

						if (minelayer != null)
						{
							var minelayerFields = minelayer.Value.Nodes;
							var limitedAmmoFields = limitedAmmo.Value.Nodes;
							var rearmSound = minelayerFields.FirstOrDefault(rs => rs.Key == "RearmSound");
							var minelayerRearmSound = rearmSound != null ? FieldLoader.GetValue<string>("RearmSound", rearmSound.Value.Value) : "minelay1.aud";

							limitedAmmoFields.Add(new MiniYamlNode("RearmSound", minelayerRearmSound));
							minelayerFields.Remove(rearmSound);
						}
					}

					// Rename LimitedAmmo to AmmoPool
					if (node.Key == "LimitedAmmo")
						node.Key = "AmmoPool";
				}

				if (engineVersion < 20150326)
				{
					// Rename BlocksBullets to BlocksProjectiles
					if (node.Key == "BlocksBullets")
						node.Key = "BlocksProjectiles";
				}

				if (engineVersion < 20150425)
				{
					if (depth == 0)
					{
						var warFact = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderBuildingWarFactory"));
						if (warFact != null)
						{
							warFact.Key = "RenderBuilding";

							if (node.Value.Nodes.Any(w => w.Key == "-RenderBuilding"))
								node.Value.Nodes.RemoveAll(p => p.Key == "-RenderBuilding");

							var doorOverlay = new MiniYamlNode("WithProductionDoorOverlay", "");
							doorOverlay.Value.Nodes.Add(new MiniYamlNode("Sequence", "build-top"));
							node.Value.Nodes.Add(doorOverlay);
						}
					}
				}

				if (engineVersion < 20150426)
				{
					// Add DamageModifiers to TakeCover with a "Prone50Percent" default
					// Add ProneTriggers to TakeCover with a "TriggerProne" default
					if (node.Key == "TakeCover")
					{
						var percent = new MiniYamlNode("Prone50Percent", "50");
						var dictionary = new MiniYamlNode("DamageModifiers", "");
						dictionary.Value.Nodes.Add(percent);

						if (node.Value.Nodes.All(x => x.Key != "DamageModifiers"))
							node.Value.Nodes.Add(dictionary);

						node.Value.Nodes.Add(new MiniYamlNode("DamageTriggers", "TriggerProne"));
					}
				}

				if (engineVersion < 20150427)
					if (node.Key.StartsWith("WithRotor"))
						node.Value.Nodes.RemoveAll(p => p.Key == "Id");

				if (engineVersion < 20150430)
				{
					if (node.Key.StartsWith("ProductionQueue@") || node.Key.StartsWith("ClassicProductionQueue@"))
						node.Value.Nodes.RemoveAll(n => n.Key == "RequireOwner");

					if (node.Key == "Buildable")
					{
						var removed = node.Value.Nodes.RemoveAll(n => n.Key == "Owner");
						if (removed > 0)
						{
							Console.WriteLine("The 'Owner' field has been removed.");
							Console.WriteLine("Please use prerequisites instead.");
						}
					}
				}

				if (engineVersion < 20150501)
				{
					// Change RenderFlare to RenderSprites + WithSpriteBody
					var flares = node.Value.Nodes.Where(x => x.Key == "RenderFlare");
					if (flares.Any())
					{
						flares.Do(x => x.Key = "RenderSprites");
						node.Value.Nodes.Add(new MiniYamlNode("WithSpriteBody", "", new List<MiniYamlNode>
						{
							new MiniYamlNode("StartSequence", "open")
						}));
					}

					// Change WithFire to RenderSprites + WithSpriteBody
					var fire = node.Value.Nodes.Where(x => x.Key == "WithFire");
					if (fire.Any())
					{
						fire.Do(x => x.Key = "RenderSprites");
						node.Value.Nodes.Add(new MiniYamlNode("WithSpriteBody", "", new List<MiniYamlNode>
						{
							new MiniYamlNode("StartSequence", "fire-start"),
							new MiniYamlNode("Sequence", "fire-loop")
						}));
					}
				}

				if (engineVersion < 20150504)
				{
					// Made buildings grant prerequisites explicitly.
					if (depth == 0 && node.Value.Nodes.Exists(n => n.Key == "Inherits" &&
						(n.Value.Value == "^Building" || n.Value.Value == "^BaseBuilding")))
						node.Value.Nodes.Add(new MiniYamlNode("ProvidesCustomPrerequisite@buildingname", ""));

					// Rename the ProvidesCustomPrerequisite trait.
					if (node.Key.StartsWith("ProvidesCustomPrerequisite"))
						node.Key = node.Key.Replace("ProvidesCustomPrerequisite", "ProvidesPrerequisite");
				}

				if (engineVersion < 20150509)
				{
					if (depth == 0 && node.Value.Nodes.Exists(n => n.Key == "Selectable"))
					{
						var selectable = node.Value.Nodes.FirstOrDefault(n => n.Key == "Selectable");
						var selectableNodes = selectable.Value.Nodes;
						var voice = selectableNodes.FirstOrDefault(n => n.Key == "Voice");
						var selectableVoice = voice != null ? FieldLoader.GetValue<string>("Voice", voice.Value.Value) : "";

						if (voice != null)
						{
							node.Value.Nodes.Add(new MiniYamlNode("Voiced", "", new List<MiniYamlNode>
							{
								new MiniYamlNode("VoiceSet", selectableVoice),
							}));
						}
					}

					if (node.Key.StartsWith("Selectable"))
						node.Value.Nodes.RemoveAll(p => p.Key == "Voice");
				}

				if (engineVersion < 20150524)
				{
					// Replace numbers with strings for DeathSounds.DeathType
					if (node.Key.StartsWith("DeathSounds"))
					{
						var deathTypes = node.Value.Nodes.FirstOrDefault(x => x.Key == "DeathTypes");
						if (deathTypes != null)
						{
							var types = FieldLoader.GetValue<string[]>("DeathTypes", deathTypes.Value.Value);
							deathTypes.Value.Value = string.Join(", ", types.Select(type => "DeathType" + type));

							RenameDamageTypes(deathTypes);
						}
					}
				}

				if (engineVersion < 20150528)
				{
					// Note (stolen from WithInfantryBody upgrade rule):
					// These rules are set up to do approximately the right thing for maps, but
					// mods need additional manual tweaks. This is the best we can do without having
					// much smarter rules parsing, because we currently can't reason about inherited traits.
					if (depth == 0)
					{
						var childKeys = new[] { "Sequence" };

						var ru = node.Value.Nodes.FirstOrDefault(n => n.Key == "RenderUnit");
						if (ru != null)
						{
							ru.Key = "WithFacingSpriteBody";
							node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));

							var rsNodes = ru.Value.Nodes.Where(n => !childKeys.Contains(n.Key)).ToList();
							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", ""));

							ru.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));
						}

						var rru = node.Value.Nodes.FirstOrDefault(n => n.Key == "-RenderUnit");
						if (rru != null)
							rru.Key = "-WithFacingSpriteBody";
					}

					// For RenderUnitReload
					var rur = node.Value.Nodes.Where(x => x.Key == "RenderUnitReload");
					if (rur.Any())
					{
						rur.Do(x => x.Key = "RenderSprites");
						node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));
						node.Value.Nodes.Add(new MiniYamlNode("WithFacingSpriteBody", "", new List<MiniYamlNode>
						{
							new MiniYamlNode("Sequence", "idle")
						}));
						node.Value.Nodes.Add(new MiniYamlNode("WithAttackAnimation", "", new List<MiniYamlNode>
						{
							new MiniYamlNode("AimSequence", "aim"),
							new MiniYamlNode("ReloadPrefix", "empty-")
						}));

						var rrur = node.Value.Nodes.FirstOrDefault(n => n.Key == "-RenderUnitReload");
						if (rrur != null)
							rrur.Key = "-WithFacingSpriteBody";
					}

					// For RenderUnitFlying
					var ruf = node.Value.Nodes.Where(x => x.Key == "RenderUnitFlying");
					if (ruf.Any())
					{
						ruf.Do(x => x.Key = "RenderSprites");
						node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));
						node.Value.Nodes.Add(new MiniYamlNode("WithFacingSpriteBody", ""));
						node.Value.Nodes.Add(new MiniYamlNode("WithMoveAnimation", "", new List<MiniYamlNode>
						{
							new MiniYamlNode("MoveSequence", "move")
						}));

						var rruf = node.Value.Nodes.FirstOrDefault(n => n.Key == "-RenderUnitFlying");
						if (rruf != null)
							rruf.Key = "-WithFacingSpriteBody";
					}
				}

				if (engineVersion < 20150607)
				{
					// Add WithRankDecoration to all actors using GainsExperience
					var ge = node.Value.Nodes.FirstOrDefault(n => n.Key == "GainsExperience");
					if (ge != null)
					{
						var nodeUpgrades = ge.Value.Nodes.FirstOrDefault(n => n.Key == "Upgrades");
						var upgrades = nodeUpgrades != null ? nodeUpgrades.Value.Nodes.Count() : 4;

						var nodeChPal = ge.Value.Nodes.FirstOrDefault(n => n.Key == "ChevronPalette");
						var chPal = nodeChPal != null && !string.IsNullOrEmpty(nodeChPal.Value.Value) ? nodeChPal.Value.Value : "effect";
						ge.Value.Nodes.Remove(nodeChPal);

						if (upgrades != 0 && nodeUpgrades != null)
						{
							foreach (var nodeUpgrade in nodeUpgrades.Value.Nodes)
								nodeUpgrade.Value.Value = "rank" + (string.IsNullOrEmpty(nodeUpgrade.Value.Value) ? null : ", ") + nodeUpgrade.Value.Value;

							node.Value.Nodes.Add(new MiniYamlNode("WithRankDecoration", null, new List<MiniYamlNode>
							{
								new MiniYamlNode("Image", "rank"),
								new MiniYamlNode("Sequence", "rank"),
								new MiniYamlNode("Palette", chPal),
								new MiniYamlNode("ReferencePoint", "Bottom, Right"),
								new MiniYamlNode("Offset", "2, 2"),
								new MiniYamlNode("UpgradeTypes", "rank"),
								new MiniYamlNode("ZOffset", "256"),
								new MiniYamlNode("UpgradeMinEnabledLevel", "1"),
								new MiniYamlNode("UpgradeMaxAcceptedLevel", upgrades.ToString())
							}));
						}
					}
				}

				// Images from WithCrateBody was moved into RenderSprites
				if (engineVersion < 20150608)
				{
					if (depth == 0)
					{
						var actorTraits = node.Value.Nodes;
						var withCrateBody = actorTraits.FirstOrDefault(t => t.Key == "WithCrateBody");
						if (withCrateBody != null)
						{
							var withCrateBodyFields = withCrateBody.Value.Nodes;
							var images = withCrateBodyFields.FirstOrDefault(n => n.Key == "Images");
							if (images == null)
								images = new MiniYamlNode("Images", "crate");
							else
								withCrateBodyFields.Remove(images);

							images.Key = "Image";

							var renderSprites = actorTraits.FirstOrDefault(t => t.Key == "RenderSprites");
							if (renderSprites != null)
								renderSprites.Value.Nodes.Add(images);
							else
							{
								Console.WriteLine("Warning: Adding RenderSprites trait to {0} in {1}".F(node.Key, node.Location.Filename));
								actorTraits.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", new List<MiniYamlNode> { images })));
							}
						}
					}
				}

				// 'Selectable' boolean was removed from selectable trait.
				if (engineVersion < 20150619)
				{
					if (depth == 1 && node.Value.Nodes.Exists(n => n.Key == "Selectable"))
					{
						var selectable = node.Value.Nodes.FirstOrDefault(n => n.Key == "Selectable");
						if (node.Key == "Selectable" && selectable.Value.Value == "false")
							node.Key = "SelectableRemoveMe";

						// To cover rare cases where the boolean was 'true'
						if (node.Key == "Selectable" && selectable.Value.Value == "true")
							node.Value.Nodes.Remove(selectable);
					}

					if (depth == 0 && node.Value.Nodes.Exists(n => n.Key == "SelectableRemoveMe"))
					{
						node.Value.Nodes.RemoveAll(n => n.Key == "SelectableRemoveMe");
						Console.WriteLine("The 'Selectable' boolean has been removed from the Selectable trait.");
						Console.WriteLine("If you just want to disable an inherited Selectable trait, use -Selectable instead.");
						Console.WriteLine("For special cases like bridge huts, which need bounds to be targetable by C4 and engineers,");
						Console.WriteLine("give them the CustomSelectionSize trait with CustomBounds.");
						Console.WriteLine("See RA and C&C bridge huts or crates for reference.");
					}
				}

				if (engineVersion < 20150620)
				{
					if (depth == 2)
					{
						if (node.Key == "DeathSound")
							node.Key = "Voice";

						if (node.Key == "KillVoice")
							node.Key = "Voice";

						if (node.Key == "BuildVoice")
							node.Key = "Voice";
					}
				}

				// WinForms editor was removed
				if (engineVersion < 20150620)
				{
					if (depth == 0 && node.Value.Nodes.Exists(n => n.Key == "EditorAppearance"))
						node.Value.Nodes.RemoveAll(n => n.Key == "EditorAppearance");

					if (depth == 1 && node.Value.Nodes.Exists(n => n.Key == "ResourceType"))
					{
						var editorSprite = node.Value.Nodes.FirstOrDefault(n => n.Key == "EditorSprite");
						if (editorSprite != null)
							node.Value.Nodes.Remove(editorSprite);
					}
				}

				// VisibilityType was introduced
				if (engineVersion < 20150704)
				{
					if (depth == 0 && node.Value.Nodes.Exists(n => n.Key == "Helicopter" || n.Key == "Plane" || n.Key == "Immobile"))
					{
						var visibility = node.Value.Nodes.FirstOrDefault(n => n.Key == "HiddenUnderShroud" || n.Key == "HiddenUnderFog");
						if (visibility != null)
							visibility.Value.Nodes.Add(new MiniYamlNode("Type", "CenterPosition"));

						var reveals = node.Value.Nodes.FirstOrDefault(n => n.Key == "RevealsShroud");
						if (reveals != null)
							reveals.Value.Nodes.Add(new MiniYamlNode("Type", "CenterPosition"));
					}
				}

				// Removed RenderUnit
				if (engineVersion < 20150704)
				{
					// Renamed WithHarvestAnimation to WithHarvestOverlay
					if (node.Key == "WithHarvestAnimation")
						node.Key = "WithHarvestOverlay";

					// Replaced RenderLandingCraft with WithFacingSpriteBody + WithLandingCraftAnimation.
					// Note: These rules are set up to do approximately the right thing for maps, but
					// mods might need additional manual tweaks. This is the best we can do without having
					// much smarter rules parsing, because we currently can't reason about inherited traits.
					if (depth == 0)
					{
						var childKeySequence = new[] { "Sequence" };
						var childKeysExcludeFromRS = new[] { "Sequence", "OpenTerrainTypes", "OpenSequence", "UnloadSequence" };

						var rlc = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderLandingCraft"));
						if (rlc != null)
						{
							rlc.Key = "WithLandingCraftAnimation";

							var rsNodes = rlc.Value.Nodes.Where(n => !childKeysExcludeFromRS.Contains(n.Key)).ToList();
							var wfsbNodes = rlc.Value.Nodes.Where(n => childKeySequence.Contains(n.Key)).ToList();

							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", ""));

							// Note: For the RA landing craft WithSpriteBody would be sufficient since it has no facings,
							// but WithFacingSpriteBody works as well and covers the potential case where a third-party mod
							// might have given their landing craft multiple facings.
							if (wfsbNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("WithFacingSpriteBody", new MiniYaml("", wfsbNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("WithFacingSpriteBody", ""));

							node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));

							rlc.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));
							rlc.Value.Nodes.RemoveAll(n => wfsbNodes.Contains(n));
						}

						var rrlc = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderLandingCraft"));
						if (rrlc != null)
							rrlc.Key = "-WithLandingCraftAnimation";
					}

					// Replaced RenderHarvester with WithFacingSpriteBody + WithHarvestAnimation + WithDockingAnimation.
					// Note: These rules are set up to do approximately the right thing for maps, but
					// mods might need additional manual tweaks. This is the best we can do without having
					// much smarter rules parsing, because we currently can't reason about inherited traits.
					if (depth == 0)
					{
						var childKeySequence = new[] { "Sequence" };
						var childKeyIBF = new[] { "ImagesByFullness" };
						var childKeysExcludeFromRS = new[] { "Sequence", "ImagesByFullness", "HarvestSequence" };

						var rh = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderHarvester"));
						if (rh != null)
						{
							rh.Key = "WithHarvestAnimation";

							var rsNodes = rh.Value.Nodes.Where(n => !childKeysExcludeFromRS.Contains(n.Key)).ToList();
							var wfsbNodes = rh.Value.Nodes.Where(n => childKeySequence.Contains(n.Key)).ToList();
							var ibfNode = rh.Value.Nodes.Where(n => childKeyIBF.Contains(n.Key)).ToList();

							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", ""));

							if (wfsbNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("WithFacingSpriteBody", new MiniYaml("", wfsbNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("WithFacingSpriteBody", ""));

							node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));
							node.Value.Nodes.Add(new MiniYamlNode("WithDockingAnimation", ""));

							rh.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));
							rh.Value.Nodes.RemoveAll(n => wfsbNodes.Contains(n));

							if (ibfNode.Any())
							{
								rh.Value.Nodes.RemoveAll(n => ibfNode.Contains(n));
								Console.WriteLine("The 'ImagesByFullness' property from the removed RenderHarvester trait has been");
								Console.WriteLine("replaced with a 'PrefixByFullness' property on the new WithHarvestAnimation trait.");
								Console.WriteLine("This cannot be reliably upgraded, as the actor sequences need to be adapted as well.");
								Console.WriteLine("Therefore, WithHarvestAnimation will use the default (no prefix) after upgrading.");
								Console.WriteLine("See RA's harvester for reference on how to re-implement this feature using the new trait.");
							}
						}

						var rrh = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderHarvester"));
						if (rrh != null)
							rrh.Key = "-WithHarvestAnimation";
					}

					// Replace RenderUnit with RenderSprites + WithFacingSpriteBody + AutoSelectionSize.
					// Normally this should have been removed by previous upgrade rules, but let's run this again
					// to make sure to get rid of potential left-over cases like D2k sandworms and harvesters.
					if (depth == 0)
					{
						var childKeys = new[] { "Sequence" };

						var ru = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderUnit"));
						if (ru != null)
						{
							ru.Key = "WithFacingSpriteBody";

							var rsNodes = ru.Value.Nodes.Where(n => !childKeys.Contains(n.Key)).ToList();

							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", ""));

							node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));

							ru.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));

							Console.WriteLine("RenderUnit has now been removed from code.");
							Console.WriteLine("Use RenderSprites + WithFacingSpriteBody (+ AutoSelectionSize, if necessary) instead.");
						}

						var rru = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderUnit"));
						if (rru != null)
							rru.Key = "-WithFacingSpriteBody";
					}
				}

				// Generalized the flash palette trait
				if (engineVersion < 20150627)
				{
					if (node.Key == "NukePaletteEffect")
						node.Key = "FlashPaletteEffect";
				}

				// InitialActivity on FreeActor and Buildable was removed (due to being too hacky)
				if (engineVersion < 20150707)
				{
					if (depth == 1)
						node.Value.Nodes.RemoveAll(n => n.Key == "InitialActivity");
				}

				// Make default upgrades explicit for GainsExperience
				if (engineVersion < 20150709)
				{
					if (depth == 1 && (node.Key == "GainsExperience" || node.Key.StartsWith("GainsExperience@"))
							&& node.Value.Nodes.FirstOrDefault(n => n.Key == "Upgrades") == null)
						node.Value.Nodes.Add(new MiniYamlNode("Upgrades", new MiniYaml("", new List<MiniYamlNode> {
							new MiniYamlNode("200", "firepower, damage, speed, reload, inaccuracy, rank"),
							new MiniYamlNode("400", "firepower, damage, speed, reload, inaccuracy, rank"),
							new MiniYamlNode("800", "firepower, damage, speed, reload, inaccuracy, rank"),
							new MiniYamlNode("1600", "firepower, damage, speed, reload, inaccuracy, rank, eliteweapon, selfheal")
						})));
				}

				if (engineVersion < 20150711)
				{
					if (depth == 0)
					{
						var emptyYaml = new MiniYaml(null);

						// Replace -GainsStatUpgrades
						var trait = node.Value.Nodes.FirstOrDefault(n => n.Key == "-GainsStatUpgrades");
						if (trait != null)
						{
							node.Value.Nodes.Add(new MiniYamlNode("-FirepowerMultiplier@EXPERIENCE", emptyYaml));
							node.Value.Nodes.Add(new MiniYamlNode("-DamageMultiplier@EXPERIENCE", emptyYaml));
							node.Value.Nodes.Add(new MiniYamlNode("-SpeedMultiplier@EXPERIENCE", emptyYaml));
							node.Value.Nodes.Add(new MiniYamlNode("-ReloadDelayMultiplier@EXPERIENCE", emptyYaml));
							node.Value.Nodes.Add(new MiniYamlNode("-InaccuracyMultiplier@EXPERIENCE", emptyYaml));
							node.Value.Nodes.Remove(trait);
						}

						// Replace GainsStatUpgrades
						trait = node.Value.Nodes.FirstOrDefault(n => n.Key == "GainsStatUpgrades");
						if (trait != null)
						{
							// Common code for making each trait
							Action<string, string, string> addTrait = (type, newType, values) =>
								{
									var upgradeTypes = trait.Value.Nodes.FirstOrDefault(n => n.Key == type + "Upgrade");
									var modifier = trait.Value.Nodes.FirstOrDefault(n => n.Key == type + "Modifier");

									if (upgradeTypes == null || !string.IsNullOrEmpty(upgradeTypes.Value.Value) || modifier == null ||
										!string.IsNullOrEmpty(modifier.Value.Value))
									{
										var yaml = new MiniYaml(null);
										if (modifier == null)
											modifier = new MiniYamlNode("Modifier", new MiniYaml(values));
										else
											modifier.Key = "Modifier";
										yaml.Nodes.Add(modifier);

										if (upgradeTypes == null)
											upgradeTypes = new MiniYamlNode("UpgradeTypes", new MiniYaml(type.ToLowerInvariant()));
										else
											upgradeTypes.Key = "UpgradeTypes";
										yaml.Nodes.Add(upgradeTypes);

										node.Value.Nodes.Add(new MiniYamlNode((newType ?? type) + "Multiplier@EXPERIENCE", yaml));
									}
								};

							// Execute common code for each trait
							addTrait("Firepower", null, "110, 115, 120, 130");
							addTrait("Damage", null, "91, 87, 83, 65");
							addTrait("Speed", null, "110, 115, 120, 150");
							addTrait("Reload", "ReloadDelay", "95, 90, 85, 75");
							addTrait("Inaccuracy", null, "90, 80, 70, 50");

							// Remove GainsStatUpgrades
							node.Value.Nodes.Remove(trait);
						}

						// Replace -InvulnerabilityUpgrade
						trait = node.Value.Nodes.FirstOrDefault(n => n.Key == "-InvulnerabilityUpgrade");
						if (trait != null)
							trait.Key = "-DamageMultiplier@INVULNERABILITY_UPGRADE";

						// Replace InvulnerabilityUpgrade with DamageMultiplier@INVULNERABILITY_UPGRADE
						trait = node.Value.Nodes.FirstOrDefault(n => n.Key == "InvulnerabilityUpgrade");
						if (trait != null)
						{
							trait.Key = "DamageMultiplier@INVULNERABILITY_UPGRADE";
							trait.Value.Nodes.Add(new MiniYamlNode("Modifier", "0"));

							// Use UpgradeMinEnabledLevel as BaseLevel; otherwise, 1
							var min = trait.Value.Nodes.FirstOrDefault(n => n.Key == "UpgradeMinEnabledLevel");
							if (min != null)
							{
								if (min.Value.Value != "1")
									min.Key = "BaseLevel";
								else
									trait.Value.Nodes.Remove(min);
							}

							// Remove since level cap is based of Modifier.Length + BaseLevel
							trait.Value.Nodes.RemoveAll(n => n.Key == "UpgradeMaxAcceptedLevel");
							trait.Value.Nodes.RemoveAll(n => n.Key == "UpgradeMaxEnabledLevel");
						}

						// Replace -InvulnerabilityUpgrade@* with -DamageMultiplier@*
						foreach (var n in node.Value.Nodes.Where(n => n.Key.StartsWith("-InvulnerabilityUpgrade@")))
							n.Key = "-DamageMultiplier@" + n.Key.Substring("-InvulnerabilityUpgrade@".Length);

						// Replace InvulnerabilityUpgrade@* with DamageMultiplier@*
						foreach (var t in node.Value.Nodes.Where(n => n.Key.StartsWith("InvulnerabilityUpgrade@")))
						{
							t.Key = "DamageMultiplier@" + t.Key.Substring("InvulnerabilityUpgrade@".Length);
							t.Value.Nodes.Add(new MiniYamlNode("Modifier", "0"));

							// Use UpgradeMinEnabledLevel as BaseLevel; otherwise, 1
							var min = t.Value.Nodes.FirstOrDefault(n => n.Key == "UpgradeMinEnabledLevel");
							if (min != null)
							{
								if (min.Value.Value != "1")
									min.Key = "BaseLevel";
								else
									t.Value.Nodes.Remove(min);
							}

							// Remove since level cap is based of Modifier.Length + BaseLevel
							t.Value.Nodes.RemoveAll(n => n.Key == "UpgradeMaxAcceptedLevel");
							t.Value.Nodes.RemoveAll(n => n.Key == "UpgradeMaxEnabledLevel");
						}

						// Replace -Invulnerable with -DamageMultiplier@INVULNERABLE
						trait = node.Value.Nodes.FirstOrDefault(n => n.Key == "-Invulnerable");
						if (trait != null)
							trait.Key = "-DamageMultiplier@INVULNERABLE";

						// Invulnerable with DamageMultiplier@INVULNERABLE
						trait = node.Value.Nodes.FirstOrDefault(n => n.Key == "Invulnerable");
						if (trait != null)
						{
							trait.Key = "DamageMultiplier@INVULNERABLE";
							trait.Value.Nodes.Add(new MiniYamlNode("Modifier", "0"));
						}
					}
				}

				// Rename the `Country` trait to `Faction`
				if (engineVersion < 20150714)
				{
					var split = node.Key.Split('@');
					if (split.Any() && split[0] == "Country")
					{
						node.Key = node.Key.Replace("Country", "Faction");

						var race = node.Value.Nodes.FirstOrDefault(x => x.Key == "Race");
						if (race != null)
							race.Key = "InternalName";

						var randomRace = node.Value.Nodes.FirstOrDefault(x => x.Key == "RandomRaceMembers");
						if (randomRace != null)
							randomRace.Key = "RandomFactionMembers";
					}
				}

				if (engineVersion < 20150714)
				{
					// Move certain properties from Parachutable to new WithParachute trait
					// Add dependency traits to actors implementing Parachutable
					// Make otherwise targetable parachuting actors untargetable
					var par = node.Value.Nodes.FirstOrDefault(n => n.Key == "Parachutable");
					if (par != null)
					{
						var withParachute = new MiniYamlNode("WithParachute", null, new List<MiniYamlNode>
						{
							new MiniYamlNode("UpgradeTypes", "parachute"),
							new MiniYamlNode("UpgradeMinEnabledLevel", "1")
						});

						var copyProp = new Action<string, string, string>((srcName, dstName, defValue) =>
						{
							var prop = par.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith(srcName));
							if (prop != null && prop.Value.Value != defValue)
								withParachute.Value.Nodes.Add(new MiniYamlNode(dstName, prop.Value.Value));
						});

						var moveProp = new Action<string, string, string>((srcName, dstName, defValue) =>
						{
							copyProp(srcName, dstName, defValue);
							par.Value.Nodes.RemoveAll(n => n.Key.StartsWith(srcName));
						});

						if (par.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("ShadowSequence")) != null)
						{
							moveProp("ShadowSequence", "ShadowImage", null);
							copyProp("ParachuteIdleSequence", "ShadowSequence", null);
						}

						moveProp("ParachuteSequence", "Image", null);
						moveProp("ParachuteIdleSequence", "Sequence", null);

						moveProp("ParachuteOpenSequence", "OpeningSequence", null);

						moveProp("ParachutePalette", "Palette", "player");
						moveProp("ShadowPalette", "ShadowPalette", "player");

						moveProp("ParachuteOffset", "Offset", "player");

						par.Value.Nodes.RemoveAll(n => n.Key.StartsWith("ParachuteShadowPalette"));

						node.Value.Nodes.Add(withParachute);

						var otherNodes = nodes;
						var inherits = new Func<string, bool>(traitName => node.Value.Nodes.Where(n => n.Key.StartsWith("Inherits"))
							.Any(inh =>
							{
								var otherNode = otherNodes.FirstOrDefault(n => n.Key.StartsWith(inh.Value.Value));

								if (otherNode == null)
									return false;

								return otherNode.Value.Nodes.Any(n => n.Key.StartsWith(traitName));
							}));

						// For actors that have or inherit a TargetableUnit, disable the trait while parachuting
						var tu = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("TargetableUnit"));
						if (tu != null)
						{
							tu.Value.Nodes.Add(new MiniYamlNode("UpgradeTypes", "parachute"));
							tu.Value.Nodes.Add(new MiniYamlNode("UpgradeMaxEnabledLevel", "0"));
						}
						else
						{
							if (inherits("TargetableUnit"))
							{
								node.Value.Nodes.Add(new MiniYamlNode("TargetableUnit", null, new List<MiniYamlNode>
								{
									new MiniYamlNode("UpgradeTypes", "parachute"),
									new MiniYamlNode("UpgradeMaxEnabledLevel", "0")
								}));
								break;
							}
						}

						var has = new Func<string, bool>(traitName => node.Value.Nodes.Any(n => n.Key.StartsWith(traitName)));

						// If actor does not have nor inherits an UpgradeManager, add one
						if (!has("UpgradeManager") && !inherits("UpgradeManager"))
								node.Value.Nodes.Add(new MiniYamlNode("UpgradeManager", ""));

						// If actor does not have nor inherits a BodyOrientation, add one
						if (!has("BodyOrientation") && !inherits("BodyOrientation"))
							node.Value.Nodes.Add(new MiniYamlNode("BodyOrientation", ""));
					}
				}

				if (engineVersion < 20150715)
				{
					// Replaced RenderGunboat with RenderSprites + WithGunboatBody.
					if (depth == 0)
					{
						var childKeysGunboat = new[] { "Turret", "LeftSequence", "RightSequence", "WakeLeftSequence", "WakeRightSequence" };

						var rgb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderGunboat"));
						if (rgb != null)
						{
							rgb.Key = "WithGunboatBody";

							var rsNodes = rgb.Value.Nodes.Where(n => !childKeysGunboat.Contains(n.Key)).ToList();
							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", ""));

							node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));

							rgb.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));
							rgb.Value.Nodes.Add(new MiniYamlNode("Sequence", "left"));
						}

						var rrgb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderGunboat"));
						if (rrgb != null)
							rrgb.Key = "-WithGunboatBody";
					}
				}

				if (engineVersion < 20150720)
				{
					// Rename RenderEditorOnly to RenderSpritesEditorOnly
					if (depth == 0)
					{
						var reo = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderEditorOnly"));
						if (reo != null)
						{
							reo.Key = "RenderSpritesEditorOnly";

							var wsbNodes = reo.Value.Nodes.Where(n => n.Key == "Sequence").ToList();

							if (wsbNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("WithSpriteBody", new MiniYaml("", wsbNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("WithSpriteBody", ""));

							reo.Value.Nodes.RemoveAll(n => wsbNodes.Contains(n));
						}

						var rreo = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderEditorOnly"));
						if (rreo != null)
							rreo.Key = "-RenderSpritesEditorOnly";
					}
				}

				if (engineVersion < 20150731)
				{
					if (node.Key.StartsWith("ProvidesPrerequisite"))
					{
						var raceNode = node.Value.Nodes.FirstOrDefault(x => x.Key == "Race");
						if (raceNode != null)
							raceNode.Key = "Factions";
					}

					if (node.Key.StartsWith("Buildable"))
					{
						var raceNode = node.Value.Nodes.FirstOrDefault(x => x.Key == "ForceRace");
						if (raceNode != null)
							raceNode.Key = "ForceFaction";
					}
				}

				// WithBuildingExplosion received support for sequence randomization
				if (engineVersion < 20150803)
				{
					if (depth == 2 && parentKey == "WithBuildingExplosion" && node.Key == "Sequence")
						node.Key = "Sequences";
				}

				// SpawnViceroid was replaced by SpawnActorOnDeath
				// And LeavesHusk was renamed to SpawnActorOnDeath
				if (engineVersion < 20150920)
				{
					if (node.Key == "SpawnViceroid")
					{
						node.Key = "SpawnActorOnDeath";

						// The default value of ViceroidActor was vice
						var actor = node.Value.Nodes.FirstOrDefault(n => n.Key == "ViceroidActor");
						if (actor != null)
							actor.Key = "Actor";
						else
							node.Value.Nodes.Add(new MiniYamlNode("Actor", "vice"));

						// The default value of Probability was 10
						var probability = node.Value.Nodes.FirstOrDefault(n => n.Key == "Probability");
						if (probability == null)
							node.Value.Nodes.Add(new MiniYamlNode("Probability", "10"));

						// The default value of Owner was Creeps
						var owner = node.Value.Nodes.FirstOrDefault(n => n.Key == "Owner");
						if (owner != null)
						{
							node.Value.Nodes.Add(new MiniYamlNode("OwnerType", "InternalName"));
							owner.Key = "InternalOwner";
						}
						else
						{
							node.Value.Nodes.Add(new MiniYamlNode("OwnerType", "InternalName"));
							node.Value.Nodes.Add(new MiniYamlNode("InternalOwner", "Creeps"));
						}

						// The default value of DeathType was TiberiumDeath
						var deathType = node.Value.Nodes.FirstOrDefault(n => n.Key == "DeathType");
						if (deathType == null)
							node.Value.Nodes.Add(new MiniYamlNode("DeathType", "TiberiumDeath"));

						node.Value.Nodes.Add(new MiniYamlNode("RequiresLobbyCreeps", "true"));
					}

					if (node.Key == "LeavesHusk")
					{
						node.Key = "SpawnActorOnDeath";

						var actor = node.Value.Nodes.FirstOrDefault(n => n.Key == "HuskActor");
						if (actor != null)
							actor.Key = "Actor";
					}

					if (node.Key == "-SpawnViceroid")
						node.Key = "-SpawnActorOnDeath";

					if (node.Key == "-LeavesHusk")
						node.Key = "-SpawnActorOnDeath";
				}

				if (engineVersion < 20150920)
				{
					if (depth == 2 && parentKey == "RallyPoint" && node.Key == "RallyPoint")
						node.Key = "Offset";
				}

				if (engineVersion < 20150920)
				{
					if (node.Key.StartsWith("ProductionQueue"))
					{
						var race = node.Value.Nodes.FirstOrDefault(x => x.Key == "Race");
						if (race != null)
							race.Key = "Factions";
					}

					if (node.Key.StartsWith("EmitInfantryOnSell"))
					{
						var race = node.Value.Nodes.FirstOrDefault(x => x.Key == "Races");
						if (race != null)
							race.Key = "Factions";
					}

					if (node.Key.StartsWith("MPStartUnits"))
					{
						var race = node.Value.Nodes.FirstOrDefault(x => x.Key == "Races");
						if (race != null)
							race.Key = "Factions";
					}
				}

				if (engineVersion < 20150920)
				{
					// Rename RenderSprites.RaceImages
					if (depth == 2 && node.Key == "RaceImages")
						node.Key = "FactionImages";
					if (depth == 2 && node.Key == "-RaceImages")
						node.Key = "-FactionImages";

					// Rename *CrateAction.ValidRaces
					if (depth == 2 && node.Key == "ValidRaces"
					    && (parentKey == "DuplicateUnitCrateAction" || parentKey == "GiveUnitCrateAction"))
						node.Key = "ValidFactions";
				}

				if (engineVersion < 20150920)
				{
					// Introduce QuantizeFacingsFromSequence
					// This will only do roughly the right thing and probably require the modder to do some manual cleanup
					if (depth == 0)
					{
						// Check if the upgrade rule ran already before
						var qffs = node.Value.Nodes.FirstOrDefault(n => n.Key == "QuantizeFacingsFromSequence");
						if (qffs == null)
						{
							var inftraits = node.Value.Nodes.FirstOrDefault(n =>
								n.Key.StartsWith("WithInfantryBody")
								|| n.Key.StartsWith("WithDisguisingInfantryBody"));
							if (inftraits != null)
							{
								node.Value.Nodes.Add(new MiniYamlNode("QuantizeFacingsFromSequence", null, new List<MiniYamlNode>
								{
									new MiniYamlNode("Sequence", "stand"),
								}));
							}

							var other = node.Value.Nodes.FirstOrDefault(x =>
								x.Key.StartsWith("RenderBuilding")
								|| x.Key.StartsWith("RenderSimple")
								|| x.Key.StartsWith("WithCrateBody")
								|| x.Key.StartsWith("WithSpriteBody")
								|| x.Key.StartsWith("WithFacingSpriteBody"));
							if (other != null)
								node.Value.Nodes.Add(new MiniYamlNode("QuantizeFacingsFromSequence", ""));
						}
					}
				}

				if (engineVersion < 20150920)
				{
					// Replaced RenderBuildingCharge with RenderSprites + WithSpriteBody + WithChargeAnimation (+AutoSelectionSize)
					if (depth == 0)
					{
						var childKeySequence = new[] { "ChargeSequence" };
						var childKeysExcludeFromRS = new[] { "Sequence", "ChargeSequence", "PauseOnLowPower" };

						var rb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderBuildingCharge"));
						if (rb != null)
						{
							rb.Key = "WithChargeAnimation";

							var rsNodes = rb.Value.Nodes.Where(n => !childKeysExcludeFromRS.Contains(n.Key)).ToList();
							var wsbNodes = rb.Value.Nodes.Where(n => childKeySequence.Contains(n.Key)).ToList();

							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", ""));

							node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));

							rb.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));
							rb.Value.Nodes.RemoveAll(n => wsbNodes.Contains(n));
						}

						var rrb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderBuildingCharge"));
						if (rrb != null)
							rrb.Key = "-WithChargeAnimation";
					}

					// Replaced RenderBuildingSilo with RenderSprites + WithSpriteBody + WithSiloAnimation (+AutoSelectionSize)
					if (depth == 0)
					{
						var childKeySequence = new[] { "Sequence", "PauseOnLowPower" };

						var rb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderBuildingSilo"));
						if (rb != null)
						{
							rb.Key = "WithSiloAnimation";

							var rsNodes = rb.Value.Nodes.Where(n => !childKeySequence.Contains(n.Key)).ToList();
							var wsbNodes = rb.Value.Nodes.Where(n => childKeySequence.Contains(n.Key)).ToList();

							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", ""));

							if (wsbNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("WithSpriteBody", new MiniYaml("", wsbNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("WithSpriteBody", ""));

							node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));

							rb.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));
							rb.Value.Nodes.RemoveAll(n => wsbNodes.Contains(n));
						}

						var rrb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderBuildingSilo"));
						if (rrb != null)
							rrb.Key = "-WithSiloAnimation";
					}

					// Replaced RenderBuildingTurreted with RenderSprites + WithTurretedSpriteBody (+AutoSelectionSize)
					if (depth == 0)
					{
						var childKeysExcludeFromRS = new[] { "Sequence", "PauseOnLowPower" };

						var rb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderBuildingTurreted"));
						if (rb != null)
						{
							rb.Key = "WithTurretedSpriteBody";

							var rsNodes = rb.Value.Nodes.Where(n => !childKeysExcludeFromRS.Contains(n.Key)).ToList();

							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", ""));

							node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));

							rb.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));
						}

						var rrb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderBuildingTurreted"));
						if (rrb != null)
							rrb.Key = "-WithTurretedSpriteBody";
					}

					// Replaced RenderBuildingWall with RenderSprites + WithWallSpriteBody (+AutoSelectionSize)
					if (depth == 0)
					{
						var childKeysExcludeFromRS = new[] { "Sequence", "Type" };

						var rb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderBuildingWall"));
						if (rb != null)
						{
							rb.Key = "WithWallSpriteBody";

							var rsNodes = rb.Value.Nodes.Where(n => !childKeysExcludeFromRS.Contains(n.Key)).ToList();

							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", ""));

							node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));

							rb.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));
						}

						var rrb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderBuildingWall"));
						if (rrb != null)
							rrb.Key = "-WithWallSpriteBody";
					}
				}

				if (engineVersion < 20150920)
				{
					// Replaced RenderBuilding with RenderSprites + WithSpriteBody (+AutoSelectionSize)
					if (depth == 0)
					{
						var childKeysExcludeFromRS = new[] { "Sequence", "PauseOnLowPower" };

						var rb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderBuilding"));
						if (rb != null)
						{
							rb.Key = "WithSpriteBody";

							var rsNodes = rb.Value.Nodes.Where(n => !childKeysExcludeFromRS.Contains(n.Key)).ToList();

							if (rsNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", new MiniYaml("", rsNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("RenderSprites", ""));

							node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));

							rb.Value.Nodes.RemoveAll(n => rsNodes.Contains(n));
						}

						var rrb = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderBuilding"));
						if (rrb != null)
							rrb.Key = "-WithSpriteBody";

						if (depth == 2 && node.Key == "PauseOnLowPower" && (parentKey == "WithSpriteBody"
							|| parentKey == "WithTurretedSpriteBody" || parentKey == "WithWallSpriteBody"))
							node.Key = "PauseAnimationWhenDisabled";
					}
				}

				if (engineVersion < 20150920)
				{
					if (depth == 1)
					{
						if (node.Key == "TargetableUnit" || node.Key == "TargetableBuilding")
							node.Key = "Targetable";
						else if (node.Key == "-TargetableUnit" || node.Key == "-TargetableBuilding")
							node.Key = "-Targetable";
					}
					else if (depth == 0)
					{
						// Split TargetableSubmarine into two Targetable traits
						var targetableSubmarine = node.Value.Nodes.FirstOrDefault(n => n.Key == "TargetableSubmarine");
						if (targetableSubmarine != null)
						{
							node.Value.Nodes.RemoveAll(n => n.Key == "-Targetable");
							targetableSubmarine.Key = "Targetable";
							targetableSubmarine.Value.Nodes.Add(new MiniYamlNode("UpgradeTypes", "underwater"));
							targetableSubmarine.Value.Nodes.Add(new MiniYamlNode("UpgradeMaxEnabledLevel", "0"));
							var cloakedTargetTypes = targetableSubmarine.Value.Nodes.FirstOrDefault(n => n.Key == "CloakedTargetTypes");
							if (cloakedTargetTypes != null)
							{
								targetableSubmarine.Value.Nodes.Remove(cloakedTargetTypes);
								cloakedTargetTypes.Key = "TargetTypes";
							}
							else
								cloakedTargetTypes = new MiniYamlNode("TargetTypes", "");
							node.Value.Nodes.Add(new MiniYamlNode("Targetable@UNDERWATER", "", new List<MiniYamlNode>
							{
								cloakedTargetTypes,
								new MiniYamlNode("UpgradeTypes", "underwater"),
								new MiniYamlNode("UpgradeMinEnabledLevel", "1")
							}));
						}

						// Add `WhileCloakedUpgrades: underwater` to Cloak trait if `CloakTypes: Underwater`
						var cloak = node.Value.Nodes.FirstOrDefault(n => (n.Key == "Cloak" || n.Key.StartsWith("Cloak@"))
							&& n.Value.Nodes.Any(p => p.Key == "CloakTypes" && p.Value.Value == "Underwater"));
						if (cloak != null && !cloak.Value.Nodes.Any(n => n.Key == "WhileCloakedUpgrades"))
							cloak.Value.Nodes.Add(new MiniYamlNode("WhileCloakedUpgrades", "underwater"));

						// Remove split traits if TargetableSubmarine was removed
						var untargetableSubmarine = node.Value.Nodes.FirstOrDefault(n => n.Key == "-TargetableSubmarine");
						if (untargetableSubmarine != null)
						{
							untargetableSubmarine.Key = "-Targetable";
							node.Value.Nodes.Add(new MiniYamlNode("-Targetable@UNDERWATER", ""));
						}

						// Split TargetableAircraft into two Targetable traits
						var targetableAircraft = node.Value.Nodes.FirstOrDefault(n => n.Key == "TargetableAircraft");
						if (targetableAircraft != null)
						{
							node.Value.Nodes.RemoveAll(n => n.Key == "-Targetable");
							targetableAircraft.Key = "Targetable@AIRBORNE";
							targetableAircraft.Value.Nodes.Add(new MiniYamlNode("UpgradeTypes", "airborne"));
							targetableAircraft.Value.Nodes.Add(new MiniYamlNode("UpgradeMinEnabledLevel", "1"));
							var groundTargetTypes = targetableAircraft.Value.Nodes.FirstOrDefault(n => n.Key == "GroundedTargetTypes");
							if (groundTargetTypes != null)
							{
								targetableAircraft.Value.Nodes.Remove(groundTargetTypes);
								groundTargetTypes.Key = "TargetTypes";
							}
							else
								groundTargetTypes = new MiniYamlNode("TargetTypes", "");
							node.Value.Nodes.Add(new MiniYamlNode("Targetable@GROUND", "", new List<MiniYamlNode>
							{
								groundTargetTypes,
								new MiniYamlNode("UpgradeTypes", "airborne"),
								new MiniYamlNode("UpgradeMaxEnabledLevel", "0")
							}));
						}

						// Add `AirborneUpgrades: airborne` to Plane and Helicopter
						var aircraft = node.Value.Nodes.FirstOrDefault(n => n.Key == "Plane" || n.Key == "Helicopter");
						if (aircraft != null)
							aircraft.Value.Nodes.Add(new MiniYamlNode("AirborneUpgrades", "airborne"));

						// Remove split traits if TargetableAircraft was removed
						var untargetableAircraft = node.Value.Nodes.FirstOrDefault(n => n.Key == "-TargetableAircraft");
						if (untargetableAircraft != null)
						{
							untargetableAircraft.Key = "-TargetableUnit@GROUND";
							node.Value.Nodes.Add(new MiniYamlNode("-TargetableUnit@AIRBORNE", ""));
						}
					}
				}

				// WaterPaletteRotation renamed to RotationPaletteEffect
				if (engineVersion < 20150920)
				{
					if (depth == 1 && node.Key == "WaterPaletteRotation")
						node.Key = "RotationPaletteEffect";
				}

				// Replace RenderSimple with RenderSprites + WithSpriteBody + AutoSelectionSize
				if (engineVersion < 20150920)
				{
					if (depth == 0)
					{
						var rs = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("RenderSimple"));
						if (rs != null)
						{
							rs.Key = "RenderSprites";

							var wsbNodes = rs.Value.Nodes.Where(n => n.Key == "Sequence").ToList();
							if (wsbNodes.Any())
								node.Value.Nodes.Add(new MiniYamlNode("WithSpriteBody", new MiniYaml("", wsbNodes)));
							else
								node.Value.Nodes.Add(new MiniYamlNode("WithSpriteBody", ""));

							node.Value.Nodes.Add(new MiniYamlNode("AutoSelectionSize", ""));
							rs.Value.Nodes.RemoveAll(n => wsbNodes.Contains(n));
						}

						var rrs = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-RenderSimple"));
						if (rrs != null)
							rrs.Key = "-WithSpriteBody";
					}
				}

				// Rename D2k actors to match the original game.
				if (engineVersion < 20150920 && Game.ModData.Manifest.Mod.Id == "d2k")
					node.Key = RenameD2kActors(node.Key);

				// Make Range WDist for all traits with circular ranges.
				if (engineVersion < 20150920 && depth == 2 && node.Key == "Range")
				{
					if ((parentKey == "DetectCloaked"
							|| parentKey == "JamsMissiles"
							|| parentKey == "JamsRadar"
							|| parentKey == "Guardable"
							|| parentKey == "BaseProvider"
							|| parentKey == "ProximityCapturable")
							&& !node.Value.Value.Contains("c0"))
						node.Value.Value = node.Value.Value + "c0";
				}

				if (engineVersion < 20150920)
				{
					// Rename WithMuzzleFlash to WithMuzzleOverlay
					if (depth == 1 && node.Key.StartsWith("WithMuzzleFlash"))
					{
						var parts = node.Key.Split('@');
						node.Key = "WithMuzzleOverlay";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					if (depth == 1 && node.Key.StartsWith("-WithMuzzleFlash"))
					{
						var parts = node.Key.Split('@');
						node.Key = "-WithMuzzleOverlay";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}
				}

				// WithSiloAnimation received own Sequence property, idle sequence is only 1 frame long now
				if (engineVersion < 20150925)
				{
					if (depth == 2 && node.Key == "WithSiloAnimation")
					{
						Console.WriteLine("WithSiloAnimation received its own Sequence property, which defaults to 'stages'.");
						Console.WriteLine("Update your sequences accordingly, if necessary.");
					}
				}

				if (engineVersion < 20150926)
				{
					if (node.Key == "CrateSpawner")
					{
						var interval = node.Value.Nodes.FirstOrDefault(n => n.Key == "SpawnInterval");
						if (interval != null)
						{
							var value = Exts.ParseIntegerInvariant(interval.Value.Value);
							interval.Value.Value = (value * 25).ToString();
						}

						var chance = node.Value.Nodes.FirstOrDefault(n => n.Key == "WaterChance");
						if (chance != null)
							ConvertFloatToIntPercentage(ref chance.Value.Value);
					}
				}

				if (engineVersion < 20150927)
				{
					if (depth == 1 && node.Key == "Plane")
						node.Key = "Aircraft";

					if (depth == 1 && node.Key == "Helicopter")
					{
						node.Key = "Aircraft";
						node.Value.Nodes.Add(new MiniYamlNode("CanHover", "True"));
					}

					var mplane = node.Value.Nodes.FirstOrDefault(n => n.Key == "-Plane");
					if (mplane != null)
					{
						// Check if a Helicopter trait was renamed to Aircraft
						// In that case, we don't want to straight negate it with -Aircraft again
						if (node.Value.Nodes.Any(n => n.Key == "Aircraft" || n.Key == "Helicopter"))
						{
							Console.WriteLine("Warning: Removed '-Plane:', this can introduce side effects with inherited 'Aircraft' definitions.");
							node.Value.Nodes.Remove(mplane);
						}
						else
							mplane.Key = "-Aircraft";
					}

					var mheli = node.Value.Nodes.FirstOrDefault(n => n.Key == "-Helicopter");
					if (mheli != null)
					{
						// Check if a Plane trait was renamed to Aircraft
						// In that case, we don't want to straight negate it with -Aircraft again
						if (node.Value.Nodes.Any(n => n.Key == "Aircraft" || n.Key == "Plane"))
						{
							Console.WriteLine("Warning: Removed '-Helicopter:', this can introduce side effects with inherited 'Aircraft' definitions.");
							node.Value.Nodes.Remove(mheli);
						}
						else
							mheli.Key = "-Aircraft";
					}
				}

				if (engineVersion < 20151004)
				{
					// Rename WithRotor to WithSpriteRotorOverlay
					if (depth == 1 && node.Key.StartsWith("WithRotor"))
					{
						var parts = node.Key.Split('@');
						node.Key = "WithSpriteRotorOverlay";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					if (depth == 1 && node.Key.StartsWith("-WithRotor"))
					{
						var parts = node.Key.Split('@');
						node.Key = "-WithSpriteRotorOverlay";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}
				}

				if (engineVersion < 20151005)
				{
					// Units with AutoHeal have it replaced by AutoTarget
					var heal = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("AutoHeal"));
					if (heal != null)
					{
						var otherNodes = nodes;
						var inherits = new Func<string, bool>(traitName => node.Value.Nodes.Where(n => n.Key.StartsWith("Inherits"))
							.Any(inh =>
							{
								var otherNode = otherNodes.FirstOrDefault(n => n.Key.StartsWith(inh.Value.Value));

								if (otherNode == null)
									return false;

								return otherNode.Value.Nodes.Any(n => n.Key.StartsWith(traitName));
							}));

						var target = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("-AutoTarget"));
						if (target != null)
							node.Value.Nodes.Remove(target);
						else if (!inherits("AutoTarget"))
							node.Value.Nodes.Add(new MiniYamlNode("AutoTarget", ""));

						node.Value.Nodes.Remove(heal);
					}

					// Units with AttackMedic have it replaced by an appropriate AttackFrontal
					var atkmedic = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("AttackMedic"));
					if (atkmedic != null)
					{
						var crsr = atkmedic.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("Cursor"));
						var hasorcrsr = atkmedic.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("OutsideRangeCursor"));
						foreach (var armmnt in node.Value.Nodes.Where(n => n.Key.StartsWith("Armament")))
						{
							if (crsr != null)
								armmnt.Value.Nodes.Add(new MiniYamlNode("Cursor", crsr.Value));

							if (hasorcrsr != null)
								armmnt.Value.Nodes.Add(new MiniYamlNode("OutsideRangeCursor", hasorcrsr.Value));
							armmnt.Value.Nodes.Add(new MiniYamlNode("TargetStances", "Ally"));
							armmnt.Value.Nodes.Add(new MiniYamlNode("ForceTargetStances", "None"));
						}

						if (crsr != null)
							atkmedic.Value.Nodes.Remove(crsr);
						if (hasorcrsr != null)
							atkmedic.Value.Nodes.Remove(hasorcrsr);

						atkmedic.Key = "AttackFrontal";
					}
				}

				// ChargeTime is now replaced by ChargeDelay.
				// ChargeDelay uses 500 as a default now.
				if (engineVersion < 20151022)
				{
					if (depth == 2 && parentKey == "PortableChrono" && node.Key == "ChargeTime")
					{
						node.Key = "ChargeDelay";

						if (node.Value.Value != null)
							node.Value.Value = (Exts.ParseIntegerInvariant(node.Value.Value) * 25).ToString();
					}
				}

				// Add InitialStance for bots
				if (engineVersion < 20151025)
				{
					if (depth == 1 && node.Key == "AutoTarget")
					{
						var stance = node.Value.Nodes.FirstOrDefault(n => n.Key == "InitialStance");
						var aiStance = node.Value.Nodes.FirstOrDefault(n => n.Key == "InitialStanceAI");
						if (stance != null && aiStance == null)
							node.Value.Nodes.Add(new MiniYamlNode("InitialStanceAI", stance.Value.Value));
					}
				}

				if (engineVersion < 20151102 && depth == 2)
				{
					if (node.Key == "Color")
					{
						if (parent.Key.StartsWith("FixedColorPalette"))
							TryUpdateHSLColor(ref node.Value.Value);
						else
							TryUpdateColor(ref node.Value.Value);
					}
					else if (node.Key == "RadarPingColor" || node.Key == "SelectionBoxColor" || node.Key == "BarColor")
						TryUpdateColor(ref node.Value.Value);
					else if (node.Key == "Fog" || node.Key == "Shroud" || node.Key == "ParticleColors")
						TryUpdateColors(ref node.Value.Value);
				}

				if (engineVersion < 20151107 && depth == 2)
				{
					if (node.Key == "PaticleSize")
						node.Key = "ParticleSize";
				}

				// DeathType on Explodes was renamed to DeathTypes
				if (engineVersion < 20151110)
				{
					if (node.Key == "Explodes")
					{
						var dt = node.Value.Nodes.FirstOrDefault(n => n.Key == "DeathType");
						if (dt != null)
							dt.Key = "DeathTypes";
					}
				}

				// Upgrades on DeployToUpgrade were renamed to DeployedUpgrades
				if (engineVersion < 20151122)
				{
					if (node.Key == "DeployToUpgrade")
					{
						var u = node.Value.Nodes.FirstOrDefault(n => n.Key == "Upgrades");
						if (u != null)
							u.Key = "DeployedUpgrades";
					}
				}

				if (engineVersion < 20151127)
				{
					// Rename WithTurret to WithSpriteTurret
					if (depth == 1 && node.Key.StartsWith("WithTurret"))
					{
						var parts = node.Key.Split('@');
						node.Key = "WithSpriteTurret";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					if (depth == 1 && node.Key.StartsWith("-WithTurret"))
					{
						var parts = node.Key.Split('@');
						node.Key = "-WithSpriteTurret";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					// Rename WithBarrel to WithSpriteBarrel
					if (depth == 1 && node.Key.StartsWith("WithBarrel"))
					{
						var parts = node.Key.Split('@');
						node.Key = "WithSpriteBarrel";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					if (depth == 1 && node.Key.StartsWith("-WithBarrel"))
					{
						var parts = node.Key.Split('@');
						node.Key = "-WithSpriteBarrel";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					// Rename WithReloadingTurret to WithReloadingSpriteTurret
					if (depth == 1 && node.Key.StartsWith("WithReloadingTurret"))
					{
						var parts = node.Key.Split('@');
						node.Key = "WithReloadingSpriteTurret";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					if (depth == 1 && node.Key.StartsWith("-WithReloadingTurret"))
					{
						var parts = node.Key.Split('@');
						node.Key = "-WithReloadingSpriteTurret";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}
				}

				// Mobile actors immobilized by Carryable, Cargo, DeployToUpgrade, and/or others using upgrade(s)
				if (engineVersion < 20151204 && depth == 0)
				{
					var notMobile = "notmobile";

					var mobileNode = node.Value.Nodes.Find(n => n.Key == "Mobile");
					var carryableNode = node.Value.Nodes.Find(n => n.Key == "Carryable");
					var cargoNode = node.Value.Nodes.Find(n => n.Key == "Cargo");
					var deployToUpgradeNode = node.Value.Nodes.Find(n => n.Key == "DeployToUpgrade");
					var disableUpgradeNode = node.Value.Nodes.Find(n => n.Key == "DisableUpgrade");
					var disableMovementOnUpgradeNode = node.Value.Nodes.Find(n => n.Key == "DisableMovementOnUpgrade");

					Action<MiniYamlNode, string> addNotMobileToTraitUpgrades = (trait, upgradesKey) =>
					{
						if (trait != null)
						{
							var upgrades = trait.Value.Nodes.Find(u => u.Key == upgradesKey);
							if (upgrades == null)
								trait.Value.Nodes.Add(new MiniYamlNode(upgradesKey, notMobile));
							else if (string.IsNullOrEmpty(upgrades.Value.Value))
								upgrades.Value.Value = notMobile;
							else if (!upgrades.Value.Value.Contains(notMobile))
								upgrades.Value.Value += ", " + notMobile;
						}
					};

					if (mobileNode != null)
					{
						var mobileUpgrades = mobileNode.Value.Nodes.Find(n => n.Key == "UpgradeTypes");
						var mobileUpgradeMaxEnabledLevel = mobileNode.Value.Nodes.Find(n => n.Key == "UpgradeMaxEnabledLevel");
						var comma = new char[] { ',' };

						Func<bool> addUpgradeMaxEnabledLevelNode = () =>
						{
							if (mobileUpgradeMaxEnabledLevel == null)
							{
								mobileUpgradeMaxEnabledLevel = new MiniYamlNode("UpgradeMaxEnabledLevel", "0");
								mobileNode.Value.Nodes.Add(mobileUpgradeMaxEnabledLevel);
								return true;
							}
							else
								return mobileUpgradeMaxEnabledLevel.Value.Value == "0";
						};

						// If exactly one upgrade type is in UpgradeTypes and UpgradeMaxEnabledLevel is/can be 0 , then use it as notmobile
						if (mobileUpgrades != null && !string.IsNullOrEmpty(mobileUpgrades.Value.Value)
							&& !mobileUpgrades.Value.Value.Contains(",") && addUpgradeMaxEnabledLevelNode())
							notMobile = mobileUpgrades.Value.Value;

						if (mobileUpgradeMaxEnabledLevel != null && mobileUpgradeMaxEnabledLevel.Value.Value != "0")
							Console.WriteLine("\t\t" + node.Key + " actor rules may require manual upgrading for immobilization upgrade logic.");
						else
						{
							Action<string> addImmobilizeUpgradeType = upgradeType =>
							{
								if (mobileUpgrades == null)
								{
									mobileUpgrades = new MiniYamlNode("UpgradeTypes", upgradeType);
									mobileNode.Value.Nodes.Add(mobileUpgrades);
								}
								else if (string.IsNullOrEmpty(mobileUpgrades.Value.Value))
									mobileUpgrades.Value.Value = upgradeType;
								else if (!mobileUpgrades.Value.Value.Split(comma).Contains(upgradeType))
									mobileUpgrades.Value.Value += ", " + upgradeType;
							};

							Predicate<string> addImmobilizeUpgradeTypes = upgradeTypes =>
							{
								if (string.IsNullOrEmpty(upgradeTypes))
									return false;

								foreach (var upgradeType in upgradeTypes.Split(comma))
									addImmobilizeUpgradeType(upgradeType);
								return true;
							};

							Predicate<MiniYamlNode> addUpgradeTypeFromTrait = trait =>
							{
								var upgradeTypesNode = trait.Value.Nodes.Find(n => n.Key == "UpgradeTypes");
								if (upgradeTypesNode == null)
									return false;

								addUpgradeMaxEnabledLevelNode();
								return addImmobilizeUpgradeTypes(upgradeTypesNode.Value.Value);
							};

							var noticeWritten = false;

							Action writeNotice = () =>
							{
								if (noticeWritten)
									return;
								Console.WriteLine("\t\t" + node.Key + " actor rules may require manual upgrading for immobilization upgrade logic.");
								noticeWritten = true;
							};

							if (disableUpgradeNode != null && !addUpgradeTypeFromTrait(disableUpgradeNode))
							{
								writeNotice();
								Console.WriteLine("\t\t\tOne or more upgrades may need to be copied from the DisableUpgrade trait to the Mobile trait.");
							}

							if (disableMovementOnUpgradeNode != null)
							{
								if (addUpgradeTypeFromTrait(disableMovementOnUpgradeNode))
									parent.Value.Nodes.Remove(disableMovementOnUpgradeNode);
								else
								{
									writeNotice();
									Console.WriteLine("\t\t\tOne or more upgrades may need to be moved from the DisableMovementOnUpgrade trait to the Mobile trait.");
									Console.WriteLine("\t\t\t\tRemember to remove the DisableMovementOnUpgrade trait.");
								}
							}

							if (carryableNode != null || cargoNode != null || deployToUpgradeNode != null)
							{
								addUpgradeMaxEnabledLevelNode();
								addImmobilizeUpgradeTypes(notMobile);

								addNotMobileToTraitUpgrades(carryableNode, "CarryableUpgrades");
								addNotMobileToTraitUpgrades(cargoNode, "LoadingUpgrades");
								addNotMobileToTraitUpgrades(deployToUpgradeNode, "DeployedUpgrades");
							}
						}
					}
					else if (!node.Value.Nodes.Exists(n => n.Key == "Husk" || n.Key == "Building" || n.Key == "Aircraft" || n.Key == "Immobile"))
					{
						if (carryableNode != null || cargoNode != null || deployToUpgradeNode != null)
						{
							Console.WriteLine("\t\tIf " + node.Key
								+ " has a Mobile trait then adding the following with <upgrade> substituted by an immobilization upgrade for "
								+ node.Key + " may be neeeded:");

							if (carryableNode != null)
							{
								Console.WriteLine("\t\t\tCarryable:");
								Console.WriteLine("\t\t\t\tCarryableUpgrades: <upgrade>");
							}

							if (cargoNode != null)
							{
								Console.WriteLine("\t\t\tCargo:");
								Console.WriteLine("\t\t\t\tLoadingUpgrades: <upgrade>");
							}

							if (deployToUpgradeNode != null)
							{
								Console.WriteLine("\t\t\tDeployToUpgrade:");
								Console.WriteLine("\t\t\t\tDeployedUpgrades: <upgrade>");
							}
						}

						var disableUpgradeUpgradeTypesNode = disableUpgradeNode != null
							? disableUpgradeNode.Value.Nodes.Find(n => n.Key == "UpgradeTypes")
							: null;
						var disableMovementOnUpgradeUpgradeTypesNode = disableMovementOnUpgradeNode != null
							? disableMovementOnUpgradeNode.Value.Nodes.Find(n => n.Key == "UpgradeTypes")
							: null;

						if (disableUpgradeUpgradeTypesNode != null || disableMovementOnUpgradeUpgradeTypesNode != null)
							Console.WriteLine("\t\t" + node.Key + " actor rules may require manual upgrading for immobilization upgrade logic.");

						if (disableUpgradeUpgradeTypesNode != null)
							Console.WriteLine("\t\t\tDisableUpgrade UpgradeTypes: " + disableUpgradeUpgradeTypesNode.Value.Value);

						if (disableMovementOnUpgradeUpgradeTypesNode != null)
							Console.WriteLine("\t\t\tDisableMovementOnUpgrade UpgradeTypes: " + disableMovementOnUpgradeUpgradeTypesNode.Value.Value);

						if (disableMovementOnUpgradeNode != null)
							node.Value.Nodes.Remove(disableMovementOnUpgradeNode);
					}
				}

				// 'CloseEnough' on 'RepairableNear' uses WDist now
				if (engineVersion < 20151214)
				{
					if (node.Key == "RepairableNear")
					{
						var ce = node.Value.Nodes.FirstOrDefault(n => n.Key == "CloseEnough");
						if (ce != null && !ce.Value.Value.Contains("c"))
							ce.Value.Value = ce.Value.Value + "c0";
					}
				}

				// Added width support for line particles
				if (engineVersion < 20151220 && node.Key == "WeatherOverlay")
				{
					var useSquares = node.Value.Nodes.FirstOrDefault(n => n.Key == "UseSquares");
					if (useSquares != null && !FieldLoader.GetValue<bool>("UseSquares", useSquares.Value.Value))
						node.Value.Nodes.Add(new MiniYamlNode("ParticleSize", "1, 1"));
				}

				// Overhauled the actor decorations traits
				if (engineVersion < 20151226)
				{
					if (depth == 1 && (node.Key.StartsWith("WithDecoration") || node.Key.StartsWith("WithRankDecoration")))
					{
						node.Value.Nodes.RemoveAll(n => n.Key == "Scale");
						node.Value.Nodes.RemoveAll(n => n.Key == "Offset");
						var sd = node.Value.Nodes.FirstOrDefault(n => n.Key == "SelectionDecoration");
						if (sd != null)
							sd.Key = "RequiresSelection";

						var reference = node.Value.Nodes.FirstOrDefault(n => n.Key == "ReferencePoint");
						if (reference != null)
						{
							var values = FieldLoader.GetValue<string[]>("ReferencePoint", reference.Value.Value);
							values = values.Where(v => v != "HCenter" && v != "VCenter").ToArray();
							if (values.Length == 0)
								values = new[] { "Center" };

							reference.Value.Value = FieldSaver.FormatValue(values);
						}

						var stance = Stance.Ally;
						var showToAllies = node.Value.Nodes.FirstOrDefault(n => n.Key == "ShowToAllies");
						if (showToAllies != null && !FieldLoader.GetValue<bool>("ShowToAllies", showToAllies.Value.Value))
							stance ^= Stance.Ally;
						var showToEnemies = node.Value.Nodes.FirstOrDefault(n => n.Key == "ShowToEnemies");
						if (showToEnemies != null && FieldLoader.GetValue<bool>("ShowToEnemies", showToEnemies.Value.Value))
							stance |= Stance.Enemy;

						if (stance != Stance.Ally)
							node.Value.Nodes.Add(new MiniYamlNode("Stance", FieldSaver.FormatValue(stance)));

						node.Value.Nodes.RemoveAll(n => n.Key == "ShowToAllies");
						node.Value.Nodes.RemoveAll(n => n.Key == "ShowToEnemies");
					}

					if (depth == 1 && node.Key == "Fake")
					{
						node.Key = "WithDecoration@fake";
						node.Value.Nodes.Add(new MiniYamlNode("RequiresSelection", "true"));
						node.Value.Nodes.Add(new MiniYamlNode("Image", "pips"));
						node.Value.Nodes.Add(new MiniYamlNode("Sequence", "tag-fake"));
						node.Value.Nodes.Add(new MiniYamlNode("ReferencePoint", "Top"));
						node.Value.Nodes.Add(new MiniYamlNode("ZOffset", "256"));
					}

					if (depth == 0 && node.Value.Nodes.Any(n => n.Key.StartsWith("PrimaryBuilding")))
					{
						var decNodes = new List<MiniYamlNode>();
						decNodes.Add(new MiniYamlNode("RequiresSelection", "true"));
						decNodes.Add(new MiniYamlNode("Image", "pips"));
						decNodes.Add(new MiniYamlNode("Sequence", "tag-primary"));
						decNodes.Add(new MiniYamlNode("ReferencePoint", "Top"));
						decNodes.Add(new MiniYamlNode("ZOffset", "256"));
						decNodes.Add(new MiniYamlNode("UpgradeTypes", "primary"));
						decNodes.Add(new MiniYamlNode("UpgradeMinEnabledLevel", "1"));
						node.Value.Nodes.Add(new MiniYamlNode("WithDecoration@primary", new MiniYaml("", decNodes)));
					}
				}

				// Refactored the low resources notification to a separate trait
				if (engineVersion < 20151227 && node.Key == "Player")
				{
					var resourcesNode = node.Value.Nodes.FirstOrDefault(x => x.Key == "PlayerResources");

					if (resourcesNode != null)
					{
						var intervalNode = resourcesNode.Value.Nodes.FirstOrDefault(x => x.Key == "AdviceInterval");
						var storageNode = new MiniYamlNode("ResourceStorageWarning", "");

						if (intervalNode != null)
						{
							// The time value is now in seconds, not ticks. We
							// divide by 25 ticks per second at Normal.
							int oldInterval;
							if (int.TryParse(intervalNode.Value.Value, out oldInterval))
								storageNode.Value.Nodes.Add(new MiniYamlNode("AdviceInterval", (oldInterval / 25).ToString()));
							resourcesNode.Value.Nodes.Remove(intervalNode);
						}

						node.Value.Nodes.Add(storageNode);
					}
				}

				// Refactored Health.Radius to HitShapes
				if (engineVersion < 20151227)
				{
					if (node.Key.StartsWith("Health"))
					{
						var radius = node.Value.Nodes.FirstOrDefault(x => x.Key == "Radius");
						if (radius != null)
						{
							var radiusValue = FieldLoader.GetValue<string>("Radius", radius.Value.Value);
							node.Value.Nodes.Add(new MiniYamlNode("Shape", "Circle"));

							var shape = node.Value.Nodes.First(x => x.Key == "Shape");
							shape.Value.Nodes.Add(new MiniYamlNode("Radius", radiusValue));

							node.Value.Nodes.Remove(radius);
						}
					}
				}

				// Remove obsolete TransformOnPassenger trait.
				if (engineVersion < 20160102)
				{
					var removed = node.Value.Nodes.RemoveAll(x => x.Key.Contains("TransformOnPassenger"));
					if (removed > 0)
					{
						Console.WriteLine("TransformOnPassenger has been removed.");
						Console.WriteLine("Use the upgrades system to apply modifiers to the transport actor instead.");
					}
				}

				if (engineVersion < 20160103)
				{
					// Overhauled WithActiveAnimation -> WithIdleAnimation
					if (node.Key == "WithActiveAnimation")
					{
						node.Key = "WithIdleAnimation";
						foreach (var n in node.Value.Nodes)
							if (n.Key == "Sequence")
								n.Key = "Sequences";
					}
				}

				if (engineVersion < 20160701 && depth == 1 && node.Key.StartsWith("Cloak"))
				{
					var defaultCloakType = Traits.UncloakType.Attack
						| Traits.UncloakType.Unload | Traits.UncloakType.Infiltrate | Traits.UncloakType.Demolish;

					// Merge Uncloak types
					var t = defaultCloakType;
					for (var i = node.Value.Nodes.Count - 1; i >= 0; i--)
					{
						var n = node.Value.Nodes[i];
						var v = string.Compare(n.Value.Value, "true", true) == 0;
						Traits.UncloakType flag;
						if (n.Key == "UncloakOnAttack")
							flag = Traits.UncloakType.Attack;
						else if (n.Key == "UncloakOnMove")
							flag = Traits.UncloakType.Move;
						else if (n.Key == "UncloakOnUnload")
							flag = Traits.UncloakType.Unload;
						else if (n.Key == "UncloakOnInfiltrate")
							flag = Traits.UncloakType.Infiltrate;
						else if (n.Key == "UncloakOnDemolish")
							flag = Traits.UncloakType.Demolish;
						else
							continue;
						t = v ? t | flag : t & ~flag;
						node.Value.Nodes.Remove(n);
					}

					if (t != defaultCloakType)
					{
						Console.WriteLine("\t\tCloak type: " + t.ToString());
						var ts = new List<string>();
						if (t.HasFlag(Traits.UncloakType.Attack))
							ts.Add("Attack");
						if (t.HasFlag(Traits.UncloakType.Unload))
							ts.Add("Unload");
						if (t.HasFlag(Traits.UncloakType.Infiltrate))
							ts.Add("Infiltrate");
						if (t.HasFlag(Traits.UncloakType.Demolish))
							ts.Add("Demolish");
						if (t.HasFlag(Traits.UncloakType.Move))
							ts.Add("Move");
						node.Value.Nodes.Add(new MiniYamlNode("UncloakOn", ts.JoinWith(", ")));
					}
				}

				UpgradeActorRules(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeWeaponRules(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			var parentKey = parent != null ? parent.Key.Split('@').First() : null;

			foreach (var node in nodes)
			{
				// Weapon definitions were converted to world coordinates
				if (engineVersion < 20131226)
				{
					if (depth == 1)
					{
						switch (node.Key)
						{
							case "Range":
							case "MinRange":
								ConvertFloatToRange(ref node.Value.Value);
								break;
							default:
								break;
						}
					}

					if (depth == 2 && parentKey == "Projectile")
					{
						switch (node.Key)
						{
							case "Inaccuracy":
								ConvertPxToRange(ref node.Value.Value);
								break;
							case "Angle":
								ConvertAngle(ref node.Value.Value);
								break;
							case "Speed":
								{
									if (parent.Value.Value == "Missile")
										ConvertPxToRange(ref node.Value.Value, 1, 5);
									if (parent.Value.Value == "Bullet")
										ConvertPxToRange(ref node.Value.Value, 2, 5);
									break;
								}

							default:
								break;
						}
					}

					if (depth == 2 && parentKey == "Warhead")
					{
						switch (node.Key)
						{
							case "Spread":
								ConvertPxToRange(ref node.Value.Value);
								break;
							default:
								break;
						}
					}
				}

				if (engineVersion < 20140615)
				{
					if (depth == 2 && parentKey == "Warhead" && node.Key == "Ore")
						node.Key = "DestroyResources";
				}

				if (engineVersion < 20140720)
				{
					// Split out the warheads to individual warhead types.
					if (depth == 0)
					{
						// Weapon's ValidTargets need to be copied to the warheads, so find it
						var validTargets = node.Value.Nodes.FirstOrDefault(n => n.Key == "ValidTargets");

						// Weapon's InvalidTargets need to be copied to the warheads, so find it
						var invalidTargets = node.Value.Nodes.FirstOrDefault(n => n.Key == "InvalidTargets");

						var warheadCounter = 0;
						foreach (var curNode in node.Value.Nodes.ToArray())
						{
							if (curNode.Key.Contains("Warhead") && curNode.Value.Value == null)
							{
								var newNodes = new List<MiniYamlNode>();
								var oldNodeAtName = "";
								if (curNode.Key.Contains('@'))
									oldNodeAtName = "_" + curNode.Key.Split('@')[1];

								// Per Cell Damage Model
								if (curNode.Value.Nodes.Any(n => n.Key.Contains("DamageModel") &&
									n.Value.Value.Contains("PerCell")))
								{
									warheadCounter++;

									var newYaml = new List<MiniYamlNode>();

									var temp = curNode.Value.Nodes.FirstOrDefault(n => n.Key == "Size"); // New PerCell warhead allows 2 sizes, as opposed to 1 size
									if (temp != null)
									{
										var newValue = temp.Value.Value.Split(',').First();
										newYaml.Add(new MiniYamlNode("Size", newValue));
									}

									var keywords = new List<string> { "Damage", "InfDeath", "PreventProne", "ProneModifier", "Delay" };

									foreach (var keyword in keywords)
									{
										var temp2 = curNode.Value.Nodes.FirstOrDefault(n => n.Key == keyword);
										if (temp2 != null)
											newYaml.Add(new MiniYamlNode(keyword, temp2.Value.Value));
									}

									if (validTargets != null)
										newYaml.Add(validTargets);
									if (invalidTargets != null)
										newYaml.Add(invalidTargets);

									var tempVersus = curNode.Value.Nodes.FirstOrDefault(n => n.Key == "Versus");
									if (tempVersus != null)
										newYaml.Add(new MiniYamlNode("Versus", tempVersus.Value));

									newNodes.Add(new MiniYamlNode("Warhead@" + warheadCounter.ToString() + "Dam" + oldNodeAtName, "PerCellDamage", newYaml));
								}

								// HealthPercentage damage model
								if (curNode.Value.Nodes.Any(n => n.Key.Contains("DamageModel") &&
									n.Value.Value.Contains("HealthPercentage")))
								{
									warheadCounter++;

									var newYaml = new List<MiniYamlNode>();

									// New HealthPercentage warhead allows 2 spreads, as opposed to 1 size
									var temp = curNode.Value.Nodes.FirstOrDefault(n => n.Key == "Size");
									if (temp != null)
									{
										var newValue = temp.Value.Value.Split(',').First() + "c0";
										newYaml.Add(new MiniYamlNode("Spread", newValue));
									}

									var keywords = new List<string> { "Damage", "InfDeath", "PreventProne", "ProneModifier", "Delay" };

									foreach (var keyword in keywords)
									{
										var temp2 = curNode.Value.Nodes.FirstOrDefault(n => n.Key == keyword);
										if (temp2 != null)
											newYaml.Add(new MiniYamlNode(keyword, temp2.Value.Value));
									}

									if (validTargets != null)
										newYaml.Add(validTargets);
									if (invalidTargets != null)
										newYaml.Add(invalidTargets);

									var tempVersus = curNode.Value.Nodes.FirstOrDefault(n => n.Key == "Versus");
									if (tempVersus != null)
										newYaml.Add(new MiniYamlNode("Versus", tempVersus.Value));

									newNodes.Add(new MiniYamlNode("Warhead@" + warheadCounter.ToString() + "Dam" + oldNodeAtName, "HealthPercentageDamage", newYaml));
								}

								// SpreadDamage
								// Always occurs, since by definition all warheads were SpreadDamage warheads before
								{
									warheadCounter++;

									var newYaml = new List<MiniYamlNode>();

									var keywords = new List<string> { "Spread", "Damage", "InfDeath", "PreventProne", "ProneModifier", "Delay" };

									foreach (var keyword in keywords)
									{
										var temp = curNode.Value.Nodes.FirstOrDefault(n => n.Key == keyword);
										if (temp != null)
											newYaml.Add(new MiniYamlNode(keyword, temp.Value.Value));
									}

									if (validTargets != null)
										newYaml.Add(validTargets);
									if (invalidTargets != null)
										newYaml.Add(invalidTargets);

									var tempVersus = curNode.Value.Nodes.FirstOrDefault(n => n.Key == "Versus");
									if (tempVersus != null)
										newYaml.Add(new MiniYamlNode("Versus", tempVersus.Value));

									newNodes.Add(new MiniYamlNode("Warhead@" + warheadCounter.ToString() + "Dam" + oldNodeAtName, "SpreadDamage", newYaml));
								}

								// DestroyResource
								if (curNode.Value.Nodes.Any(n => n.Key.Contains("DestroyResources") ||
									n.Key.Contains("Ore")))
								{
									warheadCounter++;

									var newYaml = new List<MiniYamlNode>();

									var keywords = new List<string> { "Size", "Delay", "ValidTargets", "InvalidTargets" };
									foreach (var keyword in keywords)
									{
										var temp = curNode.Value.Nodes.FirstOrDefault(n => n.Key == keyword);
										if (temp != null)
											newYaml.Add(new MiniYamlNode(keyword, temp.Value.Value));
									}

									newNodes.Add(new MiniYamlNode("Warhead@" + warheadCounter.ToString() + "Res" + oldNodeAtName, "DestroyResource", newYaml));
								}

								// CreateResource
								if (curNode.Value.Nodes.Any(n => n.Key.Contains("AddsResourceType")))
								{
									warheadCounter++;

									var newYaml = new List<MiniYamlNode>();

									var keywords = new List<string> { "AddsResourceType", "Size", "Delay", "ValidTargets", "InvalidTargets" };

									foreach (var keyword in keywords)
									{
										var temp = curNode.Value.Nodes.FirstOrDefault(n => n.Key == keyword);
										if (temp != null)
											newYaml.Add(new MiniYamlNode(keyword, temp.Value.Value));
									}

									newNodes.Add(new MiniYamlNode("Warhead@" + warheadCounter.ToString() + "Res" + oldNodeAtName, "CreateResource", newYaml));
								}

								// LeaveSmudge
								if (curNode.Value.Nodes.Any(n => n.Key.Contains("SmudgeType")))
								{
									warheadCounter++;

									var newYaml = new List<MiniYamlNode>();

									var keywords = new List<string> { "SmudgeType", "Size", "Delay", "ValidTargets", "InvalidTargets" };

									foreach (var keyword in keywords)
									{
										var temp = curNode.Value.Nodes.FirstOrDefault(n => n.Key == keyword);
										if (temp != null)
											newYaml.Add(new MiniYamlNode(keyword, temp.Value.Value));
									}

									newNodes.Add(new MiniYamlNode("Warhead@" + warheadCounter.ToString() + "Smu" + oldNodeAtName, "LeaveSmudge", newYaml));
								}

								// CreateEffect - Explosion
								if (curNode.Value.Nodes.Any(n => n.Key.Contains("Explosion") ||
									n.Key.Contains("ImpactSound")))
								{
									warheadCounter++;

									var newYaml = new List<MiniYamlNode>();

									var keywords = new List<string> { "Explosion", "ImpactSound", "Delay",
										"ValidTargets", "InvalidTargets", "ValidImpactTypes", "InvalidImpactTypes" };

									foreach (var keyword in keywords)
									{
										var temp = curNode.Value.Nodes.FirstOrDefault(n => n.Key == keyword);
										if (temp != null)
											newYaml.Add(new MiniYamlNode(keyword, temp.Value.Value));
									}

									newYaml.Add(new MiniYamlNode("InvalidImpactTypes", "Water"));
									newNodes.Add(new MiniYamlNode("Warhead@" + warheadCounter.ToString() + "Eff" + oldNodeAtName, "CreateEffect", newYaml));
								}

								// CreateEffect - Water Explosion
								if (curNode.Value.Nodes.Any(n => n.Key.Contains("WaterExplosion") ||
									n.Key.Contains("WaterImpactSound")))
								{
									warheadCounter++;

									var newYaml = new List<MiniYamlNode>();

									var keywords = new List<string> { "WaterExplosion", "WaterImpactSound", "Delay",
										"ValidTargets", "InvalidTargets", "ValidImpactTypes", "InvalidImpactTypes" };

									foreach (var keyword in keywords)
									{
										var temp = curNode.Value.Nodes.FirstOrDefault(n => n.Key == keyword);
										if (temp != null)
										{
											if (temp.Key == "WaterExplosion")
												temp.Key = "Explosion";
											if (temp.Key == "WaterImpactSound")
												temp.Key = "ImpactSound";
											newYaml.Add(new MiniYamlNode(temp.Key, temp.Value.Value));
										}
									}

									newYaml.Add(new MiniYamlNode("ValidImpactTypes", "Water"));

									newNodes.Add(new MiniYamlNode("Warhead@" + warheadCounter.ToString() + "Eff" + oldNodeAtName, "CreateEffect", newYaml));
								}

								node.Value.Nodes.InsertRange(node.Value.Nodes.IndexOf(curNode), newNodes);
								node.Value.Nodes.Remove(curNode);
							}
						}
					}
				}

				if (engineVersion < 20140818)
				{
					if (depth == 1)
					{
						if (node.Key == "ROF")
							node.Key = "ReloadDelay";
					}
				}

				if (engineVersion < 20140821)
				{
					// Converted versus definitions to integers
					if (depth == 3 && parentKey == "Versus")
						ConvertFloatArrayToPercentArray(ref node.Value.Value);
				}

				if (engineVersion < 20140830)
				{
					if (depth == 2)
					{
						if (node.Key == "InfDeath")
							node.Key = "DeathType";
					}
				}

				// Remove PerCellDamageWarhead
				if (engineVersion < 20150213)
				{
					if (depth == 1 && node.Value.Nodes.Exists(n => n.Key == "PerCellDamage"))
					{
						node.Value.Nodes.RemoveAll(n => n.Key == "PerCellDamage");
						Console.WriteLine("The 'PerCellDamage' warhead has been removed.");
						Console.WriteLine("Please use the 'SpreadDamage' warhead instead.");
					}
				}

				if (engineVersion < 20150326)
				{
					// Remove TurboBoost from missiles
					if (depth == 1 && node.Key == "Projectile" && node.Value.Nodes.Exists(n => n.Key == "TurboBoost"))
					{
						node.Value.Nodes.RemoveAll(n => n.Key == "TurboBoost");
						Console.WriteLine("'TurboBoost' has been removed.");
						Console.WriteLine("If you want to reproduce its behavior, create a duplicate");
						Console.WriteLine("of the weapon in question, change it to be anti-air only,");
						Console.WriteLine("increase its speed, make the original weapon anti-ground only,");
						Console.WriteLine("and add the new weapon as additional armament to the actor.");
					}

					// Rename ROT to RateOfTurn
					if (depth == 2 && node.Key == "ROT")
						node.Key = "RateOfTurn";

					// Rename High to Blockable
					if (depth == 2 && parentKey == "Projectile" && node.Key == "High")
					{
						var highField = node.Value.Value != null ? FieldLoader.GetValue<bool>("High", node.Value.Value) : false;
						var blockable = !highField;

						node.Value.Value = blockable.ToString().ToLowerInvariant();
						node.Key = "Blockable";
					}

					// Move Palette from weapon to projectiles
					if (depth == 0)
					{
						var weapons = node.Value.Nodes;
						var palette = weapons.FirstOrDefault(p => p.Key == "Palette");
						var projectile = weapons.FirstOrDefault(r => r.Key == "Projectile");

						if (palette != null)
						{
							var projectileFields = projectile.Value.Nodes;
							var paletteName = palette.Value.Value != null ? FieldLoader.GetValue<string>("Palette", palette.Value.Value) : "effect";

							projectileFields.Add(new MiniYamlNode("Palette", paletteName.ToString()));
							weapons.Remove(palette);
						}
					}
				}

				if (engineVersion < 20150421)
				{
					if (node.Key.StartsWith("Warhead") && node.Value.Value == "SpreadDamage")
					{
						// Add DamageTypes property to DamageWarheads with a default value "Prone50Percent"
						if (node.Value.Nodes.All(x => x.Key != "DamageTypes"))
						{
							var damage = node.Value.Nodes.FirstOrDefault(x => x.Key == "Damage");
							var damageValue = damage != null ? FieldLoader.GetValue<int>("Damage", damage.Value.Value) : -1;

							var prone = node.Value.Nodes.FirstOrDefault(x => x.Key == "PreventProne");
							var preventsProne = prone != null && FieldLoader.GetValue<bool>("PreventProne", prone.Value.Value);

							var proneModifier = node.Value.Nodes.FirstOrDefault(x => x.Key == "ProneModifier");
							var modifierValue = proneModifier == null ? "50" : proneModifier.Value.Value;

							var value = new List<string>();

							if (damageValue > 0)
								value.Add("Prone{0}Percent".F(modifierValue));

							if (!preventsProne)
								value.Add("TriggerProne");

							if (value.Any())
								node.Value.Nodes.Add(new MiniYamlNode("DamageTypes", value.JoinWith(", ")));
						}

						// Remove obsolete PreventProne and ProneModifier
						node.Value.Nodes.RemoveAll(x => x.Key == "PreventProne");
						node.Value.Nodes.RemoveAll(x => x.Key == "ProneModifier");
					}
				}

				if (engineVersion < 20150524)
				{
					// Remove DeathType from DamageWarhead
					if (node.Key.StartsWith("Warhead") && node.Value.Value == "SpreadDamage")
					{
						var deathTypeNode = node.Value.Nodes.FirstOrDefault(x => x.Key == "DeathType");
						var deathType = deathTypeNode == null ? "1" : FieldLoader.GetValue<string>("DeathType", deathTypeNode.Value.Value);
						var damageTypes = node.Value.Nodes.FirstOrDefault(x => x.Key == "DamageTypes");
						if (damageTypes != null)
							damageTypes.Value.Value += ", DeathType" + deathType;
						else
							node.Value.Nodes.Add(new MiniYamlNode("DamageTypes", "DeathType" + deathType));

						node.Value.Nodes.RemoveAll(x => x.Key == "DeathType");
					}

					// Replace "DeathTypeX" damage types with proper words
					if (node.Key.StartsWith("Warhead") && node.Value.Value == "SpreadDamage")
					{
						var damageTypes = node.Value.Nodes.FirstOrDefault(x => x.Key == "DamageTypes");
						if (damageTypes != null)
							RenameDamageTypes(damageTypes);
					}
				}

				if (engineVersion < 20150526)
				{
					var isNukePower = node.Key == "NukePower";
					var isIonCannonPower = node.Key == "IonCannonPower";

					if ((isNukePower || isIonCannonPower) && !node.Value.Nodes.Any(n => n.Key == "Cursor"))
					{
						var cursor = isIonCannonPower ? "ioncannon" : "nuke";
						node.Value.Nodes.Add(new MiniYamlNode("Cursor", cursor));
					}
				}

				if (engineVersion < 20150809)
				{
					// Removed 0% versus armor type = cannot target actor assumptions from warheads
					if (depth == 3 && parentKey == "Versus" && node.Value.Value == "0")
					{
						Console.WriteLine("The '0% versus armor type = cannot target this actor' assumption has been removed.");
						Console.WriteLine("If you want to reproduce its behavior, use ValidTargets/InvalidTargets in");
						Console.WriteLine("conjunction with one of the Targetable* actor traits.");
					}
				}

				if (engineVersion < 20150828)
				{
					if (depth == 2 && parentKey == "Projectile" && parent.Value.Value == "Bullet" && node.Key == "Sequence")
					{
						node.Key = "Sequences";
					}
				}

				if (engineVersion < 20151009)
				{
					if (depth == 2 && parentKey == "Projectile" && parent.Value.Value == "Missile" && node.Key == "Speed")
						node.Key = "MaximumLaunchSpeed";

					if (depth == 2 && parentKey == "Projectile" && parent.Value.Value == "Missile" && node.Key == "RateOfTurn")
						node.Key = "HorizontalRateOfTurn";

					if (depth == 2 && parentKey == "Projectile" && parent.Value.Value == "Missile" && node.Key == "Trail")
						node.Key = "TrailImage";
				}

				if (engineVersion < 20151102)
				{
					if (node.Key == "Color" || node.Key == "ContrailColor")
						TryUpdateColor(ref node.Value.Value);
				}

				if (engineVersion < 20151129)
				{
					if (node.Key == "BeamWidth" && parent.Value.Value == "LaserZap")
					{
						node.Key = "Width";
						ConvertPxToRange(ref node.Value.Value);
					}
				}

				UpgradeWeaponRules(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeTileset(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			var parentKey = parent != null ? parent.Key.Split('@').First() : null;
			var addNodes = new List<MiniYamlNode>();

			foreach (var node in nodes)
			{
				if (engineVersion < 20140104)
				{
					if (depth == 2 && parentKey == "TerrainType" && node.Key.Split('@').First() == "Type")
						addNodes.Add(new MiniYamlNode("TargetTypes", node.Value.Value == "Water" ? "Water" : "Ground"));
				}

				if (engineVersion < 20150330)
					if (depth == 2 && node.Key == "Image")
						node.Key = "Images";

				if (engineVersion < 20151102)
				{
					if (node.Key == "LeftColor" || node.Key == "RightColor" || node.Key == "Color")
						TryUpdateColor(ref node.Value.Value);
					else if (node.Key == "HeightDebugColors")
						TryUpdateColors(ref node.Value.Value);
				}

				UpgradeTileset(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}

			nodes.AddRange(addNodes);
		}

		internal static void UpgradeCursors(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				if (engineVersion < 20141113 && depth == 3)
				{
					if (node.Key == "start")
						node.Key = "Start";
					else if (node.Key == "length")
						node.Key = "Length";
					else if (node.Key == "end")
						node.Key = "End";
					else if (node.Key == "x")
						node.Key = "X";
					else if (node.Key == "y")
						node.Key = "Y";
				}

				UpgradeCursors(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradePlayers(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Rename PlayerReference.Race and LockRace to Faction and LockFaction
				if (engineVersion < 20150706)
				{
					var race = node.Value.Nodes.FirstOrDefault(x => x.Key == "Race");
					if (race != null)
						race.Key = "Faction";

					var lockRace = node.Value.Nodes.FirstOrDefault(x => x.Key == "LockRace");
					if (lockRace != null)
						lockRace.Key = "LockFaction";
				}

				if (engineVersion < 20151102 && node.Key == "ColorRamp")
				{
					TryUpdateHSLColor(ref node.Value.Value);
					node.Key = "Color";
				}

				UpgradePlayers(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeChromeMetrics(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				if (engineVersion < 20151102)
				{
					if (node.Key.EndsWith("Color") || node.Key.EndsWith("ColorDisabled") || node.Key.EndsWith("ColorInvalid"))
						TryUpdateColor(ref node.Value.Value);
				}

				UpgradeChromeMetrics(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeChromeLayout(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			var parentKey = parent != null ? parent.Key.Split('@').First() : null;

			foreach (var node in nodes)
			{
				if (engineVersion < 20151102)
				{
					if (node.Key == "Color" || node.Key == "ReadyTextAltColor" || node.Key == "TextColor" || node.Key == "TextColorDisabled")
					{
						if (parentKey == "MapPreview")
							TryUpdateHSLColor(ref node.Value.Value);
						else
							TryUpdateColor(ref node.Value.Value);
					}
				}

				UpgradeChromeLayout(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeActors(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				if (engineVersion < 20150430)
				{
					if (node.Key == "Health")
						ConvertFloatToIntPercentage(ref node.Value.Value);
				}

				if (engineVersion < 20150715)
				{
					if (node.Key == "Race")
						node.Key = "Faction";
				}

				// Rename D2k actors to match the original game.
				if (engineVersion < 20150909 && Game.ModData.Manifest.Mod.Id == "d2k")
				{
					node.Value.Value = RenameD2kActors(node.Value.Value);
				}

				if (engineVersion < 20150925)
				{
					if (node.Key == "DisableUpgrade")
						node.Key = "DisableOnUpgrade";
				}

				UpgradeActors(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}
	}
}
