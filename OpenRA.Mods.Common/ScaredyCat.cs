#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	class ScaredyCatInfo : ITraitInfo
	{
		public readonly int PanicLength = 25 * 10;
		public readonly decimal PanicSpeedModifier = 2;
		public readonly int AttackPanicChance = 20;

		public object Create(ActorInitializer init) { return new ScaredyCat(init.self, this); }
	}

	class ScaredyCat : ITick, INotifyIdle, INotifyDamage, INotifyAttack, ISpeedModifier, ISync
	{
		readonly ScaredyCatInfo Info;
		[Sync] readonly Actor Self;

		[Sync] public int PanicStartedTick;
		[Sync] public bool Panicking { get { return PanicStartedTick > 0; } }

		public ScaredyCat(Actor self, ScaredyCatInfo info)
		{
			Self = self;
			Info = info;
		}

		public void Panic()
		{
			if (!Panicking)
				Self.CancelActivity();
			PanicStartedTick = Self.World.WorldTick;
		}

		public void Tick(Actor self)
		{
			if (!Panicking) return;

			if (self.World.WorldTick >= PanicStartedTick + Info.PanicLength)
			{
				self.CancelActivity();
				PanicStartedTick = 0;
			}
		}

		public void TickIdle(Actor self)
		{
			if (!Panicking) return;

			self.Trait<Mobile>().Nudge(self, self, true);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage > 0)
				Panic();
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (self.World.SharedRandom.Next(100 / Info.AttackPanicChance) == 0)
				Panic();
		}

		public decimal GetSpeedModifier()
		{
			return Panicking ? Info.PanicSpeedModifier : 1;
		}
	}
}
