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

using System.Numerics;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Makes the screen shake.")]
	public class ShakeScreenWarhead : Warhead
	{
		[Desc("Duration of the shaking.")]
		public readonly int Duration = 0;

		[Desc("Shake intensity.")]
		public readonly int Intensity = 0;

		[Desc("Shake multipliers by the X and Y axis, comma-separated.")]
		public readonly Vector2 Multiplier = Vector2.Zero;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			args.SourceActor.World.WorldActor.Trait<ScreenShaker>().AddEffect(Duration, target.CenterPosition, Intensity, Multiplier);
		}
	}
}
