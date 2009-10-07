using System.Collections.Generic;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game
{
	abstract class PlayerOwned : Actor
	{
		public Animation animation;
		protected int2 location;

		public UnitMission currentOrder = null;
		public UnitMission nextOrder = null;

		protected PlayerOwned( Game game, string name, int2 location )
			: base( game )
		{
			animation = new Animation( name );
			this.location = location;
		}

		public override IEnumerable<Pair<Sprite, float2>> CurrentImages { get { yield return Pair.New( animation.Image, 24 * (float2)location ); } }
	}
}
 