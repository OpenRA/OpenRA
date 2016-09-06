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

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class CheckboxWidget : ButtonWidget
	{
		public string CheckType = "checked";
		public Func<string> GetCheckType;
		public Func<bool> IsChecked = () => false;
		public int CheckOffset = 2;
		public bool HasPressedState = ChromeMetrics.Get<bool>("CheckboxPressedState");

		[ObjectCreator.UseCtor]
		public CheckboxWidget(ModData modData)
			: base(modData)
		{
			GetCheckType = () => CheckType;
		}

		protected CheckboxWidget(CheckboxWidget other)
			: base(other)
		{
			CheckType = other.CheckType;
			GetCheckType = other.GetCheckType;
			IsChecked = other.IsChecked;
			CheckOffset = other.CheckOffset;
			HasPressedState = other.HasPressedState;
		}

		public override void Draw()
		{
			var disabled = IsDisabled();
			var highlighted = IsHighlighted();
			var font = Game.Renderer.Fonts[Font];
			var color = GetColor();
			var colordisabled = GetColorDisabled();
			var bgDark = GetContrastColorDark();
			var bgLight = GetContrastColorLight();
			var rect = RenderBounds;
			var text = GetText();
			var textSize = font.Measure(text);
			var check = new Rectangle(rect.Location, new Size(Bounds.Height, Bounds.Height));
			var state = disabled ? "checkbox-disabled" :
						highlighted ? "checkbox-highlighted" :
						Depressed && HasPressedState ? "checkbox-pressed" :
						Ui.MouseOverWidget == this ? "checkbox-hover" :
						"checkbox";

			WidgetUtils.DrawPanel(state, check);
			var position = new float2(rect.Left + rect.Height * 1.5f, RenderOrigin.Y - BaseLine + (Bounds.Height - textSize.Y) / 2);

			if (Contrast)
				font.DrawTextWithContrast(text, position,
					disabled ? colordisabled : color, bgDark, bgLight, 2);
			else
				font.DrawText(text, position,
					disabled ? colordisabled : color);

			if (IsChecked() || (Depressed && HasPressedState && !disabled))
			{
				var checkType = GetCheckType();
				if (HasPressedState && (Depressed || disabled))
					checkType += "-disabled";

				var offset = new float2(rect.Left + CheckOffset, rect.Top + CheckOffset);
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage("checkbox-bits", checkType), offset);
			}
		}

		public override Widget Clone() { return new CheckboxWidget(this); }
	}
}
