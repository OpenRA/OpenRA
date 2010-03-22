
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
				
					if (info.InitialActivity != null)
					{
						foreach (var mod in Game.ModAssemblies)
						{
							var fullTypeName = mod.Second + "." + info.InitialActivity;
							var activity = (IActivity)mod.First.CreateInstance(fullTypeName);
							if (activity == null) continue;

							unit.QueueActivity( activity );
							return;
						}
		
						throw new InvalidOperationException("Cannot locate Activity: `{0}`".F(info.InitialActivity));
					}
				});
		}
	}
}
