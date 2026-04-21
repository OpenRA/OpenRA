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
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.Effects
{
	sealed class ConyardChronoVortex : IEffect, ISpatiallyPartitionable
	{
		static readonly Size Size = new(64, 64);
		static readonly WVec Offset = new(171, 0, 0);
		readonly ChronoVortexRenderer renderer;
		readonly WPos center;
		readonly Action onCompletion;
		WPos pos;
		WAngle angle;
		int loops = 3;
		int frame;

		public ConyardChronoVortex(Actor launcher, Action onCompletion)
		{
			this.onCompletion = onCompletion;
			renderer = launcher.World.WorldActor.Trait<ChronoVortexRenderer>();
			center = launcher.CenterPosition;
			pos = center + Offset.Rotate(WRot.FromYaw(angle));
			launcher.World.ScreenMap.Add(this, pos, Size);
		}

		public void Tick(World world)
		{
			// First 16 frames are the vortex opening
			// Next 16 frames are loopable
			// Final 16 frames are the vortex closing
			if (++frame == 32 && --loops > 0)
				frame = 16;

			angle += new WAngle(42);
			pos = center + Offset.Rotate(WRot.FromYaw(angle));
			world.ScreenMap.Update(this, pos, Size);
			if (frame == 48)
				world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); onCompletion(); });
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield return new ChronoVortexRenderable(renderer, pos, frame);
		}
	}
}
