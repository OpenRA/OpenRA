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

			var isJammed = self.World.Queries.WithTrait<JamsRadar>().Any(a => self.Owner != a.Actor.Owner
				&& (self.Location - a.Actor.Location).Length < a.Actor.Info.Traits.Get<JamsRadarInfo>().Range);

			return !isJammed;
		}
	}
}
