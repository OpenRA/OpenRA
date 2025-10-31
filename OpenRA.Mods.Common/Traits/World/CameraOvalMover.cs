#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Moves the camera in an oval pattern around its starting position.")]
	sealed class CameraOvalMoverInfo : TraitInfo
	{
		[Desc("Starting angle in degrees.")]
		public int StartAngle = 45;

		[Desc("How fast the camera moves around the oval.")]
		public float DegreesPerSecond = 3f;

		[Desc("The X and Y radius of the oval path. Z is unused.")]
		public WVec OvalRadius = new(19200, 20480, 0);

		public override object Create(ActorInitializer init) { return new CameraOvalMover(this); }
	}

	sealed class CameraOvalMover : IWorldLoaded
	{
		readonly CameraOvalMoverInfo info;
		public CameraOvalMover(CameraOvalMoverInfo info)
		{
			this.info = info;
		}

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			var vo = wr.Viewport.CenterPosition;
			var viewportOrigin = new float2(vo.X, vo.Y);

			float angle = info.StartAngle;
			long lastTime = 0;
			wr.Viewport.ViewportCenterProvider = () =>
			{
				var currentTime = Game.RunTime;
				var dt = currentTime - lastTime;
				lastTime = currentTime;

				// Prevent large jumps when the game is paused or the framerate is very low.
				const float MaxStep = 0.25f;
				angle += Math.Min(dt / 1000.0f * info.DegreesPerSecond, MaxStep);

				var rad = angle * (Math.PI / 180);
				var offset = new float2((float)(info.OvalRadius.X * Math.Sin(rad)), (float)(info.OvalRadius.Y * Math.Cos(rad)));
				return viewportOrigin + offset;
			};
		}
	}
}
