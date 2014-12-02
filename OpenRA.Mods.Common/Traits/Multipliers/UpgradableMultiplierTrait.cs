#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public abstract class UpgradeMultiplierTraitInfo
	{
		[Desc("Accepted upgrade types.")]
		public readonly string[] UpgradeTypes = { };

		[Desc("The lowest upgrade level using the scale.")]
		public readonly int BaseLevel = 1;

		[Desc("Percentages to apply with the first being applied at the base level.",
			"Repeat last entry to accept time extensions.")]
		public readonly int[] Modifier = { };

		protected UpgradeMultiplierTraitInfo(string[] upgradeTypes, int[] modifiers)
		{
			UpgradeTypes = upgradeTypes;
			Modifier = modifiers;
		}
	}

	public abstract class UpgradeMultiplierTrait : IUpgradable, IDisabledTrait, ISync
	{
		readonly UpgradeMultiplierTraitInfo info;
		[Sync] int level = 0;
		public bool IsTraitDisabled { get; private set; }
		public IEnumerable<string> UpgradeTypes { get { return info.UpgradeTypes; } }

		protected UpgradeMultiplierTrait(UpgradeMultiplierTraitInfo info)
		{
			this.info = info;
			IsTraitDisabled = info.BaseLevel > 0;
		}

		public bool AcceptsUpgradeLevel(Actor self, string type, int level)
		{
			return UpgradeTypes.Contains(type) && level >= 0 && level < info.Modifier.Length + info.BaseLevel;
		}

		protected virtual void Update(Actor self) { }
		public void UpgradeLevelChanged(Actor self, string type, int oldLevel, int newLevel)
		{
			if (!UpgradeTypes.Contains(type))
				return;
			level = newLevel.Clamp(0, info.Modifier.Length + info.BaseLevel - 1);
			IsTraitDisabled = level < info.BaseLevel;
			Update(self);
		}

		public int GetModifier()
		{
			return IsTraitDisabled ? 100 : info.Modifier[level - info.BaseLevel];
		}
	}
}
