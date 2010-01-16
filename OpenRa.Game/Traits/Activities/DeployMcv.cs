using System;

namespace OpenRa.Traits.Activities
{
	class DeployMcv : IActivity
	{
		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			Game.world.AddFrameEndTask( _ =>
			{
				self.Health = 0;
				Game.world.Remove( self );
				if (self.Owner == Game.LocalPlayer)
				{
					Sound.Play("placbldg.aud");
					Sound.Play("build5.aud");
				}
				Game.world.CreateActor( "fact", self.Location - new int2( 1, 1 ), self.Owner );
			} );
			return this;
		}

		public void Cancel( Actor self )
		{
			// Cancel can't happen between this being moved to the head of the list, and it being Ticked.
			throw new InvalidOperationException( "DeployMcvAction: Cancel() should never occur." );
		}
	}
}
