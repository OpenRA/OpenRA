#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class StrategicPointInfo : TraitInfo<StrategicPoint> {}
	public class StrategicPoint {}

	public class StrategicVictoryConditionsInfo : ITraitInfo, Requires<ConquestVictoryConditionsInfo>
	{
		public readonly int TicksToHold = 25 * 60 * 5; // ~5 minutes
		public readonly bool ResetOnHoldLost = true;
		public readonly float RatioRequired = 0.5f; // 50% required of all koth locations

		public object Create(ActorInitializer init) { return new StrategicVictoryConditions(init.self, this); }
	}

	public class StrategicVictoryConditions : ITick, ISync
	{
		Actor self;
		StrategicVictoryConditionsInfo info;

		[Sync] public int TicksLeft = 0;

		public StrategicVictoryConditions(Actor self, StrategicVictoryConditionsInfo info)
		{
			this.self = self;
			this.info = info;
		}

		public IEnumerable<TraitPair<StrategicPoint>> AllPoints
		{
			get { return self.World.ActorsWithTrait<StrategicPoint>(); }
		}

		public int Total { get { return AllPoints.Count(); } }
		int Owned {	get { return AllPoints.Count( a => WorldUtils.AreMutualAllies( self.Owner, a.Actor.Owner )); } }

		public bool Holding { get { return Owned >= info.RatioRequired * Total; } }

		public void Tick(Actor self)
		{
			if (self.Owner.WinState != WinState.Undefined || self.Owner.NonCombatant) return;

			// See if any of the conditions are met to increase the count
			if (Total > 0)
			{
				if (Holding)
				{
					// Hah! We met ths critical owned condition
					if (TicksLeft == 0)
						TicksLeft = info.TicksToHold; // first tick -- this is crap.
					else if (--TicksLeft == 0)
						Won();
				}
				else if (TicksLeft != 0)
					if (info.ResetOnHoldLost)
						TicksLeft = info.TicksToHold; // Reset the time hold
			}
		}

		void Won()
		{
			// Player has won
			foreach (var p in self.World.Players)
			{
				var cvc = p.PlayerActor.Trait<ConquestVictoryConditions>();

				if (p.WinState == WinState.Undefined && WorldUtils.AreMutualAllies(self.Owner, p))
					cvc.Win(p.PlayerActor);
				else if (p.WinState == WinState.Undefined)
					cvc.Lose(p.PlayerActor);
			}
		}
	}
}
