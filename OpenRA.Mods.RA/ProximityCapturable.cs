using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ProximityCapturableInfo : ITraitInfo
	{
		public readonly bool Permanent = false;
		public readonly int Range = 5;
		public readonly bool MustBeClear = false;

		public object Create(ActorInitializer init) { return new ProximityCapturable(init.self, this); }
	}

	public class ProximityCapturable : ITick
	{
		[Sync]
		public Player Owner;

		[Sync]
		public readonly Player OriginalOwner;

		public ProximityCapturableInfo Info;

		[Sync]
		public int Range;

		[Sync]
		public bool Permanent;

		[Sync]
		public bool Captured = false;

		[Sync]
		public bool MustBeClear = false;

		public ProximityCapturable(Actor self, ProximityCapturableInfo info)
		{
			Owner = self.Owner;
			OriginalOwner = Owner;
			Info = info;
			Range = info.Range;
			Permanent = info.Permanent;
			MustBeClear = info.MustBeClear;
		}

		public Player GetCurrentOwner()
		{
			if (!Captured) return OriginalOwner;

			return Owner ?? OriginalOwner;
		}

		public void Tick(Actor self)
		{
			if (Captured && Permanent) return; // Permanent capture

			var playersNear = CountPlayersNear(self, OriginalOwner, Range);

			if (!Captured)
			{
				if (MustBeClear && playersNear != 1) return;

				var captor = GetInRange(self, OriginalOwner, Range);

				if (captor != null) ChangeOwnership(self, captor, OriginalOwner);

				return;
			}

			// if the area must be clear, and there is more than 1 player nearby => return ownership to default
			if (MustBeClear && playersNear != 1)
			{
				// Revert Ownership
				ChangeOwnership(self, GetCurrentOwner(), OriginalOwner);
			}
			// See if the 'temporary' owner still is in range
			else if (!IsStillInRange(self, self.Owner, Range))
			{
				// no.. So find a new one
				var captor = GetInRange(self, OriginalOwner, Range);

				if (captor != null) // got one
				{
					ChangeOwnership(self, captor, GetCurrentOwner());
					return;
				}

				// Revert Ownership otherwise
				ChangeOwnership(self, GetCurrentOwner(), OriginalOwner);
			}
		}

		private void ChangeOwnership(Actor self, Player previousOwner, Player originalOwner)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.Destroyed) return;

				// momentarily remove from world so the ownership queries don't get confused
				w.Remove(self);
				self.Owner = originalOwner;
				w.Add(self);

				if (self.Owner == self.World.LocalPlayer)
					w.Add(new FlashTarget(self));

				Captured = false;
				Owner = null;

				foreach (var t in self.TraitsImplementing<INotifyCapture>())
					t.OnCapture(self, self, previousOwner, self.Owner);
			});
		}

		private void ChangeOwnership(Actor self, Actor captor, Player previousOwner)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.Destroyed || (captor.Destroyed || !captor.IsInWorld)) return;

				// momentarily remove from world so the ownership queries don't get confused
				w.Remove(self);
				self.Owner = captor.Owner;
				w.Add(self);

				if (self.Owner == self.World.LocalPlayer)
					w.Add(new FlashTarget(self));

				Captured = true;
				Owner = captor.Owner;

				foreach (var t in self.TraitsImplementing<INotifyCapture>())
					t.OnCapture(self, captor, previousOwner, self.Owner);
			});
		}

		// TODO exclude other NeutralActor that arent permanent
		public static bool IsStillInRange(Actor self, Player currentOwner, int range)
		{
			var unitsInRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return unitsInRange
				.Where(a => a.Owner != null && a.Owner == currentOwner && !a.Destroyed && a.IsInWorld && a != self)
				.Any();
		}

		// TODO exclude other NeutralActor that arent permanent
		public static Actor GetInRange(Actor self, Player originalOwner, int range)
		{
			var unitsInRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return unitsInRange
				.Where(a => a.Owner != null && a.Owner != originalOwner && !a.Destroyed && a.IsInWorld && a != self)
				.Where(a => !a.Owner.PlayerRef.OwnsWorld)
				.Where(a => !a.Owner.PlayerRef.NonCombatant)
				.OrderBy(a => (a.CenterLocation - self.CenterLocation).LengthSquared)
				.FirstOrDefault();
		}

		public static int CountPlayersNear(Actor self, Player ignoreMe, int range)
		{
			var unitsInRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return unitsInRange
				.Where(a => a.Owner != null && a.Owner != ignoreMe && !a.Destroyed && a.IsInWorld && a != self)
				.Where(a => !a.Owner.PlayerRef.OwnsWorld)
				.Where(a => !a.Owner.PlayerRef.NonCombatant)
				.Select(a => a.Owner)
				.Distinct()
				.Count();
		}
	}
}
