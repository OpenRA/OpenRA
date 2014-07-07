#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Utility
{
	public static class UpgradeRules
	{
		static void ConvertFloatToRange(ref string input)
		{
			var value = float.Parse(input);
			var cells = (int)value;
			var subcells = (int)(1024 * value) - 1024 * cells;

			input = "{0}c{1}".F(cells, subcells);
		}

		static void ConvertPxToRange(ref string input)
		{
			ConvertPxToRange(ref input, 1, 1);
		}

		static void ConvertPxToRange(ref string input, int scaleMult, int scaleDiv)
		{
			var value = Exts.ParseIntegerInvariant(input);
			var ts = Game.modData.Manifest.TileSize;
			var world = value * 1024 * scaleMult / (scaleDiv * ts.Height);
			var cells = world / 1024;
			var subcells = world - 1024 * cells;

			input = cells != 0 ? "{0}c{1}".F(cells, subcells) : subcells.ToString();
		}

		static void ConvertAngle(ref string input)
		{
			var value = float.Parse(input);
			input = WAngle.ArcTan((int)(value * 4 * 1024), 1024).ToString();
		}

		static void ConvertInt2ToWVec(ref string input)
		{
			var offset = FieldLoader.GetValue<int2>("(value)", input);
			var ts = Game.modData.Manifest.TileSize;
			var world = new WVec(offset.X * 1024 / ts.Width, offset.Y * 1024 / ts.Height, 0);
			input = world.ToString();
		}

		static void UpgradeActorRules(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
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
					{
						node.Value.Nodes.Add(new MiniYamlNode("DisabledOverlay", new MiniYaml("")));
					}
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

				// make animation is now it's own trait
				if (engineVersion < 20140621)
				{
					if (depth == 1 && (node.Key.StartsWith("RenderBuilding")))
						node.Value.Nodes.RemoveAll(n => n.Key == "HasMakeAnimation");

					if (node.Value.Nodes.Any(n => n.Key.StartsWith("RenderBuilding"))
						&& !node.Value.Nodes.Any(n => n.Key == "RenderBuildingWall")
						&& !node.Value.Nodes.Any(n => n.Key == "WithMakeAnimation"))
					{
						node.Value.Nodes.Add(new MiniYamlNode("WithMakeAnimation", new MiniYaml("")));
					}
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

				UpgradeActorRules(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		static void UpgradeWeaponRules(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
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

				UpgradeWeaponRules(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		static void UpgradeTileset(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
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
				UpgradeTileset(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}

			nodes.AddRange(addNodes);
		}

		[Desc("MAP", "CURRENTENGINE", "Upgrade map rules to the latest engine version.")]
		public static void UpgradeMap(string[] args)
		{
			var map = new Map(args[1]);
			var engineDate = Exts.ParseIntegerInvariant(args[2]);

			Game.modData = new ModData(map.RequiresMod);
			UpgradeWeaponRules(engineDate, ref map.WeaponDefinitions, null, 0);
			UpgradeActorRules(engineDate, ref map.RuleDefinitions, null, 0);
			map.Save(args[1]);
		}

		[Desc("MOD", "CURRENTENGINE", "Upgrade mod rules to the latest engine version.")]
		public static void UpgradeMod(string[] args)
		{
			var mod = args[1];
			var engineDate = Exts.ParseIntegerInvariant(args[2]);

			Game.modData = new ModData(mod);
			Game.modData.MapCache.LoadMaps();

			Console.WriteLine("Processing Rules:");
			foreach (var filename in Game.modData.Manifest.Rules)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeActorRules(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.WriteLine(yaml.WriteToString());
			}

			Console.WriteLine("Processing Weapons:");
			foreach (var filename in Game.modData.Manifest.Weapons)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeWeaponRules(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.WriteLine(yaml.WriteToString());
			}

			Console.WriteLine("Processing Tilesets:");
			foreach (var filename in Game.modData.Manifest.TileSets)
			{
				Console.WriteLine("\t" + filename);
				var yaml = MiniYaml.FromFile(filename);
				UpgradeTileset(engineDate, ref yaml, null, 0);

				using (var file = new StreamWriter(filename))
					file.WriteLine(yaml.WriteToString());
			}

			Console.WriteLine("Processing Maps:");
			var maps = Game.modData.MapCache
				.Where(m => m.Status == MapStatus.Available)
				.Select(m => m.Map);

			foreach (var map in maps)
			{
				Console.WriteLine("\t" + map.Path);
				UpgradeActorRules(engineDate, ref map.RuleDefinitions, null, 0);
				UpgradeWeaponRules(engineDate, ref map.WeaponDefinitions, null, 0);
				map.Save(map.Path);
			}
		}
	}
}
