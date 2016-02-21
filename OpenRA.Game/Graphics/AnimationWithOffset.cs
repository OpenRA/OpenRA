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
			Animation = a;
			OffsetFunc = offset;
			DisableFunc = disable;
			ZOffset = zOffset;
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr, PaletteReference pal, float scale)
		{
			var center = self.CenterPosition;
			var offset = OffsetFunc != null ? OffsetFunc() : WVec.Zero;

			var z = (ZOffset != null) ? ZOffset(center + offset) : 0;
			return Animation.Render(center, offset, z, pal, scale);
		}

		public static implicit operator AnimationWithOffset(Animation a)
		{
			return new AnimationWithOffset(a, null, null, null);
		}
	}
}