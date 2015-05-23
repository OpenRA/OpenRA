#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Displays the fill status of PlayerResources with an extra sprite overlay on the actor.")]
	class WithResourcesInfo : ITraitInfo, Requires<RenderSimpleInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "resources";

		public object Create(ActorInitializer init) { return new WithResources(init.Self, this); }
	}

	class WithResources : INotifyBuildComplete, INotifySold, INotifyOwnerChanged, INotifyDamageStateChanged
	{
		readonly WithResourcesInfo info;
		readonly AnimationWithOffset anim;
		readonly RenderSimple rs;

		PlayerResources playerResources;
		bool buildComplete;

		public WithResources(Actor self, WithResourcesInfo info)
		{
			this.info = info;
			rs = self.Trait<RenderSimple>();
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
				anim.Animation.ReplaceAnim(rs.NormalizeSequence(self, info.Sequence));
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		public void Selling(Actor self) { rs.Remove(anim); }
		public void Sold(Actor self) { }
	}
}
