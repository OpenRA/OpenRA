#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingOreInfo : RenderBuildingInfo
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingOre(init, this); }
	}

	class RenderBuildingOre : RenderBuilding, INotifyBuildComplete, INotifyCapture
	{
		PlayerResources PlayerResources;
		
		public RenderBuildingOre( ActorInitializer init, RenderBuildingInfo info )
			: base(init, info)
		{
			PlayerResources = init.self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		public void BuildingComplete( Actor self )
		{
			anim.PlayFetchIndex("idle",
				() => PlayerResources.OreCapacity != 0
					? (49 * PlayerResources.Ore) / (10 * PlayerResources.OreCapacity)
					: 0);
		}
		
		public void OnCapture (Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			PlayerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}
	}
}
