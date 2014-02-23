#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA.Effects
{
	public class Beacon : IEffect
	{
		readonly Player owner;
		readonly WPos position;
		readonly string palettePrefix;
		readonly Animation arrow = new Animation("beacon");
		readonly Animation circles = new Animation("beacon");
		static readonly int maxArrowHeight = 512;
		int arrowHeight = maxArrowHeight;
		int arrowSpeed = 50;

		public Beacon(Player owner, WPos position, int duration, string palettePrefix)
		{
			this.owner = owner;
			this.position = position;
			this.palettePrefix = palettePrefix;

			arrow.Play("arrow");
			circles.Play("circles");

			owner.World.Add(new DelayedAction(duration, () => owner.World.Remove(this)));
		}

		public void Tick(World world)
		{
			arrowHeight += arrowSpeed;
			var clamped = arrowHeight.Clamp(0, maxArrowHeight);
			if (arrowHeight != clamped)
			{
				arrowHeight = clamped;
				arrowSpeed *= -1;
			}

			arrow.Tick();
			circles.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer r)
		{
			if (!owner.IsAlliedWith(owner.World.RenderPlayer))
				return SpriteRenderable.None;

			var palette = r.Palette(palettePrefix + owner.InternalName);
			return circles.Render(position, palette).Concat(arrow.Render(position + new WVec(0, 0, arrowHeight), palette));
		}
	}
}
