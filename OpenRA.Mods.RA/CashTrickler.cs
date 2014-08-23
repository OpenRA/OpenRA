#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Lets the actor generate cash in a set periodic time.")]
	class CashTricklerInfo : ITraitInfo
	{
		[Desc("Number of ticks to wait between giving money.")]
		public readonly int Period = 50;

		[Desc("Amount of money to give each time.")]
		public readonly int Amount = 15;

		[Desc("Whether to show the cash tick indicators (+$15 rising from actor).")]
		public readonly bool ShowTicks = true;

		[Desc("Amount of money awarded for capturing the actor.")]
		public readonly int CaptureAmount = 0;

		[Desc("Type of income this source falls under.")]
		public readonly string IncomeType = "CashTrickler";

		public object Create (ActorInitializer init) { return new CashTrickler(this); }
	}

	class CashTrickler : ITick, ISync, INotifyCapture
	{
		[Sync] int ticks;
		CashTricklerInfo Info;

		public CashTrickler(CashTricklerInfo info)
		{
			Info = info;
		}

		public void Tick(Actor self)
		{
			if (--ticks < 0)
			{
				ticks = Info.Period;

				var incomeMultModifier = 0;
				if (self.Owner != null)
					if (self.Owner.PlayerActor.TraitsImplementing<IAttributeModManager>().Where(man => man.ModType == ModifierType.Income).Any())
						incomeMultModifier += self.Owner.PlayerActor.TraitsImplementing<IAttributeModManager>().Where(man => man.ModType == ModifierType.Income)
						.Select(t => t.GetModifier(ModifierType.Income, Info.IncomeType)).Sum();
	
				if (self.TraitsImplementing<IAttributeModManager>().Where(man => man.ModType == ModifierType.Income).Any())
					incomeMultModifier += self.TraitsImplementing<IAttributeModManager>().Where(man => man.ModType == ModifierType.Income)
					.Select(t => t.GetModifier(ModifierType.Income, Info.IncomeType)).Sum();
	
				var incomeAmount = Info.Amount * (100 + incomeMultModifier) / 100;

				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(incomeAmount);
				MaybeAddCashTick(self, incomeAmount);
			}
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (Info.CaptureAmount > 0)
			{
				newOwner.PlayerActor.Trait<PlayerResources>().GiveCash(Info.CaptureAmount);
				MaybeAddCashTick(self, Info.CaptureAmount);
			}
		}

		void MaybeAddCashTick(Actor self, int amount)
		{
			if (Info.ShowTicks)
				self.World.AddFrameEndTask(w => w.Add(new CashTick(self.CenterPosition, self.Owner.Color.RGB, amount)));
		}
	}
}
