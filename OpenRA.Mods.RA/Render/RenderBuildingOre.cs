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

	class RenderBuildingOre : RenderBuilding, INotifyBuildComplete
	{
		public RenderBuildingOre( ActorInitializer init )
			: base(init)
		{
		}

		public void BuildingComplete( Actor self )
		{
			anim.PlayFetchIndex( "idle", 
				() => (int)( 4.9 * self.Owner.PlayerActor.Trait<PlayerResources>().GetSiloFullness() ) );
		}
	}
}
