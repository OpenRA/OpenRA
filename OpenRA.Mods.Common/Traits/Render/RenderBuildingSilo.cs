#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class RenderBuildingSiloInfo : RenderBuildingInfo
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingSilo(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			// Show a static frame instead of animating all of the fullness states
			var anim = new Animation(init.World, image, () => 0);
			anim.PlayFetchIndex(Sequence, () => 0);

			yield return new SpriteActorPreview(anim, WVec.Zero, 0, p, rs.Scale);
		}
	}

	class RenderBuildingSilo : RenderBuilding, INotifyBuildComplete, INotifyOwnerChanged
	{
		readonly RenderBuildingSiloInfo info;
		PlayerResources playerResources;

		public RenderBuildingSilo(ActorInitializer init, RenderBuildingSiloInfo info)
			: base(init, info)
		{
			this.info = info;
			playerResources = init.Self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		public override void BuildingComplete(Actor self)
		{
			var animation = RenderSprites.NormalizeSequence(DefaultAnimation, self.GetDamageState(), info.Sequence);

			DefaultAnimation.PlayFetchIndex(animation,
				() => playerResources.ResourceCapacity != 0
				? ((10 * DefaultAnimation.CurrentSequence.Length - 1) * playerResources.Resources) / (10 * playerResources.ResourceCapacity)
					: 0);
		}

		public override void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
			base.OnOwnerChanged(self, oldOwner, newOwner);
		}
	}
}
