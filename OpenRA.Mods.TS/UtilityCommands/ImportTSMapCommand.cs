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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.TS.UtilityCommands
{
	class ImportTSMapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--import-ts-map"; } }
		bool IUtilityCommand.ValidateArguments(string[] args) { return args.Length >= 2; }

		static readonly Dictionary<byte, string> OverlayToActor = new Dictionary<byte, string>()
		{
			{ 0x01, "gasand" },
			{ 0x03, "gawall" },
			/*
			{ 0x18, "bridge1" },
			{ 0x19, "bridge2" },
			*/
			{ 0x1A, "nawall" },
			{ 0x27, "tracks01" },
			{ 0x28, "tracks02" },
			{ 0x29, "tracks03" },
			{ 0x2A, "tracks04" },
			{ 0x2B, "tracks05" },
			{ 0x2C, "tracks06" },
			{ 0x2D, "tracks07" },
			{ 0x2E, "tracks08" },
			{ 0x2F, "tracks09" },
			{ 0x30, "tracks10" },
			{ 0x31, "tracks11" },
			{ 0x32, "tracks12" },
			{ 0x33, "tracks13" },
			{ 0x34, "tracks14" },
			{ 0x35, "tracks15" },
			{ 0x36, "tracks16" },
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
			{ 0x7D, "lobrdg4" },
			*/
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

		static readonly Dictionary<byte, byte[]> ResourceFromOverlay = new Dictionary<byte, byte[]>()
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

		static readonly Dictionary<string, string> DeployableActors = new Dictionary<string, string>()
		{
			{ "gadpsa", "lpst" },
			{ "gatick", "ttnk" }
		};

		[Desc("FILENAME", "Convert a Tiberian Sun map to the OpenRA format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
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

			var map = new Map(Game.ModData, utility.ModData.DefaultTileSets[tileset], size.Width, size.Height)
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

			var spawnCount = map.ActorDefinitions.Count(n => n.Value.Value == "mpspawn");
			var mapPlayers = new MapPlayers(map.Rules, spawnCount);
			map.PlayerDefinitions = mapPlayers.ToMiniYaml();

			var dest = Path.GetFileNameWithoutExtension(args[1]) + ".oramap";
			map.Save(ZipFile.Create(dest, new Folder(".")));
			Console.WriteLine(dest + " saved.");
		}

		static void UnpackLZO(byte[] src, byte[] dest)
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

		static void UnpackLCW(byte[] src, byte[] dest, byte[] temp)
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

		static void ReadTiles(Map map, IniFile file, int2 fullSize)
		{
			var tileset = Game.ModData.DefaultTileSets[map.Tileset];
			var mapSection = file.GetSection("IsoMapPack5");

			var data = Convert.FromBase64String(mapSection.Aggregate(string.Empty, (a, b) => a + b.Value));
			int cells = (fullSize.X * 2 - 1) * fullSize.Y;
			int lzoPackSize = cells * 11 + 4; // last 4 bytes contains a lzo pack header saying no more data is left
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

				int dx = rx - ry + fullSize.X - 1;
				int dy = rx + ry - fullSize.X - 1;
				var mapCell = new MPos(dx / 2, dy);
				var cell = mapCell.ToCPos(map);

				if (map.Tiles.Contains(cell))
				{
					if (!tileset.Templates.ContainsKey(tilenum))
						tilenum = subtile = 0;

					map.Tiles[cell] = new TerrainTile(tilenum, subtile);
					map.Height[cell] = z;
				}
			}
		}

		static void ReadOverlay(Map map, IniFile file, int2 fullSize)
		{
			var overlaySection = file.GetSection("OverlayPack");
			var overlayCompressed = Convert.FromBase64String(overlaySection.Aggregate(string.Empty, (a, b) => a + b.Value));
			var overlayPack = new byte[1 << 18];
			var temp = new byte[1 << 18];
			UnpackLCW(overlayCompressed, overlayPack, temp);

			var overlayDataSection = file.GetSection("OverlayDataPack");
			var overlayDataCompressed = Convert.FromBase64String(overlayDataSection.Aggregate(string.Empty, (a, b) => a + b.Value));
			var overlayDataPack = new byte[1 << 18];
			UnpackLCW(overlayDataCompressed, overlayDataPack, temp);

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

					var idx = rx + 512 * ry;
					var overlayType = overlayPack[idx];
					if (overlayType == 0xFF)
						continue;

					string actorType;
					if (OverlayToActor.TryGetValue(overlayType, out actorType))
					{
						var ar = new ActorReference(actorType)
						{
							new LocationInit(uv.ToCPos(map)),
							new OwnerInit("Neutral")
						};

						map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));
						continue;
					}

					var resourceType = ResourceFromOverlay
						.Where(kv => kv.Value.Contains(overlayType))
						.Select(kv => kv.Key)
						.FirstOrDefault();

					if (resourceType != 0)
					{
						map.Resources[uv] = new ResourceTile(resourceType, overlayDataPack[idx]);
						continue;
					}

					Console.WriteLine("{0} unknown overlay {1}", uv, overlayType);
				}
			}
		}

		static void ReadWaypoints(Map map, IniFile file, int2 fullSize)
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

				int wpindex;
				var ar = new ActorReference((!int.TryParse(kv.Key, out wpindex) || wpindex > 7) ? "waypoint" : "mpspawn");
				ar.Add(new LocationInit(cell));
				ar.Add(new OwnerInit("Neutral"));

				map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));
			}
		}

		static void ReadTerrainActors(Map map, IniFile file, int2 fullSize)
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

				var ar = new ActorReference(name);
				ar.Add(new LocationInit(cell));
				ar.Add(new OwnerInit("Neutral"));

				if (!map.Rules.Actors.ContainsKey(name))
					Console.WriteLine("Ignoring unknown actor type: `{0}`".F(name));
				else
					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));
			}
		}

		static void ReadActors(Map map, IniFile file, string type, int2 fullSize)
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
				var facing = (byte)(byte.Parse(entries[5]) + 96);

				var dx = rx - ry + fullSize.X - 1;
				var dy = rx + ry - fullSize.X - 1;
				var cell = new MPos(dx / 2, dy).ToCPos(map);

				var ar = new ActorReference(name)
				{
					new LocationInit(cell),
					new OwnerInit("Neutral"),
					new HealthInit(100 * health / 256),
					new FacingInit(facing),
				};

				if (isDeployed)
					ar.Add(new DeployStateInit(DeployState.Deployed));

				if (!map.Rules.Actors.ContainsKey(name))
					Console.WriteLine("Ignoring unknown actor type: `{0}`".F(name));
				else
					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));
			}
		}

		static void ReadLighting(Map map, IniFile file)
		{
			var lightingTypes = new[] { "Red", "Green", "Blue", "Ambient" };
			var lightingSection = file.GetSection("Lighting");
			var lightingNodes = new List<MiniYamlNode>();

			foreach (var kv in lightingSection)
			{
				if (lightingTypes.Contains(kv.Key))
				{
					var val = FieldLoader.GetValue<float>(kv.Key, kv.Value);
					if (val != 1.0f)
						lightingNodes.Add(new MiniYamlNode(kv.Key, FieldSaver.FormatValue(val)));
				}
				else
					Console.WriteLine("Ignoring unknown lighting type: `{0}`".F(kv.Key));
			}

			if (lightingNodes.Any())
			{
				map.RuleDefinitions.Nodes.Add(new MiniYamlNode("World", new MiniYaml("", new List<MiniYamlNode>()
				{
					new MiniYamlNode("GlobalLightingPaletteEffect", new MiniYaml("", lightingNodes))
				})));
			}
		}
	}
}
