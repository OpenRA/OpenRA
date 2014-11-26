#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public interface IPreventsTeleport { bool PreventsTeleport(Actor self); }

	public class Teleport : Activity
	{
		Actor teleporter;
		CPos destination;
		int? maximumDistance;
		bool killCargo;
		bool screenFlash;
		string sound;

		const int maxCellSearchRange = Map.MaxTilesInCircleRange;

		public Teleport(Actor teleporter, CPos destination, int? maximumDistance, bool killCargo, bool screenFlash, string sound)
		{
			if (maximumDistance > maxCellSearchRange)
				throw new InvalidOperationException("Teleport cannot be used with a maximum teleport distance greater than {0}.".F(maxCellSearchRange));

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
			if (pc != null && !pc.CanTeleport)
				return NextActivity;

			foreach (var condition in self.TraitsImplementing<IPreventsTeleport>())
				if (condition.PreventsTeleport(self))
					return NextActivity;

			var bestCell = ChooseBestDestinationCell(self, destination);
			if (bestCell == null)
				return NextActivity;

			destination = bestCell.Value;

			Sound.Play(sound, self.CenterPosition);
			Sound.Play(sound, self.World.Map.CenterOfCell(destination));

			self.Trait<IPositionable>().SetPosition(self, destination);
			self.Generation++;

			if (killCargo && self.HasTrait<Cargo>())
			{
				var cargo = self.Trait<Cargo>();
				if (teleporter != null)
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
			if (pc != null)
				pc.ResetChargeTime();

			// Trigger screen desaturate effect
			if (screenFlash)
				foreach (var a in self.World.ActorsWithTrait<ChronoshiftPaletteEffect>())
					a.Trait.Enable();

			if (teleporter != null && !teleporter.Destroyed)
			{
				var building = teleporter.TraitOrDefault<RenderBuilding>();
				if (building != null)
					building.PlayCustomAnim(teleporter, "active");
			}

			return NextActivity;
		}

		CPos? ChooseBestDestinationCell(Actor self, CPos destination)
		{
			var restrictTo = maximumDistance == null ? null : self.World.Map.FindTilesInCircle(self.Location, maximumDistance.Value);

			if (maximumDistance != null)
				destination = restrictTo.MinBy(x => (x - destination).LengthSquared);

			var pos = self.Trait<IPositionable>();
			if (pos.CanEnterCell(destination) && teleporter.Owner.Shroud.IsExplored(destination))
				return destination;

			var max = maximumDistance != null ? maximumDistance.Value : maxCellSearchRange;
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

	public class SimpleTeleport : Activity
	{
		CPos destination;

		public SimpleTeleport(CPos destination) { this.destination = destination; }

		public override Activity Tick(Actor self)
		{
			self.Trait<IPositionable>().SetPosition(self, destination);
			self.Generation++;
			return NextActivity;
		}
	}
}
