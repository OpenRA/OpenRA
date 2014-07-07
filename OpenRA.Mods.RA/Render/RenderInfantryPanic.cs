#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderInfantryPanicInfo : RenderInfantryInfo, Requires<ScaredyCatInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderInfantryPanic(init.self, this); }
	}

	class RenderInfantryPanic : RenderInfantry
	{
		readonly ScaredyCat sc;
		bool wasPanic;

		public RenderInfantryPanic(Actor self, RenderInfantryPanicInfo info)
			: base(self, info)
		{
			sc = self.Trait<ScaredyCat>();
		}

		protected override string NormalizeInfantrySequence(Actor self, string baseSequence)
		{
			var prefix = sc != null && sc.Panicking ? "panic-" : "";

			if (DefaultAnimation.HasSequence(prefix + baseSequence))
				return prefix + baseSequence;
			else
				return baseSequence;
		}

		protected override bool AllowIdleAnimation(Actor self)
		{
			return base.AllowIdleAnimation(self) && !sc.Panicking;
		}

		public override void TickRender(WorldRenderer wr, Actor self)
		{
			if (wasPanic != sc.Panicking)
				dirty = true;

			wasPanic = sc.Panicking;
			base.TickRender(wr, self);
		}
	}
}

