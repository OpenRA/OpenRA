#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class BurnsInfo : ITraitInfo, Requires<RenderSimpleInfo>
	{
		public readonly string Anim = "1";
		public readonly int Damage = 1;
		public readonly int Interval = 8;
		public readonly WVec Offset = new WVec(0,0,128);

		public object Create(ActorInitializer init) { return new Burns(init.self, this); }
	}

	class Burns : ITick, ISync
	{
		[Sync] int ticks;
		BurnsInfo Info;

		public Burns(Actor self, BurnsInfo info)
		{
			Info = info;

			var anim = new Animation("fire", () => 0);
			anim.PlayRepeating(Info.Anim);
			self.Trait<RenderSimple>().anims.Add("fire",
				new AnimationWithOffset(anim, () => info.Offset, null));
		}

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				self.InflictDamage(self, Info.Damage, null);
				ticks = Info.Interval;
			}
		}
	}
}
