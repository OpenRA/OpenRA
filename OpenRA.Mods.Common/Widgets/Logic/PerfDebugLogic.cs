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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class PerfDebugLogic : ChromeLogic
	{
		readonly Stopwatch fpsTimer;
		readonly List<(int Frame, TimeSpan Time)> frameTimings = new(32);

		[ObjectCreator.UseCtor]
		public PerfDebugLogic(Widget widget, WorldRenderer worldRenderer)
		{
			var perfGraph = widget.Get("GRAPH_BG");
			perfGraph.IsVisible = () => Game.Settings.Debug.PerfGraph;

			var perfText = widget.Get<LabelWidget>("PERF_TEXT");
			perfText.IsVisible = () => Game.Settings.Debug.PerfText;

			fpsTimer = Stopwatch.StartNew();
			frameTimings.Add((Game.RenderFrame, TimeSpan.Zero));
			perfText.GetText = () =>
			{
				// Calculate FPS as a rolling average over the last ~1 second of frames.
				frameTimings.Add((Game.RenderFrame, fpsTimer.Elapsed));
				var cutoffTime = frameTimings[^1].Time - TimeSpan.FromSeconds(1);
				var firstIndexPastCutoff = frameTimings.FindIndex(ft => ft.Time >= cutoffTime);
				if (frameTimings.Count - firstIndexPastCutoff >= 2) // Keep at least 2 items for comparing.
					frameTimings.RemoveRange(0, firstIndexPastCutoff);
				var (oldestFrame, oldestTime) = frameTimings[0];
				var (newestFrame, newestTime) = frameTimings[^1];
				var fps = (newestFrame - oldestFrame) / (newestTime - oldestTime).TotalSeconds;

				var wfbSize = Game.Renderer.WorldFrameBufferSize;
				var viewportSize = worldRenderer.Viewport.Rectangle.Size;
				return $"FPS: {fps:0}\nTick {Game.LocalTick} @ {PerfHistory.Items["tick_time"].Average(Game.Settings.Debug.Samples):F1} ms\n" +
					$"Render {Game.RenderFrame} @ {PerfHistory.Items["render"].Average(Game.Settings.Debug.Samples):F1} ms\n" +
					$"Batches: {PerfHistory.Items["batches"].LastValue}\n" +
					$"Viewport Size: {viewportSize.Width} x {viewportSize.Height} / {Game.Renderer.WorldDownscaleFactor}\n" +
					$"WFB Size: {wfbSize.Width} x {wfbSize.Height}";
			};

			Game.AfterGameStart += OnGameStart;
		}

		void OnGameStart()
		{
			// Reset timings so our average doesn't include loading time.
			frameTimings.Clear();
			frameTimings.Add((Game.RenderFrame, fpsTimer.Elapsed));
		}

		bool disposed;
		protected override void Dispose(bool disposing)
		{
			if (disposing && !disposed)
			{
				disposed = true;
				Game.AfterGameStart -= OnGameStart;
			}

			base.Dispose(disposing);
		}
	}
}
