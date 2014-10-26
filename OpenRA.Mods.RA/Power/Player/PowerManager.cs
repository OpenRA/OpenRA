#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Power
{
	public class PowerManagerInfo : ITraitInfo, Requires<DeveloperModeInfo>
	{
		public readonly int AdviceInterval = 250;
		public readonly string SpeechNotification = "LowPower";

		public object Create(ActorInitializer init) { return new PowerManager(init.self, this); }
	}

	public class PowerManager : ITick, ISync
	{
		readonly Actor self;
		readonly PowerManagerInfo info;
		readonly DeveloperMode devMode;

		readonly Dictionary<Actor, int> powerDrain = new Dictionary<Actor, int>();
		[Sync] int totalProvided;
		public int PowerProvided { get { return totalProvided; } }

		[Sync] int totalDrained;
		public int PowerDrained { get { return totalDrained; } }

		public int ExcessPower { get { return totalProvided - totalDrained; } }

		public int PowerOutageRemainingTicks { get; private set; }
		public int PowerOutageTotalTicks { get; private set; }

		public PowerManager(Actor self, PowerManagerInfo info)
		{
			this.self = self;
			this.info = info;

			self.World.ActorAdded += UpdateActor;
			self.World.ActorRemoved += RemoveActor;

			devMode = self.Trait<DeveloperMode>();
			wasHackEnabled = devMode.UnlimitedPower;
		}

		public void UpdateActor(Actor a)
		{
			UpdateActors(new[] { a });
		}

		public void UpdateActors(IEnumerable<Actor> actors)
		{
			foreach (var a in actors)
			{
				if (a.Owner != self.Owner)
					return;

				var power = a.TraitOrDefault<Power>();
				if (power == null)
					return;

				powerDrain[a] = power.GetCurrentPower();
			}
			UpdateTotals();
		}

		void RemoveActor(Actor a)
		{
			if (a.Owner != self.Owner || !a.HasTrait<Power>())
				return;

			powerDrain.Remove(a);
			UpdateTotals();
		}

		public void UpdateTotals()
		{
			totalProvided = 0;
			totalDrained = 0;

			foreach (var kv in powerDrain)
			{
				var p = kv.Value;
				if (p > 0)
					totalProvided += p;
				else
					totalDrained -= p;
			}

			if (devMode.UnlimitedPower)
				totalProvided = 1000000;
		}

		int nextPowerAdviceTime = 0;
		bool wasLowPower = false;
		bool wasHackEnabled;

		public void Tick(Actor self)
		{
			if (wasHackEnabled != devMode.UnlimitedPower)
			{
				UpdateTotals();
				wasHackEnabled = devMode.UnlimitedPower;
			}

			var lowPower = totalProvided < totalDrained;
			if (lowPower && !wasLowPower)
				nextPowerAdviceTime = 0;
			wasLowPower = lowPower;

			if (--nextPowerAdviceTime <= 0)
			{
				if (lowPower)
					Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.SpeechNotification, self.Owner.Country.Race);
				nextPowerAdviceTime = info.AdviceInterval;
			}

			if (PowerOutageRemainingTicks > 0 && --PowerOutageRemainingTicks == 0)
				UpdatePowerOutageActors();
		}

		public PowerState PowerState
		{
			get
			{
				if (PowerProvided >= PowerDrained) return PowerState.Normal;
				if (PowerProvided > PowerDrained / 2) return PowerState.Low;
				return PowerState.Critical;
			}
		}

		public void TriggerPowerOutage(int totalTicks)
		{
			PowerOutageTotalTicks = PowerOutageRemainingTicks = totalTicks;
			UpdatePowerOutageActors();
		}

		void UpdatePowerOutageActors()
		{
			var actors = self.World.ActorsWithTrait<AffectedByPowerOutage>()
				.Select(tp => tp.Actor)
				.Where(a => !a.IsDead() && a.IsInWorld && a.Owner == self.Owner);

			UpdateActors(actors);
		}
	}
}
