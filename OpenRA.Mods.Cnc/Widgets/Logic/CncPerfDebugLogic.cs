#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncPerfDebugLogic
	{
		[ObjectCreator.UseCtor]
		public CncPerfDebugLogic(Widget widget)
		{
			// Performance info
			var perfRoot = widget.Get("PERFORMANCE_INFO");
			perfRoot.IsVisible = () => Game.Settings.Debug.PerfGraph;
			var text = perfRoot.Get<LabelWidget>("PERF_TEXT");
			text.IsVisible = () => Game.Settings.Debug.PerfText;
			text.GetText = () =>
				"Tick {0} @ {1:F1} ms\nRender {2} @ {3:F1} ms\nBatches: {4}".F(
				Game.LocalTick, PerfHistory.items["tick_time"].Average(Game.Settings.Debug.Samples),
				Game.RenderFrame, PerfHistory.items["render"].Average(Game.Settings.Debug.Samples),
				PerfHistory.items["batches"].LastValue);
		}
	}
}
