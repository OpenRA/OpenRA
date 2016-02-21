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

using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class PerfDebugLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public PerfDebugLogic(Widget widget)
		{
			var perfGraph = widget.Get("GRAPH_BG");
			perfGraph.IsVisible = () => Game.Settings.Debug.PerfGraph;

			var perfText = widget.Get<LabelWidget>("PERF_TEXT");
			perfText.IsVisible = () => Game.Settings.Debug.PerfText;
			perfText.GetText = () =>
				"Tick {0} @ {1:F1} ms\nRender {2} @ {3:F1} ms\nBatches: {4}".F(
					Game.LocalTick, PerfHistory.Items["tick_time"].Average(Game.Settings.Debug.Samples),
					Game.RenderFrame, PerfHistory.Items["render"].Average(Game.Settings.Debug.Samples),
					PerfHistory.Items["batches"].LastValue);
		}
	}
}
