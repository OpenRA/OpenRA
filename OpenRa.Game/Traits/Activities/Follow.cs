
namespace OpenRa.Traits.Activities
{
	class Follow : IActivity
	{
		Actor Target;
		int Range;

		public Follow(Actor target, int range)
		{
			Target = target;
			Range = range;
		}

		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			if (Target == null || Target.IsDead)
				return NextActivity;

			var inRange = ( Target.Location - self.Location ).LengthSquared < Range * Range;

			if( !inRange )
				return new Move( Target, Range ) { NextActivity = this };

			return this;
		}

		public void Cancel(Actor self)
		{
			Target = null;
		}
	}
}
