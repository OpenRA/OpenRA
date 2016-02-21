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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>Use as base class for *Info to subclass of UpgradableTrait. (See UpgradableTrait.)</summary>
	public abstract class UpgradableTraitInfo : IUpgradableInfo
	{
		[UpgradeUsedReference]
		[Desc("The upgrade types which can enable or disable this trait.")]
		public readonly HashSet<string> UpgradeTypes = new HashSet<string>();

		[Desc("The minimum upgrade level at which this trait is enabled.", "Defaults to 0 (enabled by default).")]
		public readonly int UpgradeMinEnabledLevel = 0;

		[Desc("The maximum upgrade level at which the trait is enabled.",
			"Defaults to UpgradeMaxAcceptedLevel (enabled for all levels greater than UpgradeMinEnabledLevel).",
			"Set this to a value smaller than UpgradeMaxAcceptedLevel to disable the trait at higher levels.",
			"Use UpgradeMaxAcceptedLevel: 2 (1 more) to be able to extend upgrade time.")]
		public readonly int UpgradeMaxEnabledLevel = int.MaxValue;

		[Desc("The maximum upgrade level that this trait will accept.")]
		public readonly int UpgradeMaxAcceptedLevel = 1;

		public abstract object Create(ActorInitializer init);
	}

	/// <summary>
	/// Abstract base for enabling and disabling trait using upgrades.
	/// Requires basing *Info on UpgradableTraitInfo and using base(info) constructor.
	/// Note that EnabledByUpgrade is not called at creation even if this starts as enabled.
	/// </summary>
	public abstract class UpgradableTrait<InfoType> : IUpgradable, IDisabledTrait, ISync where InfoType : UpgradableTraitInfo
	{
		public readonly InfoType Info;
		public IEnumerable<string> UpgradeTypes { get { return Info.UpgradeTypes; } }
		[Sync] public bool IsTraitDisabled { get; private set; }

		public UpgradableTrait(InfoType info)
		{
			Info = info;
			IsTraitDisabled = info.UpgradeTypes != null && info.UpgradeTypes.Count > 0 && info.UpgradeMinEnabledLevel > 0;
		}

		public bool AcceptsUpgradeLevel(Actor self, string type, int level)
		{
			return level > 0 && level <= Info.UpgradeMaxAcceptedLevel;
		}

		public void UpgradeLevelChanged(Actor self, string type, int oldLevel, int newLevel)
		{
			if (!Info.UpgradeTypes.Contains(type))
				return;

			// Restrict the levels to the allowed range
			oldLevel = oldLevel.Clamp(0, Info.UpgradeMaxAcceptedLevel);
			newLevel = newLevel.Clamp(0, Info.UpgradeMaxAcceptedLevel);
			if (oldLevel == newLevel)
				return;

			var wasDisabled = IsTraitDisabled;
			IsTraitDisabled = newLevel < Info.UpgradeMinEnabledLevel || newLevel > Info.UpgradeMaxEnabledLevel;
			UpgradeLevelChanged(self, oldLevel, newLevel);

			if (IsTraitDisabled != wasDisabled)
			{
				if (wasDisabled)
					UpgradeEnabled(self);
				else
					UpgradeDisabled(self);
			}
		}

		// Subclasses can add upgrade support by querying IsTraitDisabled and/or overriding these methods.
		protected virtual void UpgradeLevelChanged(Actor self, int oldLevel, int newLevel) { }
		protected virtual void UpgradeEnabled(Actor self) { }
		protected virtual void UpgradeDisabled(Actor self) { }
	}
}
