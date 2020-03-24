#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	[Desc("Tracks the position of the player's viewport.", " Attach this to the player actor.")]
	public class ViewportTrackerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new ViewportTracker(); }
	}

	public class ViewportTracker : ITick, IResolveOrder, IWorldLoaded
	{
		static WorldRenderer worldRenderer;

		public bool IsForceLocked { get; set; }

		bool hasLockedViewport;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			worldRenderer = wr;
		}

		void ITick.Tick(Actor self)
		{
			if (self.Owner.IsBot || !self.Owner.Playable || self.Owner != self.World.LocalPlayer || worldRenderer == null)
				return;

			var centerPosition = worldRenderer.Viewport.CenterPosition;
			var viewportSize = worldRenderer.Viewport.ViewportSize;
			var zoom = worldRenderer.Viewport.Zoom;

			self.World.IssueOrder(new Order("ViewportState", self, false)
			{
				IsImmediate = false,
				TargetString = "{0},{1},{2},{3},{4},{5},{6}".F(self.ActorID, centerPosition.X, centerPosition.Y, centerPosition.Z, viewportSize.X, viewportSize.Y, zoom)
			});
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "ViewportState")
				return;

			int x, y, z;
			var numbers = order.TargetString.Split(',');
			var playerId = int.Parse(numbers[0]);

			x = int.Parse(numbers[1]);
			y = int.Parse(numbers[2]);
			z = int.Parse(numbers[3]);
			var centerPosition = new WPos(x, y, z);

			x = int.Parse(numbers[4]);
			y = int.Parse(numbers[5]);
			var viewportSize = new int2(x, y);

			var zoom = float.Parse(numbers[6]);

			var player = self.World.Players.First(p => p.PlayerActor.ActorID == playerId);
			var screenCenterPosition = worldRenderer.ScreenPxPosition(centerPosition);
			player.ViewportTopLeft = screenCenterPosition - viewportSize / 2;
			player.ViewportBottomRight = screenCenterPosition + viewportSize / 2;

			// Only manipulate the viewport when observing a game or watching a replay.
			if (self.Owner == self.World.RenderPlayer && self.World.LocalPlayer == null)
			{
				if (IsForceLocked)
				{
					hasLockedViewport = true;
					worldRenderer.Viewport.IsMovementLocked = true;
					worldRenderer.Viewport.IsZoomingLocked = true;
					worldRenderer.Viewport.Center(centerPosition, true);
					worldRenderer.Viewport.SetZoom(zoom, true);
				}
				else
				{
					if (hasLockedViewport)
					{
						worldRenderer.Viewport.IsMovementLocked = false;
						worldRenderer.Viewport.IsZoomingLocked = false;
					}
				}
			}
			else
			{
				if (hasLockedViewport)
				{
					worldRenderer.Viewport.IsMovementLocked = false;
					worldRenderer.Viewport.IsZoomingLocked = false;
				}
			}
		}
	}
}
