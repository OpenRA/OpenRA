
namespace OpenRa.GameRules
{
	public class WarheadInfo
	{
		public readonly int Spread = 1;
		public readonly float[] Verses = { 1, 1, 1, 1, 1 };
		public readonly bool Wall = false;
		public readonly bool Wood = false;
		public readonly bool Ore = false;
		public readonly int Explosion = 0;
		public readonly int InfDeath = 0;
		public readonly string ImpactSound = null;
		public readonly string WaterImpactSound = null;

		public float EffectivenessAgainst(ArmorType at) { return Verses[ (int)at ]; }
	}
}
