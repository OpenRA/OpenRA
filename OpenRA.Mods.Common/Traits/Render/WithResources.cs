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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays the fill status of PlayerResources with an extra sprite overlay on the actor.")]
	class WithResourcesInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<RenderSpritesInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "resources";

		public object Create(ActorInitializer init) { return new WithResources(init.Self, this); }
	}

	class WithResources : INotifyBuildComplete, INotifySold, INotifyOwnerChanged, INotifyDamageStateChanged
	{
		readonly WithResourcesInfo info;
		readonly AnimationWithOffset anim;
		readonly RenderSprites rs;
		readonly WithSpriteBody wsb;

		PlayerResources playerResources;
		bool buildComplete;

		public WithResources(Actor self, WithResourcesInfo info)
		{
			this.info = info;
			rs = self.Trait<RenderSprites>();
			wsb = self.Trait<WithSpriteBody>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();

			var a = new Animation(self.World, rs.GetImage(self));
			a.PlayFetchIndex(info.Sequence, () =>
				playerResources.ResourceCapacity != 0 ?
				((10 * a.CurrentSequence.Length - 1) * playerResources.Resources) / (10 * playerResources.ResourceCapacity) :
				0);

			anim = new AnimationWithOffset(a, null, () => !buildComplete, 1024);
			rs.Add(anim);
		}

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (anim.Animation.CurrentSequence != null)
				anim.Animation.ReplaceAnim(wsb.NormalizeSequence(self, info.Sequence));
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		public void Selling(Actor self) { rs.Remove(anim); }
		public void Sold(Actor self) { }
	}
}
