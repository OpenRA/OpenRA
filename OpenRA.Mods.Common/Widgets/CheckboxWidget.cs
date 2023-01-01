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
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class CheckboxWidget : ButtonWidget
	{
		public new string Background = "checkbox";
		public string Checkmark = "tick";
		public Func<string> GetCheckmark;
		public Func<bool> IsChecked = () => false;

		readonly CachedTransform<(string, bool), CachedTransform<(bool, bool, bool, bool, bool), Sprite>> getCheckmarkImageCache
			= new CachedTransform<(string, bool), CachedTransform<(bool, bool, bool, bool, bool), Sprite>>(
			((string CheckType, bool Checked) args) =>
			{
				var variantImageCollection = "checkmark-" + args.CheckType;
				var variantBaseName = args.Checked ? "checked" : "unchecked";
				return WidgetUtils.GetCachedStatefulImage(variantImageCollection, variantBaseName);
			});

		[ObjectCreator.UseCtor]
		public CheckboxWidget(ModData modData)
			: base(modData)
		{
			GetCheckmark = () => Checkmark;
			TextColor = ChromeMetrics.Get<Color>("TextColor");
			TextColorDisabled = ChromeMetrics.Get<Color>("TextDisabledColor");
			GetColor = () => TextColor;
			GetColorDisabled = () => TextColorDisabled;
		}

		protected CheckboxWidget(CheckboxWidget other)
			: base(other)
		{
			Background = other.Background;
			Checkmark = other.Checkmark;
			GetCheckmark = other.GetCheckmark;
			IsChecked = other.IsChecked;
			TextColor = other.TextColor;
			TextColorDisabled = other.TextColorDisabled;
			GetColor = other.GetColor;
			GetColorDisabled = other.GetColorDisabled;
		}

		public override void Draw()
		{
			var disabled = IsDisabled();
			var font = Game.Renderer.Fonts[Font];
			var hover = Ui.MouseOverWidget == this;
			var color = GetColor();
			var colordisabled = GetColorDisabled();
			var text = GetText();
			var rect = new Rectangle(RenderBounds.Location, new Size(Bounds.Height, Bounds.Height));

			DrawBackground(Background, rect, disabled, Depressed, hover, IsHighlighted());

			var textPosition = new float2(RenderBounds.Left + RenderBounds.Height * 1.5f, RenderOrigin.Y + (Bounds.Height - font.Measure(text).Y - font.TopOffset) / 2);
			if (Contrast)
				font.DrawTextWithContrast(text, textPosition,
					disabled ? colordisabled : color, GetContrastColorDark(), GetContrastColorLight(), 2);
			else
				font.DrawText(text, textPosition,
					disabled ? colordisabled : color);

			var checkmarkImage = getCheckmarkImageCache
				.Update((GetCheckmark(), IsChecked()))
				.Update((disabled, Depressed, hover, false, IsHighlighted()));
			WidgetUtils.DrawSprite(checkmarkImage, new float2(rect.Right - (int)((rect.Height + checkmarkImage.Size.X) / 2), rect.Top + (int)((rect.Height - checkmarkImage.Size.Y) / 2)));
		}

		public override Widget Clone() { return new CheckboxWidget(this); }
	}
}
