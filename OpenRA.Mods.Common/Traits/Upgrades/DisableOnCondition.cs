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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Disable the actor when this trait is enabled by a condition.")]
	public class DisableOnConditionInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new DisableOnCondition(this); }
	}

	public class DisableOnCondition : ConditionalTrait<DisableOnConditionInfo>, IDisable
	{
		public DisableOnCondition(DisableOnConditionInfo info)
			: base(info) { }

		public bool Disabled { get { return !IsTraitDisabled; } }
	}
}
