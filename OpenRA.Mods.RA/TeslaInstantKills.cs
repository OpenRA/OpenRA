using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class TeslaInstantKillsInfo : TraitInfo<TeslaInstantKills> { }

	class TeslaInstantKills : IDamageModifier
	{
		public float GetDamageModifier( WarheadInfo warhead )
		{
			if( warhead.InfDeath == 5 )
				return 1000f;
			return 1f;
		}
	}
}
