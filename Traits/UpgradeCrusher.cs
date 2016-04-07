#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Apply upgrades to the crushing actor.")]
	public class UpgradeCrusherInfo : ITraitInfo
	{
		[UpgradeGrantedReference]
		[Desc("The upgrades to apply.")]
		public readonly string[] Upgrades = { };

		[Desc("Duration of the upgrade (in ticks). Set to 0 for a permanent upgrade.")]
		public readonly int Duration = 0;

		public virtual object Create(ActorInitializer init) { return new UpgradeCrusher(init.Self, this); }
	}

	public class UpgradeCrusher : INotifyCrushed
	{
		public readonly UpgradeCrusherInfo Info;

		public UpgradeCrusher(Actor self, UpgradeCrusherInfo info)
		{
			this.Info = info;
		}

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			var um = crusher.TraitOrDefault<UpgradeManager>();
			if (um == null)
				return;

			foreach (var u in Info.Upgrades)
			{
				if (Info.Duration > 0)
				{
					if (um.AcknowledgesUpgrade(crusher, u))
						um.GrantTimedUpgrade(crusher, u, Info.Duration, self, Info.Upgrades.Count(upg => upg == u));
				}
				else
				{
					if (um.AcceptsUpgrade(crusher, u))
						um.GrantUpgrade(crusher, u, this);
				}
			}
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, HashSet<string> crushClasses) { }
	}
}
