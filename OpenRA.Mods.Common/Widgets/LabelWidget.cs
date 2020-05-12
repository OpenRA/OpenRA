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
      public enum States { Empty, Ready, Active, Inactive };
      public States State = States.Empty;
      public int First = -1;
      public int Last = -1;
      public LabelWidget selectionWidget = null;

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

      public bool OwnedBy(LabelWidget widget)
      {
        return selectionWidget == widget;
      }

      public bool HandleMouseDown(LabelWidget widget)
      {
        Console.WriteLine("State: " + State.ToString());
        Console.WriteLine("Mouse Down");
        switch (State)
        {
          case States.Empty:
          case States.Inactive:
            State = States.Ready;
            First = -1;
            Last = -1;
            selectionWidget = widget;
            return true;
          default:
            return false;
        }
      }

      public bool HandleMouseMove(int index)
      {
        Console.WriteLine("State: " + State.ToString());
        Console.WriteLine("Mouse Move");
        switch (State)
        {
          case States.Ready:
            First = index;
            Last = index;
            State = States.Active;
            return true;
          case States.Active:
            Last = index;
            return true;
          default:
            return false;
        }
      }

      public bool HandleMouseUp()
      {
        Console.WriteLine("State: " + State.ToString());
        Console.WriteLine("Mouse Up");
        switch (State)
        {
          case States.Active:
            State = States.Inactive;
            return true;
          case States.Ready:
            State = States.Empty;
            selectionWidget = null;
            return true;
          default:
            return false;
        }
      }

      public bool HandleMouseExit()
      {
        Console.WriteLine("State: " + State.ToString());
        Console.WriteLine("Mouse Exit");
        switch (State)
        {
          case States.Active:
            State = States.Inactive;
            return true;
          case States.Ready:
            State = States.Empty;
            selectionWidget = null;
            return true;
          default:
            return false;
        }
      }

      public bool HandleMouseEnter(LabelWidget widget)
      {
        if (!OwnedBy(widget))
          return false;

        switch(State)
        {
          case States.Inactive:
            State = States.Active;
            return true;
          default:
            return false;
        }
      }
    }

    static Selection selection = new Selection();

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

			DrawInner(text, GetFont(), GetColor(), GetPosition());
		}

		protected virtual void DrawInner(string text, SpriteFont font, Color color, int2 position)
		{
			var bgDark = GetContrastColorDark();
			var bgLight = GetContrastColorLight();
      var bgHighlight = GetColorHighlight();

      if (selection.OwnedBy(this) && selection.State != Selection.States.Empty)
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
          return selection.HandleMouseDown(this);
        case MouseInputEvent.Move:
          return selection.HandleMouseMove(WidgetUtils.FindNearestIndex(
              GetTextContent(),
              GetPosition(),
              GetFont(),
              mi.Location));
        case MouseInputEvent.Up:
          return selection.HandleMouseUp();
        default:
          throw new Exception("Unrecognized MouseEvent on Label.");
      }
    }

    public override void MouseExited()
    {
      selection.HandleMouseExit();
    }

    public override void MouseEntered()
    {
      // TODO: Enable Selections to continue after they leave the widget so long as the mouse hasn't gone down yet
      //       This is currently hard to do as widgets only listen to mouse events on themselves. We would require
      //       some sort of observer architecture where windown widgets are observable allowing us to set the selection
      //       to Ready if a mouse down occurred outside of the widget.
      // selection.HandleMouseEnter(this);
    }
  }
}
