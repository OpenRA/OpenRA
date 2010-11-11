using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.UnitStances
{
	public class AssignUnitStanceInfo : TraitInfo<AssignUnitStance>
	{
		
	}

	public class AssignUnitStance : INotifyProduction
	{
		public void UnitProduced(Actor self, Actor other, int2 exit)
		{
			var stance = UnitStance.GetActive(self);
			if (stance == null)
				return;

			var target = other.TraitsImplementing<UnitStance>().Where(t => t.GetType() == stance.GetType()).FirstOrDefault();
			if (target == null)
				return;

			target.Activate(other);
		}
	}
}
