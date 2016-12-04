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
	public class GrantPeriodicUpgradesInfo : UpgradableTraitInfo, Requires<UpgradeManagerInfo>
	{
		[UpgradeGrantedReference, FieldLoader.Require]
		[Desc("The upgrades to grant.")]
		public readonly string[] Upgrades = { };

		[Desc("The range of time (in ticks) that the upgrades will take to be granted.")]
		public readonly int[] CooldownDuration = { 1000 };

		[Desc("The range of time (in ticks) that the upgrades will be enabled.")]
		public readonly int[] ActiveDuration = { 100 };

		public readonly bool StartsGranted = false;

		public readonly bool ResetTraitOnEnable = false;

		public readonly bool ShowSelectionBar = false;
		public readonly Color CooldownColor = Color.DarkRed;
		public readonly Color ActiveColor = Color.DarkMagenta;

		public override object Create(ActorInitializer init) { return new GrantPeriodicUpgrades(init, this); }
	}

	public class GrantPeriodicUpgrades : UpgradableTrait<GrantPeriodicUpgradesInfo>, INotifyCreated, ISelectionBar, ITick, ISync
	{
		readonly Actor self;
		readonly GrantPeriodicUpgradesInfo info;
		readonly UpgradeManager manager;

		[Sync] int ticks;
		int cooldown, active;
		bool isEnabled, isSuspended;

		public GrantPeriodicUpgrades(ActorInitializer init, GrantPeriodicUpgradesInfo info)
			: base(info)
		{
			self = init.Self;
			this.info = info;
			manager = self.Trait<UpgradeManager>();
		}

		void SetDefaultState()
		{
			if (info.StartsGranted)
			{
				ticks = info.ActiveDuration.Length == 2
					? self.World.SharedRandom.Next(info.ActiveDuration[0], info.ActiveDuration[1])
					: info.ActiveDuration[0];
				active = ticks;
				if (info.StartsGranted != isEnabled)
					EnableUpgrade();
			}
			else
			{
				ticks = info.CooldownDuration.Length == 2
					? self.World.SharedRandom.Next(info.CooldownDuration[0], info.CooldownDuration[1])
					: info.CooldownDuration[0];
				cooldown = ticks;
				if (info.StartsGranted != isEnabled)
					DisableUpgrade();
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			if (!IsTraitDisabled)
				SetDefaultState();
		}

		void ITick.Tick(Actor self)
		{
			if (!IsTraitDisabled && --ticks < 0)
			{
				if (isEnabled)
				{
					ticks = info.CooldownDuration.Length == 2
						? self.World.SharedRandom.Next(info.CooldownDuration[0], info.CooldownDuration[1])
						: info.CooldownDuration[0];
					cooldown = ticks;
					DisableUpgrade();
				}
				else
				{
					ticks = info.ActiveDuration.Length == 2
						? self.World.SharedRandom.Next(info.ActiveDuration[0], info.ActiveDuration[1])
						: info.ActiveDuration[0];
					active = ticks;
					EnableUpgrade();
				}
			}
		}

		protected override void UpgradeEnabled(Actor self)
		{
			if (info.ResetTraitOnEnable)
				SetDefaultState();
			else if (isSuspended)
				EnableUpgrade();

			isSuspended = false;
		}

		protected override void UpgradeDisabled(Actor self)
		{
			if (isEnabled)
			{
				DisableUpgrade();
				isSuspended = true;
			}
		}

		void EnableUpgrade()
		{
			foreach (var up in info.Upgrades)
				manager.GrantUpgrade(self, up, this);

			isEnabled = true;
		}

		void DisableUpgrade()
		{
			foreach (var up in info.Upgrades)
				manager.RevokeUpgrade(self, up, this);

			isEnabled = false;
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
