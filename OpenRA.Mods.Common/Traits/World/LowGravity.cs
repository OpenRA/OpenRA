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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("World-wide gravity, as a percentage of Earth. Read by arcing projectiles",
		"(see the GravityBomb / Bullet edits in Projectiles/PATCH-projectile-gravity.md)",
		"to scale their downward acceleration. Attach to the World actor.")]
	public class LowGravityInfo : TraitInfo
	{
		[Desc("Gravity as a percentage of Earth. 100 = Earth, 38 = Mars, 16 = Moon.")]
		public readonly int GravityPercent = 16;

		public override object Create(ActorInitializer init) { return new LowGravity(this); }
	}

	public class LowGravity
	{
		public readonly int GravityPercent;

		public LowGravity(LowGravityInfo info)
		{
			GravityPercent = info.GravityPercent;
		}

		// Helper: scale an Earth-calibrated acceleration/velocity length by local gravity.
		public int Scale(int earthValue)
		{
			return earthValue * GravityPercent / 100;
		}
	}
}
