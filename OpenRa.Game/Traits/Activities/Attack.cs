using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

		public void Tick(Actor self, Mobile mobile)
		{
			if (Target.IsDead)
			{
				mobile.InternalSetActivity(NextActivity);
				return;
			}

			if ((Target.Location - self.Location).LengthSquared >= Range * Range)
			{
				mobile.InternalSetActivity(new Move(Target, Range));
				mobile.QueueActivity(this);
				return;
			}

			var desiredFacing = Util.GetFacing((Target.Location - self.Location).ToFloat2(), 0);
			var renderUnit = self.traits.WithInterface<RenderUnit>().First();

			if (Util.QuantizeFacing(mobile.facing, renderUnit.anim.CurrentSequence.Length) 
				!= Util.QuantizeFacing(desiredFacing, renderUnit.anim.CurrentSequence.Length))
			{
				mobile.InternalSetActivity(new Turn(desiredFacing));
				mobile.QueueActivity(this);
				return;
			}

			var attack = self.traits.WithInterface<AttackBase>().First();
			attack.target = Target;
			attack.DoAttack(self);
		}

		public void Cancel(Actor self, Mobile mobile)
		{
			mobile.InternalSetActivity(null);
		}
	}
}
