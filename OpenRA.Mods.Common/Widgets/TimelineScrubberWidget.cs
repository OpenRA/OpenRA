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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	/// <summary>
	/// A timeline scrubber widget for replay viewing.
	/// Displays a progress bar with current and total time, and allows seeking by clicking.
	/// </summary>
	public class TimelineScrubberWidget : Widget
	{
		/// <summary>Gets the current tick position.</summary>
		public Func<int> GetCurrentTick = () => 0;

		/// <summary>Gets the total number of ticks in the replay.</summary>
		public Func<int> GetTotalTicks = () => 1;

		/// <summary>Gets the timestep for time display calculations.</summary>
		public Func<int> GetTimestep = () => 40;

		/// <summary>Called when the user seeks to a new position.</summary>
		public Action<int> OnSeek = _ => { };

		/// <summary>Returns true if currently rewinding.</summary>
		public Func<bool> IsRewinding = () => false;

		/// <summary>Gets the target tick when rewinding.</summary>
		public Func<int> RewindTargetTick = () => 0;

		/// <summary>Color of the background bar.</summary>
		public Color BarColor = Color.FromArgb(150, 50, 50, 50);

		/// <summary>Color of the progress fill.</summary>
		public Color ProgressColor = Color.FromArgb(200, 50, 150, 50);

		/// <summary>Color shown during rewind operation.</summary>
		public Color RewindColor = Color.FromArgb(200, 200, 150, 50);

		/// <summary>Color of the position handle.</summary>
		public Color HandleColor = Color.White;

		/// <summary>Height of the track bar.</summary>
		public int TrackHeight = 12;

		bool isDragging;

		public TimelineScrubberWidget() { }

		protected TimelineScrubberWidget(TimelineScrubberWidget other)
			: base(other)
		{
			GetCurrentTick = other.GetCurrentTick;
			GetTotalTicks = other.GetTotalTicks;
			GetTimestep = other.GetTimestep;
			OnSeek = other.OnSeek;
			IsRewinding = other.IsRewinding;
			RewindTargetTick = other.RewindTargetTick;
			BarColor = other.BarColor;
			ProgressColor = other.ProgressColor;
			RewindColor = other.RewindColor;
			HandleColor = other.HandleColor;
			TrackHeight = other.TrackHeight;
		}

		public override void Draw()
		{
			var bounds = RenderBounds;
			var currentTick = GetCurrentTick();
			var totalTicks = Math.Max(1, GetTotalTicks());
			var progress = Math.Clamp((float)currentTick / totalTicks, 0f, 1f);

			// Background bar
			var trackY = bounds.Y + (bounds.Height - TrackHeight) / 2;
			var trackBounds = new Rectangle(bounds.X, trackY, bounds.Width, TrackHeight);
			WidgetUtils.FillRectWithColor(trackBounds, BarColor);

			// Progress fill
			var progressWidth = (int)(bounds.Width * progress);
			if (progressWidth > 0)
			{
				var progressBounds = new Rectangle(bounds.X, trackY, progressWidth, TrackHeight);
				WidgetUtils.FillRectWithColor(progressBounds, ProgressColor);
			}

			// Rewind indicator (show target position during rewind)
			if (IsRewinding())
			{
				var targetTick = RewindTargetTick();
				var targetProgress = Math.Clamp((float)targetTick / totalTicks, 0f, 1f);
				var targetX = bounds.X + (int)(bounds.Width * targetProgress);

				// Draw a marker at the rewind target position
				const int MarkerWidth = 4;
				var markerBounds = new Rectangle(targetX - MarkerWidth / 2, trackY - 2, MarkerWidth, TrackHeight + 4);
				WidgetUtils.FillRectWithColor(markerBounds, RewindColor);
			}

			// Handle (position indicator)
			var handleX = bounds.X + progressWidth;
			const int HandleWidth = 6;
			var handleBounds = new Rectangle(handleX - HandleWidth / 2, trackY - 3, HandleWidth, TrackHeight + 6);
			WidgetUtils.FillRectWithColor(handleBounds, HandleColor);

			// Time labels
			var font = Game.Renderer.Fonts["TinyBold"];
			var timestep = GetTimestep();
			var currentTime = WidgetUtils.FormatTime(currentTick, timestep);
			var totalTime = WidgetUtils.FormatTime(totalTicks, timestep);

			// Current time on the left, total time on the right
			var textY = trackY + TrackHeight + 4;
			font.DrawTextWithContrast(currentTime, new float2(bounds.X + 2, textY), Color.White, Color.Black, 1);

			var totalTimeSize = font.Measure(totalTime);
			font.DrawTextWithContrast(totalTime, new float2(bounds.Right - totalTimeSize.X - 2, textY), Color.White, Color.Black, 1);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				if (mi.Event == MouseInputEvent.Down)
				{
					isDragging = true;
					SeekToPosition(mi.Location.X);
					return true;
				}

				if (mi.Event == MouseInputEvent.Up)
				{
					isDragging = false;
					return true;
				}
			}

			if (isDragging && mi.Event == MouseInputEvent.Move)
			{
				SeekToPosition(mi.Location.X);
				return true;
			}

			return false;
		}

		void SeekToPosition(int screenX)
		{
			var bounds = RenderBounds;
			var relativeX = screenX - bounds.X;
			var progress = Math.Clamp((float)relativeX / bounds.Width, 0f, 1f);
			var targetTick = (int)(GetTotalTicks() * progress);
			OnSeek(targetTick);
		}

		public override Widget Clone() { return new TimelineScrubberWidget(this); }
	}
}
