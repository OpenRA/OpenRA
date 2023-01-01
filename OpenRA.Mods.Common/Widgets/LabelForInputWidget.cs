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
	public class LabelForInputWidget : LabelWidget
	{
		public string For = null;
		public readonly Color TextDisabledColor = ChromeMetrics.Get<Color>("TextDisabledColor");
		readonly Lazy<InputWidget> inputWidget;
		readonly CachedTransform<bool, Color> textColor;

		[ObjectCreator.UseCtor]
		public LabelForInputWidget()
			: base()
		{
			inputWidget = Exts.Lazy(() => Parent.Get<InputWidget>(For));
			textColor = new CachedTransform<bool, Color>(disabled => disabled ? TextDisabledColor : TextColor);
		}

		protected LabelForInputWidget(LabelForInputWidget other)
			: base(other)
		{
			inputWidget = Exts.Lazy(() => Parent.Get<InputWidget>(other.For));
			textColor = new CachedTransform<bool, Color>(disabled => disabled ? TextDisabledColor : TextColor);
		}

		protected override void DrawInner(string text, SpriteFont font, Color color, int2 position)
		{
			font.DrawText(text, position, textColor.Update(inputWidget.Value.IsDisabled()));
		}

		public override Widget Clone() { return new LabelForInputWidget(this); }
	}
}
