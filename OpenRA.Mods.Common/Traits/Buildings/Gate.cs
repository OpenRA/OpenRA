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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Will open and be passable for actors that appear friendly when there are no enemies in range.")]
	public class GateInfo : UpgradableTraitInfo, ITraitInfo, Requires<RenderSimpleInfo>
	{
		public readonly string OpenSequence = "open";
		public readonly string ClosingSequence = "closing";
		public readonly string ClosedSequence = "closed";

		public readonly string OpeningSound = null;
		public readonly string ClosingSound = null;

		[Desc("`-` means it blocks when closed.")]
		public readonly string Footprint = "-";
		public readonly CVec Dimensions = new CVec(1, 1);

		[Desc("How far to search for allied and enemy units.")]
		public readonly WRange ScanRange = WRange.FromCells(3);

		public object Create(ActorInitializer init) { return new Gate(init, this); }
	}

	public class Gate : ITick, IOccupySpace, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly GateInfo info;
		readonly Actor self;
		readonly RenderSimple renderSimple;
		readonly CPos topLeft;

		WPos center;
		bool cachedOpen;
		Pair<CPos, SubCell>[] blockedCells;

		public Gate(ActorInitializer init, GateInfo info)
		{
			this.info = info;
			this.self = init.Self;

			renderSimple = self.Trait<RenderSimple>();

			topLeft = init.Get<LocationInit, CPos>();
			center = init.World.Map.CenterOfCell(topLeft) +
				((init.World.Map.CenterOfCell(CPos.Zero + new CVec(info.Dimensions.X, info.Dimensions.Y))
					- init.World.Map.CenterOfCell(new CPos(1, 1))) / 2);

			blockedCells = BlockedCells().Select(c => Pair.New(c, SubCell.FullCell)).ToArray();
		}

		public CPos TopLeft { get { return topLeft; } }
		public WPos CenterPosition { get { return center; } }
		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { return cachedOpen ? new Pair<CPos, SubCell>[0] : blockedCells; }

		public IEnumerable<CPos> BlockedCells()
		{
			var footprint = info.Footprint.Where(x => !char.IsWhiteSpace(x)).ToArray();
			foreach (var tile in FootprintUtils.TilesWhere(self.Info.Name, (CVec)info.Dimensions, footprint, a => a == '-'))
				yield return tile + topLeft;
		}

		public void AddedToWorld(Actor self)
		{
			self.World.ActorMap.AddInfluence(self, this);
			self.World.ActorMap.AddPosition(self, this);
			self.World.ScreenMap.Add(self);
		}

		public void RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			self.World.ActorMap.RemovePosition(self, this);
			self.World.ScreenMap.Remove(self);
		}

		public void Tick(Actor self)
		{
			if (self.IsDisabled())
				return;

			var open = UnitsInRange().Where(a => !a.Owner.NonCombatant && a.HasTrait<Mobile>()).All(a => a.AppearsFriendlyTo(self));
			if (open != cachedOpen)
			{
				self.World.ActorMap.RemoveInfluence(self, this);

				cachedOpen = open;

				Action after = () => self.World.ActorMap.AddInfluence(self, this);

				if (cachedOpen)
					Open(after);
				else
					Close(after);
			}
		}

		IEnumerable<Actor> UnitsInRange()
		{
			return self.World.FindActorsInCircle(self.CenterPosition, info.ScanRange)
				.Where(a => a.IsInWorld && a != self && !a.Destroyed);
		}

		void Open(Action after)
		{
			Sound.Play(info.OpeningSound, self.CenterPosition);

			renderSimple.DefaultAnimation.PlayBackwardsThen(info.ClosingSequence,
				() => { renderSimple.DefaultAnimation.PlayRepeating(info.OpenSequence); after(); });
		}

		void Close(Action after)
		{
			Sound.Play(info.ClosingSound, self.CenterPosition);

			renderSimple.DefaultAnimation.PlayThen(info.ClosingSequence,
				() => { renderSimple.DefaultAnimation.PlayRepeating(info.ClosedSequence); after(); });
		}
	}
}
