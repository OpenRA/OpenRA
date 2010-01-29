using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits.Activities
{
	public class FlyAttack : IActivity
	{
		public IActivity NextActivity { get; set; }
		Actor Target;

		public FlyAttack(Actor target) { Target = target; }

		public IActivity Tick(Actor self)
		{
			if (Target == null || Target.IsDead) 
				return NextActivity;

			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return NextActivity;

			return Util.SequenceActivities(
				new Fly(Target.CenterLocation),
				new FlyTimed(50, 20),
				this);
		}

		public void Cancel(Actor self) { Target = null; NextActivity = null; }
	}

	public class FlyCircle : IActivity
	{
		public IActivity NextActivity { get; set; }
		int2 Target;
		bool isCanceled;

		public FlyCircle(int2 target) { Target = target; }

		public IActivity Tick(Actor self)
		{
			if (isCanceled)
				return NextActivity;

			return Util.SequenceActivities(
				new Fly(Util.CenterOfCell(Target)),
				new FlyTimed(50, 20),
				this);
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
