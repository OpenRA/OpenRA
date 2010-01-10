using System;

namespace OpenRa.Game.Traits
{
	class RenderBuildingOreInfo : RenderBuildingInfo
	{
		public override object Create(Actor self) { return new RenderBuildingOre(self); }
	}

	class RenderBuildingOre : RenderBuilding, INotifyBuildComplete
	{
		public RenderBuildingOre(Actor self)
			: base(self)
		{
		}

		public void BuildingComplete( Actor self )
		{
			anim.PlayFetchIndex( "idle", () => (int)( 4.9 * self.Owner.GetSiloFullness() ) );
		}
	}
}
