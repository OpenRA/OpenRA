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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	public class PowerManagerInfo : ITraitInfo, Requires<DeveloperModeInfo>
	{
		public readonly int AdviceInterval = 250;
		public object Create(ActorInitializer init) { return new PowerManager(init, this); }
	}

	public class PowerManager : ITick, ISync
	{
		readonly PowerManagerInfo info;
		readonly Player player;
		readonly DeveloperMode devMode;

		readonly Dictionary<Actor, int> powerDrain = new Dictionary<Actor, int>();
		[Sync] int totalProvided;
		public int PowerProvided { get { return totalProvided; } }

		[Sync] int totalDrained;
		public int PowerDrained { get { return totalDrained; } }

		public int ExcessPower { get { return totalProvided - totalDrained; } }

		public PowerManager(ActorInitializer init, PowerManagerInfo info)
		{
			this.info = info;
			player = init.self.Owner;

			init.world.ActorAdded += ActorAdded;
			init.world.ActorRemoved += ActorRemoved;

			devMode = init.self.Trait<DeveloperMode>();
			wasHackEnabled = devMode.UnlimitedPower;
		}

		void ActorAdded(Actor a)
		{
			if (a.Owner != player)
				return;

			var power = a.TraitOrDefault<Power>();
			if (power == null)
				return;

			powerDrain.Add(a, power.CurrentPower);
			UpdateTotals();
		}

		void ActorRemoved(Actor a)
		{
			if (a.Owner != player || !a.HasTrait<Power>())
				return;
			powerDrain.Remove(a);
			UpdateTotals();
		}

		void UpdateTotals()
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

		public void UpdateActor(Actor a, int newPower)
		{
			if (a.Owner != player || !a.HasTrait<Power>())
				return;

			powerDrain[a] = newPower;
			UpdateTotals();
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
					Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", "LowPower", self.Owner.Country.Race);
				nextPowerAdviceTime = info.AdviceInterval;
			}
		}

		public PowerState PowerState
		{
			get {
				if (PowerProvided >= PowerDrained) return PowerState.Normal;
				if (PowerProvided > PowerDrained / 2) return PowerState.Low;
				return PowerState.Critical;
			}
		}
	}
}
