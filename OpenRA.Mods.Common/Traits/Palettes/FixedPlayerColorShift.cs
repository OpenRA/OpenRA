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
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Add fixed color shifts to player palettes. Use to add RGBA compatibility to IndexedPlayerPalette.")]
	public class FixedPlayerColorShiftInfo : TraitInfo
	{
		[PaletteReference(true)]
		[FieldLoader.Require]
		[Desc("The name of the palette to base off.")]
		public readonly string BasePalette = null;

		public readonly Dictionary<string, float[]> PlayerIndex;

		public override object Create(ActorInitializer init) { return new FixedPlayerColorShift(this); }
	}

	public class FixedPlayerColorShift : ILoadsPlayerPalettes
	{
		readonly FixedPlayerColorShiftInfo info;

		public FixedPlayerColorShift(FixedPlayerColorShiftInfo info)
		{
			this.info = info;
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, Color color, bool replaceExisting)
		{
			if (info.PlayerIndex.TryGetValue(playerName, out var shift))
				wr.SetPaletteColorShift(info.BasePalette + playerName, shift[0], shift[1], shift[2], shift[3], shift[4]);
		}
	}
}
