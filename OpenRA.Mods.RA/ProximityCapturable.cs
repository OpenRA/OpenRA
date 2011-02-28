#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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
		public readonly string[] CaptorTypes = {"Vehicle", "Tank", "Infantry"};

		public object Create(ActorInitializer init) { return new ProximityCapturable(init.self, this); }
	}

	public class ProximityCapturable : ITick, ISync
	{
		[Sync]
		public Player Owner { get { return Captured ? Self.Owner : OriginalOwner; } }

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

		public string[] CaptorTypes = {};

		public Actor Self;

		public ProximityCapturable(Actor self, ProximityCapturableInfo info)
		{
			Info = info;
			Range = info.Range;
			Permanent = info.Permanent;
			MustBeClear = info.MustBeClear;
			Self = self;
			OriginalOwner = self.Owner;
			CaptorTypes = info.CaptorTypes;
		}

		public void Tick(Actor self)
		{
			if (Captured && Permanent) return; // Permanent capture

			//var playersNear = CountPlayersNear(self, OriginalOwner, Range);

			if (!Captured)
			{
				var captor = GetInRange(self, OriginalOwner, Range, CaptorTypes);

				if (captor != null)
				{
					if (MustBeClear && !IsClear(self, captor.Owner, Range, OriginalOwner, CaptorTypes)) return;

					ChangeOwnership(self, captor, OriginalOwner);
				}

				return;
			}

			// if the area must be clear, and there is more than 1 player nearby => return ownership to default
			if (MustBeClear && !IsClear(self, Owner, Range, OriginalOwner, CaptorTypes))
			{
				// Revert Ownership
				ChangeOwnership(self, Owner, OriginalOwner);
				return;
			}

			// See if the 'temporary' owner still is in range
			if (!IsStillInRange(self, self.Owner, Range, CaptorTypes))
			{
				// no.. So find a new one
				var captor = GetInRange(self, OriginalOwner, Range, CaptorTypes);

				if (captor != null) // got one
				{
					ChangeOwnership(self, captor, Owner);
					return;
				}

				// Revert Ownership otherwise
				ChangeOwnership(self, Owner, OriginalOwner);
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

				foreach (var t in self.TraitsImplementing<INotifyCapture>())
					t.OnCapture(self, captor, previousOwner, self.Owner);
			});
		}

		static bool AreMutualAllies(Player a, Player b)
		{
			return a.Stances[b] == Stance.Ally &&
				b.Stances[a] == Stance.Ally;
		}

		public static bool IsClear(Actor self, Player currentOwner, int range, Player originalOwner, string[] actorTypes)
		{
			var unitsInRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return unitsInRange.Where(a => !a.Destroyed && a.IsInWorld && a != self && !a.Owner.NonCombatant && a.Owner != originalOwner)
				.Where(a => a.Owner != currentOwner)
				.Where(a => actorTypes.Length == 0 || (a.HasTrait<ProximityCaptor>() && a.Trait<ProximityCaptor>().HasAny(actorTypes)))
					.All(a => AreMutualAllies(a.Owner, currentOwner));
		}

		// TODO exclude other NeutralActor that arent permanent
		public static bool IsStillInRange(Actor self, Player currentOwner, int range, string[] actorTypes)
		{
			var unitsInRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return unitsInRange
				.Where(a => a.Owner == currentOwner && !a.Destroyed && a.IsInWorld && a != self)
				.Where(a => actorTypes.Length == 0 || (a.HasTrait<ProximityCaptor>() && a.Trait<ProximityCaptor>().HasAny(actorTypes)))
				.Any();
		}

		// TODO exclude other NeutralActor that arent permanent
		public static Actor GetInRange(Actor self, Player originalOwner, int range, string[] actorTypes)
		{
			var unitsInRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return unitsInRange
				.Where(a => a.Owner != originalOwner && !a.Destroyed && a.IsInWorld && a != self)
				.Where(a => !a.Owner.PlayerRef.OwnsWorld)
				.Where(a => !a.Owner.PlayerRef.NonCombatant)
				.Where(a => actorTypes.Length == 0 || (a.HasTrait<ProximityCaptor>() && a.Trait<ProximityCaptor>().HasAny(actorTypes)))
				.OrderBy(a => (a.CenterLocation - self.CenterLocation).LengthSquared)
				.FirstOrDefault();
		}

		public static int CountPlayersNear(Actor self, Player ignoreMe, int range, string[] actorTypes)
		{
			var unitsInRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return unitsInRange
				.Where(a => a.Owner != ignoreMe && !a.Destroyed && a.IsInWorld && a != self)
				.Where(a => !a.Owner.PlayerRef.OwnsWorld)
				.Where(a => !a.Owner.PlayerRef.NonCombatant)
				.Where(a =>actorTypes.Length == 0 || (  a.HasTrait<ProximityCaptor>() && a.Trait<ProximityCaptor>().HasAny(actorTypes)))
				.Select(a => a.Owner)
				.Distinct()
				.Count();
		}
	}
}
