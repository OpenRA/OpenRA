using System;

namespace OpenRa.Game.Traits.Activities
{
	class DeployMcv : IActivity
	{
		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			Game.world.AddFrameEndTask( _ =>
			{
				Game.world.Remove( self );
				Sound.Play("placbldg.aud");
				Sound.Play("build5.aud");
				Game.world.Add( new Actor( Rules.UnitInfo["fact"], self.Location - new int2( 1, 1 ), self.Owner ) );
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
