#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Support;

namespace OpenRA.Widgets
{
	public class TimerWidget : Widget
	{
		public Stopwatch Stopwatch;
		
		public TimerWidget ()
		{
			IsVisible = () => Game.Settings.ShowGameTimer;
		}

		public override void DrawInner(World world)
		{
			var s = WorldUtils.FormatTime(Game.LocalTick);
			var f = Game.chrome.renderer.TitleFont;
			var size = f.Measure(s);
			f.DrawText(s, new float2(RenderBounds.Left - size.X / 2, RenderBounds.Top - 20), Color.White);
		}
	}
}

