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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Changes the visual Z position periodically.")]
	public class HoversInfo : UpgradableTraitInfo, Requires<IMoveInfo>
	{
		[Desc("Amount of Z axis changes in world units.")]
		public readonly int OffsetModifier = -43;

		public readonly int MinHoveringAltitude = 0;

		public override object Create(ActorInitializer init) { return new Hovers(this, init.Self); }
	}

	public class Hovers : UpgradableTrait<HoversInfo>, IRenderModifier
	{
		readonly HoversInfo info;

		public Hovers(HoversInfo info, Actor self)
			: base(info)
		{
			this.info = info;
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (self.World.Paused || IsTraitDisabled)
				return r;

			var visualOffset = self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length >= info.MinHoveringAltitude
				? (int)Math.Abs((self.ActorID + Game.LocalTick) / 5 % 4 - 1) - 1
				: 0;
			var worldVisualOffset = new WVec(0, 0, info.OffsetModifier * visualOffset);

			return r.Select(a => a.OffsetBy(worldVisualOffset));
		}
	}
}
