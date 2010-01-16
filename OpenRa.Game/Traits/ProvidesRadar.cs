
namespace OpenRa.Traits
{
	class ProvidesRadarInfo : StatelessTraitInfo<ProvidesRadar> {}

	class ProvidesRadar
	{
		public bool IsActive(Actor self)
		{
			// TODO: Check for nearby MRJ
			
			// Check if powered
			var b = self.traits.Get<Building>();
			if (b != null && b.Disabled)
				return false;
			
			return true;
		}
	}
}
