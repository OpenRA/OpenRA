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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ScrollableLineGraphWidget : Widget
	{
		protected readonly Ruleset ModRules;
		public Func<IEnumerable<ScrollableLineGraphSeries>> GetSeries;
		public Func<string> GetValueFormat;
		public Func<string> GetXAxisValueFormat;
		public Func<string> GetYAxisValueFormat;
		public Func<int> GetXAxisSize;
		public Func<int> GetYAxisSize;
		public Func<string> GetXAxisLabel;
		public Func<string> GetYAxisLabel;
		public Func<bool> GetDisplayFirstYAxisValue;
		public Func<string> GetLabelFont;
		public Func<string> GetAxisFont;
		public string ValueFormat = "{0}";
		public string XAxisValueFormat = "{0}";
		public string YAxisValueFormat = "{0}";
		public int XAxisSize = 10;
		public int YAxisSize = 10;
		public int XAxisTicksPerLabel = 1;
		public string XAxisLabel = "";
		public string YAxisLabel = "";
		public bool DisplayFirstYAxisValue = false;
		public string LabelFont;
		public string AxisFont;
		public Color BackgroundColorDark = ChromeMetrics.Get<Color>("TextContrastColorDark");
		public Color BackgroundColorLight = ChromeMetrics.Get<Color>("TextContrastColorLight");
		public string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");
		public int Padding = 5;

		// Horizontal scrolling properties
		public int ScrollbarHeight = 16;
		public string ScrollbarBackground = "scrollpanel-bg";
		public string ScrollbarButton = "scrollpanel-button";
		public string ScrollbarDecorations = "scrollpanel-decorations";
		public readonly string DecorationScrollLeft = "left";
		public readonly string DecorationScrollRight = "right";
		public int MinimumThumbWidth = 20;
		public float SmoothScrollSpeed = 0.333f;

		readonly CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getLeftArrowImage;
		readonly CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getRightArrowImage;

		// Scroll state
		float horizontalOffset = 0;
		float targetHorizontalOffset = 0;
		bool leftPressed = false;
		bool rightPressed = false;
		bool thumbPressed = false;
		bool leftDisabled = false;
		bool rightDisabled = false;
		bool autoScrollEnabled = true;
		bool manuallyScrolled = true;
		int2 lastMousePos;
		Rectangle leftButtonRect;
		Rectangle rightButtonRect;
		Rectangle scrollbarRect;
		Rectangle thumbRect;
		long lastSmoothScrollTime = 0;

		[ObjectCreator.UseCtor]
		public ScrollableLineGraphWidget(ModData modData)
		{
			ModRules = modData.DefaultRules;
			GetValueFormat = () => ValueFormat;
			GetXAxisValueFormat = () => XAxisValueFormat;
			GetYAxisValueFormat = () => YAxisValueFormat;
			GetXAxisSize = () => XAxisSize;
			GetYAxisSize = () => YAxisSize;
			GetXAxisLabel = () => XAxisLabel;
			GetYAxisLabel = () => YAxisLabel;
			GetDisplayFirstYAxisValue = () => DisplayFirstYAxisValue;
			GetLabelFont = () => LabelFont;
			GetAxisFont = () => AxisFont;

			getLeftArrowImage = WidgetUtils.GetCachedStatefulImage(ScrollbarDecorations, DecorationScrollLeft);
			getRightArrowImage = WidgetUtils.GetCachedStatefulImage(ScrollbarDecorations, DecorationScrollRight);
		}

		protected ScrollableLineGraphWidget(ScrollableLineGraphWidget other)
			: base(other)
		{
			ModRules = other.ModRules;
			GetSeries = other.GetSeries;
			GetValueFormat = other.GetValueFormat;
			GetXAxisValueFormat = other.GetXAxisValueFormat;
			GetYAxisValueFormat = other.GetYAxisValueFormat;
			GetXAxisSize = other.GetXAxisSize;
			GetYAxisSize = other.GetYAxisSize;
			GetXAxisLabel = other.GetXAxisLabel;
			GetYAxisLabel = other.GetYAxisLabel;
			GetDisplayFirstYAxisValue = other.GetDisplayFirstYAxisValue;
			GetLabelFont = other.GetLabelFont;
			GetAxisFont = other.GetAxisFont;
			ValueFormat = other.ValueFormat;
			XAxisValueFormat = other.XAxisValueFormat;
			YAxisValueFormat = other.YAxisValueFormat;
			XAxisSize = other.XAxisSize;
			YAxisSize = other.YAxisSize;
			XAxisTicksPerLabel = other.XAxisTicksPerLabel;
			XAxisLabel = other.XAxisLabel;
			YAxisLabel = other.YAxisLabel;
			DisplayFirstYAxisValue = other.DisplayFirstYAxisValue;
			LabelFont = other.LabelFont;
			AxisFont = other.AxisFont;
			BackgroundColorDark = other.BackgroundColorDark;
			BackgroundColorLight = other.BackgroundColorLight;
			Padding = other.Padding;
			ScrollbarHeight = other.ScrollbarHeight;
			ScrollbarBackground = other.ScrollbarBackground;
			ScrollbarButton = other.ScrollbarButton;
			ScrollbarDecorations = other.ScrollbarDecorations;
			DecorationScrollLeft = other.DecorationScrollLeft;
			DecorationScrollRight = other.DecorationScrollRight;
			MinimumThumbWidth = other.MinimumThumbWidth;
			SmoothScrollSpeed = other.SmoothScrollSpeed;

			getLeftArrowImage = WidgetUtils.GetCachedStatefulImage(ScrollbarDecorations, DecorationScrollLeft);
			getRightArrowImage = WidgetUtils.GetCachedStatefulImage(ScrollbarDecorations, DecorationScrollRight);
		}

		void SetHorizontalOffset(float value, bool smooth)
		{
			targetHorizontalOffset = value;
			if (!smooth)
			{
				horizontalOffset = value;
				Ui.ResetTooltips();
			}
		}

		void UpdateSmoothScrolling()
		{
			if (lastSmoothScrollTime == 0)
			{
				lastSmoothScrollTime = Game.RunTime;
				return;
			}

			var dt = Game.RunTime - lastSmoothScrollTime;
			lastSmoothScrollTime = Game.RunTime;

			var offsetDiff = targetHorizontalOffset - horizontalOffset;
			var absOffsetDiff = Math.Abs(offsetDiff);
			if (absOffsetDiff > 1f)
			{
				var speed = Math.Max(0.01f, Math.Min(1f, SmoothScrollSpeed * dt / 40f));
				horizontalOffset += offsetDiff * speed;
			}
			else
			{
				horizontalOffset = targetHorizontalOffset;
			}
		}

		void Scroll(float amount, bool smooth = true)
		{
			var newTarget = targetHorizontalOffset + amount * Game.Settings.Game.UIScrollSpeed;
			SetHorizontalOffset(newTarget, smooth);
			autoScrollEnabled = false;
			manuallyScrolled = true;
		}

		public override void Draw()
		{
			if (GetSeries == null || GetLabelFont == null)
				return;

			var series = GetSeries();
			if (!series.Any())
				return;

			var font = GetLabelFont();
			if (font == null)
				return;

			UpdateSmoothScrolling();

			var cr = Game.Renderer.RgbaColorRenderer;
			var rect = RenderBounds;

			var labelFont = Game.Renderer.Fonts[font];
			var axisFont = Game.Renderer.Fonts[GetAxisFont()];

			var xAxisSize = GetXAxisSize();
			var yAxisSize = GetYAxisSize();

			var xAxisLabel = GetXAxisLabel();
			var xAxisLabelSize = axisFont.Measure(xAxisLabel);

			var xAxisPointLabelHeight = labelFont.Measure("0").Y;

			var graphBottomOffset = Padding * 2 + xAxisLabelSize.Y + xAxisPointLabelHeight + ScrollbarHeight;
			var height = rect.Height - (graphBottomOffset + Padding * 4);

			var maxValue = series.Select(p => p.Points).SelectMany(d => d).Concat([0f]).Max();
			var longestName = series.Select(s => s.Key).OrderByDescending(s => s.Length).FirstOrDefault() ?? "";

			var scaledMaxValue = Math.Max((float)Math.Ceiling(maxValue / 1000) * 1000, 5000f);
			var scale = height / scaledMaxValue;

			var widthMaxValue = labelFont.Measure(GetYAxisValueFormat().FormatCurrent(scaledMaxValue)).X;
			var widthLongestName = labelFont.Measure(longestName).X;

			// y axis label
			var yAxisLabel = GetYAxisLabel();
			var yAxisLabelSize = axisFont.Measure(yAxisLabel);

			var width = rect.Width - (Padding * 10 + widthMaxValue + widthLongestName + yAxisLabelSize.Y);

			var pointCount = series.Max(s => s.Points.Count());
			var totalDataWidth = pointCount * (width / xAxisSize);
			var maxHorizontalOffset = Math.Max(0, totalDataWidth - width);

			targetHorizontalOffset = Math.Max(-maxHorizontalOffset, Math.Min(0, targetHorizontalOffset));
			horizontalOffset = Math.Max(-maxHorizontalOffset, Math.Min(0, horizontalOffset));

			var xStep = width / xAxisSize;
			var yStep = height / yAxisSize;

			var visibleStart = Math.Max(0, (int)Math.Floor(-horizontalOffset / xStep));
			var visibleEnd = Math.Min(pointCount, visibleStart + xAxisSize + 1);

			var graphOrigin = new float2(rect.Left, rect.Bottom) + new float2(Padding * 3 + widthMaxValue + yAxisLabelSize.Y, -graphBottomOffset);

			var origin = new float2(rect.Left, rect.Bottom);

			var keyOffset = 0;

			// added sorting so that names appear in order of highest value to lowest value
			series = series.OrderByDescending(s => s.Points.LastOrDefault()).ToList();

			// Enable clipping to prevent graph lines from bleeding into Y axis labels and extending beyond bounds
			// Clip both left and right sides to contain the graph within proper bounds
			var graphClipRect = new Rectangle((int)graphOrigin.X, (int)(graphOrigin.Y - height), width, height);
			Game.Renderer.EnableScissor(graphClipRect);

			foreach (var s in series)
			{
				var key = s.Key;
				var color = s.Color;
				var points = s.Points.ToArray();
				if (points.Length > 0)
				{
					var visiblePoints = new List<float3>();
					for (var i = visibleStart; i < Math.Min(visibleEnd, points.Length); i++)
					{
						var screenX = i * xStep + horizontalOffset;
						var screenY = -points[i] * scale;
						visiblePoints.Add(graphOrigin + new float3(screenX, screenY, 0));
					}

					if (visiblePoints.Count > 1)
					{
						cr.DrawLine(visiblePoints, 1, color);
					}
				}
			}

			// Disable clipping before drawing labels and other elements
			Game.Renderer.DisableScissor();

			foreach (var s in series)
			{
				var key = s.Key;
				var color = s.Color;
				var points = s.Points.ToArray();
				if (points.Length > 0)
				{
					var visiblePoints = new List<float3>();
					for (var i = visibleStart; i < Math.Min(visibleEnd, points.Length); i++)
					{
						var screenX = i * xStep + horizontalOffset;
						var screenY = -points[i] * scale;

						if (screenX >= -xStep && screenX <= width + xStep)
						{
							visiblePoints.Add(graphOrigin + new float3(screenX, screenY, 0));
						}
					}

					if (visiblePoints.Count > 0)
					{
						var lastPoint = visiblePoints[^1];

						if (lastPoint.X >= graphOrigin.X && lastPoint.X <= graphOrigin.X + width)
						{
							var lastIndex = visibleStart + visiblePoints.Count - 1;
							var lastValue = points[Math.Min(lastIndex, points.Length - 1)];
							if (lastValue != 0f)
							{
								labelFont.DrawTextWithShadow(GetValueFormat().FormatCurrent(lastValue),
									new float2(lastPoint.X, lastPoint.Y - 2),
									color, BackgroundColorDark, BackgroundColorLight, 1);
							}
						}
					}
				}

				labelFont.DrawTextWithShadow(key, new float2(rect.Right, rect.Top) + new float2(-(widthLongestName + Padding), 10 * keyOffset + 3),
					color, BackgroundColorDark, BackgroundColorLight, 1);
				keyOffset++;
			}

			var scrollbarY = rect.Bottom - ScrollbarHeight;
			var scrollbarWidth = width;
			var scrollbarX = graphOrigin.X;

			scrollbarRect = new Rectangle((int)scrollbarX, scrollbarY, scrollbarWidth, ScrollbarHeight);
			leftButtonRect = new Rectangle((int)scrollbarX, scrollbarY, ScrollbarHeight, ScrollbarHeight);
			rightButtonRect = new Rectangle((int)(scrollbarX + scrollbarWidth - ScrollbarHeight), scrollbarY, ScrollbarHeight, ScrollbarHeight);

			WidgetUtils.DrawPanel(ScrollbarBackground, scrollbarRect);

			var availableThumbSpace = scrollbarWidth - 2 * ScrollbarHeight;
			var thumbWidth = maxHorizontalOffset > 0 ?
				Math.Max(MinimumThumbWidth, Math.Min(availableThumbSpace - MinimumThumbWidth * 2, availableThumbSpace * width / totalDataWidth)) :
				availableThumbSpace;
			var actualthumbWidth = Math.Min(thumbWidth, availableThumbSpace);

			var thumbCalculationOffset = autoScrollEnabled && maxHorizontalOffset > 0 ? -maxHorizontalOffset : horizontalOffset;
			var thumbPosition = maxHorizontalOffset > 0 ?
				ScrollbarHeight + (int)((availableThumbSpace - actualthumbWidth) * (-thumbCalculationOffset / maxHorizontalOffset)) :
				ScrollbarHeight;

			thumbRect = new Rectangle((int)scrollbarX + thumbPosition, scrollbarY, actualthumbWidth, ScrollbarHeight);

			var mouseButtonDown = leftPressed || thumbPressed || (rightPressed && !rightDisabled);
			if (autoScrollEnabled && maxHorizontalOffset > 0 && !mouseButtonDown)
			{
				SetHorizontalOffset(-maxHorizontalOffset, false);
			}

			if (!autoScrollEnabled && maxHorizontalOffset > 0 && !mouseButtonDown && rightDisabled && manuallyScrolled)
			{
				if (rightPressed || Math.Abs(targetHorizontalOffset + maxHorizontalOffset) < 1)
				{
					autoScrollEnabled = true;
					manuallyScrolled = false;
				}
			}

			// Draw scrollbar elements
			// When auto-scrolling is enabled, force the scrollbar to move to the rightmost position
			var effectiveHorizontalOffset = autoScrollEnabled && maxHorizontalOffset > 0 ? -maxHorizontalOffset : horizontalOffset;

			leftDisabled = effectiveHorizontalOffset >= 0;
			rightDisabled = effectiveHorizontalOffset <= -maxHorizontalOffset;

			var leftHover = Ui.MouseOverWidget == this && leftButtonRect.Contains(Viewport.LastMousePos);
			var rightHover = Ui.MouseOverWidget == this && rightButtonRect.Contains(Viewport.LastMousePos);
			var thumbHover = Ui.MouseOverWidget == this && thumbRect.Contains(Viewport.LastMousePos);

			ButtonWidget.DrawBackground(ScrollbarButton, leftButtonRect, leftDisabled, leftPressed, leftHover, false);
			ButtonWidget.DrawBackground(ScrollbarButton, rightButtonRect, rightDisabled, rightPressed, rightHover, false);

			if (maxHorizontalOffset > 0)
				ButtonWidget.DrawBackground(ScrollbarButton, thumbRect, false, thumbPressed, thumbHover, false);

			// Draw arrow decorations
			var leftOffset = !leftPressed || leftDisabled ? 0 : 1; // Using 1 instead of ButtonDepth for simplicity
			var rightOffset = !rightPressed || rightDisabled ? 0 : 1;

			var leftArrowImage = getLeftArrowImage.Update((leftDisabled, leftPressed, leftHover, false, false));
			WidgetUtils.DrawSprite(leftArrowImage,
				new float2(leftButtonRect.Left + leftOffset, leftButtonRect.Top + leftOffset));

			var rightArrowImage = getRightArrowImage.Update((rightDisabled, rightPressed, rightHover, false, false));
			WidgetUtils.DrawSprite(rightArrowImage,
				new float2(rightButtonRect.Left + rightOffset, rightButtonRect.Top + rightOffset));

			// Draw x axis
			axisFont.DrawTextWithShadow(xAxisLabel,
				new float2(graphOrigin.X, origin.Y) + new float2(width / 2 - xAxisLabelSize.X / 2, -(xAxisLabelSize.Y + Padding + ScrollbarHeight)),
				Color.White, BackgroundColorDark, BackgroundColorLight, 1);

			// Enable clipping for x-axis labels to prevent them from extending beyond the right graph bound
			var xAxisClipRect = new Rectangle((int)graphOrigin.X - 100, (int)(graphOrigin.Y - height), width + 100, height + xAxisPointLabelHeight + 10);
			Game.Renderer.EnableScissor(xAxisClipRect);

			// Draw x axis ticks and labels
			var maxDataPoints = pointCount;
			var labelsToShow = Math.Max(xAxisSize + 2, maxDataPoints);

			for (var i = 0; i < labelsToShow; i++)
			{
				var screenX = i * xStep + horizontalOffset;

				if (screenX >= 0)
				{
					cr.DrawLine(graphOrigin + new float2(screenX, 0), graphOrigin + new float2(screenX, -5), 1, Color.White);
					if (i % XAxisTicksPerLabel == 0)
					{
						var xAxisText = GetXAxisValueFormat().FormatCurrent(i / XAxisTicksPerLabel);
						var xAxisTickTextWidth = labelFont.Measure(xAxisText).X;
						var xLocation = screenX - xAxisTickTextWidth / 2;
						labelFont.DrawTextWithShadow(xAxisText,
							graphOrigin + new float2(xLocation, 2),
							Color.White, BackgroundColorDark, BackgroundColorLight, 1);
					}
				}
			}

			// Disable clipping after drawing x-axis labels
			Game.Renderer.DisableScissor();

			// Draw y axis
			axisFont.DrawTextWithShadow(yAxisLabel,
				new float2(origin.X, graphOrigin.Y) + new float2(5 - axisFont.TopOffset, -(height / 2 - yAxisLabelSize.X / 2)),
				Color.White, BackgroundColorDark, BackgroundColorLight, 1, (float)Math.PI / 2);

			for (var y = GetDisplayFirstYAxisValue() ? 0 : yStep; y <= height; y += yStep)
			{
				var yValue = y / scale;
				cr.DrawLine(graphOrigin + new float2(0, -y), graphOrigin + new float2(5, -y), 1, Color.White);
				var text = GetYAxisValueFormat().FormatCurrent(yValue);

				var textWidth = labelFont.Measure(text);

				var yLocation = y + (textWidth.Y + labelFont.TopOffset) / 2;

				labelFont.DrawTextWithShadow(text,
					graphOrigin + new float2(-(textWidth.X + 3), -yLocation),
					Color.White, BackgroundColorDark, BackgroundColorLight, 1);
			}

			// Bottom line
			cr.DrawLine(graphOrigin, graphOrigin + new float2(width, 0), 1, Color.White);

			// Left line
			cr.DrawLine(graphOrigin, graphOrigin + new float2(0, -height), 1, Color.White);
		}

		public override ScrollableLineGraphWidget Clone()
		{
			return new ScrollableLineGraphWidget(this);
		}

		public override bool YieldMouseFocus(MouseInput mi)
		{
			leftPressed = rightPressed = thumbPressed = false;
			return base.YieldMouseFocus(mi);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Scroll && EventBounds.Contains(mi.Location))
			{
				Scroll(mi.Delta.Y, true);
				return true;
			}

			if (mi.Button != MouseButton.Left)
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
				return false;

			if (!HasMouseFocus)
				return false;

			if (mi.Event == MouseInputEvent.Up)
			{
				leftPressed = rightPressed = thumbPressed = false;
				YieldMouseFocus(mi);
				return true;
			}

			if (mi.Event == MouseInputEvent.Move && thumbPressed)
			{
				var deltaX = mi.Location.X - lastMousePos.X;
				var series = GetSeries();
				if (series.Any())
				{
					var pointCount = series.Max(s => s.Points.Count());
					var rect = RenderBounds;

					var font = GetLabelFont();
					if (font == null)
						return false;

					var labelFont = Game.Renderer.Fonts[font];
					var axisFont = Game.Renderer.Fonts[GetAxisFont()];

					var maxValue = series.Select(p => p.Points).SelectMany(d => d).Concat([0f]).Max();
					var longestName = series.Select(s => s.Key).OrderByDescending(s => s.Length).FirstOrDefault() ?? "";
					var scaledMaxValue = Math.Max((float)Math.Ceiling(maxValue / 1000) * 1000, 5000f);

					var widthMaxValue = labelFont.Measure(GetYAxisValueFormat().FormatCurrent(scaledMaxValue)).X;
					var widthLongestName = labelFont.Measure(longestName).X;
					var yAxisLabel = GetYAxisLabel();
					var yAxisLabelSize = axisFont.Measure(yAxisLabel);

					var width = rect.Width - (Padding * 10 + widthMaxValue + widthLongestName + yAxisLabelSize.Y);

					var totalDataWidth = pointCount * (width / GetXAxisSize());
					var maxHorizontalOffset = Math.Max(0, totalDataWidth - width);

					if (maxHorizontalOffset > 0)
					{
						var availableThumbSpace = scrollbarRect.Width - 2 * ScrollbarHeight;
						var proportionalThumbSize = availableThumbSpace * width / totalDataWidth;
						var actualThumbSize = Math.Max(MinimumThumbWidth, Math.Min(proportionalThumbSize, availableThumbSpace - MinimumThumbWidth * 2));
						var thumbRange = availableThumbSpace - actualThumbSize;

						if (thumbRange > 0)
						{
							var scrollAmount = deltaX / (float)thumbRange * maxHorizontalOffset;
							SetHorizontalOffset(targetHorizontalOffset - scrollAmount, false);
							autoScrollEnabled = false;
							manuallyScrolled = true;
						}
					}
				}

				lastMousePos = mi.Location;
				return true;
			}

			if (mi.Event == MouseInputEvent.Down)
			{
				if (leftButtonRect.Contains(mi.Location) && !leftDisabled)
				{
					leftPressed = true;
					Scroll(1, true);
					PlayClickSound();
					return true;
				}
				else if (rightButtonRect.Contains(mi.Location) && !rightDisabled)
				{
					rightPressed = true;
					Scroll(-1, true);
					PlayClickSound();
					return true;
				}
				else if (thumbRect.Contains(mi.Location))
				{
					thumbPressed = true;
					lastMousePos = mi.Location;
					autoScrollEnabled = false;
					manuallyScrolled = true;
					PlayClickSound();
					return true;
				}
				else if (scrollbarRect.Contains(mi.Location))
				{
					var clickX = mi.Location.X;
					var thumbCenterX = thumbRect.Left + thumbRect.Width / 2;

					if (clickX < thumbCenterX)
						Scroll(2, true);
					else
						Scroll(-2, true);

					return true;
				}
			}

			return false;
		}

		void PlayClickSound()
		{
			if ((thumbPressed && (!rightDisabled || !leftDisabled)) || (rightPressed && !rightDisabled) || (leftPressed && !leftDisabled))
				Game.Sound.PlayNotification(ModRules, null, "Sounds", ClickSound, null);
			else if ((rightPressed && rightDisabled) || (leftPressed && leftDisabled))
				Game.Sound.PlayNotification(ModRules, null, "Sounds", ClickDisabledSound, null);
		}

		public override void Tick()
		{
			if (leftPressed && !leftDisabled)
				Scroll(1, true);

			if (rightPressed && !rightDisabled)
				Scroll(-1, true);
		}
	}

	public record ScrollableLineGraphSeries(string Key, Color Color, IEnumerable<float> Points);
}
