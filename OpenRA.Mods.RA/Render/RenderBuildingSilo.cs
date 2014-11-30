#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Mods.Common.Graphics;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingSiloInfo : RenderBuildingInfo
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingSilo(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			// Show a static frame instead of animating all of the fullness states
			var anim = new Animation(init.World, image, () => 0);
			anim.PlayFetchIndex("idle", () => 0);

			yield return new SpriteActorPreview(anim, WVec.Zero, 0, p, rs.Scale);
		}
	}

	class RenderBuildingSilo : RenderBuilding, INotifyBuildComplete, INotifyOwnerChanged
	{
		PlayerResources playerResources;

		public RenderBuildingSilo(ActorInitializer init, RenderBuildingSiloInfo info)
			: base(init, info)
		{
			playerResources = init.self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		public override void BuildingComplete(Actor self)
		{
			var animation = (self.GetDamageState() >= DamageState.Heavy) ? "damaged-idle" : "idle";

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
