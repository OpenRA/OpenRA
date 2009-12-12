using System.Linq;

namespace OpenRa.Game.Traits.Activities
{
	/* non-turreted attack */
	class Attack : IActivity
	{
		Actor Target;
		int Range;

		public Attack(Actor target, int range)
		{
			Target = target;
			Range = range;
		}

		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			var unit = self.traits.Get<Unit>();

			if (Target == null || Target.IsDead)
				return NextActivity;

			if ((Target.Location - self.Location).LengthSquared >= Range * Range)
				return new Move( Target, Range ) { NextActivity = this };

			var desiredFacing = Util.GetFacing((Target.Location - self.Location).ToFloat2(), 0);
			var renderUnit = self.traits.WithInterface<RenderUnit>().FirstOrDefault();
			var numDirs = (renderUnit != null)
				? renderUnit.anim.CurrentSequence.Length : 8;

			if (Util.QuantizeFacing(unit.Facing, numDirs) 
				!= Util.QuantizeFacing(desiredFacing, numDirs))
			{
				return new Turn( desiredFacing ) { NextActivity = this };
			}

			var attack = self.traits.WithInterface<AttackBase>().First();
			attack.target = Target;
			attack.DoAttack(self);
			return null;
		}

		public void Cancel(Actor self)
		{
			Target = null;
		}
	}
}
