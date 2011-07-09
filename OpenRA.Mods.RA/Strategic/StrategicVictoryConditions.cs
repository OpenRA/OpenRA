#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	/// <summary>
	/// Attach to players only kthx :)
	/// </summary>
	public class StrategicVictoryConditionsInfo : ITraitInfo, Requires<ConquestVictoryConditionsInfo>
	{
		public readonly int TicksToHold = 25 * 60 * 5; // ~5 minutes
		public readonly bool ResetOnHoldLost = true;
		public readonly float RatioRequired = 0.5f; // 50% required of all koth locations
		public readonly float CriticalRatioRequired = 1f; // if someone owns 100% of all critical locations	
		public readonly bool SplitHolds = true; // disallow or allow the 'holdsrequired' to include critical locations

		public object Create(ActorInitializer init) { return new StrategicVictoryConditions(init.self, this); }	
	}

	public class StrategicVictoryConditions : ITick, ISync
	{
		Actor self;
		StrategicVictoryConditionsInfo info;

		[Sync] bool SplitHolds;
		[Sync] public int TicksLeft = 0;
		[Sync] public int CriticalTicksLeft = 0;

		public StrategicVictoryConditions(Actor self, StrategicVictoryConditionsInfo info)
		{
			this.self = self;
			this.info = info;
			SplitHolds = info.SplitHolds;
		}

		/// <summary>
		/// Includes your allies as well
		/// </summary>
		public int Owned
		{
			get { return CountOwnedPoints(false) + (SplitHolds ? 0 : OwnedCritical); }
		}

		/// <summary>
		/// Includes your allies as well
		/// </summary>
		public int OwnedCritical
		{
			get { return CountOwnedPoints(true); }
		}

		IEnumerable<TraitPair<StrategicPoint>> AllPoints
		{
			get { return self.World.ActorsWithTrait<StrategicPoint>(); }
		}

		public int Total
		{
			get
			{
				return SplitHolds
					? AllPoints.Count( a => a.Trait.Critical )
					: AllPoints.Count();
			}
		}

		public int TotalCritical
		{
			get
			{
				return AllPoints.Count( a => a.Trait.Critical );
			}
		}

		public int CountOwnedPoints(bool critical)
		{
			return AllPoints.Count( a => a.Trait.Critical == critical &&
				WorldUtils.AreMutualAllies( self.Owner, a.Actor.Owner ));
		}

		public bool HoldingCritical
		{
			get
			{
				var criticalOwned = 1f / TotalCritical * OwnedCritical;

				return criticalOwned >= info.CriticalRatioRequired;
			}
		}

		public bool Holding
		{
			get
			{
				var owned = 1f / Total * Owned;

				return owned >= info.RatioRequired;
			}
		}

		public void Tick(Actor self)
		{
			if (self.Owner.WinState != WinState.Undefined || self.Owner.NonCombatant) return;

			// See if any of the conditions are met to increase the count
			if (TotalCritical > 0)
			{
				if (HoldingCritical)
				{
					// Hah! We met ths critical owned condition
					if (CriticalTicksLeft == 0)
						CriticalTicksLeft = info.TicksToHold;	// crap
					else if (--CriticalTicksLeft == 0)
						Won();
				}
				else if (CriticalTicksLeft != 0)
					if (info.ResetOnHoldLost)
						CriticalTicksLeft = info.TicksToHold; // Reset the time hold
			}

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

		public void Won()
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
