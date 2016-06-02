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

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render trait for buildings that change the sprite according to the remaining resource storage capacity across all depots.")]
	class WithSiloAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<RenderSpritesInfo>
	{
		[Desc("Sequence to use for resources-dependent 'stages'."), SequenceReference]
		public readonly string Sequence = "stages";

		[Desc("Internal resource stages. Does not have to match number of sequence frames.")]
		public readonly int Stages = 10;

		public object Create(ActorInitializer init) { return new WithSiloAnimation(init, this); }
	}

	class WithSiloAnimation : INotifyBuildComplete, INotifyOwnerChanged
	{
		readonly WithSiloAnimationInfo info;
		readonly WithSpriteBody wsb;
		PlayerResources playerResources;

		public WithSiloAnimation(ActorInitializer init, WithSiloAnimationInfo info)
		{
			this.info = info;
			wsb = init.Self.Trait<WithSpriteBody>();
			playerResources = init.Self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		public void BuildingComplete(Actor self)
		{
			wsb.DefaultAnimation.PlayFetchIndex(wsb.NormalizeSequence(self, info.Sequence),
				() => playerResources.ResourceCapacity != 0
				? ((info.Stages * wsb.DefaultAnimation.CurrentSequence.Length - 1) * playerResources.Resources) / (info.Stages * playerResources.ResourceCapacity)
					: 0);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();

			wsb.DefaultAnimation.PlayFetchIndex(wsb.NormalizeSequence(self, info.Sequence),
				() => playerResources.ResourceCapacity != 0
				? ((info.Stages * wsb.DefaultAnimation.CurrentSequence.Length - 1) * playerResources.Resources) / (info.Stages * playerResources.ResourceCapacity)
					: 0);
		}
	}
}
