#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public interface IPreventsTeleport { bool PreventsTeleport(Actor self); }

	public class Teleport : Activity
	{
		const int MaxCellSearchRange = Map.MaxTilesInCircleRange;
		readonly Actor teleporter;
		readonly int? maximumDistance;
		CPos destination;
		bool killCargo;
		bool screenFlash;
		bool force;
		string sound;

		public Teleport(Actor teleporter, CPos destination, int? maximumDistance, bool killCargo, bool screenFlash, string sound, bool force = false)
		{
			if (maximumDistance > MaxCellSearchRange)
				throw new InvalidOperationException("Teleport cannot be used with a maximum teleport distance greater than {0}.".F(MaxCellSearchRange));

			this.teleporter = teleporter;
			this.destination = destination;
			this.maximumDistance = maximumDistance;
			this.killCargo = killCargo;
			this.screenFlash = screenFlash;
			this.force = force;
			this.sound = sound;
		}

		public override Activity Tick(Actor self)
		{
			var pc = self.TraitOrDefault<PortableChrono>();
			if (teleporter == self && pc != null && !pc.CanTeleport)
				return NextActivity;

			foreach (var condition in self.TraitsImplementing<IPreventsTeleport>())
				if (condition.PreventsTeleport(self))
					return NextActivity;

			if (!force)
			{
				var bestCell = ChooseBestDestinationCell(self, destination);
				if (bestCell == null)
					return NextActivity;

				destination = bestCell.Value;
			}

			Game.Sound.Play(sound, self.CenterPosition);
			Game.Sound.Play(sound, self.World.Map.CenterOfCell(destination));

			var pos = self.Trait<IPositionable>();
			if (pos.CanEnterCell(destination))
				force = false;

			pos.SetPosition(self, destination);
			if (force)
			{
				var subcell = pos.OccupiedCells().First(p => p.First == destination).Second;
				var health = self.TraitOrDefault<Health>();
				var givenDamage = health != null ? (health.HP + health.MaxHP) / 2 : 0;

				foreach (var other in self.World.ActorMap.GetActorsAt(destination))
				{
					if (other == self)
						continue;

					var otherOccupation = other.TraitOrDefault<IOccupySpace>();
					if (otherOccupation == null || !otherOccupation.OccupiedCells().Any(p => p.First == destination))
						continue;

					if (subcell != SubCell.FullCell)
					{
						var otherSubcell = otherOccupation.OccupiedCells().First(p => p.First == destination).Second;
						if (otherSubcell != SubCell.FullCell && otherSubcell != subcell)
							continue;
					}

					var otherHealth = other.TraitOrDefault<Health>();
					if (otherHealth == null)
						continue;

					if (health != null)
					{
						var maxHealthRadius = health.Info.Radius;
						if (otherHealth.Info.Radius > maxHealthRadius)
							maxHealthRadius = otherHealth.Info.Radius;

						if ((other.CenterPosition - self.CenterPosition).LengthSquared > maxHealthRadius.LengthSquared)
							continue;

						var otherGivenDamage = (otherHealth.HP + otherHealth.MaxHP) / 2;
						otherHealth.InflictDamage(other, self, givenDamage, null, true);
						health.InflictDamage(self, other, otherGivenDamage, null, true);
					}
					else
					{
						var healthRadius = otherHealth.Info.Radius;
						if ((other.CenterPosition - self.CenterPosition).LengthSquared > healthRadius.LengthSquared)
							continue;

						otherHealth.InflictDamage(other, self, otherHealth.HP, null, true);
					}
				}
			}

			self.Generation++;

			if (killCargo)
			{
				var cargo = self.TraitOrDefault<Cargo>();
				if (cargo != null && teleporter != null)
				{
					while (!cargo.IsEmpty(self))
					{
						var a = cargo.Unload(self);

						// Kill all the units that are unloaded into the void
						// Kill() handles kill and death statistics
						a.Kill(teleporter);
					}
				}
			}

			// Consume teleport charges if this wasn't triggered via chronosphere
			if (teleporter == self && pc != null)
				pc.ResetChargeTime();

			// Trigger screen desaturate effect
			if (screenFlash)
				foreach (var a in self.World.ActorsWithTrait<ChronoshiftPaletteEffect>())
					a.Trait.Enable();

			if (teleporter != null && self != teleporter && !teleporter.Disposed)
			{
				var building = teleporter.TraitOrDefault<WithSpriteBody>();
				if (building != null)
					building.PlayCustomAnimation(teleporter, "active");
			}

			return NextActivity;
		}

		CPos? ChooseBestDestinationCell(Actor self, CPos destination)
		{
			if (teleporter == null)
				return null;

			var restrictTo = maximumDistance == null ? null : self.World.Map.FindTilesInCircle(self.Location, maximumDistance.Value);

			if (maximumDistance != null)
				destination = restrictTo.MinBy(x => (x - destination).LengthSquared);

			var pos = self.Trait<IPositionable>();
			if (pos.CanEnterCell(destination) && teleporter.Owner.Shroud.IsExplored(destination))
				return destination;

			var max = maximumDistance != null ? maximumDistance.Value : MaxCellSearchRange;
			foreach (var tile in self.World.Map.FindTilesInCircle(destination, max))
			{
				if (teleporter.Owner.Shroud.IsExplored(tile)
					&& (restrictTo == null || (restrictTo != null && restrictTo.Contains(tile)))
					&& pos.CanEnterCell(tile))
					return tile;
			}

			return null;
		}
	}
}
