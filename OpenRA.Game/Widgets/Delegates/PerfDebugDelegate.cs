#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion
using System;
using OpenRA.FileFormats;
using OpenRA.Support;

namespace OpenRA.Widgets.Delegates
{
	public class PerfDebugDelegate : IWidgetDelegate
	{
		public PerfDebugDelegate()
		{
			var r = Chrome.rootWidget;
			var perfRoot = r.GetWidget("PERF_BG");
			perfRoot.IsVisible = () => {return (perfRoot.Visible && Game.Settings.PerfDebug);};
			
			// Perf text
			var perfText = perfRoot.GetWidget<LabelWidget>("TEXT");
			perfText.GetText = () => {
				return "Render {0} ({5}={2:F1} ms)\nTick {4} ({3:F1} ms)".F(
					Game.RenderFrame,
					Game.orderManager.FrameNumber,
					PerfHistory.items["render"].LastValue,
					PerfHistory.items["tick_time"].LastValue,
					Game.LocalTick,
					PerfHistory.items["batches"].LastValue);
			};
		}
	}
}
