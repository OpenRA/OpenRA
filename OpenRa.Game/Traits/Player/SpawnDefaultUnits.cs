
namespace OpenRa.Traits
{
	class SpawnDefaultUnitsInfo : StatelessTraitInfo<SpawnDefaultUnits> { }

	class SpawnDefaultUnits : IOnGameStart
	{
		public void SpawnStartingUnits(Player p, int2 sp)
		{
			p.PlayerActor.World.CreateActor("mcv", sp, p);
		}
	}
}
