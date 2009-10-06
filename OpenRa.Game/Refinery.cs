using OpenRa.Game.Graphics;
using System.Linq;

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

    class WarFactory : Building
    {
        Animation roof;

        public WarFactory(int2 location, Player owner, Game game)
            : base("weap", location, owner, game)
        {
            
            animation.PlayThen("make", () =>
                {
                    roof = new Animation("weap");
                    animation.PlayRepeating("idle");
                    roof.PlayRepeating("idle-top");
                });
        }

        public override Sprite[] CurrentImages
        {
            get
            {
                return (roof == null)
                    ? base.CurrentImages
                    : (base.CurrentImages.Concat( roof.Images ).ToArray());
            }
        }

        public override void Tick(Game game, int t)
        {
            base.Tick(game, t);
            if (roof != null) roof.Tick(t);
        }
    }
}
