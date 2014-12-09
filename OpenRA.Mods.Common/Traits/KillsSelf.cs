#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class KillsSelfInfo : UpgradableTraitInfo, ITraitInfo
	{
		[Desc("Remove the actor from the world (and destroy it) instead of killing it.")]
		public readonly bool RemoveInstead = false;

		public object Create(ActorInitializer init) { return new KillsSelf(this); }
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

			if (Info.RemoveInstead || !self.HasTrait<Health>())
				self.Destroy();
			else
				self.Kill(self);
		}
	}
}
