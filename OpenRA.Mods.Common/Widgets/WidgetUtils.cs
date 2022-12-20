#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public static class WidgetUtils
	{
		public static string GetStatefulImageName(string baseName, bool disabled = false, bool pressed = false, bool hover = false, bool focused = false)
		{
			var suffix = disabled ? "-disabled" :
				focused ? "-focused" :
				pressed ? "-pressed" :
				hover ? "-hover" :
				"";

			return baseName + suffix;
		}

		public static CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> GetCachedStatefulImage(string collection, string imageName)
		{
			return new CachedTransform<(bool, bool, bool, bool, bool), Sprite>(
				((bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted) args) =>
					{
						var collectionName = collection + (args.Highlighted ? "-highlighted" : "");
						var variantImageName = GetStatefulImageName(imageName, args.Disabled, args.Pressed, args.Hover, args.Focused);
						return ChromeProvider.TryGetImage(collectionName, variantImageName) ?? ChromeProvider.GetImage(collectionName, imageName);
					});
		}

		// TODO: refactor buttons and related UI to use this function
		public static CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite[]> GetCachedStatefulPanelImages(string collection)
		{
			return new CachedTransform<(bool, bool, bool, bool, bool), Sprite[]>(
				((bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted) args) =>
					{
						var collectionName = collection + (args.Highlighted ? "-highlighted" : "");
						var variantCollectionName = GetStatefulImageName(collectionName, args.Disabled, args.Pressed, args.Hover, args.Focused);
						return ChromeProvider.TryGetPanelImages(variantCollectionName) ?? ChromeProvider.GetPanelImages(collectionName);
					});
		}

		public static void DrawSprite(Sprite s, float2 pos)
		{
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(s, pos);
		}

		public static void DrawSprite(Sprite s, float2 pos, Size size)
		{
			var scale = new float3(size.Width / s.Size.X, size.Height / s.Size.Y, 1f);
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(s, pos, scale);
		}

		public static void DrawSprite(Sprite s, float2 pos, float2 size)
		{
			var scale = new float3(size.X / s.Size.X, size.Y / s.Size.Y, 1f);
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(s, pos, scale);
		}

		public static void DrawSpriteCentered(Sprite s, PaletteReference p, float2 pos, float scale = 1f)
		{
			Game.Renderer.SpriteRenderer.DrawSprite(s, p, pos - 0.5f * scale * s.Size, scale);
		}

		public static void DrawPanel(string collection, Rectangle bounds)
		{
			var sprites = ChromeProvider.TryGetPanelImages(collection);
			if (sprites != null)
				DrawPanel(bounds, sprites);
		}

		public static void FillRectWithSprite(Rectangle r, Sprite s)
		{
			var scale = s.Size.X / s.Bounds.Width;
			for (var x = (float)r.Left; x < r.Right; x += s.Size.X)
			{
				for (var y = (float)r.Top; y < r.Bottom; y += s.Size.Y)
				{
					var ss = s;
					var dx = r.Right - x;
					var dy = r.Bottom - y;
					if (dx < s.Size.X || dy < s.Size.Y)
					{
						var rr = new Rectangle(
							s.Bounds.Left,
							s.Bounds.Top,
							Math.Min(s.Bounds.Width, (int)(dx / scale)),
							Math.Min(s.Bounds.Height, (int)(dy / scale)));
						ss = new Sprite(s.Sheet, rr, s.Channel, scale);
					}

					DrawSprite(ss, new float2(x, y));
				}
			}
		}

		public static void FillRectWithColor(Rectangle r, Color c)
		{
			// Offset to the edges of the pixels
			var tl = new float2(r.Left - 0.5f, r.Top - 0.5f);
			var br = new float2(r.Right - 0.5f, r.Bottom - 0.5f);
			Game.Renderer.RgbaColorRenderer.FillRect(tl, br, c);
		}

		public static void FillRectWithColor(Rectangle r, Color topLeftColor, Color topRightColor, Color bottomRightColor, Color bottomLeftColor)
		{
			var tl = new float2(r.Left - 0.5f, r.Top - 0.5f);
			var br = new float2(r.Right - 0.5f, r.Bottom - 0.5f);

			var tr = new float3(br.X, tl.Y, 0);
			var bl = new float3(tl.X, br.Y, 0);

			Game.Renderer.RgbaColorRenderer.FillRect(tl, tr, br, bl, topLeftColor, topRightColor, bottomRightColor, bottomLeftColor);
		}

		public static void FillEllipseWithColor(Rectangle r, Color c)
		{
			var tl = new float2(r.Left, r.Top);
			var br = new float2(r.Right, r.Bottom);
			Game.Renderer.RgbaColorRenderer.FillEllipse(tl, br, c);
		}

		public static Rectangle InflateBy(this Rectangle rect, int l, int t, int r, int b)
		{
			return Rectangle.FromLTRB(rect.Left - l, rect.Top - t,
				rect.Right + r, rect.Bottom + b);
		}

		/// <summary>
		/// Fill a rectangle with sprites defining a panel layout.
		/// Draw order is center, borders, corners to allow mods to define fancy border and corner overlays.
		/// </summary>
		/// <param name="bounds">Rectangle to fill.</param>
		/// <param name="sprites">Nine sprites defining the panel: TL, T, TR, L, C, R, BL, B, BR.</param>
		public static void DrawPanel(Rectangle bounds, Sprite[] sprites)
		{
			if (sprites.Length != 9)
				return;

			var marginTop = sprites[1] == null ? 0 : (int)sprites[1].Size.Y;
			var marginLeft = sprites[3] == null ? 0 : (int)sprites[3].Size.X;
			var marginRight = sprites[5] == null ? 0 : (int)sprites[5].Size.X;
			var marginBottom = sprites[7] == null ? 0 : (int)sprites[7].Size.Y;
			var marginWidth = marginRight + marginLeft;
			var marginHeight = marginBottom + marginTop;

			// Center
			if (sprites[4] != null)
				FillRectWithSprite(new Rectangle(bounds.Left + marginLeft, bounds.Top + marginTop,
					bounds.Width - marginWidth, bounds.Height - marginHeight), sprites[4]);

			// Left edge
			if (sprites[3] != null)
				FillRectWithSprite(new Rectangle(bounds.Left, bounds.Top + marginTop,
						marginLeft, bounds.Height - marginHeight), sprites[3]);

			// Right edge
			if (sprites[5] != null)
				FillRectWithSprite(new Rectangle(bounds.Right - marginRight, bounds.Top + marginTop,
					marginLeft, bounds.Height - marginHeight), sprites[5]);

			// Top edge
			if (sprites[1] != null)
				FillRectWithSprite(new Rectangle(bounds.Left + marginLeft, bounds.Top,
					bounds.Width - marginWidth, marginTop), sprites[1]);

			// Bottom edge
			if (sprites[7] != null)
				FillRectWithSprite(new Rectangle(bounds.Left + marginLeft, bounds.Bottom - marginBottom,
					bounds.Width - marginWidth, marginTop), sprites[7]);

			// Top-left corner
			if (sprites[0] != null)
				DrawSprite(sprites[0], new float2(bounds.Left, bounds.Top));

			// Top-right corner
			if (sprites[2] != null)
				DrawSprite(sprites[2], new float2(bounds.Right - sprites[2].Size.X, bounds.Top));

			// Bottom-left corner
			if (sprites[6] != null)
				DrawSprite(sprites[6], new float2(bounds.Left, bounds.Bottom - sprites[6].Size.Y));

			// Bottom-right corner
			if (sprites[8] != null)
				DrawSprite(sprites[8], new float2(bounds.Right - sprites[8].Size.X, bounds.Bottom - sprites[8].Size.Y));
		}

		public static string FormatTime(int ticks, int timestep)
		{
			return FormatTime(ticks, true, timestep);
		}

		public static string FormatTime(int ticks, bool leadingMinuteZero, int timestep)
		{
			var seconds = (int)Math.Ceiling(ticks * timestep / 1000f);
			return FormatTimeSeconds(seconds, leadingMinuteZero);
		}

		public static string FormatTimeSeconds(int seconds)
		{
			return FormatTimeSeconds(seconds, true);
		}

		public static string FormatTimeSeconds(int seconds, bool leadingMinuteZero)
		{
			var minutes = seconds / 60;

			if (minutes >= 60)
				return $"{minutes / 60:D}:{minutes % 60:D2}:{seconds % 60:D2}";
			if (leadingMinuteZero)
				return $"{minutes:D2}:{seconds % 60:D2}";
			return $"{minutes:D}:{seconds % 60:D2}";
		}

		public static string WrapText(string text, int width, SpriteFont font)
		{
			var textSize = font.Measure(text);
			if (textSize.X > width)
			{
				var lines = text.Split('\n').ToList();

				for (var i = 0; i < lines.Count; i++)
				{
					var line = lines[i];
					if (font.Measure(line).X <= width)
						continue;

					// Scan forwards until we find the last word that fits
					// This guarantees a small bound on the amount of string we need to search before a linebreak
					var start = 0;
					while (true)
					{
						var spaceIndex = line.IndexOf(' ', start);
						if (spaceIndex == -1)
							break;

						var fragmentWidth = font.Measure(line.Substring(0, spaceIndex)).X;
						if (fragmentWidth > width)
							break;

						start = spaceIndex + 1;
					}

					if (start > 0)
					{
						lines[i] = line.Substring(0, start - 1);
						lines.Insert(i + 1, line.Substring(start));
					}
				}

				return string.Join("\n", lines);
			}

			return text;
		}

		public static string TruncateText(string text, int width, SpriteFont font)
		{
			var trimmedWidth = font.Measure(text).X;
			if (trimmedWidth <= width)
				return text;

			var trimmed = text;
			while (trimmedWidth > width && trimmed.Length > 3)
			{
				trimmed = text.Substring(0, trimmed.Length - 4) + "...";
				trimmedWidth = font.Measure(trimmed).X;
			}

			return trimmed;
		}

		public static void TruncateLabelToTooltip(LabelWithTooltipWidget label, string text)
		{
			var truncatedText = TruncateText(text, label.Bounds.Width, Game.Renderer.Fonts[label.Font]);

			label.GetText = () => truncatedText;

			if (text != truncatedText)
				label.GetTooltipText = () => text;
			else
				label.GetTooltipText = null;
		}

		public static void TruncateButtonToTooltip(ButtonWidget button, string text)
		{
			var truncatedText = TruncateText(text, button.Bounds.Width - button.LeftMargin - button.RightMargin, Game.Renderer.Fonts[button.Font]);

			button.GetText = () => truncatedText;

			if (text != truncatedText)
				button.GetTooltipText = () => text;
			else
				button.GetTooltipText = null;
		}

		public static void BindButtonIcon(ButtonWidget button)
		{
			var icon = button.Get<ImageWidget>("ICON");

			var cache = GetCachedStatefulImage(icon.ImageCollection, icon.ImageName);
			icon.GetSprite = () => cache.Update((button.IsDisabled(), button.Depressed, Ui.MouseOverWidget == button, false, button.IsHighlighted()));
		}

		public static void BindPlayerNameAndStatus(LabelWidget label, Player p)
		{
			var client = p.World.LobbyInfo.ClientWithIndex(p.ClientIndex);
			var nameFont = Game.Renderer.Fonts[label.Font];
			var name = new CachedTransform<(string Name, WinState WinState, Session.ClientState ClientState), string>(c =>
			{
				var suffix = c.WinState == WinState.Undefined ? "" : " (" + c.Item2 + ")";
				if (c.ClientState == Session.ClientState.Disconnected)
					suffix = " (Gone)";

				return TruncateText(c.Name, label.Bounds.Width - nameFont.Measure(suffix).X, nameFont) + suffix;
			});

			label.GetText = () =>
			{
				var clientState = client != null ? client.State : Session.ClientState.Ready;
				return name.Update((p.PlayerName, p.WinState, clientState));
			};
		}

		public static void SetupTextNotification(Widget notificationWidget, TextNotification notification, int boxWidth, bool withTimestamp)
		{
			var timeLabel = notificationWidget.GetOrNull<LabelWidget>("TIME");
			var prefixLabel = notificationWidget.GetOrNull<LabelWidget>("PREFIX");
			var textLabel = notificationWidget.Get<LabelWidget>("TEXT");

			var textFont = Game.Renderer.Fonts[textLabel.Font];
			var textWidth = boxWidth - notificationWidget.Bounds.X - textLabel.Bounds.X;

			var hasPrefix = !string.IsNullOrEmpty(notification.Prefix) && prefixLabel != null;
			var timeOffset = 0;

			if (withTimestamp && timeLabel != null)
			{
				var time = $"{notification.Time.Hour:D2}:{notification.Time.Minute:D2}";
				timeOffset = timeLabel.Bounds.Width + timeLabel.Bounds.X;

				timeLabel.GetText = () => time;

				textWidth -= timeOffset;
				textLabel.Bounds.X += timeOffset;

				if (hasPrefix)
					prefixLabel.Bounds.X += timeOffset;
			}

			if (hasPrefix)
			{
				var prefix = notification.Prefix + ":";
				var prefixSize = Game.Renderer.Fonts[prefixLabel.Font].Measure(prefix);
				var prefixOffset = prefixSize.X + prefixLabel.Bounds.X;

				prefixLabel.GetColor = () => notification.PrefixColor ?? prefixLabel.TextColor;
				prefixLabel.GetText = () => prefix;
				prefixLabel.Bounds.Width = prefixSize.X;

				textWidth -= prefixOffset;
				textLabel.Bounds.X += prefixOffset - timeOffset;
			}

			textLabel.GetColor = () => notification.TextColor ?? textLabel.TextColor;
			textLabel.Bounds.Width = textWidth;

			// Hack around our hacky wordwrap behavior: need to resize the widget to fit the text
			var text = WrapText(notification.Text, textLabel.Bounds.Width, textFont);
			textLabel.GetText = () => text;
			var dh = textFont.Measure(text).Y - textLabel.Bounds.Height;
			if (dh > 0)
			{
				textLabel.Bounds.Height += dh;
				notificationWidget.Bounds.Height += dh;
			}

			notificationWidget.Bounds.Width = boxWidth - notificationWidget.Bounds.X;
		}
	}

	public class CachedTransform<T, U>
	{
		readonly Func<T, U> transform;

		bool initialized;
		T lastInput;
		U lastOutput;

		public CachedTransform(Func<T, U> transform)
		{
			this.transform = transform;
		}

		public U Update(T input)
		{
			if (initialized && ((input == null && lastInput == null) || (input != null && input.Equals(lastInput))))
				return lastOutput;

			lastInput = input;
			lastOutput = transform(input);
			initialized = true;

			return lastOutput;
		}
	}

	public class PredictedCachedTransform<T, U>
	{
		readonly Func<T, U> transform;

		bool initialized;
		T lastInput;
		U lastOutput;

		bool predicted;
		U prediction;

		public PredictedCachedTransform(Func<T, U> transform)
		{
			this.transform = transform;
		}

		public void Predict(U value)
		{
			predicted = true;
			prediction = value;
		}

		public U Update(T input)
		{
			if ((predicted || initialized) && ((input == null && lastInput == null) || (input != null && input.Equals(lastInput))))
				return predicted ? prediction : lastOutput;

			predicted = false;
			initialized = true;
			lastInput = input;
			lastOutput = transform(input);

			return lastOutput;
		}
	}
}
