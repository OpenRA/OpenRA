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
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class CashTick : IEffect
	{
		string s;
		int lifetime = 50;
		int remaining;
		int velocity;
		float2 pos;
		Color color;
		
		public CashTick(int value, int lifetime, int velocity, float2 pos, Color color)
		{
			this.color = color;
			this.lifetime = lifetime;
			this.velocity = velocity;
			s = "${0}".F(value);
			this.pos = pos - 0.5f*Game.Renderer.BoldFont.Measure(s).ToFloat2();
			remaining = lifetime;
		}

		public void Tick(World world)
		{
			if (--remaining <= 0)
				world.AddFrameEndTask(w => w.Remove(this));
			pos.Y -= velocity;
		}

		public IEnumerable<Renderable> Render()
		{
			Game.Renderer.BoldFont.DrawTextWithContrast(s, pos - Game.viewport.Location, color, Color.Black,1);
			yield break;
		}
	}
}
