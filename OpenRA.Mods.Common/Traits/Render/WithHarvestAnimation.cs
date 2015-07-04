#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
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
			if (!IsModifying && !string.IsNullOrEmpty(wsb.Info.Sequence) && wsb.DefaultAnimation.HasSequence(NormalizeHarvesterSequence(self, wsb.Info.Sequence)))
			{
				if (wsb.DefaultAnimation.CurrentSequence.Name != NormalizeHarvesterSequence(self, wsb.Info.Sequence))
					wsb.DefaultAnimation.ReplaceAnim(NormalizeHarvesterSequence(self, wsb.Info.Sequence));
			}
		}

		public void Harvested(Actor self, ResourceType resource)
		{
			if (!IsModifying && !string.IsNullOrEmpty(Info.HarvestSequence) && wsb.DefaultAnimation.HasSequence(NormalizeHarvesterSequence(self, Info.HarvestSequence)))
			{
				IsModifying = true;
				wsb.PlayCustomAnimation(self, NormalizeHarvesterSequence(self, Info.HarvestSequence), () => IsModifying = false);
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
