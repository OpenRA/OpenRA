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

using System.Collections.Generic;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	sealed class ImportTiberianSunMapCommand : ImportGen2MapCommand, IUtilityCommand
	{
		string IUtilityCommand.Name => "--import-ts-map";

		bool IUtilityCommand.ValidateArguments(string[] args) { return args.Length >= 2; }

		[Desc("FILENAME", "Convert a Tiberian Sun map to the OpenRA format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Run(utility, args);
		}

		#region Mod-specific data

		protected override Dictionary<byte, string> OverlayToActor { get; } = new()
		{
			{ 0x00, "gasand" },
			{ 0x01, "gasand" },
			{ 0x02, "gawall" },
			{ 0x03, "gawall" },
			{ 0x18, "bridge1" },
			{ 0x19, "bridge2" },
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
			{ 0x3B, "railbrdg1" },
			{ 0x3C, "railbrdg2" },
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

			// Bridges
			{ 0x4A, "lobrdg_b" }, // lobrdg01
			{ 0x4B, "lobrdg_b" }, // lobrdg02
			{ 0x4C, "lobrdg_b" }, // lobrdg03
			{ 0x4D, "lobrdg_b" }, // lobrdg04
			{ 0x4E, "lobrdg_b" }, // lobrdg05
			{ 0x4F, "lobrdg_b" }, // lobrdg06
			{ 0x50, "lobrdg_b" }, // lobrdg07
			{ 0x51, "lobrdg_b" }, // lobrdg08
			{ 0x52, "lobrdg_b" }, // lobrdg09
			{ 0x53, "lobrdg_a" }, // lobrdg10
			{ 0x54, "lobrdg_a" }, // lobrdg11
			{ 0x55, "lobrdg_a" }, // lobrdg12
			{ 0x56, "lobrdg_a" }, // lobrdg13
			{ 0x57, "lobrdg_a" }, // lobrdg14
			{ 0x58, "lobrdg_a" }, // lobrdg15
			{ 0x59, "lobrdg_a" }, // lobrdg16
			{ 0x5A, "lobrdg_a" }, // lobrdg17
			{ 0x5B, "lobrdg_a" }, // lobrdg18
			{ 0x5C, "lobrdg_r_se" }, // lobrdg19
			{ 0x5D, "lobrdg_r_se" }, // lobrdg20
			{ 0x5E, "lobrdg_r_nw" }, // lobrdg21
			{ 0x5F, "lobrdg_r_nw" }, // lobrdg22
			{ 0x60, "lobrdg_r_ne" }, // lobrdg23
			{ 0x61, "lobrdg_r_ne" }, // lobrdg24
			{ 0x62, "lobrdg_r_sw" }, // lobrdg25
			{ 0x63, "lobrdg_r_sw" }, // lobrdg26
			{ 0x64, "lobrdg_b_d" }, // lobrdg27
			{ 0x65, "lobrdg_a_d" }, // lobrdg28

			// Ramps
			{ 0x7A, "lobrdg_r_se" }, // lobrdg1
			{ 0x7B, "lobrdg_r_nw" }, // lobrdg2
			{ 0x7C, "lobrdg_r_ne" }, // lobrdg3
			{ 0x7D, "lobrdg_r_sw" }, // lobrdg4

			// Other
			{ 0x1B, "bigblue" },
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
			{ 0xB2, null }, // veinholedummy
			{ 0xB3, "crate" }
		};

		protected override Dictionary<byte, Size> OverlayShapes { get; } = new()
		{
			{ 0x4A, new Size(1, 3) },
			{ 0x4B, new Size(1, 3) },
			{ 0x4C, new Size(1, 3) },
			{ 0x4D, new Size(1, 3) },
			{ 0x4E, new Size(1, 3) },
			{ 0x4F, new Size(1, 3) },
			{ 0x50, new Size(1, 3) },
			{ 0x51, new Size(1, 3) },
			{ 0x52, new Size(1, 3) },
			{ 0x53, new Size(3, 1) },
			{ 0x54, new Size(3, 1) },
			{ 0x55, new Size(3, 1) },
			{ 0x56, new Size(3, 1) },
			{ 0x57, new Size(3, 1) },
			{ 0x58, new Size(3, 1) },
			{ 0x59, new Size(3, 1) },
			{ 0x5A, new Size(3, 1) },
			{ 0x5B, new Size(3, 1) },
			{ 0x5C, new Size(1, 3) },
			{ 0x5D, new Size(1, 3) },
			{ 0x5E, new Size(1, 3) },
			{ 0x5F, new Size(1, 3) },
			{ 0x60, new Size(3, 1) },
			{ 0x61, new Size(3, 1) },
			{ 0x62, new Size(3, 1) },
			{ 0x63, new Size(3, 1) },
			{ 0x64, new Size(1, 3) },
			{ 0x65, new Size(3, 1) },
			{ 0x7A, new Size(1, 3) },
			{ 0x7B, new Size(1, 3) },
			{ 0x7C, new Size(3, 1) },
			{ 0x7D, new Size(3, 1) },
		};

		protected override Dictionary<byte, DamageState> OverlayToHealth { get; } = new()
		{
			// 1,3 bridge tiles
			{ 0x4A, DamageState.Undamaged },
			{ 0x4B, DamageState.Undamaged },
			{ 0x4C, DamageState.Undamaged },
			{ 0x4D, DamageState.Undamaged },
			{ 0x4E, DamageState.Heavy },
			{ 0x4F, DamageState.Heavy },
			{ 0x50, DamageState.Heavy },
			{ 0x51, DamageState.Critical },
			{ 0x52, DamageState.Critical },

			// 3,1 bridge tiles
			{ 0x53, DamageState.Undamaged },
			{ 0x54, DamageState.Undamaged },
			{ 0x55, DamageState.Undamaged },
			{ 0x56, DamageState.Undamaged },
			{ 0x57, DamageState.Heavy },
			{ 0x58, DamageState.Heavy },
			{ 0x59, DamageState.Heavy },
			{ 0x5A, DamageState.Critical },
			{ 0x5B, DamageState.Critical },

			// Ramps
			{ 0x5C, DamageState.Undamaged },
			{ 0x5D, DamageState.Heavy },
			{ 0x5E, DamageState.Undamaged },
			{ 0x5F, DamageState.Heavy },
			{ 0x60, DamageState.Undamaged },
			{ 0x61, DamageState.Heavy },
			{ 0x62, DamageState.Undamaged },
			{ 0x63, DamageState.Heavy },

			// Ramp duplicates
			{ 0x7A, DamageState.Undamaged },
			{ 0x7B, DamageState.Undamaged },
			{ 0x7C, DamageState.Undamaged },
			{ 0x7D, DamageState.Undamaged },

			// Actually dead, placeholders for resurrection
			{ 0x64, DamageState.Undamaged },
			{ 0x65, DamageState.Undamaged },
		};

		protected override Dictionary<byte, byte[]> ResourceFromOverlay { get; } = new()
		{
			// "tib" - Regular Tiberium
			{
				0x01, new byte[]
				{
					0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
					0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79
				}
			},

			// "btib" - Blue Tiberium
			{
				0x02, new byte[]
				{
					0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26,

					// Should be "tib2"
					0x7F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88,
					0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F, 0x90, 0x91, 0x92,

					// Should be "tib3"
					0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C,
					0x9D, 0x9E, 0x9F, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6
				}
			},

			// Veins
			{ 0x03, new byte[] { 0x7E } }
		};

		protected override Dictionary<string, string> DeployableActors { get; } = new()
		{
			{ "gadpsa", "lpst" },
			{ "gatick", "ttnk" },
			{ "gaarty", "art2" },
			{ "djugg", "jugg" },

			// Not yet implemented actors:
			// { "gaicbm", "icbm" },
			// { "dlimpet", "limpet" },
			// { "dgweap", "mobwarg" },
			// { "dnweap", "mobwarn" },
			// { "mstl", "sgen" },
			// { "ddefd", "defender" },
		};

		protected override string[] LampActors { get; } =
		{
			"GALITE", "INGALITE", "NEGLAMP", "REDLAMP", "NEGRED", "GRENLAMP", "BLUELAMP", "YELWLAMP",
			"INYELWLAMP", "PURPLAMP", "INPURPLAMP", "INORANLAMP", "INGRNLMP", "INREDLMP", "INBLULMP"
		};

		protected override string[] CreepActors { get; } = { "DOGGIE", "VISC_SML", "VISC_LRG", "JFISH" };

		#endregion

		#region Method overrides

		protected override bool TryHandleOverlayToActorInner(CPos cell, byte[] overlayPack, CellLayer<int> overlayIndex, byte overlayType, out ActorReference actorReference)
		{
			actorReference = null;
			if (!OverlayToActor.TryGetValue(overlayType, out var actorType))
				return false;

			// Just a dummy handler apparently (see "veinholedummy").
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

			// Fix position of vein hole actors.
			var location = cell;
			if (actorType == "veinhole")
				location -= new CVec(1, 1);

			actorReference = new ActorReference(actorType)
			{
				new LocationInit(location),
				new OwnerInit("Neutral")
			};

			TryHandleOverlayToHealthInner(overlayType, actorReference);

			return true;
		}

		protected override bool TryHandleOtherOverlayInner(Map map, CPos cell, byte[] overlayDataPack, CellLayer<int> overlayIndex, byte overlayType)
		{
			// TS maps encode the non-harvestable border tiles as overlay
			// Only convert to vein resources if the overlay data specifies non-border frames
			if (overlayType == 0x7E)
			{
				var frame = overlayDataPack[overlayIndex[cell]];
				if (frame < 48 || frame > 60)
					return true;

				// Pick half or full density based on the frame
				map.Resources[cell] = new ResourceTile(3, (byte)(frame == 52 ? 1 : 2));
				return true;
			}

			return false;
		}

		#endregion
	}
}
