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

using System;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	class LabelWithSelectionWidget : LabelWidget
	{
		[Translate]
		public Color TextColorHighlight = ChromeMetrics.Get<Color>("TextfieldColorHighlight");
		public Func<Color> GetColorHighlight;
		static Selection selection = new Selection();


		public LabelWithSelectionWidget()
		{
			GetColorHighlight = () => TextColorHighlight;
		}

		protected LabelWithSelectionWidget(LabelWithSelectionWidget other)
			: base(other)
		{
			GetColorHighlight = other.GetColorHighlight;
		}

		protected override void DrawInner(string text, SpriteFont font, Color color, int2 position)
		{
			var bgDark = GetContrastColorDark();
			var bgLight = GetContrastColorLight();
			var bgHighlight = GetColorHighlight();

			if (selection.State != Selection.States.Empty && selection.OwnedBy(this))
				font.DrawTextWithSelection(text, position, color, bgHighlight, selection.Start, selection.End);
			else if (Contrast)
				font.DrawTextWithContrast(text, position, color, bgDark, bgLight, 2);
			else if (Shadow)
				font.DrawTextWithShadow(text, position, color, bgDark, bgLight, 1);
			else
				font.DrawText(text, position, color);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			switch (mi.Event)
			{
				case MouseInputEvent.Down:
					return selection.HandleMouseDown(this, mi.Location);
				case MouseInputEvent.Move:
					return selection.HandleMouseMove(mi.Location);
				case MouseInputEvent.Up:
					// Can't copy stuff if we don't have keyboard focus!
					if (!RenderBounds.Contains(mi.Location) || !TakeKeyboardFocus())
						return false;

					return selection.HandleMouseUp();
			}

			return false;
		}

		public override void MouseExited()
		{
			selection.HandleMouseExit();
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up)
				return false;

			var isOSX = Platform.CurrentPlatform == PlatformType.OSX;

			if ((!isOSX && !e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && !e.Modifiers.HasModifier(Modifiers.Meta)))
				return false;

			if (e.Key != Keycode.C)
				return false;

			if (!selection.OwnedBy(this))
				return false;

			if (selection.State == Selection.States.Empty)
				return false;

			if (string.IsNullOrEmpty(selection.SelectedText))
				return false;

			Game.Renderer.SetClipboardText(selection.SelectedText);

			return true;
		}

		public override Widget Clone() { return new LabelWithSelectionWidget(this); }
	}
}
