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
	/// <summary>Use as base class for *Info to subclass of ConditionalTrait. (See ConditionalTrait.)</summary>
	public abstract class ConditionalTraitInfo
	{
		[Desc("The condition types which can enable or disable this trait.")]
		public readonly string[] ConditionTypes = { };

		[Desc("The minimum condition level at which this trait is enabled.", "Defaults to 0 (enabled by default).")]
		public readonly int MinEnabledConditionLevel = 0;

		[Desc("The maximum condition level at which the trait is enabled.",
			"Defaults to MaxAcceptedConditionLevel (enabled for all levels greater than MinEnabledConditionLevel).",
			"Set this to a value smaller than MaxAcceptedConditionLevel to disable the trait at higher levels.",
			"Use MaxAcceptedConditionLevel: 2 (1 more) to be able to extend condition time.")]
		public readonly int MaxEnabledConditionLevel = int.MaxValue;

		[Desc("The maximum condition level that this trait will accept.")]
		public readonly int MaxAcceptedConditionLevel = 1;
	}

	/// <summary>
	/// Abstract base for enabling and disabling trait using conditions.
	/// Requires basing *Info on ConditionalTraitInfo and using base(info) constructor.
	/// Note that EnabledByCondition is not called at creation even if this starts as enabled.
	/// </summary>,
	public abstract class ConditionalTrait<InfoType> : IConditional, IDisabledTrait, ISync where InfoType : ConditionalTraitInfo
	{
		public readonly InfoType Info;
		public IEnumerable<string> ConditionTypes { get { return Info.ConditionTypes; } }
		[Sync] public bool IsTraitDisabled { get; private set; }

		public ConditionalTrait(InfoType info)
		{
			Info = info;
			IsTraitDisabled = info.ConditionTypes != null && info.ConditionTypes.Length > 0 && info.MinEnabledConditionLevel > 0;
		}

		public bool AcceptsConditionLevel(Actor self, string type, int level)
		{
			return level > 0 && level <= Info.MaxAcceptedConditionLevel;
		}

		public void ConditionLevelChanged(Actor self, string type, int oldLevel, int newLevel)
		{
			if (!Info.ConditionTypes.Contains(type))
				return;

			// Restrict the levels to the allowed range
			oldLevel = oldLevel.Clamp(0, Info.MaxAcceptedConditionLevel);
			newLevel = newLevel.Clamp(0, Info.MaxAcceptedConditionLevel);
			if (oldLevel == newLevel)
				return;

			var wasDisabled = IsTraitDisabled;
			IsTraitDisabled = newLevel < Info.MinEnabledConditionLevel || newLevel > Info.MaxEnabledConditionLevel;
			ConditionLevelChanged(self, oldLevel, newLevel);

			if (IsTraitDisabled != wasDisabled)
			{
				if (wasDisabled)
					EnabledByCondition(self);
				else
					DisabledByCondition(self);
			}
		}

		// Subclasses can add condition support by querying IsTraitDisabled and/or overriding these methods.
		protected virtual void ConditionLevelChanged(Actor self, int oldLevel, int newLevel) { }
		protected virtual void EnabledByCondition(Actor self) { }
		protected virtual void DisabledByCondition(Actor self) { }
	}
}
