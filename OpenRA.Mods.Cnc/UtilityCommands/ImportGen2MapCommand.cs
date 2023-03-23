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
using OpenRA.FileSystem;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	public abstract class ImportGen2MapCommand
	{
		protected abstract Dictionary<byte, string> OverlayToActor { get; }

		protected abstract Dictionary<byte, Size> OverlayShapes { get; }

		protected abstract Dictionary<byte, DamageState> OverlayToHealth { get; }

		protected abstract Dictionary<byte, byte[]> ResourceFromOverlay { get; }

		protected abstract Dictionary<string, string> DeployableActors { get; }

		protected abstract string[] LampActors { get; }

		protected abstract string[] CreepActors { get; }

		protected void Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			var filename = args[1];
			var file = new IniFile(File.Open(args[1], FileMode.Open));
			var basic = file.GetSection("Basic");
			var mapSection = file.GetSection("Map");
			var tileset = mapSection.GetValue("Theater", "");
			var iniSize = mapSection.GetValue("Size", "0, 0, 0, 0").Split(',').Select(int.Parse).ToArray();
			var iniBounds = mapSection.GetValue("LocalSize", "0, 0, 0, 0").Split(',').Select(int.Parse).ToArray();
			var size = new Size(iniSize[2], 2 * iniSize[3]);

			if (!utility.ModData.DefaultTerrainInfo.TryGetValue(tileset, out var terrainInfo))
				throw new InvalidDataException($"Unknown tileset {tileset}");

			var map = new Map(Game.ModData, terrainInfo, size.Width, size.Height)
			{
				Title = basic.GetValue("Name", Path.GetFileNameWithoutExtension(filename)),
				Author = "Westwood Studios",
				Bounds = new Rectangle(iniBounds[0], iniBounds[1], iniBounds[2], 2 * iniBounds[3] + 2 * iniBounds[1]),
				RequiresMod = utility.ModData.Manifest.Id
			};

			var fullSize = new int2(iniSize[2], iniSize[3]);
			ReadTiles(map, file, fullSize);
			ReadActors(map, file, "Structures", fullSize);
			ReadActors(map, file, "Units", fullSize);
			ReadActors(map, file, "Infantry", fullSize);
			ReadTerrainActors(map, file, fullSize);
			ReadWaypoints(map, file, fullSize);
			ReadOverlay(map, file, fullSize);
			ReadLighting(map, file);
			ReadLamps(map, file);

			var spawnCount = map.ActorDefinitions.Count(n => n.Value.Value == "mpspawn");
			var mapPlayers = new MapPlayers(map.Rules, spawnCount);
			map.PlayerDefinitions = mapPlayers.ToMiniYaml();

			var dest = Path.GetFileNameWithoutExtension(args[1]) + ".oramap";
			map.Save(ZipFileLoader.Create(dest));
			Console.WriteLine(dest + " saved.");
		}

		protected virtual void ReadTiles(Map map, IniFile file, int2 fullSize)
		{
			var terrainInfo = (ITemplatedTerrainInfo)Game.ModData.DefaultTerrainInfo[map.Tileset];
			var mapSection = file.GetSection("IsoMapPack5");

			var data = Convert.FromBase64String(string.Concat(mapSection.Select(kvp => kvp.Value)));
			var cells = (fullSize.X * 2 - 1) * fullSize.Y;
			var lzoPackSize = cells * 11 + 4; // The last 4 bytes contain a LZO pack header saying no more data is left.
			var isoMapPack = new byte[lzoPackSize];
			UnpackLZO(data, isoMapPack);

			var mf = new MemoryStream(isoMapPack);
			for (var i = 0; i < cells; i++)
			{
				var rx = mf.ReadUInt16();
				var ry = mf.ReadUInt16();
				var tilenum = mf.ReadUInt16();
				/*var zero1 = */mf.ReadInt16();
				var subtile = mf.ReadUInt8();
				var z = mf.ReadUInt8();
				/*var zero2 = */mf.ReadUInt8();

				var dx = rx - ry + fullSize.X - 1;
				var dy = rx + ry - fullSize.X - 1;
				var mapCell = new MPos(dx / 2, dy);
				var cell = mapCell.ToCPos(map);

				if (map.Tiles.Contains(cell))
				{
					if (!terrainInfo.Templates.ContainsKey(tilenum))
						tilenum = subtile = 0;

					map.Tiles[cell] = new TerrainTile(tilenum, subtile);
					map.Height[cell] = z;
				}
			}
		}

		protected virtual void ReadOverlay(Map map, IniFile file, int2 fullSize)
		{
			var overlaySection = file.GetSection("OverlayPack");
			var overlayCompressed = Convert.FromBase64String(string.Concat(overlaySection.Select(kvp => kvp.Value)));
			var overlayPack = new byte[1 << 18];
			var temp = new byte[1 << 18];
			UnpackLCW(overlayCompressed, overlayPack, temp);

			var overlayDataSection = file.GetSection("OverlayDataPack");
			var overlayDataCompressed = Convert.FromBase64String(string.Concat(overlayDataSection.Select(kvp => kvp.Value)));
			var overlayDataPack = new byte[1 << 18];
			UnpackLCW(overlayDataCompressed, overlayDataPack, temp);

			var overlayIndex = new CellLayer<int>(map);
			overlayIndex.Clear(0xFF);

			for (var y = 0; y < fullSize.Y; y++)
			{
				for (var x = fullSize.X * 2 - 2; x >= 0; x--)
				{
					var dx = (ushort)x;
					var dy = (ushort)(y * 2 + x % 2);

					var uv = new MPos(dx / 2, dy);
					var rx = (ushort)((dx + dy) / 2 + 1);
					var ry = (ushort)(dy - rx + fullSize.X + 1);

					if (!map.Resources.Contains(uv))
						continue;

					overlayIndex[uv] = rx + 512 * ry;
				}
			}

			foreach (var cell in map.AllCells)
			{
				var overlayType = overlayPack[overlayIndex[cell]];
				if (overlayType == 0xFF)
					continue;

				if (TryHandleOverlayToActorInner(cell, overlayPack, overlayIndex, overlayType, out var ar))
				{
					if (ar != null)
						map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));

					continue;
				}

				if (TryHandleResourceFromOverlayInner(overlayType, overlayDataPack[overlayIndex[cell]], out var resourceTile))
				{
					map.Resources[cell] = resourceTile;
					continue;
				}

				if (TryHandleOtherOverlayInner(map, cell, overlayDataPack, overlayIndex, overlayType))
					continue;

				Console.WriteLine($"Cell {cell}: unknown overlay {overlayType}");
			}
		}

		protected virtual void ReadWaypoints(Map map, IniFile file, int2 fullSize)
		{
			var waypointsSection = file.GetSection("Waypoints", true);
			foreach (var kv in waypointsSection)
			{
				var pos = int.Parse(kv.Value);
				var ry = pos / 1000;
				var rx = pos - ry * 1000;
				var dx = rx - ry + fullSize.X - 1;
				var dy = rx + ry - fullSize.X - 1;
				var cell = new MPos(dx / 2, dy).ToCPos(map);

				var ar = new ActorReference((!int.TryParse(kv.Key, out var wpindex) || wpindex > 7) ? "waypoint" : "mpspawn")
				{
					new LocationInit(cell),
					new OwnerInit("Neutral")
				};

				map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));
			}
		}

		protected virtual void ReadTerrainActors(Map map, IniFile file, int2 fullSize)
		{
			var terrainSection = file.GetSection("Terrain", true);
			foreach (var kv in terrainSection)
			{
				var pos = int.Parse(kv.Key);
				var ry = pos / 1000;
				var rx = pos - ry * 1000;
				var dx = rx - ry + fullSize.X - 1;
				var dy = rx + ry - fullSize.X - 1;
				var cell = new MPos(dx / 2, dy).ToCPos(map);
				var name = kv.Value.ToLowerInvariant();

				var ar = new ActorReference(name)
				{
					new LocationInit(cell),
					new OwnerInit("Neutral")
				};

				if (!map.Rules.Actors.ContainsKey(name))
					Console.WriteLine($"Ignoring unknown actor type: `{name}`");
				else
					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));
			}
		}

		protected virtual void ReadActors(Map map, IniFile file, string type, int2 fullSize)
		{
			var structuresSection = file.GetSection(type, true);
			foreach (var kv in structuresSection)
			{
				var isDeployed = false;
				var entries = kv.Value.Split(',');

				var name = entries[1].ToLowerInvariant();

				if (DeployableActors.ContainsKey(name))
				{
					name = DeployableActors[name];
					isDeployed = true;
				}

				var health = short.Parse(entries[2]);
				var rx = int.Parse(entries[3]);
				var ry = int.Parse(entries[4]);
				var facing = (byte)(224 - byte.Parse(entries[type == "Infantry" ? 7 : 5]));

				var dx = rx - ry + fullSize.X - 1;
				var dy = rx + ry - fullSize.X - 1;
				var cell = new MPos(dx / 2, dy).ToCPos(map);

				var ar = new ActorReference(name)
				{
					new LocationInit(cell),
					new OwnerInit(CreepActors.Contains(entries[1]) ? "Creeps" : "Neutral")
				};

				if (type == "Infantry")
				{
					var subcell = 0;
					switch (byte.Parse(entries[5]))
					{
						case 2: subcell = 3; break;
						case 3: subcell = 1; break;
						case 4: subcell = 2; break;
					}

					if (subcell != 0)
						ar.Add(new SubCellInit((SubCell)subcell));
				}

				if (health != 256)
					ar.Add(new HealthInit(100 * health / 256));

				ar.Add(new FacingInit(WAngle.FromFacing(facing)));

				if (isDeployed)
					ar.Add(new DeployStateInit(DeployState.Deployed));

				if (!map.Rules.Actors.ContainsKey(name))
					Console.WriteLine($"Ignoring unknown actor type: `{name}`");
				else
					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));
			}
		}

		protected virtual void ReadLighting(Map map, IniFile file)
		{
			var lightingTypes = new Dictionary<string, string>()
			{
				{ "Red", "RedTint" },
				{ "Green", "GreenTint" },
				{ "Blue", "BlueTint" },
				{ "Ambient", "Intensity" },
				{ "Level", "HeightStep" },
				{ "Ground", null }
			};

			var lightingSection = file.GetSection("Lighting");
			var parsed = new Dictionary<string, float>();
			var lightingNodes = new List<MiniYamlNode>();

			foreach (var kv in lightingSection)
			{
				if (lightingTypes.ContainsKey(kv.Key))
					parsed[kv.Key] = FieldLoader.GetValue<float>(kv.Key, kv.Value);
				else
					Console.WriteLine($"Ignoring unknown lighting type: `{kv.Key}`");
			}

			// Merge Ground into Ambient
			if (parsed.TryGetValue("Ground", out var ground))
			{
				if (!parsed.ContainsKey("Ambient"))
					parsed["Ambient"] = 1f;
				parsed["Ambient"] -= ground;
			}

			foreach (var node in lightingTypes)
			{
				if (node.Value != null && parsed.TryGetValue(node.Key, out var val) && ((node.Key == "Level" && val != 0) || (node.Key != "Level" && val != 1.0f)))
					lightingNodes.Add(new MiniYamlNode(node.Value, FieldSaver.FormatValue(val)));
			}

			if (lightingNodes.Count > 0)
			{
				map.RuleDefinitions.Nodes.Add(new MiniYamlNode("^BaseWorld", new MiniYaml("", new List<MiniYamlNode>()
				{
					new MiniYamlNode("TerrainLighting", new MiniYaml("", lightingNodes))
				})));
			}
		}

		protected virtual void ReadLamps(Map map, IniFile file)
		{
			var lightingTypes = new Dictionary<string, string>()
			{
				{ "LightIntensity", "Intensity" },
				{ "LightRedTint", "RedTint" },
				{ "LightGreenTint", "GreenTint" },
				{ "LightBlueTint", "BlueTint" },
			};

			foreach (var lamp in LampActors)
			{
				var lightingSection = file.GetSection(lamp, true);
				var lightingNodes = new List<MiniYamlNode>();

				foreach (var kv in lightingSection)
				{
					if (kv.Key == "LightVisibility")
					{
						// Convert leptons to WDist
						var visibility = FieldLoader.GetValue<int>(kv.Key, kv.Value);
						lightingNodes.Add(new MiniYamlNode("Range", FieldSaver.FormatValue(new WDist(visibility * 4))));
					}
					else if (lightingTypes.ContainsKey(kv.Key))
					{
						// Some maps use "," instead of "."!
						var value = FieldLoader.GetValue<float>(kv.Key, kv.Value.Replace(',', '.'));
						lightingNodes.Add(new MiniYamlNode(lightingTypes[kv.Key], FieldSaver.FormatValue(value)));
					}
				}

				if (lightingNodes.Count > 0)
				{
					map.RuleDefinitions.Nodes.Add(new MiniYamlNode(lamp, new MiniYaml("", new List<MiniYamlNode>()
					{
						new MiniYamlNode("TerrainLightSource", new MiniYaml("", lightingNodes))
					})));
				}
			}
		}

		protected virtual bool TryHandleOverlayToActorInner(CPos cell, byte[] overlayPack, CellLayer<int> overlayIndex, byte overlayType, out ActorReference actorReference)
		{
			actorReference = null;
			if (!OverlayToActor.TryGetValue(overlayType, out var actorType))
				return false;

			// This could be just a dummy handler that we want to ignore.
			if (string.IsNullOrEmpty(actorType))
				return true;

			if (OverlayShapes.TryGetValue(overlayType, out var shape))
			{
				// Only import the top-left cell of multi-celled overlays
				// Returning true here means this is a part of a bigger overlay that has already been handled.
				var aboveType = overlayPack[overlayIndex[cell - new CVec(1, 0)]];
				if (shape.Width > 1 && aboveType != 0xFF)
					if (OverlayToActor.TryGetValue(aboveType, out var a) && a == actorType)
						return true;

				var leftType = overlayPack[overlayIndex[cell - new CVec(0, 1)]];
				if (shape.Height > 1 && leftType != 0xFF)
					if (OverlayToActor.TryGetValue(leftType, out var a) && a == actorType)
						return true;
			}

			actorReference = new ActorReference(actorType)
			{
				new LocationInit(cell),
				new OwnerInit("Neutral")
			};

			TryHandleOverlayToHealthInner(overlayType, actorReference);

			return true;
		}

		protected virtual bool TryHandleOverlayToHealthInner(byte overlayType, ActorReference actorReference)
		{
			if (!OverlayToHealth.TryGetValue(overlayType, out var damageState))
				return false;

			var health = 100;
			if (damageState == DamageState.Critical)
				health = 25;
			else if (damageState == DamageState.Heavy)
				health = 50;
			else if (damageState == DamageState.Medium)
				health = 75;

			if (health != 100)
				actorReference.Add(new HealthInit(health));

			return true;
		}

		protected virtual bool TryHandleResourceFromOverlayInner(byte overlayType, byte densityIndex, out ResourceTile resourceTile)
		{
			var resourceType = ResourceFromOverlay
				.Where(kv => kv.Value.Contains(overlayType))
				.Select(kv => kv.Key)
				.FirstOrDefault();

			resourceTile = new ResourceTile(resourceType, densityIndex);
			return resourceType != 0;
		}

		protected virtual bool TryHandleOtherOverlayInner(Map map, CPos cell, byte[] overlayDataPack, CellLayer<int> overlayIndex, byte overlayType)
		{
			return false;
		}

		#region Helper methods

		protected static void UnpackLZO(byte[] src, byte[] dest)
		{
			var srcOffset = 0U;
			var destOffset = 0U;

			while (destOffset < dest.Length && srcOffset < src.Length)
			{
				var srcLength = BitConverter.ToUInt16(src, (int)srcOffset);
				var destLength = (uint)BitConverter.ToUInt16(src, (int)srcOffset + 2);
				srcOffset += 4;
				LZOCompression.DecodeInto(src, srcOffset, srcLength, dest, destOffset, ref destLength);
				srcOffset += srcLength;
				destOffset += destLength;
			}
		}

		protected static void UnpackLCW(byte[] src, byte[] dest, byte[] temp)
		{
			var srcOffset = 0;
			var destOffset = 0;

			while (destOffset < dest.Length)
			{
				var srcLength = BitConverter.ToUInt16(src, srcOffset);
				var destLength = BitConverter.ToUInt16(src, srcOffset + 2);
				srcOffset += 4;
				LCWCompression.DecodeInto(src, temp, srcOffset);
				Array.Copy(temp, 0, dest, destOffset, destLength);
				srcOffset += srcLength;
				destOffset += destLength;
			}
		}

		#endregion
	}
}
