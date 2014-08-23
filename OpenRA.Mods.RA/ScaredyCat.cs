#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Makes the unit automatically run around when taking damage.")]
	class ScaredyCatInfo : ITraitInfo
	{
		[Desc("How long (in ticks) the actor should panic for.")]
		public readonly int PanicLength = 25 * 10;

		[Desc("Panic movement speed as a precentage of the normal speed.")]
		public readonly int PanicSpeedModifier = 200;

		[Desc("Chance (out of 100) the unit has to enter panic mode when attacked.")]
		public readonly int AttackPanicChance = 20;

		public object Create(ActorInitializer init) { return new ScaredyCat(init.self, this); }
	}

	class ScaredyCat : ITick, INotifyIdle, INotifyDamage, INotifyAttack, ISpeedModifier, ISync, IRenderInfantrySequenceModifier
	{
		readonly ScaredyCatInfo info;
		[Sync] readonly Actor self;
		[Sync] int panicStartedTick;
		bool panicking { get { return panicStartedTick > 0; } }

		public bool IsModifyingSequence { get { return panicking; } }
		public string SequencePrefix { get { return "panic-"; } }

		public ScaredyCat(Actor self, ScaredyCatInfo info)
		{
			this.self = self;
			this.info = info;
		}

		public void Panic()
		{
			if (!panicking)
				self.CancelActivity();

			panicStartedTick = self.World.WorldTick;
		}

		public void Tick(Actor self)
		{
			if (!panicking)
				return;

			if (self.World.WorldTick >= panicStartedTick + info.PanicLength)
			{
				self.CancelActivity();
				panicStartedTick = 0;
			}
		}

		public void TickIdle(Actor self)
		{
			if (!panicking)
				return;

			self.Trait<Mobile>().Nudge(self, self, true);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage > 0)
				Panic();
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (self.World.SharedRandom.Next(100 / info.AttackPanicChance) == 0)
				Panic();
		}

		public int GetSpeedModifier()
		{
			return panicking ? info.PanicSpeedModifier : 100;
		}
	}
}
