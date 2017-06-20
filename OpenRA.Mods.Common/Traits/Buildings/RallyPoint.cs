#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used to waypoint units after production or repair is finished.")]
	public class RallyPointInfo : ITraitInfo
	{
		public readonly string Image = "rallypoint";
		[SequenceReference("Image")] public readonly string FlagSequence = "flag";
		[SequenceReference("Image")] public readonly string CirclesSequence = "circles";

		[Desc("Custom indicator palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = "player";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = true;

		public readonly CVec Offset = new CVec(1, 3);

		public object Create(ActorInitializer init) { return new RallyPoint(init.Self, this); }
	}

	public class RallyPoint : IIssueOrder, IResolveOrder, ISync, INotifyOwnerChanged, INotifyCreated
	{
		[Sync] public CPos Location;
		public RallyPointInfo Info;
		public string PaletteName { get; private set; }

		// Keep track of rally pointed acceptor actors
		bool dirty = true;
		Actor cachedResult = null;

		public void ResetLocation(Actor self)
		{
			dirty = true;
			Location = self.Location + Info.Offset;
		}

		public RallyPoint(Actor self, RallyPointInfo info)
		{
			Info = info;
			ResetLocation(self);
			PaletteName = info.IsPlayerPalette ? info.Palette + self.Owner.InternalName : info.Palette;
		}

		public void Created(Actor self)
		{
			self.World.Add(new RallyPointIndicator(self, this, self.Info.TraitInfos<ExitInfo>().ToArray()));
			dirty = true;
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (Info.IsPlayerPalette)
				PaletteName = Info.Palette + newOwner.InternalName;

			ResetLocation(self);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new RallyPointOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "SetRallyPoint")
				return new Order(order.OrderID, self, false) { TargetLocation = self.World.Map.CellContaining(target.CenterPosition), SuppressVisualFeedback = true };

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SetRallyPoint")
			{
				Location = order.TargetLocation;
				dirty = true;
			}
		}

		Actor GetRallyAcceptor(Actor self, CPos location)
		{
			if (!dirty)
			{
				if (cachedResult == null)
					return null;

				if (!cachedResult.IsDead && !cachedResult.Disposed)
					return cachedResult;
			}

			var actors = self.World.ActorMap.GetActorsAt(Location).Where(
				a => a.TraitsImplementing<IAcceptsRallyPoint>().Count() > 0);

			dirty = false;

			if (!actors.Any())
			{
				cachedResult = null;
				return null;
			}

			// If we have multiple of them, let's just go for first.
			cachedResult = actors.First();
			return cachedResult;
		}

		// self isn't the rally point but the one that has rally point trait.
		// unit is the one that is to follow the rally point.
		public void QueueRallyOrder(Actor self, Actor unit)
		{
			if (unit.TraitOrDefault<IMove>() == null)
				throw new InvalidOperationException("How come rally point mover not have IMove trait? Actor: " + unit.ToString());

			var rallyAcceptor = GetRallyAcceptor(self, Location);

			if (rallyAcceptor == null)
			{
				unit.QueueActivity(new AttackMoveActivity(unit, unit.Trait<IMove>().MoveTo(Location, 1)));
				return;
			}

			var ars = rallyAcceptor.TraitsImplementing<IAcceptsRallyPoint>();
			if (ars.Count() > 1)
				throw new InvalidOperationException(
					"Actor {0} has multiple traits implementing IAcceptsRallyPoint!".F(rallyAcceptor.ToString()));

			var ar = ars.First();
			if (ar.IsAcceptableActor(unit, rallyAcceptor))
				unit.QueueActivity(ar.RallyActivities(unit, rallyAcceptor));
			else
				unit.QueueActivity(new AttackMoveActivity(unit, unit.Trait<IMove>().MoveTo(Location, 1)));
		}

		class RallyPointOrderTargeter : IOrderTargeter
		{
			public string OrderID { get { return "SetRallyPoint"; } }
			public int OrderPriority { get { return 0; } }
			public bool TargetOverridesSelection(TargetModifiers modifiers) { return true; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				if (target.Type != TargetType.Terrain)
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				if (self.World.Map.Contains(location))
				{
					cursor = "ability";
					return true;
				}

				return false;
			}

			public bool IsQueued { get { return false; } } // unused
		}
	}
}
