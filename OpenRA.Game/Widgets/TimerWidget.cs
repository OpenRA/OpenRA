#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
			IsVisible = () => Game.Settings.Game.MatchTimer;
		}

		public override void Draw()
		{
			var s = WidgetUtils.FormatTime(Game.LocalTick);
			var size = Game.Renderer.Fonts["Title"].Measure(s);
            var pos = new float2(RenderBounds.Left - size.X / 2, RenderBounds.Top - 20);

            Game.Renderer.Fonts["Title"].DrawTextWithContrast(s, pos, Color.White, Color.Black, 1);
		}
	}
}

