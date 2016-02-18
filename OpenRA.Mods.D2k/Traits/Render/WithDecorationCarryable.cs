#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Displays a sprite when the carryable actor is waiting for pickup.")]
	public class WithDecorationCarryableInfo : WithDecorationInfo, Requires<CarryableInfo>
	{
		public override object Create(ActorInitializer init) { return new WithDecorationCarryable(init.Self, this); }
	}

	public class WithDecorationCarryable : WithDecoration
	{
		readonly Carryable carryable;

		public WithDecorationCarryable(Actor self, WithDecorationCarryableInfo info)
			: base(self, info)
		{
			carryable = self.Trait<Carryable>();
		}

		public override bool ShouldRender(Actor self)
		{
			return carryable.Reserved;
		}
	}
}
