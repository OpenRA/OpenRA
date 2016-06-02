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

using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithHarvestAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<HarvesterInfo>
	{
		[Desc("Prefix added to idle and harvest sequences depending on fullness of harvester.")]
		[SequenceReference(null, true)] public readonly string[] PrefixByFullness = { "" };

		[Desc("Displayed while harvesting.")]
		[SequenceReference] public readonly string HarvestSequence = "harvest";

		public object Create(ActorInitializer init) { return new WithHarvestAnimation(init, this); }
	}

	public class WithHarvestAnimation : ITick, INotifyHarvesterAction
	{
		public readonly WithHarvestAnimationInfo Info;
		readonly WithSpriteBody wsb;
		readonly Harvester harv;

		public bool IsModifying;

		public WithHarvestAnimation(ActorInitializer init, WithHarvestAnimationInfo info)
		{
			Info = info;
			harv = init.Self.Trait<Harvester>();
			wsb = init.Self.Trait<WithSpriteBody>();
		}

		protected virtual string NormalizeHarvesterSequence(Actor self, string baseSequence)
		{
			var desiredState = harv.Fullness * (Info.PrefixByFullness.Length - 1) / 100;
			var desiredPrefix = Info.PrefixByFullness[desiredState];

			if (wsb.DefaultAnimation.HasSequence(desiredPrefix + baseSequence))
				return desiredPrefix + baseSequence;
			else
				return baseSequence;
		}

		public void Tick(Actor self)
		{
			var baseSequence = wsb.NormalizeSequence(self, wsb.Info.Sequence);
			var sequence = NormalizeHarvesterSequence(self, baseSequence);
			if (!IsModifying && wsb.DefaultAnimation.HasSequence(sequence) && wsb.DefaultAnimation.CurrentSequence.Name != sequence)
				wsb.DefaultAnimation.ReplaceAnim(sequence);
		}

		public void Harvested(Actor self, ResourceType resource)
		{
			var baseSequence = wsb.NormalizeSequence(self, Info.HarvestSequence);
			var sequence = NormalizeHarvesterSequence(self, baseSequence);
			if (!IsModifying && wsb.DefaultAnimation.HasSequence(sequence))
			{
				IsModifying = true;
				wsb.PlayCustomAnimation(self, sequence, () => IsModifying = false);
			}
		}

		// If IsModifying isn't set to true, the docking animation
		// will be overridden by the WithHarvestAnimation fullness modifier.
		public void Docked()
		{
			IsModifying = true;
		}

		public void Undocked()
		{
			IsModifying = false;
		}

		public void MovingToResources(Actor self, CPos targetCell, Activity next) { }
		public void MovingToRefinery(Actor self, CPos targetCell, Activity next) { }
		public void MovementCancelled(Actor self) { }
	}
}
