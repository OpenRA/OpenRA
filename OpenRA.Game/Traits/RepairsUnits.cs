
using System;

namespace OpenRA.Traits
{

	public class RepairsUnitsInfo : StatelessTraitInfo<RepairsUnits>
	{
		public readonly float URepairPercent = 0.2f;
		public readonly int URepairStep = 10;	
	}
	
	public class RepairsUnits{}
}
