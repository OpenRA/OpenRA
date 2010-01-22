using System.Linq;

namespace OpenRa.Traits
{
	class ProvidesRadarInfo : StatelessTraitInfo<ProvidesRadar> {}

	class ProvidesRadar : ITick
	{
		public bool IsActive { get; private set; }

		public void Tick(Actor self) { IsActive = UpdateActive(self); }

		bool UpdateActive(Actor self)
		{
			// Check if powered
			var b = self.traits.Get<Building>();
			if (b != null && b.Disabled) return false;

			var isJammed = self.World.Actors.Any(a => a.traits.Contains<JamsRadar>()
				&& self.Owner != a.Owner
				&& (self.Location - a.Location).Length < a.Info.Traits.Get<JamsRadarInfo>().Range);

			return !isJammed;
		}
	}
}
