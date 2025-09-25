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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.ProjectileEffects
{
	[Desc("Draw a colored contrail as the projectile moves.")]
	public sealed class ContrailProjectileEffectInfo : IProjectileEffectInfo
	{
		[Desc("When set, display a line behind the actor. Length is measured in ticks after appearing.")]
		public readonly int Length = 25;

		[Desc("Time (in ticks) after which the line should start to appear.")]
		public readonly int StartDelay = 0;

		[Desc("Time (in ticks) after which the line should appear. Controls the distance to the actor.")]
		public readonly int Delay = 1;

		[Desc("Thickness of the emitted line at the start of the contrail.")]
		public readonly WDist StartWidth = new(64);

		[Desc("Thickness of the emitted line at the end of the contrail. Will default to " + nameof(StartWidth) + " if left undefined")]
		public readonly WDist? EndWidth = null;

		[Desc("RGB color at the contrail start.")]
		public readonly Color StartColor = Color.White;

		[Desc("Use player remap color instead of a custom color at the contrail the start.")]
		public readonly bool StartColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color at the contrail the start.")]
		public readonly int StartColorAlpha = 255;

		[Desc("RGB color at the contrail end. Will default to " + nameof(StartColor) + " if left undefined")]
		public readonly Color? EndColor;

		[Desc("Use player remap color instead of a custom color at the contrail end.")]
		public readonly bool EndColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color at the contrail end.")]
		public readonly int EndColorAlpha = 0;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 2047;

		IProjectileEffect IProjectileEffectInfo.Create(ProjectileArgs args, Func<WAngle> facing)
		{
			return new ContrailProjectileEffect(args, this);
		}
	}

	public sealed class ContrailProjectileEffect : IProjectileEffect
	{
		readonly ContrailRenderable trail;
		int delay;

		public ContrailProjectileEffect(ProjectileArgs args, ContrailProjectileEffectInfo info)
		{
			var startcolor = Color.FromArgb(info.StartColorAlpha, info.StartColor);
			var endcolor = Color.FromArgb(info.EndColorAlpha, info.EndColor ?? startcolor);
			trail = new ContrailRenderable(args.SourceActor.World, args.SourceActor,
				startcolor, info.StartColorUsePlayerColor,
				endcolor, info.EndColor == null ? info.StartColorUsePlayerColor : info.EndColorUsePlayerColor,
				info.StartWidth,
				info.EndWidth ?? info.StartWidth,
				info.Length, info.Delay, info.ZOffset);

			delay = info.StartDelay;
		}

		void IProjectileEffect.Tick(World world, WPos pos, WRot orientation)
		{
			if (delay == 0)
				trail.Update(pos);
			else
				delay--;
		}

		IEnumerable<IRenderable> IProjectileEffect.Render(WorldRenderer wr)
		{
			yield return trail;
		}

		void IProjectileEffect.Destroy(World world, WPos pos)
		{
			world.AddFrameEndTask(w => w.Add(new ContrailFader(pos, trail)));
		}
	}
}
