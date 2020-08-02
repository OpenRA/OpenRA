#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	// Assumes you have Minelayer on that unit
	public class LayMines : Activity
	{
		readonly Minelayer minelayer;
		readonly AmmoPool[] ammoPools;
		readonly IMove movement;
		readonly IMoveInfo moveInfo;
		readonly RearmableInfo rearmableInfo;

		List<CPos> minefield;
		bool returnToBase;
		Actor rearmTarget;

		public LayMines(Actor self, List<CPos> minefield = null)
		{
			minelayer = self.Trait<Minelayer>();
			ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
			movement = self.Trait<IMove>();
			moveInfo = self.Info.TraitInfo<IMoveInfo>();
			rearmableInfo = self.Info.TraitInfoOrDefault<RearmableInfo>();
			this.minefield = minefield;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (minefield == null)
				minefield = new List<CPos> { self.Location };
		}

		CPos? NextValidCell(Actor self)
		{
			if (minefield != null)
				foreach (var c in minefield)
					if (CanLayMine(self, c))
						return c;

			return null;
		}

		public override bool Tick(Actor self)
		{
			returnToBase = false;

			if (IsCanceling)
				return true;

			if ((minefield == null || minefield.Contains(self.Location)) && CanLayMine(self, self.Location))
			{
				if (rearmableInfo != null && ammoPools.Any(p => p.Info.Name == minelayer.Info.AmmoPoolName && !p.HasAmmo))
				{
					// Rearm (and possibly repair) at rearm building, then back out here to refill the minefield some more
					rearmTarget = self.World.Actors.Where(a => self.Owner.RelationshipWith(a.Owner) == PlayerRelationship.Ally && rearmableInfo.RearmActors.Contains(a.Info.Name))
						.ClosestTo(self);

					if (rearmTarget == null)
						return true;

					// Add a CloseEnough range of 512 to the Rearm/Repair activities in order to ensure that we're at the host actor
					QueueChild(new MoveAdjacentTo(self, Target.FromActor(rearmTarget)));
					QueueChild(movement.MoveTo(self.World.Map.CellContaining(rearmTarget.CenterPosition), ignoreActor: rearmTarget));
					QueueChild(new Resupply(self, rearmTarget, new WDist(512)));
					returnToBase = true;
					return false;
				}

				LayMine(self);
				QueueChild(new Wait(20)); // A little wait after placing each mine, for show
				minefield.Remove(self.Location);
				return false;
			}

			var nextCell = NextValidCell(self);
			if (nextCell != null)
			{
				QueueChild(movement.MoveTo(nextCell.Value, 0));
				return false;
			}

			// TODO: Return somewhere likely to be safe (near rearm building) so we're not sitting out in the minefield.
			return true;
		}

		public void CleanMineField(Actor self)
		{
			// Remove cells that have already been mined
			// or that are revealed to be unmineable.
			if (minefield != null)
			{
				var positionable = (IPositionable)movement;
				var mobile = positionable as Mobile;
				minefield.RemoveAll(c => self.World.ActorMap.GetActorsAt(c)
					.Any(a => a.Info.Name == minelayer.Info.Mine.ToLowerInvariant() && a.CanBeViewedByPlayer(self.Owner)) ||
						((!positionable.CanEnterCell(c, null, BlockedByActor.Immovable) || (mobile != null && !mobile.CanStayInCell(c)))
						&& self.Owner.Shroud.IsVisible(c)));
			}
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (returnToBase)
				yield return new TargetLineNode(Target.FromActor(rearmTarget), moveInfo.GetTargetLineColor());

			if (minefield == null || minefield.Count == 0)
				yield break;

			var nextCell = NextValidCell(self);
			if (nextCell != null)
				yield return new TargetLineNode(Target.FromCell(self.World, nextCell.Value), minelayer.Info.TargetLineColor);

			foreach (var c in minefield)
				yield return new TargetLineNode(Target.FromCell(self.World, c), minelayer.Info.TargetLineColor, tile: minelayer.Tile);
		}

		static bool CanLayMine(Actor self, CPos p)
		{
			// If there is no unit (other than me) here, we can place a mine here
			return self.World.ActorMap.GetActorsAt(p).All(a => a == self);
		}

		void LayMine(Actor self)
		{
			if (ammoPools != null)
			{
				var pool = ammoPools.FirstOrDefault(x => x.Info.Name == minelayer.Info.AmmoPoolName);
				if (pool == null)
					return;

				pool.TakeAmmo(self, 1);
			}

			self.World.AddFrameEndTask(w => w.CreateActor(minelayer.Info.Mine, new TypeDictionary
			{
				new LocationInit(self.Location),
				new OwnerInit(self.Owner),
			}));
		}
	}
}
