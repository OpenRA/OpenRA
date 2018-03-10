#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	[Desc("Additional info shown in the battlefield tooltip.")]
	public class TooltipDescriptionInfo : ConditionalTraitInfo
	{
		[Desc("Text shown in tooltip.")]
		[Translate]
		public readonly string Description = "";

		public override object Create(ActorInitializer init) { return new TooltipDescription(this); }
	}

	public class TooltipDescription : ConditionalTrait<TooltipDescriptionInfo>, IProvideTooltipInfo
	{
		public TooltipDescription(TooltipDescriptionInfo info)
			: base(info) { }

		public bool IsTooltipVisible(Player forPlayer) { return !IsTraitDisabled; }

		public string TooltipText
		{
			get
			{
				return Info.Description;
			}
		}
	}
}
