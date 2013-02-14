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

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class PerfDebugLogic
	{
		public PerfDebugLogic()
		{
			var r = Ui.Root;
			var perfRoot = r.Get("PERF_BG");
			perfRoot.IsVisible = () => perfRoot.Visible && Game.Settings.Debug.PerfGraph;

			// Perf text
			var perfText = perfRoot.Get<LabelWidget>("TEXT");
			perfText.IsVisible = () => Game.Settings.Debug.PerfText;
			perfText.GetText = () => "Render {0} ({5}={2:F1} ms)\nTick {4} ({3:F1} ms)".F(
					Game.RenderFrame,
					Game.NetFrameNumber,
					PerfHistory.items["render"].Average(Game.Settings.Debug.Samples),
					PerfHistory.items["tick_time"].Average(Game.Settings.Debug.Samples),
					Game.LocalTick,
					PerfHistory.items["batches"].LastValue);
		}
	}
}
