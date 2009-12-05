
namespace OpenRa.Game.Traits
{
	class RenderBuildingOre : RenderBuilding
	{
		public RenderBuildingOre(Actor self)
			: base(self)
		{
			Make( () => anim.PlayFetchIndex("idle", 
				() => (int)(5 * self.Owner.GetSiloFullness())), self);
		}
	}
}
