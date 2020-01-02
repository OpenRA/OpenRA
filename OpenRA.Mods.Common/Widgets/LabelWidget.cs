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
		public Color TextColorHighlight = ChromeMetrics.Get<Color>("TextfieldColorHighlight");
		public bool Contrast = ChromeMetrics.Get<bool>("TextContrast");
		public bool Shadow = ChromeMetrics.Get<bool>("TextShadow");
		public Color ContrastColorDark = ChromeMetrics.Get<Color>("TextContrastColorDark");
		public Color ContrastColorLight = ChromeMetrics.Get<Color>("TextContrastColorLight");
		public int ContrastRadius = ChromeMetrics.Get<int>("TextContrastRadius");
		public bool WordWrap = false;
		public Func<string> GetText;
		public Func<Color> GetColor;
		public Func<Color> GetColorHighlight;
		public Func<Color> GetContrastColorDark;
		public Func<Color> GetContrastColorLight;

    class Selection
    {
      public enum States { Empty, Primed, Active, Inactive };
      public States State = States.Empty;
      public int First = -1;
      public int Last = -1;

      public int Start {
        get {
          return Math.Min(First, Last);
        }
      }

      public int End {
        get {
          return Math.Max(First, Last);
        }
      }

      public Selection() { }

      public bool Prime()
      {
        if (State == States.Active && State == States.Inactive)
          return false;

        State = States.Primed;
        return true;
      }

      public bool Activate(int first)
      {
        if (State == States.Primed)
        {
          State = States.Active;
          First = first;
          Last = first;
          return true;
        }

        return false;
      }

      public bool Update(int last)
      {
        if (State == States.Active)
        {
          Last = last;
          return true;
        }

        return false;
      }

      public bool Deactivate()
      {
        State = States.Inactive;
        return true;
      }

      public bool Reset()
      {
        State = States.Empty;
        return true;
      }
    }

    Selection selection = new Selection();

		public LabelWidget()
		{
			GetText = () => Text;
			GetColor = () => TextColor;
      GetColorHighlight = () => TextColorHighlight;
			GetContrastColorDark = () => ContrastColorDark;
			GetContrastColorLight = () => ContrastColorLight;
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
			ContrastRadius = other.ContrastRadius;
			Shadow = other.Shadow;
			WordWrap = other.WordWrap;
			GetText = other.GetText;
			GetColor = other.GetColor;
      GetColorHighlight = other.GetColorHighlight;
			GetContrastColorDark = other.GetContrastColorDark;
			GetContrastColorLight = other.GetContrastColorLight;
		}

    // Allow same checks when ever we get thee font, should this be cached?
    SpriteFont GetFont()
    {
			SpriteFont font;
			if (!Game.Renderer.Fonts.TryGetValue(Font, out var font))
				throw new ArgumentException("Requested font '{0}' was not found.".F(Font));

      return font;
    }

    // allow getting the content with the same checks elsewhere, should this be cached?
    string GetTextContent()
    {
      var text = GetText();
      var font = GetFont();

			if (WordWrap)
				text = WidgetUtils.WrapText(text, Bounds.Width, font);

      return text;
    }

    // Allow access to the position elsewhere, should this be cached?
    int2 GetPosition()
    {
      var font = GetFont();
			var text = GetTextContent();

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

      return position;
    }

		public override void Draw()
		{
      var text = GetTextContent();
      if (text == null)
        return;

			DrawInner(text, font, GetColor(), position);
		}

		protected virtual void DrawInner(string text, SpriteFont font, Color color, int2 position)
		{
      var font = GetFont();
			var color = GetColor();
			var bgDark = GetContrastColorDark();
			var bgLight = GetContrastColorLight();
      var bgHighlight = GetColorHighlight();
      var position = GetPosition();

      if (selection.State != Selection.States.Empty)
        font.DrawTextWithSelection(text, position, color, bgHighlight, selection.Start, selection.End);
			else if (Contrast)
				font.DrawTextWithContrast(text, position, color, bgDark, bgLight, 2);
			else if (Shadow)
				font.DrawTextWithShadow(text, position, color, bgDark, bgLight, 1);
			else
				font.DrawText(text, position, color);
		}

		public override Widget Clone() { return new LabelWidget(this); }

		public override bool HandleMouseInput(MouseInput mi)
    {
      switch (mi.Event)
      {
        case MouseInputEvent.Down:
          if (selection.State == Selection.States.Empty)
            return selection.Prime();

          if (selection.State == Selection.States.Inactive)
            return selection.Reset();

          break;

        case MouseInputEvent.Move:
          // only calculate the nearest index if we have to.
          if (selection.State == Selection.States.Empty || selection.State == Selection.States.Inactive)
            return false;

          var nearestIndex = WidgetUtils.FindNearestIndex(
              GetTextContent(),
              GetPosition(),
              GetFont(),
              mi.Location);

          if (selection.State == Selection.States.Primed)
            return selection.Activate(nearestIndex);

          if (selection.State == Selection.States.Active)
            return selection.Update(nearestIndex);

          break;

        case MouseInputEvent.Up:
          if (selection.State == Selection.States.Active)
            return selection.Deactivate();

          break;

        default:
          throw new Exception("Unrecognized MouseEvent on Label.");
      }

      return false;
    }
	}
}
