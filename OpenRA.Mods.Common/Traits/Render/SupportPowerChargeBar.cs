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

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Display the time remaining until the super weapon attached to the actor is ready.")]
	class SupportPowerChargeBarInfo : ConditionalTraitInfo
	{
		[Desc("Defines to which players the bar is to be shown.")]
		public readonly PlayerRelationship DisplayRelationships = PlayerRelationship.Ally;

		public readonly Color Color = Color.Magenta;

		public override object Create(ActorInitializer init) { return new SupportPowerChargeBar(init.Self, this); }
	}

	class SupportPowerChargeBar : ConditionalTrait<SupportPowerChargeBarInfo>, ISelectionBar, INotifyOwnerChanged
	{
		readonly Actor self;
		SupportPowerManager spm;

		public SupportPowerChargeBar(Actor self, SupportPowerChargeBarInfo info)
			: base(info)
		{
			this.self = self;
			spm = self.Owner.PlayerActor.Trait<SupportPowerManager>();
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled)
				return 0;

			var power = spm.GetPowersForActor(self).FirstOrDefault(sp => !sp.Disabled);
			if (power == null)
				return 0;

			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			if (viewer != null && !Info.DisplayRelationships.HasStance(self.Owner.RelationshipWith(viewer)))
				return 0;

			return 1 - (float)power.RemainingTicks / power.TotalTicks;
		}

		Color ISelectionBar.GetColor() { return Info.Color; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			spm = newOwner.PlayerActor.Trait<SupportPowerManager>();
		}
	}
}
