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
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Mods.Common.UtilityCommands;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	class ImportTiberianDawnLegacyMapCommand : ImportLegacyMapCommand, IUtilityCommand
	{
		// NOTE: 64x64 map size is a C&C95 engine limitation
		public ImportTiberianDawnLegacyMapCommand() : base(64) { }

		string IUtilityCommand.Name { get { return "--import-td-map"; } }
		bool IUtilityCommand.ValidateArguments(string[] args) { return ValidateArguments(args); }

		[Desc("FILENAME", "Convert a legacy Tiberian Dawn INI/MPR map to the OpenRA format.")]
		void IUtilityCommand.Run(Utility utility, string[] args) { Run(utility, args); }

		public override void ValidateMapFormat(int format)
		{
			if (format > 1)
			{
				Console.WriteLine("ERROR: Detected NewINIFormat {0}. Are you trying to import a Red Alert map?".F(format));
				return;
			}
		}

		static Dictionary<string, Pair<byte, byte>> overlayResourceMapping = new Dictionary<string, Pair<byte, byte>>()
		{
			// Tiberium
			{ "ti1", new Pair<byte, byte>(1, 0) },
			{ "ti2", new Pair<byte, byte>(1, 1) },
			{ "ti3", new Pair<byte, byte>(1, 2) },
			{ "ti4", new Pair<byte, byte>(1, 3) },
			{ "ti5", new Pair<byte, byte>(1, 4) },
			{ "ti6", new Pair<byte, byte>(1, 5) },
			{ "ti7", new Pair<byte, byte>(1, 6) },
			{ "ti8", new Pair<byte, byte>(1, 7) },
			{ "ti9", new Pair<byte, byte>(1, 8) },
			{ "ti10", new Pair<byte, byte>(1, 9) },
			{ "ti11", new Pair<byte, byte>(1, 10) },
			{ "ti12", new Pair<byte, byte>(1, 11) },
		};

		void UnpackTileData(Stream ms)
		{
			for (var j = 0; j < MapSize; j++)
			{
				for (var i = 0; i < MapSize; i++)
				{
					var type = ms.ReadUInt8();
					var index = ms.ReadUInt8();
					Map.Tiles[new CPos(i, j)] = new TerrainTile(type, index);
				}
			}
		}

		static string[] overlayActors = new string[]
		{
			// Fences
			"sbag", "cycl", "brik", "fenc", "wood", "wood",

			// Fields
			"v12", "v13", "v14", "v15", "v16", "v17", "v18"
		};

		void ReadOverlay(IniFile file)
		{
			var overlay = file.GetSection("OVERLAY", true);
			if (overlay == null)
				return;

			foreach (var kv in overlay)
			{
				var loc = Exts.ParseIntegerInvariant(kv.Key);
				var cell = new CPos(loc % MapSize, loc / MapSize);

				var res = Pair.New((byte)0, (byte)0);
				var type = kv.Value.ToLowerInvariant();
				if (overlayResourceMapping.ContainsKey(type))
					res = overlayResourceMapping[type];

				Map.Resources[cell] = new ResourceTile(res.First, res.Second);
				if (overlayActors.Contains(type))
				{
					var ar = new ActorReference(type)
					{
						new LocationInit(cell),
						new OwnerInit("Neutral")
					};

					var actorCount = Map.ActorDefinitions.Count;
					Map.ActorDefinitions.Add(new MiniYamlNode("Actor" + actorCount++, ar.Save()));
				}
			}
		}

		public override string ParseTreeActor(string input)
		{
			return input.Split(',')[0].ToLowerInvariant();
		}

		public override CPos ParseActorLocation(string input, int loc)
		{
			var newLoc = new CPos(loc % MapSize, loc / MapSize);
			var vectorDown = new CVec(0, 1);

			if (input == "obli" || input == "atwr" || input == "weap" || input == "hand" || input == "tmpl" || input == "split2" || input == "split3")
				newLoc += vectorDown;

			return newLoc;
		}

		public override void LoadPlayer(IniFile file, string section)
		{
			string color;
			string faction;
			switch (section)
			{
			case "GoodGuy":
				color = "gold";
				faction = "gdi";
				break;
			case "BadGuy":
				color = "red"; // TODO: use the grey unit color theme for missions
				faction = "nod";
				break;
			case "Special":
			case "Neutral":
			default:
				color = "neutral";
				faction = "gdi";
				break;
			}

			SetMapPlayers(section, faction, color, file, Players, MapPlayers);
		}

		public override void ReadPacks(IniFile file, string filename)
		{
			using (var s = File.OpenRead(filename.Substring(0, filename.Length - 4) + ".bin"))
				UnpackTileData(s);

			ReadOverlay(file);
		}

		public override void SaveWaypoint(int waypointNumber, ActorReference waypointReference)
		{
			var waypointName = "waypoint" + waypointNumber;
			if (waypointNumber == 25)
				waypointName = "DefaultFlareLocation";
			else if (waypointNumber == 26)
				waypointName = "DefaultCameraPosition";
			else if (waypointNumber == 27)
				waypointName = "DefaultChinookTarget";
			Map.ActorDefinitions.Add(new MiniYamlNode(waypointName, waypointReference.Save()));
		}
	}
}
