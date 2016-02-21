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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class UpgradeMultiplierTraitInfo : ITraitInfo
	{
		[UpgradeUsedReference]
		[Desc("Accepted upgrade types.")]
		public readonly string[] UpgradeTypes = { };

		[Desc("The lowest upgrade level using the scale.")]
		public readonly int BaseLevel = 1;

		[FieldLoader.Require]
		[Desc("Percentages to apply with the first being applied at the base level.",
			"Repeat last entry to accept time extensions.",
			"If no upgrade types are specified, then the first/only modifier is always applied.")]
		public readonly int[] Modifier = { };

		public abstract object Create(ActorInitializer init);
	}

	public abstract class UpgradeMultiplierTrait : IUpgradable, IDisabledTrait, ISync
	{
		readonly UpgradeMultiplierTraitInfo info;
		[Sync] int level = 0;
		[Sync] public bool IsTraitDisabled { get; private set; }
		public int AdjustedLevel { get { return level - info.BaseLevel; } }
		public IEnumerable<string> UpgradeTypes { get { return info.UpgradeTypes; } }

		protected UpgradeMultiplierTrait(UpgradeMultiplierTraitInfo info, string modifierType, string actorType)
		{
			if (info.Modifier.Length == 0)
				throw new Exception("No modifiers in " + modifierType + " for " + actorType);
			this.info = info;
			IsTraitDisabled = info.UpgradeTypes.Length > 0 && info.BaseLevel > 0;
			level = IsTraitDisabled ? 0 : info.BaseLevel;
		}

		public bool AcceptsUpgradeLevel(Actor self, string type, int level)
		{
			return level < info.Modifier.Length + info.BaseLevel;
		}

		// Override to receive notice of level change.
		protected virtual void Update(Actor self) { }

		public void UpgradeLevelChanged(Actor self, string type, int oldLevel, int newLevel)
		{
			if (!UpgradeTypes.Contains(type))
				return;
			level = newLevel.Clamp(0, Math.Max(info.Modifier.Length + info.BaseLevel - 1, 0));
			IsTraitDisabled = level < info.BaseLevel;
			Update(self);
		}

		public int GetModifier()
		{
			return IsTraitDisabled ? 100 : info.Modifier[level - info.BaseLevel];
		}
	}
}
