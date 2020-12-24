#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		public readonly string Description = "";

		[Desc("Player relationships who can view the description.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally | PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		public override object Create(ActorInitializer init) { return new TooltipDescription(init.Self, this); }
	}

	public class TooltipDescription : ConditionalTrait<TooltipDescriptionInfo>, IProvideTooltipInfo
	{
		readonly Actor self;

		public Player Owner
		{
			get
			{
				return self.EffectiveOwner != null ? self.EffectiveOwner.Owner : self.Owner;
			}
		}

		public TooltipDescription(Actor self, TooltipDescriptionInfo info)
			: base(info)
		{
			this.self = self;
		}

		public bool IsTooltipVisible(Player forPlayer)
		{
			if (IsTraitDisabled)
				return false;

			// Visibility can't be determined for null owners or viewers
			if (Owner == null || forPlayer == null)
				return false;

			return Info.ValidRelationships.HasStance(Owner.RelationshipWith(forPlayer));
		}

		public string TooltipText
		{
			get
			{
				return Info.Description;
			}
		}
	}
}
