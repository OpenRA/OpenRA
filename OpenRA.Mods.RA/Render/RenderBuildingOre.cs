#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingOreInfo : RenderBuildingInfo
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingOre(init); }
	}

	class RenderBuildingOre : RenderBuilding, INotifyBuildComplete, INotifyCapture
	{
		PlayerResources PlayerResources;
		
		public RenderBuildingOre( ActorInitializer init )
			: base(init)
		{
			PlayerResources = init.self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		public void BuildingComplete( Actor self )
		{
			anim.PlayFetchIndex( "idle", 
				() => (49 * PlayerResources.Ore) / (10*PlayerResources.OreCapacity));
		}
		
		public void OnCapture (Actor self, Actor captor, Player oldOwner, Player newOwner)
		{		
			PlayerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}
	}
}
