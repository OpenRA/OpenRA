#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Render
{
	class WithShadowInfo : TraitInfo<WithShadow> {}

	class WithShadow : IRenderModifier
	{
		public IEnumerable<Renderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<Renderable> r)
		{
			var move = self.Trait<IMove>();

			/* rude hack */
			var visualOffset = ((move is Helicopter || move is Mobile) && move.Altitude > 0)
				? Math.Abs((self.ActorID + Game.LocalTick) / 5 % 4 - 1) - 1 : 0;

			var shadowSprites = r.Select(a => a.WithPalette(wr.Palette("shadow")));
			var flyingSprites = (move.Altitude <= 0) ? r
				: r.Select(a => a.WithPxOffset(new float2(0, -(move.Altitude + visualOffset))).WithZOffset(move.Altitude + a.ZOffset));

			return shadowSprites.Concat(flyingSprites);
		}
	}
}
