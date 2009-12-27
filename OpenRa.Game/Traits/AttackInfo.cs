using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class AttackInfo
	{
		public Actor Attacker;
		public WarheadInfo Warhead;
		public int Damage;
		public DamageState DamageState;
		public bool DamageStateChanged;
	}
}
