#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithHarvestAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<HarvesterInfo>
	{
		[Desc("Displayed while harvesting.")]
		[SequenceReference] public readonly string HarvestSequence = "harvest";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public object Create(ActorInitializer init) { return new WithHarvestAnimation(init, this); }
	}

	public class WithHarvestAnimation : INotifyHarvesterAction
	{
		public readonly WithHarvestAnimationInfo Info;
		readonly WithSpriteBody wsb;

		public WithHarvestAnimation(ActorInitializer init, WithHarvestAnimationInfo info)
		{
			Info = info;
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
		}

		void INotifyHarvesterAction.Harvested(Actor self, ResourceType resource)
		{
			var sequence = wsb.NormalizeSequence(self, Info.HarvestSequence);
			if (wsb.DefaultAnimation.HasSequence(sequence) && wsb.DefaultAnimation.CurrentSequence.Name != sequence)
				wsb.PlayCustomAnimation(self, sequence);
		}

		void INotifyHarvesterAction.Docked() { }
		void INotifyHarvesterAction.Undocked() { }
		void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell, Activity next) { }
		void INotifyHarvesterAction.MovingToRefinery(Actor self, Actor refineryActor, Activity next) { }
		void INotifyHarvesterAction.MovementCancelled(Actor self) { }
	}
}
