#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Replaces the sprite during construction.")]
	public class WithMakeAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<UpgradeManagerInfo>
	{
		[Desc("Sequence name to use.")]
		[SequenceReference] public readonly string Sequence = "make";

		[UpgradeGrantedReference]
		[Desc("The upgrades to grant while the make animation runs.")]
		public readonly string[] MakeUpgrades = { };

		public object Create(ActorInitializer init) { return new WithMakeAnimation(init, this); }
	}

	public class WithMakeAnimation : INotifyCreated
	{
		readonly WithMakeAnimationInfo info;
		readonly WithSpriteBody wsb;
		readonly UpgradeManager manager;

		public WithMakeAnimation(ActorInitializer init, WithMakeAnimationInfo info)
		{
			this.info = info;
			var self = init.Self;
			wsb = self.Trait<WithSpriteBody>();
			manager = self.Trait<UpgradeManager>();
		}

		public void Created(Actor self)
		{
			var building = self.TraitOrDefault<Building>();
			if (building != null && !building.SkipMakeAnimation)
			{
				foreach (var up in info.MakeUpgrades)
					manager.GrantUpgrade(self, up, this);

				wsb.PlayCustomAnimation(self, info.Sequence, () =>
				{
					building.NotifyBuildingComplete(self);
					foreach (var up in info.MakeUpgrades)
						manager.RevokeUpgrade(self, up, this);
				});
			}
			else if (building == null)
			{
				foreach (var up in info.MakeUpgrades)
					manager.GrantUpgrade(self, up, this);

				wsb.PlayCustomAnimation(self, info.Sequence, () =>
				{
					foreach (var up in info.MakeUpgrades)
						manager.RevokeUpgrade(self, up, this);
				});
			}
		}

		public void Reverse(Actor self, Activity activity, bool queued = true)
		{
			foreach (var up in info.MakeUpgrades)
				manager.GrantUpgrade(self, up, this);

			wsb.PlayCustomAnimationBackwards(self, info.Sequence, () =>
			{
				foreach (var up in info.MakeUpgrades)
					manager.RevokeUpgrade(self, up, this);

				// avoids visual glitches as we wait for the actor to get destroyed
				wsb.DefaultAnimation.PlayFetchIndex(info.Sequence, () => 0);
				self.QueueActivity(queued, activity);
			});
		}
	}
}
