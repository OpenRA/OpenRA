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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor will play a fire animation over its body and take damage over time.")]
	class BurnsInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		public readonly string Anim = "1";
		public readonly int Damage = 1;
		public readonly int Interval = 8;

		public object Create(ActorInitializer init) { return new Burns(init.Self, this); }
	}

	class Burns : ITick, ISync
	{
		readonly BurnsInfo info;
		[Sync] int ticks;

		public Burns(Actor self, BurnsInfo info)
		{
			this.info = info;

			var anim = new Animation(self.World, "fire", () => 0);
			anim.IsDecoration = true;
			anim.PlayRepeating(info.Anim);
			self.Trait<RenderSprites>().Add(anim);
		}

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				self.InflictDamage(self, info.Damage, null);
				ticks = info.Interval;
			}
		}
	}
}
