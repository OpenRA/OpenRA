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

using System.Diagnostics;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class PerfDebugLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public PerfDebugLogic(Widget widget, WorldRenderer worldRenderer)
		{
			var perfGraph = widget.Get("GRAPH_BG");
			perfGraph.IsVisible = () => Game.Settings.Debug.PerfGraph;

			var perfText = widget.Get<LabelWidget>("PERF_TEXT");
			perfText.IsVisible = () => Game.Settings.Debug.PerfText;

			var fpsTimer = Stopwatch.StartNew();
			var fpsReferenceFrame = 0;
			var fps = 0;
			perfText.GetText = () =>
			{
				var elapsed = fpsTimer.ElapsedMilliseconds;
				if (elapsed > 1000)
				{
					// Round to closest integer
					fps = (int)(1000.0f * (Game.RenderFrame - fpsReferenceFrame) / fpsTimer.ElapsedMilliseconds + 0.5f);
					fpsTimer.Restart();
					fpsReferenceFrame = Game.RenderFrame;
				}

				var wfbSize = Game.Renderer.WorldFrameBufferSize;
				var viewportSize = worldRenderer.Viewport.Rectangle.Size;
				return $"FPS: {fps}\nTick {Game.LocalTick} @ {PerfHistory.Items["tick_time"].Average(Game.Settings.Debug.Samples):F1} ms\n" +
					$"Render {Game.RenderFrame} @ {PerfHistory.Items["render"].Average(Game.Settings.Debug.Samples):F1} ms\n" +
					$"Batches: {PerfHistory.Items["batches"].LastValue}\n" +
					$"Viewport Size: {viewportSize.Width} x {viewportSize.Height} / {Game.Renderer.WorldDownscaleFactor}\n" +
					$"WFB Size: {wfbSize.Width} x {wfbSize.Height}";
			};
		}
	}
}
