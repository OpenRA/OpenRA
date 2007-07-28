using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using OpenRa.FileFormats;
using BluntDirectX.Direct3D;
using System.Windows.Forms;

namespace OpenRa.Game
{
	abstract class Actor
	{
		public readonly Game game;

		public abstract float2 RenderLocation { get; }
		public Player owner;
		public abstract Sprite[] CurrentImages { get; }
		public virtual void Tick(Game game, int t) { }

		protected Actor(Game game)
		{
			this.game = game;
		}
	}
}
