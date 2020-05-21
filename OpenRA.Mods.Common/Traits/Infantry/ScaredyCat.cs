#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Makes the unit automatically run around when taking damage.")]
	class ScaredyCatInfo : TraitInfo, Requires<MobileInfo>
	{
		[Desc("Chance (out of 100) the unit has to enter panic mode when attacked.")]
		public readonly int PanicChance = 100;

		[Desc("How long (in ticks) the actor should panic for.")]
		public readonly int PanicLength = 25 * 10;

		[Desc("Panic movement speed as a percentage of the normal speed.")]
		public readonly int PanicSpeedModifier = 200;

		[Desc("Chance (out of 100) the unit has to enter panic mode when attacking.")]
		public readonly int AttackPanicChance = 20;

		[SequenceReference(null, true)]
		public readonly string PanicSequencePrefix = "panic-";

		public override object Create(ActorInitializer init) { return new ScaredyCat(init.Self, this); }
	}

	class ScaredyCat : ITick, INotifyIdle, INotifyDamage, INotifyAttack, ISpeedModifier, ISync, IRenderInfantrySequenceModifier
	{
		readonly ScaredyCatInfo info;
		readonly Mobile mobile;
		readonly Actor self;

		[Sync]
		int panicStartedTick;
		bool Panicking { get { return panicStartedTick > 0; } }

		bool IRenderInfantrySequenceModifier.IsModifyingSequence { get { return Panicking; } }
		string IRenderInfantrySequenceModifier.SequencePrefix { get { return info.PanicSequencePrefix; } }

		public ScaredyCat(Actor self, ScaredyCatInfo info)
		{
			this.self = self;
			this.info = info;
			mobile = self.Trait<Mobile>();
		}

		void Panic()
		{
			if (!Panicking)
				self.CancelActivity();

			panicStartedTick = self.World.WorldTick;
		}

		void ITick.Tick(Actor self)
		{
			if (!Panicking)
				return;

			if (self.World.WorldTick >= panicStartedTick + info.PanicLength)
			{
				self.CancelActivity();
				panicStartedTick = 0;
			}
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (!Panicking)
				return;

			mobile.Nudge(self);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value > 0 && self.World.SharedRandom.Next(100) < info.PanicChance)
				Panic();
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (self.World.SharedRandom.Next(100) < info.AttackPanicChance)
				Panic();
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		int ISpeedModifier.GetSpeedModifier()
		{
			return Panicking ? info.PanicSpeedModifier : 100;
		}
	}
}
