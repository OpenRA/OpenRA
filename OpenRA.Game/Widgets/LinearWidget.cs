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
using System.Collections.Generic;

namespace OpenRA.Widgets
{
	/// <summary>
	/// Abstract base for Flutter-style Row and Column layout widgets.
	/// Implements the Flutter layout protocol:
	///   1. Constraints go down  — parent passes <see cref="BoxConstraints"/> to each child.
	///   2. Sizes go up          — each child reports the size it chose.
	///   3. Parent sets position — this widget places each child at its final position.
	/// Layout is performed in four sub-passes: (1) measure non-flexible children with an
	/// unbounded main axis, (2) allocate remaining space to flexible children proportionally,
	/// (3) stretch cross-axis if CrossAxisAlignment is Stretch, (4) position all children.
	/// </summary>
	public abstract class LinearWidget : Widget
	{
		// -----------------------------------------------------------------------
		// YAML-configurable properties
		// -----------------------------------------------------------------------

		/// <summary>Spacing (in pixels) between consecutive children.</summary>
		public int Spacing;

		/// <summary>How children are placed along the main axis.</summary>
		public MainAxisAlignment MainAxisAlignment = MainAxisAlignment.Start;

		/// <summary>How children are aligned along the cross axis.</summary>
		public CrossAxisAlignment CrossAxisAlignment = CrossAxisAlignment.Start;

		/// <summary>
		/// Whether this widget claims all available main-axis space (Max) or
		/// only as much as its children need (Min).
		/// </summary>
		public MainAxisSize MainAxisSize = MainAxisSize.Max;

		// -----------------------------------------------------------------------
		// Subclass contract
		// -----------------------------------------------------------------------

		/// <summary>True for <see cref="RowWidget"/>, false for <see cref="ColumnWidget"/>.</summary>
		protected abstract bool IsRow { get; }

		protected LinearWidget() { }

		protected LinearWidget(LinearWidget other)
			: base(other)
		{
			Spacing = other.Spacing;
			MainAxisAlignment = other.MainAxisAlignment;
			CrossAxisAlignment = other.CrossAxisAlignment;
			MainAxisSize = other.MainAxisSize;
		}

		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			var isRow = IsRow;
			var insetH = Padding.Horizontal + Border.Horizontal;
			var insetV = Padding.Vertical + Border.Vertical;

			// Constraints available to the children (inside our padding/border).
			var innerConstraints = constraints.Deflate(Padding).Deflate(Border);

			// ------------------------------------------------------------------
			// Collect visible children.
			// ------------------------------------------------------------------
			var allChildren = new List<Widget>(Children.Count);
			foreach (var child in Children)
				if (child.IsVisible())
					allChildren.Add(child);

			var count = allChildren.Count;
			var sizes = new (int Width, int Height)[count];

			// Spacing only applies for Start/End/Center alignments.
			// SpaceBetween/SpaceAround/SpaceEvenly compute their own inter-child gaps.
			var spacingApplies = MainAxisAlignment is MainAxisAlignment.Start
				or MainAxisAlignment.End
				or MainAxisAlignment.Center;
			var gapTotal = spacingApplies && count > 1 ? (count - 1) * Spacing : 0;

			// ------------------------------------------------------------------
			// Sub-pass 1: measure non-Expanded children with unconstrained main axis.
			// ------------------------------------------------------------------
			var occupiedMain = 0;
			var totalFlexFactor = 0f;
			for (var i = 0; i < count; i++)
			{
				var child = allChildren[i];
				if (child is IFlexible flexible)
				{
					totalFlexFactor += flexible.Flex;
				}
				else
				{
					var mc = innerConstraints.WithUnboundedMain(isRow);
					sizes[i] = child.Measure(mc);
					occupiedMain += isRow ? sizes[i].Width : sizes[i].Height;
				}
			}

			// ------------------------------------------------------------------
			// Sub-pass 2: allocate remaining main-axis space to Expanded children.
			// ------------------------------------------------------------------
			var mainMax = isRow ? innerConstraints.MaxWidth : innerConstraints.MaxHeight;
			var freeMain = Math.Max(0, mainMax - occupiedMain - gapTotal);

			// Cumulative allocation avoids rounding drift: each child gets
			// floor(target) and the last child absorbs any leftover pixel.
			var cumulativeFlex = 0f;
			var usedFlexMain = 0;
			for (var i = 0; i < count; i++)
			{
				if (allChildren[i] is not IFlexible flexible)
					continue;

				cumulativeFlex += flexible.Flex;
				var targetMain = totalFlexFactor > 0
					? (int)(freeMain * cumulativeFlex / totalFlexFactor)
					: 0;
				var allocation = targetMain - usedFlexMain;
				usedFlexMain = targetMain;

				BoxConstraints mc;
				if (flexible.FitTight)
					mc = innerConstraints.WithTightMain(isRow, allocation);
				else
					mc = innerConstraints.WithLooseMain(isRow, allocation);
				sizes[i] = allChildren[i].Measure(mc);
			}

			// ------------------------------------------------------------------
			// Compute this widget's own size.
			// ------------------------------------------------------------------
			var totalChildMain = 0;
			var maxChildCross = 0;
			for (var i = 0; i < count; i++)
			{
				totalChildMain += isRow ? sizes[i].Width : sizes[i].Height;
				maxChildCross = Math.Max(maxChildCross, isRow ? sizes[i].Height : sizes[i].Width);
			}

			totalChildMain += gapTotal;

			// If this widget has an explicit YAML size on an axis, treat it as a tight
			// declaration — it overrides both "fill parent" and "wrap content" behaviour on
			// that axis.  This lets Row/Column widgets with a declared size coexist inside
			// a larger parent without consuming all available space on the cross axis.
			var declaredW = Bounds.Width > 0 ? Bounds.Width - insetH : -1;
			var declaredH = Bounds.Height > 0 ? Bounds.Height - insetV : -1;

			int desiredW, desiredH;
			if (isRow)
			{
				desiredW = declaredW >= 0
					? declaredW
					: MainAxisSize == MainAxisSize.Max && innerConstraints.MaxWidth < BoxConstraints.Unbounded
						? innerConstraints.MaxWidth
						: totalChildMain;
				desiredH = declaredH >= 0
					? declaredH
					: innerConstraints.MaxHeight < BoxConstraints.Unbounded
						? innerConstraints.MaxHeight
						: maxChildCross;
			}
			else
			{
				desiredW = declaredW >= 0
					? declaredW
					: innerConstraints.MaxWidth < BoxConstraints.Unbounded
						? innerConstraints.MaxWidth
						: maxChildCross;
				desiredH = declaredH >= 0
					? declaredH
					: MainAxisSize == MainAxisSize.Max && innerConstraints.MaxHeight < BoxConstraints.Unbounded
						? innerConstraints.MaxHeight
						: totalChildMain;
			}

			var (w, h) = constraints.Constrain(desiredW + insetH, desiredH + insetV);
			Bounds.Width = w;
			Bounds.Height = h;

			// Cross-axis space available to children (inside padding/border).
			var innerCross = isRow ? h - insetV : w - insetH;

			// ------------------------------------------------------------------
			// Sub-pass 3: apply Stretch to cross axis if required.
			// ------------------------------------------------------------------
			if (CrossAxisAlignment == CrossAxisAlignment.Stretch)
			{
				for (var i = 0; i < count; i++)
				{
					var child = allChildren[i];
					var mc = isRow
						? BoxConstraints.Tight(sizes[i].Width, innerCross)
						: BoxConstraints.Tight(innerCross, sizes[i].Height);
					sizes[i] = child.Measure(mc);
				}
			}

			// ------------------------------------------------------------------
			// Sub-pass 4: position children.
			// ------------------------------------------------------------------
			var innerMain = isRow ? w - insetH : h - insetV;
			var mainPositions = CalculateMainPositions(MainAxisAlignment, innerMain, sizes, isRow);

			for (var i = 0; i < count; i++)
			{
				var child = allChildren[i];
				var sz = sizes[i];
				var childCross = isRow ? sz.Height : sz.Width;

				int crossPos;
				switch (CrossAxisAlignment)
				{
					case CrossAxisAlignment.End:
						crossPos = innerCross - childCross;
						break;
					case CrossAxisAlignment.Center:
						crossPos = (innerCross - childCross) / 2;
						break;
					default: // Start or Stretch (Stretch already sized cross to innerCross)
						crossPos = 0;
						break;
				}

				if (isRow)
				{
					child.Bounds.X = mainPositions[i];
					child.Bounds.Y = crossPos;
				}
				else
				{
					child.Bounds.X = crossPos;
					child.Bounds.Y = mainPositions[i];
				}
			}

			return (w, h);
		}

		public override void PerformLayoutIfNeeded()
		{
			if (!layoutDirty)
				return;

			// Use Unbounded for axes that have no explicit size (0), so the widget wraps its children.
			var maxW = Bounds.Width > 0 ? Bounds.Width : BoxConstraints.Unbounded;
			var maxH = Bounds.Height > 0 ? Bounds.Height : BoxConstraints.Unbounded;
			Measure(new BoxConstraints(Bounds.Width, maxW, Bounds.Height, maxH));
			layoutDirty = false;

			foreach (var child in Children)
				child.PerformLayoutIfNeeded();
		}

		int[] CalculateMainPositions(MainAxisAlignment align, int available,
			(int Width, int Height)[] sizes, bool isRow)
		{
			var count = sizes.Length;
			var positions = new int[count];
			if (count == 0)
				return positions;

			var totalChildMain = 0;
			for (var i = 0; i < count; i++)
				totalChildMain += isRow ? sizes[i].Width : sizes[i].Height;

			// Spacing only participates in Start/End/Center; space* modes compute their own gaps.
			var useSpacing = align is MainAxisAlignment.Start
				or MainAxisAlignment.End
				or MainAxisAlignment.Center;
			var gapTotal = useSpacing && count > 1 ? (count - 1) * Spacing : 0;
			var remaining = available - totalChildMain - gapTotal;

			switch (align)
			{
				case MainAxisAlignment.End:
				{
					var pos = remaining;
					for (var i = 0; i < count; i++)
					{
						positions[i] = pos;
						pos += (isRow ? sizes[i].Width : sizes[i].Height) + Spacing;
					}

					break;
				}

				case MainAxisAlignment.Center:
				{
					var pos = remaining / 2;
					for (var i = 0; i < count; i++)
					{
						positions[i] = pos;
						pos += (isRow ? sizes[i].Width : sizes[i].Height) + Spacing;
					}

					break;
				}

				case MainAxisAlignment.SpaceBetween:
				{
					var space = count > 1 ? (float)(available - totalChildMain) / (count - 1) : 0;
					var pos = 0f;
					for (var i = 0; i < count; i++)
					{
						positions[i] = (int)pos;
						pos += (isRow ? sizes[i].Width : sizes[i].Height) + space;
					}

					break;
				}

				case MainAxisAlignment.SpaceAround:
				{
					var space = count > 0 ? (float)(available - totalChildMain) / count : 0;
					var pos = space / 2;
					for (var i = 0; i < count; i++)
					{
						positions[i] = (int)pos;
						pos += (isRow ? sizes[i].Width : sizes[i].Height) + space;
					}

					break;
				}

				case MainAxisAlignment.SpaceEvenly:
				{
					var space = count > 0 ? (float)(available - totalChildMain) / (count + 1) : 0;
					var pos = space;
					for (var i = 0; i < count; i++)
					{
						positions[i] = (int)pos;
						pos += (isRow ? sizes[i].Width : sizes[i].Height) + space;
					}

					break;
				}

				default: // Start
				{
					var pos = 0;
					for (var i = 0; i < count; i++)
					{
						positions[i] = pos;
						pos += (isRow ? sizes[i].Width : sizes[i].Height) + Spacing;
					}

					break;
				}
			}

			return positions;
		}
	}
}
