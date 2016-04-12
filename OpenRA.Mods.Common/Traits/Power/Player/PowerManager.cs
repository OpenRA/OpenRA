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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the player actor.")]
	public class PowerManagerInfo : ITraitInfo, Requires<DeveloperModeInfo>
	{
		public readonly int AdviceInterval = 250;
		public readonly string SpeechNotification = "LowPower";

		public object Create(ActorInitializer init) { return new PowerManager(init.Self, this); }
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

			devMode = self.Trait<DeveloperMode>();
			wasHackEnabled = devMode.UnlimitedPower;
		}

		public void UpdateActor(Actor a)
		{
			int old;
			powerDrain.TryGetValue(a, out old); // old is 0 if a is not in powerDrain
			var amount = a.TraitsImplementing<Power>().Where(t => !t.IsTraitDisabled).Aggregate(0, (v, p) => v + p.GetEnabledPower());
			powerDrain[a] = amount;
			if (amount == old || devMode.UnlimitedPower)
				return;
			if (old > 0)
				totalProvided -= old;
			else if (old < 0)
				totalDrained += old;
			if (amount > 0)
				totalProvided += amount;
			else if (amount < 0)
				totalDrained -= amount;
		}

		public void RemoveActor(Actor a)
		{
			int amount;
			if (!powerDrain.TryGetValue(a, out amount))
				return;
			powerDrain.Remove(a);

			if (devMode.UnlimitedPower)
				return;

			if (amount > 0)
				totalProvided -= amount;
			else if (amount < 0)
				totalDrained += amount;
		}

		int nextPowerAdviceTime = 0;
		bool wasLowPower = false;
		bool wasHackEnabled;

		public void Tick(Actor self)
		{
			if (wasHackEnabled != devMode.UnlimitedPower)
			{
				totalProvided = 0;
				totalDrained = 0;

				if (!devMode.UnlimitedPower)
					foreach (var kv in powerDrain)
						if (kv.Value > 0)
							totalProvided += kv.Value;
						else if (kv.Value < 0)
							totalDrained -= kv.Value;

				wasHackEnabled = devMode.UnlimitedPower;
			}

			var lowPower = totalProvided < totalDrained;
			if (lowPower && !wasLowPower)
				nextPowerAdviceTime = 0;
			wasLowPower = lowPower;

			if (--nextPowerAdviceTime <= 0)
			{
				if (lowPower)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.SpeechNotification, self.Owner.Faction.InternalName);
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
			var actors = self.World.ActorsHavingTrait<AffectedByPowerOutage>()
				.Where(a => !a.IsDead && a.IsInWorld && a.Owner == self.Owner);

			foreach (var a in actors)
				UpdateActor(a);
		}
	}
}
