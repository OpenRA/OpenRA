﻿#region Copyright & License Information
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
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.UtilityCommands
{
	public class Format5
	{
		public static unsafe uint DecodeInto(byte[] src, byte[] dest, int format = 5)
		{
			fixed (byte* pr = src, pw = dest)
			{
				byte* r = pr, w = pw;
				byte* w_end = w + dest.Length;

				while (w < w_end) {
					ushort size_in = *(ushort*)r;
					r += 2;
					uint size_out = *(ushort*)r;
					r += 2;

					if (size_in == 0 || size_out == 0)
						break;

					if (format == 80)
						Format80.DecodeInto(r, w);
					else
						MiniLZO.Decompress(r, size_in, w, ref size_out);

					r += size_in;
					w += size_out;
				}

				return (uint)(w - pw);
			}
		}
	}

	class ImportIsometricLegacyMapCommand : IUtilityCommand
	{
		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		public string Name { get { return "--import-isometric-map"; } }

		int2 FullSize;
		int actorCount = 0;
		int spawnCount = 0;

		static readonly Dictionary<byte, string> overlayToActor = new Dictionary<byte, string>()
		{
			{ 0x01, "gasand" },
			{ 0x03, "gawall" },
			/*
			{ 0x18, "bridge1" },
			{ 0x19, "bridge2" },
			*/
			{ 0x1A, "nawall" },

			{ 0x37, "tracktunnel01" },
			{ 0x38, "tracktunnel02" },
			{ 0x39, "tracktunnel03" },
			{ 0x3A, "tracktunnel04" },

			/*
			{ 0x3B, "railbrdg1" },
			{ 0x3C, "railbrdg2" },
			*/

			{ 0x3D, "crat01" },
			{ 0x3E, "crat02" },
			{ 0x3F, "crat03" },
			{ 0x40, "crat04" },
			{ 0x41, "crat0A" },
			{ 0x42, "crat0B" },
			{ 0x43, "crat0C" },
			{ 0x44, "drum01" },
			{ 0x45, "drum02" },
			{ 0x46, "palet01" },
			{ 0x47, "palet02" },
			{ 0x48, "palet03" },
			{ 0x49, "palet04" },

			/*
			{ 0x4A, "lobrdg01" },
			{ 0x4B, "lobrdg02" },
			{ 0x4C, "lobrdg03" },
			{ 0x4D, "lobrdg04" },
			{ 0x4E, "lobrdg05" },
			{ 0x4F, "lobrdg06" },
			{ 0x50, "lobrdg07" },
			{ 0x51, "lobrdg08" },
			{ 0x52, "lobrdg09" },
			{ 0x53, "lobrdg10" },
			{ 0x54, "lobrdg11" },
			{ 0x55, "lobrdg12" },
			{ 0x56, "lobrdg13" },
			{ 0x57, "lobrdg14" },
			{ 0x58, "lobrdg15" },
			{ 0x59, "lobrdg16" },
			{ 0x5A, "lobrdg17" },
			{ 0x5B, "lobrdg18" },
			{ 0x5C, "lobrdg19" },
			{ 0x5D, "lobrdg20" },
			{ 0x5E, "lobrdg21" },
			{ 0x5F, "lobrdg22" },
			{ 0x60, "lobrdg23" },
			{ 0x61, "lobrdg24" },
			{ 0x62, "lobrdg25" },
			{ 0x63, "lobrdg26" },
			{ 0x64, "lobrdg27" },
			{ 0x65, "lobrdg28" },
			{ 0x7A, "lobrdg1" },
			{ 0x7B, "lobrdg2" },
			{ 0x7C, "lobrdg3" },
			{ 0x7D, "lobrdg4" },*/

			{ 0xA7, "veinhole" },
			{ 0xA8, "srock01" },
			{ 0xA9, "srock02" },
			{ 0xAA, "srock03" },
			{ 0xAB, "srock04" },
			{ 0xAC, "srock05" },
			{ 0xAD, "trock01" },
			{ 0xAE, "trock02" },
			{ 0xAF, "trock03" },
			{ 0xB0, "trock04" },
			{ 0xB1, "trock05" },
			{ 0xBB, "veinholedummy" },
			{ 0xBC, "crate" }
		};

		static readonly Dictionary<byte, byte[]> resourceFromOverlay = new Dictionary<byte, byte[]>()
		{
			// "tib" - Regular Tiberium
			{ 0x01, new byte[] { 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
					0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79 } },

			// "btib" - Blue Tiberium
			{ 0x02, new byte[] { 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26,

					// Should be "tib2"
					0x7F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88,
					0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F, 0x90, 0x91, 0x92,

					// Should be "tib3"
					0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C,
					0x9D, 0x9E, 0x9F, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6 } },

			// Veins
			{ 0x03, new byte[] { 0x7E } }
		};

		[Desc("FILENAME", "Convert a Tiberian Sun map to the OpenRA format.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;
			Game.ModData.MountFiles();

			var filename = args[1];
			var file = new IniFile(File.Open(args[1], FileMode.Open));
			var map = GenerateMapHeader(filename, file, modData);

			ReadTiles(map, file);
			ReadActors(map, file, "Structures");
			ReadActors(map, file, "Units");
			ReadActors(map, file, "Infantry");
			ReadTerrainActors(map, file);
			ReadWaypoints(map, file);
			ReadOverlay(map, file);


			var mapPlayers = new MapPlayers(map.Rules, spawnCount);
			map.PlayerDefinitions = mapPlayers.ToMiniYaml();

			map.Save("mods/" + modData.Manifest.Mod.Id + "/maps/" + Path.GetFileNameWithoutExtension(filename) + "/");
		}

		Map GenerateMapHeader(string filename, IniFile file, ModData modData)
		{
			var basic = file.GetSection("Basic");
			var mapSection = file.GetSection("Map");

			// TODO: Fix tileset naming
			var tileset = mapSection.GetValue("Theater", "TEMPERAT");

			var iniSize = mapSection.GetValue("Size", "0, 0, 0, 0").Split(',').Select(x => int.Parse(x)).ToArray();
			var iniBounds = mapSection.GetValue("LocalSize", "0, 0, 0, 0").Split(',').Select(x => int.Parse(x)).ToArray();
			var size = new Size(iniSize[2], 2 * iniSize[3]);

			FullSize = new int2(iniSize[2], iniSize[3]);

			var map = new Map(modData.DefaultRules.TileSets[tileset], size.Width, size.Height);
			map.Title = basic.GetValue("Name", Path.GetFileNameWithoutExtension(filename));
			map.Author = "Westwood Studios";
			map.Bounds = new Rectangle(iniBounds[0], iniBounds[1], iniBounds[2], 2 * iniBounds[3]);

			map.MapResources = Exts.Lazy(() => new CellLayer<ResourceTile>(map.Grid.Type, size));
			map.MapTiles = Exts.Lazy(() => new CellLayer<TerrainTile>(map.Grid.Type, size));
			map.MapHeight = Exts.Lazy(() => new CellLayer<byte>(map.Grid.Type, size));

			map.Options = new MapOptions();

			map.RequiresMod = modData.Manifest.Mod.Id;

			return map;
		}

		void ReadTiles(Map map, IniFile file)
		{
			var tileset = Game.ModData.DefaultRules.TileSets[map.Tileset];
			var mapSection = file.GetSection("IsoMapPack5");

			var data = Convert.FromBase64String(mapSection.Aggregate(string.Empty, (a, b) => a + b.Value));
			int cells = (FullSize.X * 2 - 1) * FullSize.Y;
			int lzoPackSize = cells * 11 + 4; // last 4 bytes contains a lzo pack header saying no more data is left
			var isoMapPack = new byte[lzoPackSize];
			Format5.DecodeInto(data, isoMapPack);

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

				int dx = rx - ry + FullSize.X - 1;
				int dy = rx + ry - FullSize.X - 1;
				var mapCell = new MPos(dx / 2, dy);
				var cell = mapCell.ToCPos(map);

				if (map.MapTiles.Value.Contains(cell))
				{
					if (!tileset.Templates.ContainsKey(tilenum))
					{
						Console.WriteLine("unknown tile ({3},{4}) at {0},{1} ({2}) -> ", cell, z, mapCell, tilenum, subtile);
						tilenum = subtile = 0;
					}
					else

					map.MapTiles.Value[cell] = new TerrainTile(tilenum, subtile);
					map.MapHeight.Value[cell] = z;
				}
				else
					Console.WriteLine("unknown cell {0},{1},{2} -> {3},{4}", rx, ry, z, tilenum, subtile);
			}
		}

		void ReadOverlay(Map map, IniFile file)
		{
			var overlaySection = file.GetSection("OverlayPack");
			var overlayCompressed = Convert.FromBase64String(overlaySection.Aggregate(string.Empty, (a, b) => a + b.Value));
			var overlayPack = new byte[1 << 18];
			Format5.DecodeInto(overlayCompressed, overlayPack, 80);

			var overlayDataSection = file.GetSection("OverlayDataPack");
			var overlayDataCompressed = Convert.FromBase64String(overlayDataSection.Aggregate(string.Empty, (a, b) => a + b.Value));
			var overlayDataPack = new byte[1 << 18];
			Format5.DecodeInto(overlayDataCompressed, overlayDataPack, 80);

			for (int y = 0; y < FullSize.Y; y++)
			{
				for (int x = FullSize.X * 2 - 2; x >= 0; x--)
				{
					ushort dx = (ushort)(x);
					ushort dy = (ushort)(y * 2 + x % 2);

					var uv = new MPos(dx / 2, dy);
					var rx = (ushort)((dx + dy) / 2 + 1);
					var ry = (ushort)(dy - rx + FullSize.X + 1);

					if (!map.MapResources.Value.Contains(uv))
						continue;

					var idx = rx + 512 * ry;
					var overlayType = overlayPack[idx];
					if (overlayType == 0xFF)
						continue;

					string actorType;
					if (overlayToActor.TryGetValue(overlayType, out actorType))
					{
						var ar = new ActorReference(actorType)
						{
							new LocationInit(uv.ToCPos(map)),
							new OwnerInit("Neutral")
						};

						map.ActorDefinitions.Add(new MiniYamlNode("Actor" + actorCount++, ar.Save()));
						continue;
					}

					var resourceType = resourceFromOverlay
						.Where(kv => kv.Value.Contains(overlayType))
						.Select(kv => kv.Key)
						.FirstOrDefault();

					if (resourceType != 0)
					{
						map.MapResources.Value[uv] = new ResourceTile(resourceType, overlayDataPack[idx]);
						continue;
					}

					Console.WriteLine("{0} unknown overlay {1}", uv, overlayType);
				}
			}
		}

		void ReadWaypoints(Map map, IniFile file)
		{
			var waypointsSection = file.GetSection("Waypoints", true);
			foreach (var kv in waypointsSection)
			{
				var pos = int.Parse(kv.Value);
				var ry = pos / 1000;
				var rx = pos - ry * 1000;
				int dx = rx - ry + FullSize.X - 1;
				int dy = rx + ry - FullSize.X - 1;
				var cell = new MPos(dx / 2, dy).ToCPos(map);

				var ar = new ActorReference(int.Parse(kv.Key) <= 7 ? "mpspawn" : "waypoint");
				ar.Add(new LocationInit(cell));
				ar.Add(new OwnerInit("Neutral"));

				map.ActorDefinitions.Add(new MiniYamlNode("Actor" + actorCount++, ar.Save()));

				if (ar.Type == "mpspawn")
					spawnCount++;
			}
		}

		void ReadTerrainActors(Map map, IniFile file)
		{
			var terrainSection = file.GetSection("Terrain", true);
			foreach (var kv in terrainSection)
			{
				var pos = int.Parse(kv.Key);
				var ry = pos / 1000;
				var rx = pos - ry * 1000;
				int dx = rx - ry + FullSize.X - 1;
				int dy = rx + ry - FullSize.X - 1;
				var cell = new MPos(dx / 2, dy).ToCPos(map);
				var name = kv.Value.ToLowerInvariant();

				var ar = new ActorReference(name);
				ar.Add(new LocationInit(cell));
				ar.Add(new OwnerInit("Neutral"));

				if (!map.Rules.Actors.ContainsKey(name))
					Console.WriteLine("Ignoring unknown actor type: `{0}`".F(name));
				else
					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + actorCount++, ar.Save()));
			}
		}

		void ReadActors(Map map, IniFile file, string type)
		{
			var structuresSection = file.GetSection(type, true);
			foreach (var kv in structuresSection)
			{
				var entries = kv.Value.Split(',');

				//var owner = entries[0];
				var name = entries[1].ToLowerInvariant();
				var health = short.Parse(entries[2]);
				var rx = int.Parse(entries[3]);
				var ry = int.Parse(entries[4]);
				var facing = (byte)(byte.Parse(entries[5]) + 96);

				int dx = rx - ry + FullSize.X - 1;
				int dy = rx + ry - FullSize.X - 1;
				var cell = new MPos(dx / 2, dy).ToCPos(map);

				var ar = new ActorReference(name)
				{
					new LocationInit(cell),
					new OwnerInit("Neutral"),
					new HealthInit(100 * health / 256),
					new FacingInit(facing),
				};

				if (!map.Rules.Actors.ContainsKey(name))
					Console.WriteLine("Ignoring unknown actor type: `{0}`".F(name));
				else
					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + actorCount++, ar.Save()));
			}
		}
	}
}
