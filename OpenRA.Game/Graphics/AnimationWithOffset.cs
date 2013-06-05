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
		public readonly Animation Animation;
		public readonly Func<WVec> OffsetFunc;
		public readonly Func<bool> DisableFunc;
		public readonly Func<WPos, int> ZOffset;

		public AnimationWithOffset(Animation a, Func<WVec> offset, Func<bool> disable)
			: this(a, offset, disable, null) { }

		public AnimationWithOffset(Animation a, Func<WVec> offset, Func<bool> disable, int zOffset)
			: this(a, offset, disable, _ => zOffset) { }

		public AnimationWithOffset(Animation a, Func<WVec> offset, Func<bool> disable, Func<WPos, int> zOffset)
		{
			this.Animation = a;
			this.OffsetFunc = offset;
			this.DisableFunc = disable;
			this.ZOffset = zOffset;
		}

		public IRenderable Image(Actor self, WorldRenderer wr, PaletteReference pal)
		{
			return Image(self, wr, pal, 1f);
		}

		public IRenderable Image(Actor self, WorldRenderer wr, PaletteReference pal, float scale)
		{
			var p = self.CenterPosition;
			if (OffsetFunc != null)
				p += OffsetFunc();

			var z = (ZOffset != null) ? ZOffset(p) : 0;
			return new SpriteRenderable(Animation.Image, p, z, pal, scale);
		}

		public static implicit operator AnimationWithOffset(Animation a)
		{
			return new AnimationWithOffset(a, null, null, null);
		}
	}
}

