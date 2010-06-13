using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	class PlayerResourcesInfo : ITraitInfo
	{
		public readonly int InitialCash = 10000;
		public readonly int AdviceInterval = 250;
		public object Create(Actor self) { return new PlayerResources(self); }
	}

	public class PlayerResources : ITick
	{
		Player Owner;
		int AdviceInterval;
		int nextSiloAdviceTime = 0;
		int nextPowerAdviceTime = 0;
		public PlayerResources(Actor self)
		{
			var p = self.Owner;
			Owner = p;
			Cash = self.Info.Traits.Get<PlayerResourcesInfo>().InitialCash;
			AdviceInterval = self.Info.Traits.Get<PlayerResourcesInfo>().AdviceInterval;
		}

		[Sync]
		public int Cash;
		[Sync]
		public int CashCapacity;
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
					nextPowerAdviceTime = 0;
		}

		public PowerState GetPowerState()
		{
			if (PowerProvided >= PowerDrained) return PowerState.Normal;
			if (PowerProvided > PowerDrained / 2) return PowerState.Low;
			return PowerState.Critical;
		}

		public float GetSiloFullness() { return (float)Cash / CashCapacity; }

		public void GiveCash(int num)
		{
			Cash += num;
			
			if (Cash > CashCapacity)
			{
				nextSiloAdviceTime = 0;
				Cash = CashCapacity;
			}
		}
		
		public bool TakeCash(int num)
		{			
			if (Cash < num) return false;
			Cash -= num;
			return true;
		}

		const float displayCashFracPerFrame = .07f;
		const int displayCashDeltaPerFrame = 37;

		public void Tick(Actor self)
		{
			UpdatePower();

			if (--nextPowerAdviceTime <= 0)
			{
				if (PowerProvided - PowerDrained < 0)
					Owner.GiveAdvice(Rules.Info["world"].Traits.Get<EvaAlertsInfo>().LowPower);
				
				nextPowerAdviceTime = AdviceInterval;
			}
			
			CashCapacity = self.World.Queries.OwnedBy[Owner].WithTrait<StoresCash>()
				.Sum(a => a.Actor.Info.Traits.Get<StoresCashInfo>().Capacity);

			if (--nextSiloAdviceTime <= 0)
			{
				if (Cash > 0.8*CashCapacity)
					Owner.GiveAdvice(Owner.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>().SilosNeeded);
				
				nextSiloAdviceTime = AdviceInterval;
			}
			
			var diff = Math.Abs(Cash - DisplayCash);
			var move = Math.Min(Math.Max((int)(diff * displayCashFracPerFrame),
					displayCashDeltaPerFrame), diff);

			var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			if (DisplayCash < Cash)
			{
				DisplayCash += move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickUp);
			}
			else if (DisplayCash > Cash)
			{
				DisplayCash -= move;
				Sound.PlayToPlayer(self.Owner, eva.CashTickDown);
			}
		}
	}
}
