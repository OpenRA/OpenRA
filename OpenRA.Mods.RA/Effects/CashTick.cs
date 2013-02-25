#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class CashTick : IEffect
	{
		readonly string s;
		int remaining;
		readonly int velocity;
		PPos pos;
		readonly float2 offset;
		readonly Color color;
		readonly SpriteFont font;

		static string FormatCashAmount(int x) { return "{0}${1}".F(x < 0 ? "-" : "+", x); }

		public CashTick(int value, int lifetime, int velocity, PPos pos, Color color)
			: this( FormatCashAmount(value), lifetime, velocity, pos, color ) { }

		public CashTick(string value, int lifetime, int velocity, PPos pos, Color color)
		{
			this.color = color;
			this.velocity = velocity;
			this.pos = pos;
			s = value;
			font = Game.Renderer.Fonts["TinyBold"];
			offset = 0.5f*font.Measure(s).ToFloat2();
			remaining = lifetime;
		}

		public void Tick(World world)
		{
			if (--remaining <= 0)
				world.AddFrameEndTask(w => w.Remove(this));
			pos -= new PVecInt(0, velocity);
		}

		public IEnumerable<Renderable> Render(WorldRenderer wr)
		{
			font.DrawTextWithContrast(s, Game.viewport.Zoom*(pos.ToFloat2() - Game.viewport.Location) - offset, color, Color.Black,1);
			yield break;
		}
	}
}
