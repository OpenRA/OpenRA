#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class PowerManagerInfo : ITraitInfo
	{
		public readonly int AdviceInterval = 250;
		public object Create(ActorInitializer init) { return new PowerManager(init, this); }
	}

	public class PowerManager : ITick
	{
		PowerManagerInfo Info;
		Player Player;
		
		Dictionary<Actor, int> PowerDrain = new Dictionary<Actor, int>();
		[Sync] int totalProvided;
		public int PowerProvided { get { return totalProvided; } }
	
		[Sync] int totalDrained;
		public int PowerDrained { get { return totalDrained; } }
		
		public PowerManager(ActorInitializer init, PowerManagerInfo info)
		{
			Info = info;
			Player = init.self.Owner;
			
			init.world.ActorAdded += ActorAdded;
			init.world.ActorRemoved += ActorRemoved;
		}
		
		void ActorAdded(Actor a)
		{
			if (a.Owner != Player || !a.HasTrait<Building>())
				return;
			Game.Debug("Added {0}: {1}",a.Info.Name, a.Trait<Building>().GetPowerUsage());
			PowerDrain.Add(a, a.Trait<Building>().GetPowerUsage());
			UpdateTotals();
		}
		
		void ActorRemoved(Actor a)
		{
			if (a.Owner != Player || !a.HasTrait<Building>())
				return;
			Game.Debug("Removing {0}",a.Info.Name);
			PowerDrain.Remove(a);
			UpdateTotals();
		}
		
		void UpdateTotals()
		{
			totalProvided = 0;
			totalDrained = 0;
			foreach (var kv in PowerDrain)
			{
				var p = kv.Value;
				if (p > 0)
					totalProvided += p;
				else
					totalDrained -= p;
			}
			Game.Debug("Provided: {0} Drained: {1}",totalProvided, totalDrained);
		}
		
		public void UpdateActor(Actor a, int newPower)
		{
			if (a.Owner != Player || !a.HasTrait<Building>())
				return;
		
			PowerDrain[a] = newPower;
			UpdateTotals();
		}
		
		int nextPowerAdviceTime = 0;
		bool wasLowPower = false;
		public void Tick(Actor self)
		{
			var lowPower = totalProvided < totalDrained;
			if (lowPower && !wasLowPower)
				nextPowerAdviceTime = 0;
			wasLowPower = lowPower;
			
			if (--nextPowerAdviceTime <= 0)
			{
				if (lowPower)
					Player.GiveAdvice(Rules.Info["world"].Traits.Get<EvaAlertsInfo>().LowPower);
				
				nextPowerAdviceTime = Info.AdviceInterval;
			}
		}
		
		public PowerState GetPowerState()
		{
			if (PowerProvided >= PowerDrained) return PowerState.Normal;
			if (PowerProvided > PowerDrained / 2) return PowerState.Low;
			return PowerState.Critical;
		}
	}
}
