#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Effects
{
	public class FloatingText : IEffect, IEffectAnnotation
	{
		static readonly WVec Velocity = new WVec(0, 0, 86);

		readonly SpriteFont font;
		readonly string text;
		readonly Color color;
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

		void IEffect.Tick(World world)
		{
			if (--remaining <= 0)
				world.AddFrameEndTask(w => w.Remove(this));

			pos += Velocity;
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr) { return SpriteRenderable.None; }

		IEnumerable<IRenderable> IEffectAnnotation.RenderAnnotation(WorldRenderer wr)
		{
			if (wr.World.FogObscures(pos) || wr.World.ShroudObscures(pos))
				yield break;

			yield return new TextAnnotationRenderable(font, pos, 0, color, text);
		}

		public static string FormatCashTick(int cashAmount)
		{
			return $"{(cashAmount < 0 ? "-" : "+")}${Math.Abs(cashAmount)}";
		}
	}
}
