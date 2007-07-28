using System;
using System.Collections.Generic;
using System.Text;

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

		public override float2 RenderLocation
		{
			get { return 24.0f * (float2)location; }
		}

		public override Sprite[] CurrentImages { get { return animation.Images; } }
	}
}
