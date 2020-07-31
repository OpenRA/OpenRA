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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	class Selection
	{
		public enum States { Empty, Ready, Active, Inactive }
		public States State = States.Empty;
		public LabelWithSelectionWidget SelectionWidget = null;
		public int2 StartingLocation;
		public int2 EndingLocation;

		RenderedText TextAsDrawn
		{
			get
			{
				return RenderedText.From(
						SelectionWidget.GetTextContent(),
						SelectionWidget.GetFont(),
						SelectionWidget.GetPosition());
			}
		}

		public int Start
		{
			get
			{
				return Math.Min(
						TextAsDrawn.ClosestCharacter(StartingLocation).Index,
						TextAsDrawn.ClosestCharacter(EndingLocation).Index);
			}
		}

		public int End
		{
			get
			{
				return Math.Max(
						TextAsDrawn.ClosestCharacter(StartingLocation).Index,
						TextAsDrawn.ClosestCharacter(EndingLocation).Index);
			}
		}

		public Selection() { }

		public bool OwnedBy(LabelWidget widget)
		{
			return SelectionWidget == widget;
		}

		public bool HandleMouseDown(LabelWithSelectionWidget widget, int2 location)
		{
			if (object.ReferenceEquals(widget, null))
				return false;

			switch (State)
			{
				case States.Empty:
				case States.Inactive:
					SelectionWidget = widget;
					StartingLocation = location;
					EndingLocation = location;
					State = States.Ready;
					return true;
				default:
					return false;
			}
		}

		public bool HandleMouseMove(int2 location)
		{
			if (object.ReferenceEquals(SelectionWidget, null))
				return false;

			switch (State)
			{
				case States.Ready:
					EndingLocation = location;
					State = States.Active;
					return true;
				case States.Active:
					EndingLocation = location;
					return true;
				default:
					return false;
			}
		}

		public bool HandleMouseUp()
		{
			switch (State)
			{
				case States.Active:
					State = States.Inactive;
					return true;
				case States.Ready:
					State = States.Empty;
					SelectionWidget = null;
					return true;
				default:
					return false;
			}
		}

		public bool HandleLooseMouseFocus()
		{
			State = States.Empty;
			SelectionWidget = null;
			return true;
		}

		class RenderedText
		{
			public class Character
			{
				// NOTE: What happens when we handle unicode?
				public char Value;
				public int Index;
				public int LineIndex;
				public Rectangle Bounds;

				public static Character From(char val, int index, int lineIndex, SpriteFont font, int2 topLeft)
				{
					var bounds = Rectangle.FromCorners(topLeft, topLeft + font.Measure(val.ToString()));
					return new Character(val, index, lineIndex, bounds);
				}

				public Character(char val, int index, int lineIndex, Rectangle bounds)
				{
					Value = val;
					Index = index;
					LineIndex = lineIndex;
					Bounds = bounds;
				}
			}

			public class Line
			{
				public string Content;
				public int Index;
				public Rectangle Bounds;
				public List<Character> Characters;

				public static Line From(string content, int lineIndex, int startingCharIndex, SpriteFont font, int2 topLeft)
				{
					var bounds = Rectangle.FromCorners(topLeft, topLeft + font.Measure(content));
					var characters = new List<Character>();
					var left = topLeft.X;

					for (var index = 0; index < content.Length; index++)
					{
						var character = Character.
							From(content[index], index + startingCharIndex, index, font, new int2(left, topLeft.Y));
						characters.Add(character);
						left += character.Bounds.Width;
					}

					return new Line(content, lineIndex, bounds, characters);
				}

				public Line(string content, int index, Rectangle bounds, List<Character> characters)
				{
					Content = content;
					Index = index;
					Bounds = bounds;
					Characters = characters;
				}

				public Character ClosestCharacter(int2 point)
				{
					return Characters.
						OrderBy(character => character.Bounds.DistanceFromCenter(point)).
						First();
				}
			}

			public string Content;
			public Rectangle Bounds;
			public List<Line> Lines;

			public static RenderedText From(string content, SpriteFont font, int2 topLeft)
			{
				var bounds = Rectangle.FromCorners(topLeft, topLeft + font.Measure(content));
				var rawLines = content.Split('\n');
				var lines = new List<Line>();
				var top = topLeft.Y;
				var characterIndex = 0;

				for (var index = 0; index < rawLines.Count(); index++)
				{
					var line = Line.From(rawLines[index], index, characterIndex, font, new int2(topLeft.X, top));
					lines.Add(line);
					top += line.Bounds.Height;

					// add one for the new line
					characterIndex += line.Content.Length;
				}

				return new RenderedText(content, bounds, lines);
			}

			public RenderedText(string content, Rectangle bounds, List<Line> lines)
			{
				Content = content;
				Bounds = bounds;
				Lines = lines;
			}

			public Character ClosestCharacter(int2 point)
			{
				var closestLine = Lines.FirstOrDefault(line => line.Bounds.Contains(point));

				if (object.ReferenceEquals(closestLine, null))
					closestLine = Lines.
						OrderBy(line => line.Bounds.ShortestDistanceFromEdge(point)).
						First();

				return closestLine.ClosestCharacter(point);
			}
		}

		public string SelectedText
		{
			get
			{
				return SelectionWidget.GetTextContent().Substring(Start, End - Start + 1);
			}
		}
	}
}
