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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	/// <summary>
	/// Attach to players only kthx :)
	/// </summary>
	public class StrategicVictoryConditionsInfo : ITraitInfo
	{
		public readonly int TicksToHold = 25 * 60 * 5; // ~5 minutes
		public readonly bool ResetOnHoldLost = true;
		public readonly float RatioRequired = 0.5f; // 50% required of all koth locations
		public readonly float CriticalRatioRequired = 1f; // if someone owns 100% of all critical locations	
		public readonly bool SplitHolds = true; // disallow or allow the 'holdsrequired' to include critical locations

		public object Create(ActorInitializer init) { return new StrategicVictoryConditions(init.self, this); }	
	}

	public class StrategicVictoryConditions : ITick
	{
		[Sync] public Actor Self;
		public StrategicVictoryConditionsInfo Info;

		[Sync] public int TicksToHold;
		[Sync] public bool ResetOnHoldLost;
		public float RatioRequired;
		public float CriticalRatioRequired;
		[Sync] public bool SplitHolds;
		[Sync] public int TicksLeft = 0;
		[Sync] public int CriticalTicksLeft = 0;

		public StrategicVictoryConditions(Actor self, StrategicVictoryConditionsInfo info)
		{
			Self = self;
			Info = info;

			TicksToHold = info.TicksToHold;
			ResetOnHoldLost = info.ResetOnHoldLost;
			RatioRequired = info.RatioRequired;
			CriticalRatioRequired = info.CriticalRatioRequired;
			SplitHolds = info.SplitHolds;
		}

		/// <summary>
		/// Includes your allies as well
		/// </summary>
		public int Owned
		{
			get { return (SplitHolds) ? CountOwnedPoints(false) : CountOwnedPoints(false) + OwnedCritical; }
		}

		/// <summary>
		/// Includes your allies as well
		/// </summary>
		public int OwnedCritical
		{
			get { return CountOwnedPoints(true); }
		}

		public int Total
		{
			get
			{
				return (SplitHolds) ? Self.World.Actors.Where(a => !a.Destroyed && a.HasTrait<StrategicPoint>() && a.TraitOrDefault<StrategicPoint>().Critical == false).Count() : Self.World.Actors.Where(a => a.HasTrait<StrategicPoint>()).Count();
			}
		}

		public int TotalCritical
		{
			get
			{
				return Self.World.Actors.Where(a => !a.Destroyed && a.HasTrait<StrategicPoint>() && a.TraitOrDefault<StrategicPoint>().Critical).Count();
			}
		}

		public int CountOwnedPoints(bool critical)
		{
			int total = 0;

			foreach (var p in Self.World.players.Select(k => k.Value))
			{
				if (p == Self.Owner || (p.Stances[Self.Owner] == Stance.Ally && Self.Owner.Stances[p] == Stance.Ally))
				{
					total += Self.World.Queries.OwnedBy[p].Where(a => a.HasTrait<StrategicPoint>() && a.TraitOrDefault<StrategicPoint>().Critical == critical).Count();
				}
			}
			return total;
		}

		public bool HoldingCritical
		{
			get
			{
				var criticalOwned = 1f / TotalCritical * OwnedCritical;

				return (criticalOwned >= CriticalRatioRequired);
			}
		}

		public bool Holding
		{
			get
			{
				var owned = 1f / Total * Owned;

				return (owned >= RatioRequired);
			}
		}

		public void Tick(Actor self)
		{
			if (self.Owner.WinState != WinState.Undefined || self.Owner.NonCombatant) return;

			var cvc = self.TraitOrDefault<ConquestVictoryConditions>();
			if (cvc == null)
				return; // Cannot work without ConquestVictoryConditions

			// See if any of the conditions are met to increase the count
			if (TotalCritical > 0)
			{
				if (HoldingCritical)
				{
					// Hah! We met ths critical owned condition
					if (CriticalTicksLeft == 0)
					{
						// First time
						CriticalTicksLeft = TicksToHold;
					}
					else
					{
						// nth time
						if (--CriticalTicksLeft == 0)
						{
							// Player & allies have won!
							Won();
						}
					}
				}
				else if (CriticalTicksLeft != 0)
				{
					// we lost the hold :/
					if (ResetOnHoldLost)
					{
						CriticalTicksLeft = TicksToHold; // Reset the time hold
					}
				}
			}

			// See if any of the conditions are met to increase the count
			if (Total > 0)
			{
				if (Holding)
				{
					// Hah! We met ths critical owned condition
					if (TicksLeft == 0)
					{
						// First time
						TicksLeft = TicksToHold;
					}
					else
					{
						// nth time
						if (--TicksLeft == 0)
						{
							// Player & allies have won!
							Won();
						}
					}
				}
				else if (TicksLeft != 0)
				{
					// we lost the hold :/
					if (ResetOnHoldLost)
					{
						TicksLeft = TicksToHold; // Reset the time hold
					}
				}
			}
		}

		public void Won()
		{
			// Player has won
			foreach (var p in Self.World.players.Select(k => k.Value))
			{
				var cvc = p.PlayerActor.TraitOrDefault<ConquestVictoryConditions>();

				if ((p.WinState == WinState.Undefined) && (p == Self.Owner || (p.Stances[Self.Owner] == Stance.Ally && Self.Owner.Stances[p] == Stance.Ally)))
				{
					cvc.Win(p.PlayerActor);
				}
				else if (p.WinState == WinState.Undefined)
				{
					cvc.Surrender(p.PlayerActor);
				}
			}
		}
	}
}
