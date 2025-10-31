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

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ShakeOnDeathInfo : TraitInfo
	{
		[Desc("DeathType(s) that trigger the shake. Leave empty to always trigger a shake.")]
		public readonly BitSet<DamageType> DeathTypes = default;

		public readonly int Duration = 10;
		public readonly int Intensity = 1;
		public override object Create(ActorInitializer init) { return new ShakeOnDeath(this); }
	}

	public class ShakeOnDeath : INotifyKilled
	{
		readonly ShakeOnDeathInfo info;

		public ShakeOnDeath(ShakeOnDeathInfo info)
		{
			this.info = info;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!info.DeathTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(info.DeathTypes))
				return;

			self.World.WorldActor.Trait<ScreenShaker>().AddEffect(info.Duration, self.CenterPosition, info.Intensity);
		}
	}
}
