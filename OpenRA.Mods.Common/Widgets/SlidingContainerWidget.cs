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
using System.Drawing;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class SlidingContainerWidget : Widget
	{
		public int2 OpenOffset = int2.Zero;
		public int2 ClosedOffset = int2.Zero;
		public int AnimationLength = 0;
		public Func<bool> IsOpen = () => false;
		public Action AfterOpen = () => {};
		public Action AfterClose = () => {};

		int2 offset;
		int frame;

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			// Start in the closed position
			offset = ClosedOffset;
		}

		public override void Tick()
		{
			var open = IsOpen();

			var targetFrame = open ? AnimationLength : 0;
			if (frame == targetFrame)
				return;

			// Update child origin
			frame += open ? 1 : -1;
			offset = int2.Lerp(ClosedOffset, OpenOffset, frame, AnimationLength);

			// Animation is complete
			if (frame == targetFrame)
			{
				if (open)
					AfterOpen();
				else
					AfterClose();
			}
		}

		public override Rectangle EventBounds { get { return Rectangle.Empty; } }
		public override int2 ChildOrigin { get { return RenderOrigin + offset; } }
	}
}
