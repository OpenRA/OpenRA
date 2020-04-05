#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class NetDebugLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public NetDebugLogic(Widget widget)
		{
			var netGraph = widget.Get("GRAPH_BG");
			netGraph.IsVisible = () => Game.Settings.Debug.NetGraph;

			var netText = widget.Get<LabelWidget>("NET_TEXT");
			netText.IsVisible = () => Game.Settings.Debug.NetText;

			netText.GetText = () =>
			{
				var history = NetHistory.GetHistory().FirstOrDefault();
				if (history == null)
					return "Waiting for net history";

				return string.Format(
					"Ticked: {0}\n" +
					"To Catchup: {1}\n" +
					"Self buffer size: {2}\n" +
					"{3}ms (+/- {4:F1}ms)\n" +
					"peak+delta: {5}ms",
					history.Ticked,
					history.CatchUpNetFrames,
					history.CurrentClientBufferSize,
					history.MeasuredLatency,
					history.MeasuredJitter,
					history.PeakJitter);

			};
		}
	}
}
