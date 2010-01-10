
namespace OpenRa.Game.Traits
{
	class MineImmuneInfo : ITraitInfo
	{
		public object Create(Actor self) { return new MineImmune(self); }
	}

	class MineImmune
	{
		public MineImmune(Actor self) { }
	}
}
