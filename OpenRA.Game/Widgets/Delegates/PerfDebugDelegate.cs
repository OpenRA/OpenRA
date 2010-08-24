#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Support;

namespace OpenRA.Widgets.Delegates
{
	public class PerfDebugDelegate : IWidgetDelegate
	{
		public PerfDebugDelegate()
		{
			var r = Widget.RootWidget;
			var perfRoot = r.GetWidget("PERF_BG");
			perfRoot.IsVisible = () => perfRoot.Visible && Game.Settings.Debug.PerfGraph;

			// Perf text
			var perfText = perfRoot.GetWidget<LabelWidget>("TEXT");
			perfText.GetText = () => "Render {0} ({5}={2:F1} ms)\nTick {4} ({3:F1} ms)".F(
					Game.RenderFrame,
					Game.orderManager.FrameNumber,
					PerfHistory.items["render"].LastValue,
					PerfHistory.items["tick_time"].LastValue,
					Game.LocalTick,
					PerfHistory.items["batches"].LastValue);
		}
	}
}
