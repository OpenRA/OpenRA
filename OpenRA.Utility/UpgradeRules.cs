#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

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
			var value = int.Parse(input);
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

				UpgradeWeaponRules(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		static void UpgradeTileset(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			var parentKey = parent != null ? parent.Key.Split('@').First() : null;
			List<MiniYamlNode> addNodes = new List<MiniYamlNode>();

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
			var engineDate = int.Parse(args[2]);

			Game.modData = new ModData(map.RequiresMod);
			UpgradeWeaponRules(engineDate, ref map.Weapons, null, 0);
			UpgradeActorRules(engineDate, ref map.Rules, null, 0);
			map.Save(args[1]);
		}

		[Desc("MOD", "CURRENTENGINE", "Upgrade mod rules to the latest engine version.")]
		public static void UpgradeMod(string[] args)
		{
			var mod = args[1];
			var engineDate = int.Parse(args[2]);

			Game.modData = new ModData(mod);

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
			foreach (var map in Game.modData.FindMaps().Values)
			{
				Console.WriteLine("\t" + map.Path);
				UpgradeActorRules(engineDate, ref map.Rules, null, 0);
				UpgradeWeaponRules(engineDate, ref map.Weapons, null, 0);
				map.Save(map.Path);
			}
		}
	}
}
