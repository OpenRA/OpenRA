using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class AttackMove : Move
	{
		public AttackMove(int2 destination) : base(destination) { }
		public AttackMove(int2 destination, int nearEnough) : base(destination, nearEnough) { }
		public AttackMove(int2 destination, Actor ignoreBuilding) : base(destination, ignoreBuilding) { }
		public AttackMove(Actor target, int range) : base(target, range) { }
		public AttackMove(Target target, int range) : base(target, range) { }
		public AttackMove(Func<List<int2>> getPath) : base(getPath) { }
	}
}
