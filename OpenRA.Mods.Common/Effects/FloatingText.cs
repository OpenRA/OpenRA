#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;

namespace OpenRA.Mods.Common.Effects
{
	public class FloatingText : IEffect, IEffectAboveShroud
	{
		static readonly WVec Velocity = new WVec(0, 0, 86);

		readonly SpriteFont font;
		readonly string text;
		Color color;
		int remaining;
		WPos pos;

		public FloatingText(WPos pos, Color color, string text, int duration)
		{
			font = Game.Renderer.Fonts["TinyBold"];
			this.pos = pos;
			this.color = color;
			this.text = text;
			remaining = duration;
		}

		public void Tick(World world)
		{
			if (--remaining <= 0)
				world.AddFrameEndTask(w => w.Remove(this));

			pos += Velocity;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr) { return SpriteRenderable.None; }

		public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr)
		{
			if (wr.World.FogObscures(pos) || wr.World.ShroudObscures(pos))
				yield break;

			// Arbitrary large value used for the z-offset to try and ensure the text displays above everything else.
			yield return new TextRenderable(font, pos, 4096, color, text);
		}

		public static string FormatCashTick(int cashAmount)
		{
			return "{0}${1}".F(cashAmount < 0 ? "-" : "+", Math.Abs(cashAmount));
		}
	}
}
