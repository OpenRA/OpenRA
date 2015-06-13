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
using System.Globalization;
using System.Linq;

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

			if (value < 1)
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
			var ts = Game.ModData.Manifest.TileSize;
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
			var ts = Game.ModData.Manifest.TileSize;
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

				// AttackMove was generalized to support all moveable actor types
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

					if (depth == 2 && parentKey == "SpawnsViceroid" && node.Key == "InfDeath")
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
				if (engineVersion < 20150530)
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
								if (curNode.Value.Nodes.Where(n => n.Key.Contains("DamageModel") &&
										n.Value.Value.Contains("PerCell")).Any())
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
								if (curNode.Value.Nodes.Where(n => n.Key.Contains("DamageModel") &&
										n.Value.Value.Contains("HealthPercentage")).Any())
								{
									warheadCounter++;

									var newYaml = new List<MiniYamlNode>();

									var temp = curNode.Value.Nodes.FirstOrDefault(n => n.Key == "Size"); // New HealthPercentage warhead allows 2 spreads, as opposed to 1 size
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
								{ // Always occurs, since by definition all warheads were SpreadDamage warheads before
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
								if (curNode.Value.Nodes.Where(n => n.Key.Contains("DestroyResources") ||
										n.Key.Contains("Ore")).Any())
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
								if (curNode.Value.Nodes.Where(n => n.Key.Contains("AddsResourceType")).Any())
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
								if (curNode.Value.Nodes.Where(n => n.Key.Contains("SmudgeType")).Any())
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
								if (curNode.Value.Nodes.Where(n => n.Key.Contains("Explosion") ||
										n.Key.Contains("ImpactSound")).Any())
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
								if (curNode.Value.Nodes.Where(n => n.Key.Contains("WaterExplosion") ||
										n.Key.Contains("WaterImpactSound")).Any())
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
				UpgradePlayers(engineVersion, ref node.Value.Nodes, node, depth + 1);
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

				UpgradeActors(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}
	}
}
