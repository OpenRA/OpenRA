using OpenRa.GameRules;

namespace OpenRa.Traits
{
	public class AttackInfo
	{
		public Actor Attacker;
		public WarheadInfo Warhead;
		public int Damage;
		public DamageState DamageState;
		public bool DamageStateChanged;
	}
}
