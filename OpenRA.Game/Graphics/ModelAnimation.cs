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
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public readonly struct ModelAnimation
	{
		public readonly IModel Model;
		public readonly Func<WVec> OffsetFunc;
		public readonly Func<WRot> RotationFunc;
		public readonly Func<bool> DisableFunc;
		public readonly Func<uint> FrameFunc;
		public readonly bool ShowShadow;

		public ModelAnimation(IModel model, Func<WVec> offset, Func<WRot> rotation, Func<bool> disable, Func<uint> frame, bool showshadow)
		{
			Model = model;
			OffsetFunc = offset;
			RotationFunc = rotation;
			DisableFunc = disable;
			FrameFunc = frame;
			ShowShadow = showshadow;
		}

		public Rectangle ScreenBounds(WPos pos, WorldRenderer wr, float scale)
		{
			var r = Model.AggregateBounds;
			var offset = OffsetFunc != null ? OffsetFunc() : WVec.Zero;
			var xy = wr.ScreenPxPosition(pos) + wr.ScreenPxOffset(offset);

			return Rectangle.FromLTRB(
				xy.X + (int)(r.Left * scale),
				xy.Y + (int)(r.Top * scale),
				xy.X + (int)(r.Right * scale),
				xy.Y + (int)(r.Bottom * scale));
		}

		public bool IsVisible => DisableFunc == null || !DisableFunc();
	}
}
