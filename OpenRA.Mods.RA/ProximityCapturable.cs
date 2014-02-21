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
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ProximityCapturableInfo : ITraitInfo
	{
		public readonly bool Permanent = false;
		public readonly int Range = 5;
		public readonly bool MustBeClear = false;
		public readonly string[] CaptorTypes = { "Vehicle", "Tank", "Infantry" };

		public object Create(ActorInitializer init) { return new ProximityCapturable(init.self, this); }
	}

	public class ProximityCapturable : ITick
	{
		public readonly Player OriginalOwner;
		public bool Captured { get { return Self.Owner != OriginalOwner; } }

		public ProximityCapturableInfo Info;
		public Actor Self;

		public ProximityCapturable(Actor self, ProximityCapturableInfo info)
		{
			Info = info;
			Self = self;
			OriginalOwner = self.Owner;
		}

		public void Tick(Actor self)
		{
			if (Captured && Info.Permanent) return; // Permanent capture

			if (!Captured)
			{
				var captor = GetInRange(self);

				if (captor != null)
					if (!Info.MustBeClear || IsClear(self, captor.Owner, OriginalOwner))
						ChangeOwnership(self, captor);

				return;
			}

			// if the area must be clear, and there is more than 1 player nearby => return ownership to default
			if (Info.MustBeClear && !IsClear(self, self.Owner, OriginalOwner))
			{
				// Revert Ownership
				ChangeOwnership(self, OriginalOwner.PlayerActor);
				return;
			}

			// See if the 'temporary' owner still is in range
			if (!IsStillInRange(self))
			{
				// no.. So find a new one
				var captor = GetInRange(self);

				if (captor != null) // got one
				{
					ChangeOwnership(self, captor);
					return;
				}

				// Revert Ownership otherwise
				ChangeOwnership(self, OriginalOwner.PlayerActor);
			}
		}

		void ChangeOwnership(Actor self, Actor captor)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.Destroyed || captor.Destroyed) return;

				var previousOwner = self.Owner;

				// momentarily remove from world so the ownership queries don't get confused
				w.Remove(self);
				self.Owner = captor.Owner;
				w.Add(self);

				if (self.Owner == self.World.LocalPlayer)
					w.Add(new FlashTarget(self));

				foreach (var t in self.TraitsImplementing<INotifyCapture>())
					t.OnCapture(self, captor, previousOwner, self.Owner);
			});
		}

		bool CanBeCapturedBy(Actor a)
		{
			var pc = a.TraitOrDefault<ProximityCaptor>();
			return pc != null && pc.HasAny(Info.CaptorTypes);
		}

		IEnumerable<Actor> UnitsInRange()
		{
			return Self.World.FindActorsInCircle(Self.CenterPosition, WDist.FromCells(Info.Range))
				.Where(a => a.IsInWorld && a != Self && !a.Destroyed)
				.Where(a => !a.Owner.NonCombatant);
		}

		bool IsClear(Actor self, Player currentOwner, Player originalOwner)
		{
			return UnitsInRange().Where(a => a.Owner != originalOwner)
				.Where(a => a.Owner != currentOwner)
				.Where(a => CanBeCapturedBy(a))
				.All(a => WorldUtils.AreMutualAllies(a.Owner, currentOwner));
		}

		// TODO exclude other NeutralActor that arent permanent
		bool IsStillInRange(Actor self)
		{
			return UnitsInRange()
				.Where(a => a.Owner == self.Owner)
				.Where(a => CanBeCapturedBy(a))
				.Any();
		}

		IEnumerable<Actor> CaptorsInRange(Actor self)
		{
			return UnitsInRange()
				.Where(a => a.Owner != OriginalOwner)
				.Where(a => CanBeCapturedBy(a));
		}

		// TODO exclude other NeutralActor that arent permanent
		Actor GetInRange(Actor self)
		{
			return CaptorsInRange(self).ClosestTo(self);
		}

		int CountPlayersNear(Actor self, Player ignoreMe)
		{
			return CaptorsInRange(self).Select(a => a.Owner)
				.Distinct().Count(p => p != ignoreMe);
		}
	}
}
