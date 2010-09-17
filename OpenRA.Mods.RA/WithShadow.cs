#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class WithShadowInfo : TraitInfo<WithShadow> {}

	class WithShadow : IRenderModifier
	{
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			var move = self.Trait<IMove>();

			var shadowSprites = r.Select(a => a.WithPalette("shadow"));
			var flyingSprites = (move.Altitude <= 0) ? r 
				: r.Select(a => a.WithPos(a.Pos - new float2(0, move.Altitude)).WithZOffset(move.Altitude));

			return shadowSprites.Concat(flyingSprites);
		}
	}
}
