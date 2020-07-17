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
	class Selection
	{
		public enum States { Empty, Ready, Active, Inactive }
		public States State = States.Empty;
		public int First = -1;
		public int Last = -1;
		public LabelWidget SelectionWidget = null;
		public int2 StartingLocation;

		public int Start
		{
			get
			{
				return Math.Min(First, Last);
			}
		}

		public int End
		{
			get
			{
				return Math.Max(First, Last);
			}
		}

		public string SelectedText
		{
			get
			{
				var text = SelectionWidget.GetTextContent();
				return text.Substring(Math.Max(0, Start), Math.Min(End - Start, text.Length));
			}
		}

		public Selection() { }

		public bool OwnedBy(LabelWidget widget)
		{
			return SelectionWidget == widget;
		}

		public bool HandleMouseDown(LabelWidget widget, int2 location)
		{
			if (object.ReferenceEquals(widget, null))
				return false;

			switch (State)
			{
				case States.Empty:
				case States.Inactive:
					SelectionWidget = widget;
					StartingLocation = location;
					State = States.Ready;
					First = Last = FindNearestIndex(location);
					return true;
				default:
					return false;
			}
		}

		public bool HandleMouseMove(int2 location)
		{
			if (object.ReferenceEquals(SelectionWidget, null))
				return false;

			var index = FindNearestIndex(location);

			switch (State)
			{
				case States.Ready:
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
			switch (State)
			{
				case States.Active:
					State = States.Inactive;
					return true;
				case States.Ready:
					State = States.Empty;
					SelectionWidget = null;
					// while we did technically handle the event, it was really a false alarm.
					// No selection was made so let parent elements handle the interaction.
					return false;
				default:
					return false;
			}
		}

		public bool HandleMouseExit()
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

		public bool HandleMouseEnter(LabelWidget widget)
		{
			if (!OwnedBy(widget))
				return false;

			switch (State)
			{
				case States.Inactive:
					State = States.Active;
					return true;
				default:
					return false;
			}
		}

		class Constraint
		{
			public int Low;
			public int High;
			public int Threshold;
			public bool LowToHigh;

			public Constraint(int low, int high, int threshold = 0, bool lowToHigh = true)
			{
				Low = low;
				High = high;
				Threshold = threshold;
				LowToHigh = lowToHigh;
			}

			public bool Check(int val)
			{
				if (LowToHigh)
				{
					return Low <= val && val < High;
				}
				else
				{
					return Low < val && val <= High;
				}
			}

			public bool MeetsThreshold(int val)
			{
				if (LowToHigh)
				{
					return Low + Threshold <= val;
				}
				else
				{
					return val <= High - Threshold;
				}
			}
		}

		public int FindNearestIndex(int2 location)
		{
			if (object.ReferenceEquals(SelectionWidget, null))
				return -1;

			if (object.ReferenceEquals(location, null))
				return -1;

			var font = SelectionWidget.GetFont();
			var nearestIndex = -1;
			var x = SelectionWidget.GetPosition().X;
			var y = SelectionWidget.GetPosition().Y;
			string startingLine = null;

			foreach (var line in SelectionWidget.GetTextContent().Split('\n'))
			{
				var lineTop = y;
				var lineHeight = font.Measure(line).Y;
				var lineBottom = lineTop + lineHeight;
				var isInThisLine = new Constraint(lineTop, lineBottom);

				if (isInThisLine.Check(StartingLocation.Y))
				{
					startingLine = line;
				}

				if (isInThisLine.Check(location.Y))
				{
					foreach (var character in line)
					{
						nearestIndex += 1;

						// We need the direction of selection to determine how to apply the threshold
						// of selecting the character e.g. If we're selecting left to right we want to
						// only select the character once we're > half way through comming from the start
						// of the character, else we want to select the character when we're > half way
						// through comming from the end of the character.
						var leftToRight = (startingLine == line && StartingLocation.X < location.X)
							|| (startingLine != line && StartingLocation.Y < location.Y);

						var characterLeft = x;
						var characterWidth = font.Measure(character.ToString()).X;
						var characterRight = characterLeft + characterWidth;
						var isThisCharacter = new Constraint(
								characterLeft,
								characterRight,
								characterWidth / 2,
								leftToRight);

						if (isThisCharacter.Check(location.X))
						{
							if (isThisCharacter.MeetsThreshold(location.X))
							{
								return leftToRight ? nearestIndex + 1 : nearestIndex;
							}
							else
							{
								return leftToRight ? nearestIndex : nearestIndex + 1;
							}
						}

						x = characterRight;
					}
				}
				else
				{
					nearestIndex += line.Length;
				}

				// for the missing "\n"
				nearestIndex += 1;
				y = lineBottom;
			}

			return nearestIndex;
		}
	}
}
