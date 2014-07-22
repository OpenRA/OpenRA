#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Graphics;

namespace OpenRA.Mods.RA.Effects
{
	class CashTick : IEffect
	{
		readonly SpriteFont font;
		readonly string text;
		Color color;
		int remaining = 30;
		WPos pos;

		public CashTick(WPos pos, Color color, int value)
		{
			this.font = Game.Renderer.Fonts["TinyBold"];
			this.pos = pos;
			this.color = color;
			this.text = "{0}${1}".F(value < 0 ? "-" : "+", Math.Abs(value));
		}

		static readonly WVec velocity = new WVec(0,0,86);
		public void Tick(World world)
		{
			if (--remaining <= 0)
				world.AddFrameEndTask(w => w.Remove(this));

			pos += velocity;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.world.FogObscures(wr.world.Map.CellContaining(pos)))
				yield break;

			yield return new TextRenderable(font, pos, 0, color, text);
		}
	}
}
