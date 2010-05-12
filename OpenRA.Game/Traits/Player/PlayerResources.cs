using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	class PlayerResourcesInfo : ITraitInfo
	{
		public readonly int InitialCash = 10000;
		public readonly int InitialOre = 0;

		public object Create(Actor self) { return new PlayerResources(self.Owner); }
	}

	public class PlayerResources : ITick
	{
		Player Owner;

		public PlayerResources(Player p)
		{
			Owner = p;
			Cash = p.PlayerActor.Info.Traits.Get<PlayerResourcesInfo>().InitialCash;
			Ore = p.PlayerActor.Info.Traits.Get<PlayerResourcesInfo>().InitialOre;
		}

		[Sync]
		public int Cash;
		[Sync]
		public int Ore;
		[Sync]
		public int OreCapacity;
		[Sync]
		public int DisplayCash;
		[Sync]
		public int PowerProvided;
		[Sync]
		public int PowerDrained;

		void UpdatePower()
		{
			var oldBalance = PowerProvided - PowerDrained;

			PowerProvided = 0;
			PowerDrained = 0;

			var myBuildings = Owner.World.Queries.OwnedBy[Owner].WithTrait<Building>();

			foreach (var a in myBuildings)
			{
				var q = a.Trait.GetPowerUsage();
				if (q > 0)
					PowerProvided += q;
				else
					PowerDrained -= q;
			}

			if (PowerProvided - PowerDrained < 0)
				if (PowerProvided - PowerDrained != oldBalance)
					Owner.GiveAdvice(Rules.Info["world"].Traits.Get<EvaAlertsInfo>().LowPower);
		}

		public PowerState GetPowerState()
		{
			if (PowerProvided >= PowerDrained) return PowerState.Normal;
			if (PowerProvided > PowerDrained / 2) return PowerState.Low;
			return PowerState.Critical;
		}

		public float GetSiloFullness() { return (float)Ore / OreCapacity; }

		public void GiveCash(int num) { Cash += num; }
		public void GiveOre(int num)
		{
			Ore += num;

			if (Ore > OreCapacity)
				Ore = OreCapacity;		// trim off the overflow.

			if (Ore > .8 * OreCapacity)
				Owner.GiveAdvice(Owner.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>().SilosNeeded);
		}

		public bool TakeCash(int num)
		{
			if (Cash + Ore < num) return false;
			if (Ore <= num)
			{
				num -= Ore;
				Ore = 0;
				Cash -= num;
			}
			else
				Ore -= num;

			return true;
		}

		const float displayCashFracPerFrame = .07f;
		const int displayCashDeltaPerFrame = 37;

		public void Tick(Actor self)
		{
			UpdatePower();

			OreCapacity = self.World.Queries.OwnedBy[Owner].WithTrait<StoresOre>()
				.Sum(a => a.Actor.Info.Traits.Get<StoresOreInfo>().Capacity);

			var totalMoney = Cash + Ore;
			var diff = Math.Abs(totalMoney - DisplayCash);
			var move = Math.Min(Math.Max((int)(diff * displayCashFracPerFrame),
					displayCashDeltaPerFrame), diff);

			var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			if (DisplayCash < totalMoney)
			{
				DisplayCash += move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickUp);
			}
			else if (DisplayCash > totalMoney)
			{
				DisplayCash -= move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickDown);
			}
		}
	}
}
