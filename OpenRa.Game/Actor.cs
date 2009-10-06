using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using OpenRa.FileFormats;

using System.Windows.Forms;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game
{
	abstract class Actor
	{
		public readonly Game game;

		public Player owner;
		public abstract IEnumerable<Pair<Sprite,float2>> CurrentImages { get; }
		public virtual void Tick(Game game, int t) { }

		protected Actor(Game game)
		{
			this.game = game;
		}
	}
}
