
using System;

namespace OpenRA.Traits
{

	class HasUnitOnBuildInfo : ITraitInfo
	{
		public readonly string Unit = null;
		public readonly string InitialActivity = null;
		public readonly int2 SpawnOffset = int2.Zero;
		public readonly int Facing = 0;
		
		public object Create( Actor self ) { return new HasUnitOnBuild(self); }
	}

	public class HasUnitOnBuild
	{
		
		public HasUnitOnBuild(Actor self)
		{
			var info = self.Info.Traits.Get<HasUnitOnBuildInfo>();
			
			self.World.AddFrameEndTask(
				w =>
				{
					var unit = w.CreateActor(info.Unit, self.Location 
						+ info.SpawnOffset, self.Owner);
					var unitTrait = unit.traits.Get<Unit>();
					unitTrait.Facing = info.Facing;
					//unit.QueueActivity( new Harvest() );
				});
		}
	}
}
