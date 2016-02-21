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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class KillsSelfInfo : UpgradableTraitInfo
	{
		[Desc("Remove the actor from the world (and destroy it) instead of killing it.")]
		public readonly bool RemoveInstead = false;

		public override object Create(ActorInitializer init) { return new KillsSelf(this); }
	}

	class KillsSelf : UpgradableTrait<KillsSelfInfo>, INotifyAddedToWorld
	{
		public KillsSelf(KillsSelfInfo info)
			: base(info) { }

		public void AddedToWorld(Actor self)
		{
			if (!IsTraitDisabled)
				UpgradeEnabled(self);
		}

		protected override void UpgradeEnabled(Actor self)
		{
			if (self.IsDead)
				return;

			if (Info.RemoveInstead || !self.Info.HasTraitInfo<HealthInfo>())
				self.Dispose();
			else
				self.Kill(self);
		}
	}
}
