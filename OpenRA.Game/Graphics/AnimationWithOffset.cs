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
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public class AnimationWithOffset
	{
		public Animation Animation;
		public Func<WorldRenderer, float2> OffsetFunc;
		public Func<bool> DisableFunc;
		public int ZOffset;

		public AnimationWithOffset(Animation a)
			: this(a, null, null)
		{
		}

		public AnimationWithOffset(Animation a, Func<WorldRenderer, float2> o, Func<bool> d)
		{
			this.Animation = a;
			this.OffsetFunc = o;
			this.DisableFunc = d;
		}

		public Renderable Image(Actor self, WorldRenderer wr, PaletteReference pal)
		{
			var p = self.CenterLocation;
			var loc = p.ToFloat2() - 0.5f * Animation.Image.size
				+ (OffsetFunc != null ? OffsetFunc(wr) : float2.Zero);
			var r = new Renderable(Animation.Image, loc, pal, p.Y);

			return ZOffset != 0 ? r.WithZOffset(ZOffset) : r;
		}

		public static implicit operator AnimationWithOffset(Animation a)
		{
			return new AnimationWithOffset(a);
		}
	}
}

