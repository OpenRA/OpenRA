
namespace OpenRa.Game.Traits.Activities
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

			var canMove = !self.traits.Contains<Building>();
			var inRange = ( Target.Location - self.Location ).LengthSquared < Range * Range;

			if( canMove && !inRange )
				return new Move( Target, Range ) { NextActivity = this };

			return null;
		}

		public void Cancel(Actor self)
		{
			Target = null;
		}
	}
}
