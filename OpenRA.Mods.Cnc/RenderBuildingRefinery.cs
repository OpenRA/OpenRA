﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.Cnc
{
	class RenderBuildingRefineryInfo : RenderBuildingInfo
	{
		public readonly WVec Offset = new WVec(1365, 896, 0);

		public override object Create(ActorInitializer init) { return new RenderBuildingRefinery(init, this); }
	}

	class RenderBuildingRefinery : RenderBuilding, INotifyBuildComplete, INotifySold, INotifyCapture
	{
		public Animation lights;
		PlayerResources playerResources;
		bool buildComplete;

		public RenderBuildingRefinery(ActorInitializer init, RenderBuildingRefineryInfo info)
			: base(init, info)
		{
			playerResources = init.self.Owner.PlayerActor.Trait<PlayerResources>();

			lights = new Animation(GetImage(init.self));
			lights.PlayFetchIndex("lights",
				() => playerResources.OreCapacity != 0
					? (59 * playerResources.Ore) / (10 * playerResources.OreCapacity)
					: 0);

			anims.Add("lights", new AnimationWithOffset(lights, () => info.Offset, () => !buildComplete, 1024));
		}

		public void BuildingComplete( Actor self )
		{
			buildComplete = true;
		}

		public override void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (lights.CurrentSequence != null)
				lights.ReplaceAnim(NormalizeSequence(self, "lights"));

			base.DamageStateChanged(self, e);
		}

		public void OnCapture (Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		public void Selling(Actor self) { anims.Remove("lights"); }
		public void Sold(Actor self) { }
	}
}
