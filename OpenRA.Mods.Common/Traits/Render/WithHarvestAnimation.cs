#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithHarvestAnimationInfo : TraitInfo, Requires<WithSpriteBodyInfo>, Requires<HarvesterInfo>
	{
		[SequenceReference]
		[Desc("Displayed while harvesting.")]
		public readonly string HarvestSequence = "harvest";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithHarvestAnimation(init, this); }
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

		void INotifyHarvesterAction.Harvested(Actor self, string resourceType)
		{
			var sequence = wsb.NormalizeSequence(self, Info.HarvestSequence);
			if (wsb.DefaultAnimation.HasSequence(sequence) && wsb.DefaultAnimation.CurrentSequence.Name != sequence)
				wsb.PlayCustomAnimation(self, sequence);
		}

		void INotifyHarvesterAction.Docked() { }
		void INotifyHarvesterAction.Undocked() { }
		void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell) { }
		void INotifyHarvesterAction.MovingToRefinery(Actor self, Actor refineryActor) { }
		void INotifyHarvesterAction.MovementCancelled(Actor self) { }
	}
}
