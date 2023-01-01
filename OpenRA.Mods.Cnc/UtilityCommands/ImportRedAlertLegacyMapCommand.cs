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
using System.Text;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Mods.Common.UtilityCommands;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	class ImportRedAlertLegacyMapCommand : ImportLegacyMapCommand, IUtilityCommand
	{
		// TODO: 128x128 is probably not true for "mega maps" from the expansions.
		public ImportRedAlertLegacyMapCommand()
			: base(128) { }

		string IUtilityCommand.Name => "--import-ra-map";
		bool IUtilityCommand.ValidateArguments(string[] args) { return ValidateArguments(args); }

		[Desc("FILENAME", "Convert a legacy Red Alert INI/MPR map to the OpenRA format.")]
		void IUtilityCommand.Run(Utility utility, string[] args) { Run(utility, args); }

		public override void ValidateMapFormat(int format)
		{
			if (format < 2)
			{
				Console.WriteLine($"ERROR: Detected NewINIFormat {format}. Are you trying to import a Tiberian Dawn map?");
				return;
			}
		}

		// Mapping from RA95 overlay index to type string
		static readonly string[] RedAlertOverlayNames =
		{
			"sbag", "cycl", "brik", "fenc", "wood",
			"gold01", "gold02", "gold03", "gold04",
			"gem01", "gem02", "gem03", "gem04",
			"v12", "v13", "v14", "v15", "v16", "v17", "v18",
			"fpls", "wcrate", "scrate", "barb", "sbag",
		};

		static readonly Dictionary<string, (byte Type, byte Index)> OverlayResourceMapping = new Dictionary<string, (byte, byte)>()
		{
			// RA ore & crystals
			{ "gold01", (1, 0) },
			{ "gold02", (1, 1) },
			{ "gold03", (1, 2) },
			{ "gold04", (1, 3) },
			{ "gem01", (2, 0) },
			{ "gem02", (2, 1) },
			{ "gem03", (2, 2) },
			{ "gem04", (2, 3) },
		};

		void UnpackTileData(MemoryStream ms)
		{
			var types = new ushort[MapSize, MapSize];
			for (var j = 0; j < MapSize; j++)
			{
				for (var i = 0; i < MapSize; i++)
				{
					var tileID = ms.ReadUInt16();
					types[i, j] = tileID == 0 ? (ushort)255 : tileID; // RAED weirdness
				}
			}

			for (var j = 0; j < MapSize; j++)
				for (var i = 0; i < MapSize; i++)
					Map.Tiles[new CPos(i, j)] = new TerrainTile(types[i, j], ms.ReadUInt8());
		}

		static readonly string[] OverlayActors = new string[]
		{
			// Fences
			"sbag", "cycl", "brik", "fenc", "wood",

			// Fields
			"v12", "v13", "v14", "v15", "v16", "v17", "v18",

			// Crates
			"wcrate", "scrate"
		};

		void UnpackOverlayData(MemoryStream ms)
		{
			for (var j = 0; j < MapSize; j++)
			{
				for (var i = 0; i < MapSize; i++)
				{
					var o = ms.ReadUInt8();
					var res = (Type: (byte)0, Index: (byte)0);

					if (o != 255 && OverlayResourceMapping.ContainsKey(RedAlertOverlayNames[o]))
						res = OverlayResourceMapping[RedAlertOverlayNames[o]];

					var cell = new CPos(i, j);
					Map.Resources[cell] = new ResourceTile(res.Type, res.Index);

					if (o != 255 && OverlayActors.Contains(RedAlertOverlayNames[o]))
					{
						var ar = new ActorReference(RedAlertOverlayNames[o])
						{
							new LocationInit(cell),
							new OwnerInit("Neutral")
						};

						var actorCount = Map.ActorDefinitions.Count;
						Map.ActorDefinitions.Add(new MiniYamlNode("Actor" + actorCount++, ar.Save()));
					}
				}
			}
		}

		public override string ParseTreeActor(string input)
		{
			return input.ToLowerInvariant();
		}

		public override CPos ParseActorLocation(string input, int loc)
		{
			var newLoc = new CPos(loc % MapSize, loc / MapSize);
			var vectorDown = new CVec(0, 1);

			if (input == "tsla" || input == "agun" || input == "gap" || input == "apwr" || input == "iron")
				newLoc += vectorDown;

			return newLoc;
		}

		public override void LoadPlayer(IniFile file, string section)
		{
			string color;
			string faction;
			switch (section)
			{
			case "Spain":
				color = "gold";
				faction = "allies";
				break;
			case "England":
				color = "green";
				faction = "allies";
				break;
			case "Ukraine":
				color = "orange";
				faction = "soviet";
				break;
			case "Germany":
				color = "black";
				faction = "allies";
				break;
			case "France":
				color = "teal";
				faction = "allies";
				break;
			case "Turkey":
				color = "salmon";
				faction = "allies";
				break;
			case "Greece":
			case "GoodGuy":
				color = "blue";
				faction = "allies";
				break;
			case "USSR":
			case "BadGuy":
				color = "red";
				faction = "soviet";
				break;
			case "Special":
			case "Neutral":
			default:
				color = "neutral";
				faction = "allies";
				break;
			}

			SetMapPlayers(section, faction, color, file, Players, MapPlayers);
		}

		public static MemoryStream ReadPackedSection(IniSection mapPackSection)
		{
			var sb = new StringBuilder();
			for (var i = 1; ; i++)
			{
				var line = mapPackSection.GetValue(i.ToString(), null);
				if (line == null)
					break;

				sb.Append(line.Trim());
			}

			var data = Convert.FromBase64String(sb.ToString());
			var chunks = new List<byte[]>();
			var reader = new BinaryReader(new MemoryStream(data));

			try
			{
				while (true)
				{
					var length = reader.ReadUInt32() & 0xdfffffff;
					var dest = new byte[8192];
					var src = reader.ReadBytes((int)length);

					LCWCompression.DecodeInto(src, dest);

					chunks.Add(dest);
				}
			}
			catch (EndOfStreamException) { }

			var ms = new MemoryStream();
			foreach (var chunk in chunks)
				ms.Write(chunk, 0, chunk.Length);

			ms.Position = 0;

			return ms;
		}

		public override void ReadPacks(IniFile file, string filename)
		{
			UnpackTileData(ReadPackedSection(file.GetSection("MapPack")));
			UnpackOverlayData(ReadPackedSection(file.GetSection("OverlayPack")));
		}

		public override void ReadActors(IniFile file)
		{
			base.ReadActors(file);
			LoadActors(file, "SHIPS", Players, Map);
		}

		public override void SaveWaypoint(int waypointNumber, ActorReference waypointReference)
		{
			var waypointName = "waypoint" + waypointNumber;
			if (waypointNumber == 98)
				waypointName = "DefaultCameraPosition";
			Map.ActorDefinitions.Add(new MiniYamlNode(waypointName, waypointReference.Save()));
		}
	}
}
