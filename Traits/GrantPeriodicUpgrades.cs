#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grant upgrades periodically.")]
	public class GrantPeriodicUpgradesInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[UpgradeGrantedReference, FieldLoader.Require]
		[Desc("The upgrades to grant.")]
		public readonly string[] Upgrades = { };

		[Desc("The range of time (in ticks) that the upgrades will take to be granted.")]
		public readonly int[] CooldownDuration = { 1000 };

		[Desc("The range of time (in ticks) that the upgrades will be enabled.")]
		public readonly int[] ActiveDuration = { 100 };

		public readonly bool StartsEnabled = false;

		public readonly bool ShowSelectionBar = true;
		public readonly Color CooldownColor = Color.DarkRed;
		public readonly Color ActiveColor = Color.DarkMagenta;

		public object Create(ActorInitializer init) { return new GrantPeriodicUpgrades(init, this); }
	}

	public class GrantPeriodicUpgrades : INotifyCreated, ISelectionBar, ISync, ITick
	{
		readonly Actor self;
		readonly GrantPeriodicUpgradesInfo info;
		readonly UpgradeManager manager;

		[Sync] int ticks;
		int cooldown, active;
		bool isEnabled;

		public GrantPeriodicUpgrades(ActorInitializer init, GrantPeriodicUpgradesInfo info)
		{
			self = init.Self;
			this.info = info;
			manager = self.Trait<UpgradeManager>();

			if (info.StartsEnabled)
			{
				ticks = info.ActiveDuration.Length == 2
					? self.World.SharedRandom.Next(info.ActiveDuration[0], info.ActiveDuration[1])
					: info.ActiveDuration[0];
				active = ticks;
				isEnabled = true;
			}
			else
			{
				ticks = info.CooldownDuration.Length == 2
					? self.World.SharedRandom.Next(info.CooldownDuration[0], info.CooldownDuration[1])
					: info.CooldownDuration[0];
				cooldown = ticks;
				isEnabled = false;
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			if (isEnabled)
				foreach (var up in info.Upgrades)
					manager.GrantUpgrade(self, up, this);
		}

		void ITick.Tick(Actor self)
		{
			if (--ticks < 0)
			{
				if (isEnabled)
				{
					foreach (var up in info.Upgrades)
						manager.RevokeUpgrade(self, up, this);

					ticks = info.CooldownDuration.Length == 2
						? self.World.SharedRandom.Next(info.CooldownDuration[0], info.CooldownDuration[1])
						: info.CooldownDuration[0];
					cooldown = ticks;
					isEnabled = false;
				}
				else
				{
					foreach (var up in info.Upgrades)
						manager.GrantUpgrade(self, up, this);

					ticks = info.ActiveDuration.Length == 2
						? self.World.SharedRandom.Next(info.ActiveDuration[0], info.ActiveDuration[1])
						: info.ActiveDuration[0];
					active = ticks;
					isEnabled = true;
				}
			}
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar)
				return 0f;

			return isEnabled
				? (float)(active - ticks) / active
					: (float)ticks / cooldown;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return info.ShowSelectionBar; } }

		Color ISelectionBar.GetColor() { return isEnabled ? info.ActiveColor : info.CooldownColor; }
	}
}
