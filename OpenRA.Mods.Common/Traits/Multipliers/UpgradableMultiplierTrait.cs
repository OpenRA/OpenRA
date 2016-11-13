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
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class UpgradeMultiplierTraitInfo : ITraitInfo
	{
		[Desc("Boolean expression defining the condition to enable this trait.",
			"Overrides UpgradeTypes/BaseLevel if set.",
			"Only the first Modifier will be used when the condition is enabled.")]
		[UpgradeUsedReference]
		public readonly BooleanExpression RequiresCondition = null;

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
		readonly Dictionary<string, bool> conditions = new Dictionary<string, bool>();

		[Sync] int level = 0;
		[Sync] public bool IsTraitDisabled { get; private set; }

		IEnumerable<string> IUpgradable.UpgradeTypes
		{
			get
			{
				if (info.RequiresCondition != null)
					return info.RequiresCondition.Variables;

				return info.UpgradeTypes;
			}
		}

		protected UpgradeMultiplierTrait(UpgradeMultiplierTraitInfo info, string modifierType, string actorType)
		{
			this.info = info;
			if (info.Modifier.Length == 0)
				throw new ArgumentException("No modifiers in " + modifierType + " for " + actorType);

			// TODO: Set initial state from a future ConditionsInit
			if (info.RequiresCondition != null)
				IsTraitDisabled = !info.RequiresCondition.Evaluate(conditions);
			else
			{
				IsTraitDisabled = info.UpgradeTypes != null && info.UpgradeTypes.Length > 0 && info.BaseLevel > 0;
				level = IsTraitDisabled ? 0 : info.BaseLevel;
			}
		}

		public bool AcceptsUpgradeLevel(Actor self, string type, int level)
		{
			if (info.RequiresCondition != null)
				return level == 1;

			return level < info.Modifier.Length + info.BaseLevel;
		}

		// Override to receive notice of level change.
		protected virtual void Update(Actor self) { }

		public void UpgradeLevelChanged(Actor self, string type, int oldLevel, int newLevel)
		{
			if (info.RequiresCondition != null)
			{
				conditions[type] = newLevel > 0;
				IsTraitDisabled = !info.RequiresCondition.Evaluate(conditions);
			}
			else
			{
				if (!info.UpgradeTypes.Contains(type))
					return;

				level = newLevel.Clamp(0, Math.Max(info.Modifier.Length + info.BaseLevel - 1, 0));
				IsTraitDisabled = level < info.BaseLevel;
			}

			Update(self);
		}

		public int GetModifier()
		{
			if (info.RequiresCondition != null)
				return IsTraitDisabled ? 100 : info.Modifier[0];

			return IsTraitDisabled ? 100 : info.Modifier[level - info.BaseLevel];
		}
	}
}
