using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class TargetableBuildingInfo : TargetableInfo, ITraitPrerequisite<BuildingInfo>
	{
		public override object Create( ActorInitializer init ) { return new TargetableBuilding( this ); }
	}

	class TargetableBuilding : Targetable
	{
		public TargetableBuilding( TargetableBuildingInfo info )
			: base( info )
		{
		}

		public override IEnumerable<int2> TargetableSquares( Actor self )
		{
			return self.Trait<Building>().OccupiedCells();
		}
	}
}
