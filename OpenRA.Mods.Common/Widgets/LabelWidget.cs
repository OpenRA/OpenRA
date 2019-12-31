#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public enum TextAlign { Left, Center, Right }
	public enum TextVAlign { Top, Middle, Bottom }

	public class LabelWidget : Widget
	{
		[Translate]
		public string Text = null;
		public TextAlign Align = TextAlign.Left;
		public TextVAlign VAlign = TextVAlign.Middle;
		public string Font = ChromeMetrics.Get<string>("TextFont");
		public Color TextColor = ChromeMetrics.Get<Color>("TextColor");
		public bool Contrast = ChromeMetrics.Get<bool>("TextContrast");
		public bool Shadow = ChromeMetrics.Get<bool>("TextShadow");
		public Color ContrastColorDark = ChromeMetrics.Get<Color>("TextContrastColorDark");
		public Color ContrastColorLight = ChromeMetrics.Get<Color>("TextContrastColorLight");
		public Color URLColor = ChromeMetrics.Get<Color>("TextURLColor");
		public string ClickURL = null;
		public string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public bool WordWrap = false;
		public Func<string> GetText;
		public Func<Color> GetColor;
		public Func<Color> GetContrastColorDark;
		public Func<Color> GetContrastColorLight;
		public Func<Color> GetURLColor;
		public readonly Ruleset ModRules;

		[ObjectCreator.UseCtor]
		public LabelWidget(ModData modData)
		{
			ModRules = modData.DefaultRules;
			GetText = () => Text;
			GetColor = () => TextColor;
			GetContrastColorDark = () => ContrastColorDark;
			GetContrastColorLight = () => ContrastColorLight;
			GetURLColor = () => URLColor;
		}

		public LabelWidget(Ruleset modRules)
		{
			GetText = () => Text;
			GetColor = () => TextColor;
			GetContrastColorDark = () => ContrastColorDark;
			GetContrastColorLight = () => ContrastColorLight;
			GetURLColor = () => URLColor;
			ModRules = modRules;
		}

		protected LabelWidget(LabelWidget other)
			: base(other)
		{
			Text = other.Text;
			Align = other.Align;
			VAlign = other.VAlign;
			Font = other.Font;
			TextColor = other.TextColor;
			Contrast = other.Contrast;
			ContrastColorDark = other.ContrastColorDark;
			ContrastColorLight = other.ContrastColorLight;
			Shadow = other.Shadow;
			WordWrap = other.WordWrap;
			GetText = other.GetText;
			GetColor = other.GetColor;
			GetContrastColorDark = other.GetContrastColorDark;
			GetContrastColorLight = other.GetContrastColorLight;
			GetURLColor = other.GetURLColor;
			URLColor = other.URLColor;
			ClickURL = other.ClickURL;
			ModRules = other.ModRules;
		}

		public override void Draw()
		{
			SpriteFont font;
			if (!Game.Renderer.Fonts.TryGetValue(Font, out font))
				throw new ArgumentException("Requested font '{0}' was not found.".F(Font));

			var text = GetText();
			if (text == null)
				return;

			// At first we only match whole messages that are urls. This is because this logic should
			// be moved out of the label and into the areas of the app that allow urls.
			//
			// My plan would be to have the module that manages chat to parse messages as they are
			// recieved and decide if a piece of text within a message is a url. If it is it should
			// split the message into first part, url and second part. Each of these parts should get
			// it's own LabelWidget. We could then create our own URLLabelWidget to be used for links.
			// This would need to be a repeated opperation for each text part of a message.
			Regex urlRegex = new Regex(@"https://www.google.com");
			if (urlRegex.IsMatch(text))
				ClickURL = text;

			var textSize = font.Measure(text);
			var position = RenderOrigin;
			var offset = font.TopOffset;

			if (VAlign == TextVAlign.Top)
				position += new int2(0, -offset);

			if (VAlign == TextVAlign.Middle)
				position += new int2(0, (Bounds.Height - textSize.Y - offset) / 2);

			if (VAlign == TextVAlign.Bottom)
				position += new int2(0, Bounds.Height - textSize.Y);

			if (Align == TextAlign.Center)
				position += new int2((Bounds.Width - textSize.X) / 2, 0);

			if (Align == TextAlign.Right)
				position += new int2(Bounds.Width - textSize.X, 0);

			if (WordWrap)
				text = WidgetUtils.WrapText(text, Bounds.Width, font);

			var color = ClickURL != null ? GetURLColor() : GetColor();
			var bgDark = GetContrastColorDark();
			var bgLight = GetContrastColorLight();

			if (Contrast)
				font.DrawTextWithContrast(text, position, color, bgDark, bgLight, 2);
			else if (Shadow)
				font.DrawTextWithShadow(text, position, color, bgDark, bgLight, 1);
			else
				font.DrawText(text, position, color);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event != MouseInputEvent.Down && mi.Event != MouseInputEvent.Up)
				return false;

			if (mi.Event == MouseInputEvent.Down && ClickURL != null)
			{
				Game.Sound.PlayNotification(ModRules, null, "Sounds", ClickSound, null);

				// Looks like Process.Start(url) won't work in Mono 6 and up because:
				// https://github.com/mono/mono/issues/17204
				//
				// Instead, we'll need to check platform and implement our own solution.
				// I'veused the code from  https://stackoverflow.com/a/43232486 as the basis
				// for our solution. We should think about how we're going to do this more as
				// it presents an attack vector for maliciously sending users to bad urls or
				// running arbitrary code on their machines e.g.
				//
				// Process.Start("https://google.com; nc -l 8000 | sh"r
				switch (OpenRA.Platform.CurrentPlatform)
				{
					case OpenRA.PlatformType.Windows:
						string url = ClickURL.Replace("&", "^&");
						Process.Start(new ProcessStartInfo("cmd", "/c start " + url) { CreateNoWindow = true });
						break;

					case OpenRA.PlatformType.OSX:
						Process.Start("open", ClickURL);
						break;

					case OpenRA.PlatformType.Linux:
						Process.Start("xdg-open", ClickURL);
						break;

					case OpenRA.PlatformType.Unknown:
					default:
						throw new Exception("Cannot determine operating system while trying to open '" + ClickURL + "' in browser.");
				}

				return true;
			}

			return false;
		}

		public override string GetCursor(int2 pos) { return (ClickURL != null) ? "default-hand" : "default"; }
		public override Widget Clone() { return new LabelWidget(this); }
	}
}
