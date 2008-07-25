
namespace OpenRa.Game
{
	class Refinery : Building
	{
		public Refinery( int2 location, Player owner, Game game )
			: base( "proc", location, owner, game )
		{
			animation.PlayThen("make", () =>
			{
				animation.PlayRepeating("idle");

				game.world.AddFrameEndTask( _ =>
				{
					Unit harvester = new Unit( "harv", location + new int2( 1, 2 ), owner, game );
					harvester.facing = 8;
					game.world.Add(harvester);
					game.controller.orderGenerator = harvester;
				});
			});
		}
	}
}
