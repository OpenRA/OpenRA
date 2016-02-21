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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Will open and be passable for actors that appear friendly when there are no enemies in range.")]
	public class GateInfo : BuildingInfo
	{
		public readonly string OpeningSound = null;
		public readonly string ClosingSound = null;

		[Desc("Ticks until the gate closes.")]
		public readonly int CloseDelay = 150;

		[Desc("Ticks until the gate is considered open.")]
		public readonly int TransitionDelay = 33;

		[Desc("Blocks bullets scaled to open value.")]
		public readonly int BlocksProjectilesHeight = 640;

		public override object Create(ActorInitializer init) { return new Gate(init, this); }
	}

	public class Gate : Building, ITick, ITemporaryBlocker, IBlocksProjectiles, INotifyBlockingMove, ISync
	{
		readonly GateInfo info;
		readonly Actor self;
		IEnumerable<CPos> blockedPositions;

		public readonly int OpenPosition;
		[Sync] public int Position { get; private set; }
		int desiredPosition;
		int remainingOpenTime;

		public Gate(ActorInitializer init, GateInfo info)
			: base(init, info)
		{
			this.info = info;
			self = init.Self;
			OpenPosition = info.TransitionDelay;
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDisabled() || Locked || !BuildComplete)
				return;

			if (desiredPosition < Position)
			{
				// Gate was fully open
				if (Position == OpenPosition)
				{
					Game.Sound.Play(info.ClosingSound, self.CenterPosition);
					self.World.ActorMap.AddInfluence(self, this);
				}

				Position--;
			}
			else if (desiredPosition > Position)
			{
				// Gate was fully closed
				if (Position == 0)
					Game.Sound.Play(info.OpeningSound, self.CenterPosition);

				Position++;

				// Gate is now fully open
				if (Position == OpenPosition)
				{
					self.World.ActorMap.RemoveInfluence(self, this);
					remainingOpenTime = info.CloseDelay;
				}
			}

			if (Position == OpenPosition)
			{
				if (IsBlocked())
					remainingOpenTime = info.CloseDelay;
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

		bool CanRemoveBlockage(Actor self, Actor blocking)
		{
			return !self.IsDisabled() && BuildComplete && blocking.AppearsFriendlyTo(self);
		}

		public override void AddedToWorld(Actor self)
		{
			base.AddedToWorld(self);
			blockedPositions = FootprintUtils.Tiles(self);
		}

		bool IsBlocked()
		{
			return blockedPositions.Any(loc => self.World.ActorMap.GetActorsAt(loc).Any(a => a != self));
		}

		WDist IBlocksProjectiles.BlockingHeight
		{
			get
			{
				return new WDist(info.BlocksProjectilesHeight * (OpenPosition - Position) / OpenPosition);
			}
		}
	}
}
