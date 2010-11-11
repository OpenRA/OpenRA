using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class UnitStanceNoneInfo : ITraitInfo
	{
		public readonly bool Default = false;

		public object Create(ActorInitializer init) { return new UnitStanceNone(init.self, this); }
	}
	public class UnitStanceNone : UnitStance
	{
		public readonly UnitStanceNoneInfo Info;

		public UnitStanceNone(Actor self, UnitStanceNoneInfo info)
		{
			Info = info;
			Active = (self.World.LocalPlayer == self.Owner || (self.Owner.IsBot && Game.IsHost)) ? Info.Default : false;
		}
	}
}