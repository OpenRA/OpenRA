using System;

namespace OpenRa.Traits.Activities
{
	class DeployMcv : IActivity
	{
		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			self.World.AddFrameEndTask( _ =>
			{
				self.Health = 0;
				self.World.Remove( self );
				Sound.PlayToPlayer(self.Owner, "placbldg.aud");
				Sound.PlayToPlayer(self.Owner, "build5.aud");
				self.World.CreateActor( "fact", self.Location - new int2( 1, 1 ), self.Owner );
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
