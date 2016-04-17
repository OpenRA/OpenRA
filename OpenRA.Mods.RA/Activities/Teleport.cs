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

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public interface IPreventsTeleport { bool PreventsTeleport(Actor self); }

	public class Teleport : Activity
	{
		readonly Actor teleporter;
		readonly int? maximumDistance;
		CPos destination;
		bool killCargo;
		bool screenFlash;
		string sound;

		public Teleport(Actor teleporter, CPos destination, int? maximumDistance, bool killCargo, bool screenFlash, string sound)
		{
			var max = teleporter.World.Map.Grid.MaximumTileSearchRange;
			if (maximumDistance > max)
				throw new InvalidOperationException("Teleport distance cannot exceed the value of MaximumTileSearchRange ({0}).".F(max));

			this.teleporter = teleporter;
			this.destination = destination;
			this.maximumDistance = maximumDistance;
			this.killCargo = killCargo;
			this.screenFlash = screenFlash;
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

			var bestCell = ChooseBestDestinationCell(self, destination);
			if (bestCell == null)
				return NextActivity;

			destination = bestCell.Value;

			Game.Sound.Play(sound, self.CenterPosition);
			Game.Sound.Play(sound, self.World.Map.CenterOfCell(destination));

			self.Trait<IPositionable>().SetPosition(self, destination);
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
				if (building != null && building.DefaultAnimation.HasSequence("active"))
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

			var max = maximumDistance != null ? maximumDistance.Value : teleporter.World.Map.Grid.MaximumTileSearchRange;
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
