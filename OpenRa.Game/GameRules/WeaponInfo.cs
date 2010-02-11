
namespace OpenRa.GameRules
{
	public class WeaponInfo
	{
		public readonly int Burst = 1;
		public readonly bool Charges = false;
		public readonly int Damage = 0;
		public readonly string Projectile = "Invisible";
		public readonly int ROF = 1; // in 1/15 second units.
		public readonly float Range = 0;
		public readonly string Report = null;
		public readonly int Speed = -1;
		public readonly bool TurboBoost = false;
		public readonly string Warhead = null;

		public readonly bool RenderAsTesla = false;
		public readonly bool RenderAsLaser = false;
		public readonly bool UsePlayerColor = true;
		public readonly int BeamRadius = 1;
	}
}
