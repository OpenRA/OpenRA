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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Changes the visual Z position periodically.")]
	class HoversInfo : ITraitInfo, Requires<IMoveInfo>
	{
		[Desc("Amount of Z axis changes in world units.")]
		public readonly int OffsetModifier = -43;

		public object Create(ActorInitializer init) { return new Hovers(this, init.self); }
	}

	class Hovers : IRenderModifier
	{
		readonly HoversInfo info;
		readonly bool aircraft;

		public Hovers(HoversInfo info, Actor self)
		{
			this.info = info;
			aircraft = self.HasTrait<Aircraft>();
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (self.World.Paused)
				return r;

			var visualOffset = !aircraft || self.CenterPosition.Z > 0 ? (int)Math.Abs((self.ActorID + Game.LocalTick) / 5 % 4 - 1) - 1 : 0;
			var worldVisualOffset = new WVec(0, 0, info.OffsetModifier * visualOffset);

			return r.Select(a => a.OffsetBy(worldVisualOffset));
		}
	}
}
