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

using System;
using System.IO;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("General")]
	public class UpgradeProperties : ScriptActorProperties, Requires<UpgradeManagerInfo>
	{
		readonly UpgradeManager um;
		readonly ScriptUpgradesCache validUpgrades;

		public UpgradeProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			um = self.Trait<UpgradeManager>();
			validUpgrades = self.World.WorldActor.TraitOrDefault<ScriptUpgradesCache>();
		}

		[Desc("Grant an upgrade to this actor.")]
		public void GrantUpgrade(string upgrade)
		{
			if (validUpgrades == null)
				throw new InvalidOperationException("Can not grant upgrades because there is no ScriptUpgradesCache defined!");

			if (validUpgrades.Info.Upgrades.Contains(upgrade))
				um.GrantUpgrade(Self, upgrade, this);
			else
				throw new InvalidDataException("The ScriptUpgradesCache does not contain a definition for upgrade `{0}`".F(upgrade));
		}

		[Desc("Revoke an upgrade that was previously granted using GrantUpgrade.")]
		public void RevokeUpgrade(string upgrade)
		{
			if (validUpgrades == null)
				throw new InvalidOperationException("Can not grant upgrades because there is no ScriptUpgradesCache defined!");

			if (validUpgrades.Info.Upgrades.Contains(upgrade))
				um.RevokeUpgrade(Self, upgrade, this);
			else
				throw new InvalidDataException("The ScriptUpgradesCache does not contain a definition for upgrade `{0}`".F(upgrade));
		}

		[Desc("Grant a limited-time upgrade to this actor.")]
		public void GrantTimedUpgrade(string upgrade, int duration)
		{
			if (validUpgrades == null)
				throw new InvalidOperationException("Can not grant upgrades because there is no ScriptUpgradesCache defined!");

			if (validUpgrades.Info.Upgrades.Contains(upgrade))
				um.GrantTimedUpgrade(Self, upgrade, duration);
			else
				throw new InvalidDataException("The ScriptUpgradesCache does not contain a definition for upgrade `{0}`".F(upgrade));
		}

		[Desc("Check whether this actor accepts a specific upgrade.")]
		public bool AcceptsUpgrade(string upgrade)
		{
			return validUpgrades != null && validUpgrades.Info.Upgrades.Contains(upgrade) && um.AcceptsUpgrade(Self, upgrade);
		}
	}
}