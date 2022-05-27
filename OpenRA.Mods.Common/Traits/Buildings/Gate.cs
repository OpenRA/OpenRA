#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Will open and be passable for actors that appear friendly when there are no enemies in range.")]
	public class GateInfo : PausableConditionalTraitInfo, ITemporaryBlockerInfo, IBlocksProjectilesInfo, Requires<BuildingInfo>
	{
		public readonly string OpeningSound = null;
		public readonly string ClosingSound = null;

		[Desc("Ticks until the gate closes.")]
		public readonly int CloseDelay = 150;

		[Desc("Ticks until the gate is considered open.")]
		public readonly int TransitionDelay = 33;

		[Desc("Blocks bullets scaled to open value.")]
		public readonly WDist BlocksProjectilesHeight = new WDist(640);

		[Desc("Determines what projectiles to block based on their allegiance to the gate owner.")]
		public readonly PlayerRelationship BlocksProjectilesValidRelationships = PlayerRelationship.Ally | PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		public override object Create(ActorInitializer init) { return new Gate(init, this); }
	}

	public class Gate : PausableConditionalTrait<GateInfo>, ITick, ITemporaryBlocker, IBlocksProjectiles,
		INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyBlockingMove
	{
		readonly Actor self;
		readonly Building building;
		IEnumerable<CPos> blockedPositions;
		public readonly IEnumerable<CPos> Footprint;

		public readonly int OpenPosition;

		[Sync]
		public int Position { get; private set; }

		int desiredPosition;
		int remainingOpenTime;

		public Gate(ActorInitializer init, GateInfo info)
			: base(info)
		{
			self = init.Self;
			Position = OpenPosition = Info.TransitionDelay;
			building = self.Trait<Building>();
			blockedPositions = building.Info.Tiles(self.Location);
			Footprint = blockedPositions;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (desiredPosition < Position)
			{
				// Gate was fully open
				if (Position == OpenPosition)
				{
					Game.Sound.Play(SoundType.World, Info.ClosingSound, self.CenterPosition);
					self.World.ActorMap.AddInfluence(self, building);
				}

				Position--;
			}
			else if (desiredPosition > Position)
			{
				// Gate was fully closed
				if (Position == 0)
					Game.Sound.Play(SoundType.World, Info.OpeningSound, self.CenterPosition);

				Position++;

				// Gate is now fully open
				if (Position == OpenPosition)
				{
					self.World.ActorMap.RemoveInfluence(self, building);
					remainingOpenTime = Info.CloseDelay;
				}
			}

			if (Position == OpenPosition)
			{
				if (IsBlocked())
					remainingOpenTime = Info.CloseDelay;
				else if (--remainingOpenTime <= 0)
					desiredPosition = 0;
			}
		}

		bool ITemporaryBlocker.IsBlocking(Actor self, CPos cell)
		{
			return Position != OpenPosition && blockedPositions.Contains(cell);
		}

		bool ITemporaryBlocker.CanRemoveBlockage(Actor self, Actor blocking)
		{
			return CanRemoveBlockage(self, blocking);
		}

		void INotifyBlockingMove.OnNotifyBlockingMove(Actor self, Actor blocking)
		{
			if (Position != OpenPosition && CanRemoveBlockage(self, blocking))
				desiredPosition = OpenPosition;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			blockedPositions = Footprint;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			blockedPositions = Enumerable.Empty<CPos>();
		}

		bool CanRemoveBlockage(Actor self, Actor blocking)
		{
			return !IsTraitDisabled && !IsTraitPaused && blocking.AppearsFriendlyTo(self);
		}

		bool IsBlocked()
		{
			return blockedPositions.Any(loc => self.World.ActorMap.GetActorsAt(loc).Any(a => a != self));
		}

		WDist IBlocksProjectiles.BlockingHeight => new WDist(Info.BlocksProjectilesHeight.Length * (OpenPosition - Position) / OpenPosition);

		PlayerRelationship IBlocksProjectiles.ValidRelationships => Info.BlocksProjectilesValidRelationships;
	}
}
