#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Displays the fill status of PlayerResources with an extra sprite overlay on the actor.")]
	class WithResourcesInfo : ITraitInfo, Requires<RenderSimpleInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "resources";

		public object Create(ActorInitializer init) { return new WithResources(init.self, this); }
	}

	class WithResources : INotifyBuildComplete, INotifySold, INotifyCapture, INotifyDamageStateChanged
	{
		WithResourcesInfo info;
		Animation anim;
		RenderSimple rs;
		PlayerResources playerResources;
		bool buildComplete;

		public WithResources(Actor self, WithResourcesInfo info)
		{
			this.info = info;
			rs = self.Trait<RenderSimple>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();

			anim = new Animation(self.World, rs.GetImage(self));
			anim.PlayFetchIndex(info.Sequence,
			                    () => playerResources.ResourceCapacity != 0
			                    ? ((10 * anim.CurrentSequence.Length - 1) * playerResources.Resources) / (10 * playerResources.ResourceCapacity)
			                    : 0);

			rs.Add("resources_{0}".F(info.Sequence), new AnimationWithOffset(
				anim, null, () => !buildComplete, 1024));
		}

		public void BuildingComplete( Actor self )
		{
			buildComplete = true;
		}

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (anim.CurrentSequence != null)
				anim.ReplaceAnim(rs.NormalizeSequence(self, info.Sequence));
		}

		public void OnCapture (Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		public void Selling(Actor self) { rs.Remove("resources_{0}".F(info.Sequence)); }
		public void Sold(Actor self) { }
	}
}
