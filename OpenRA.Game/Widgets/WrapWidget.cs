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
	/// Flutter-style widget that lays out children in a horizontal run and wraps to a
	/// new line when they would overflow. Equivalent to Flutter's <c>Wrap</c> widget.
	/// <para>
	/// Children are measured with unconstrained constraints on the main axis. If a child
	/// is wider than the available width it is placed on its own line.
	/// </para>
	/// </summary>
	public class WrapWidget : Widget
	{
		// -----------------------------------------------------------------------
		// YAML-configurable properties
		// -----------------------------------------------------------------------

		/// <summary>Spacing between children on the main (horizontal) axis.</summary>
		public int Spacing;

		/// <summary>Spacing between successive runs on the cross (vertical) axis.</summary>
		public int RunSpacing;

		/// <summary>How each run is aligned on the main axis.</summary>
		public WrapAlignment Alignment = WrapAlignment.Start;

		/// <summary>How runs are aligned on the cross axis.</summary>
		public WrapCrossAlignment CrossAxisAlignment = WrapCrossAlignment.Start;

		public WrapWidget() { }

		public WrapWidget(WrapWidget other)
			: base(other)
		{
			Spacing = other.Spacing;
			RunSpacing = other.RunSpacing;
			Alignment = other.Alignment;
			CrossAxisAlignment = other.CrossAxisAlignment;
		}

		public override WrapWidget Clone() { return new WrapWidget(this); }

		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			var maxWidth = constraints.MaxWidth < BoxConstraints.Unbounded
				? constraints.MaxWidth
				: BoxConstraints.Unbounded;

			// Collect visible children and measure them with an unbounded main axis.
			var allChildren = new List<Widget>(Children.Count);
			foreach (var child in Children)
				if (child.IsVisible())
					allChildren.Add(child);

			var childSizes = new (int Width, int Height)[allChildren.Count];
			for (var i = 0; i < allChildren.Count; i++)
				childSizes[i] = allChildren[i].Measure(BoxConstraints.Loose(maxWidth, constraints.MaxHeight));

			// Build runs.
			var runs = new List<List<int>>();   // each run holds child indices
			var run = new List<int>();
			var runWidth = 0;

			for (var i = 0; i < allChildren.Count; i++)
			{
				var childW = childSizes[i].Width;
				var needed = run.Count > 0 ? runWidth + Spacing + childW : childW;

				if (run.Count > 0 && needed > maxWidth)
				{
					runs.Add(run);
					run = [];
					runWidth = childW;
				}
				else
				{
					runWidth = needed;
				}

				run.Add(i);
			}

			if (run.Count > 0)
				runs.Add(run);

			// Position children run by run.
			var y = 0;
			var totalWidth = 0;
			var runCount = runs.Count;

			for (var runIndex = 0; runIndex < runCount; runIndex++)
			{
				var r = runs[runIndex];

				// Height of this run.
				var runH = 0;
				var runTotalW = 0;
				for (var k = 0; k < r.Count; k++)
				{
					runH = Math.Max(runH, childSizes[r[k]].Height);
					runTotalW += childSizes[r[k]].Width;
					if (k > 0) runTotalW += Spacing;
				}

				// Main-axis starting offset for this run.
				int startX;
				switch (Alignment)
				{
					case WrapAlignment.End:
						startX = Math.Max(0, maxWidth - runTotalW);
						break;
					case WrapAlignment.Center:
						startX = Math.Max(0, (maxWidth - runTotalW) / 2);
						break;
					case WrapAlignment.SpaceBetween:
						startX = 0;
						break;
					case WrapAlignment.SpaceAround:
						startX = r.Count > 0 ? (maxWidth - runTotalW) / (r.Count * 2) : 0;
						break;
					case WrapAlignment.SpaceEvenly:
						startX = r.Count > 0 ? (maxWidth - runTotalW) / (r.Count + 1) : 0;
						break;
					default: // Start
						startX = 0;
						break;
				}

				// Inter-child gap for spacing modes. SpaceBetween/SpaceAround/SpaceEvenly each
				// compute their own gap; Start/End/Center fall back to the plain Spacing value.
				// SpaceBetween with a single child degenerates to Start (Flutter semantics).
				float gapBetween;
				if (Alignment == WrapAlignment.SpaceBetween && r.Count > 1)
					gapBetween = (float)(maxWidth - runTotalW) / (r.Count - 1);
				else if (Alignment == WrapAlignment.SpaceAround && r.Count > 0)
					gapBetween = (float)(maxWidth - runTotalW) / r.Count;
				else if (Alignment == WrapAlignment.SpaceEvenly && r.Count > 0)
					gapBetween = (float)(maxWidth - runTotalW) / (r.Count + 1);
				else
					gapBetween = Spacing;

				var x = (float)startX;
				for (var k = 0; k < r.Count; k++)
				{
					var ci = r[k];
					var child = allChildren[ci];
					var sz = childSizes[ci];

					int crossOffset;
					switch (CrossAxisAlignment)
					{
						case WrapCrossAlignment.End:
							crossOffset = runH - sz.Height;
							break;
						case WrapCrossAlignment.Center:
							crossOffset = (runH - sz.Height) / 2;
							break;
						default: // Start
							crossOffset = 0;
							break;
					}

					child.Bounds.X = (int)x;
					child.Bounds.Y = y + crossOffset;

					x += sz.Width + (k < r.Count - 1 ? gapBetween : 0);
					totalWidth = Math.Max(totalWidth, child.Bounds.X + sz.Width);
				}

				y += runH + (runIndex < runCount - 1 ? RunSpacing : 0);
			}

			var (w, h) = constraints.Constrain(
				maxWidth < BoxConstraints.Unbounded ? maxWidth : totalWidth,
				y);

			Bounds.Width = w;
			Bounds.Height = h;
			return (w, h);
		}

		public override void PerformLayoutIfNeeded()
		{
			if (!layoutDirty)
				return;

			var maxW = Bounds.Width > 0 ? Bounds.Width : BoxConstraints.Unbounded;
			var maxH = Bounds.Height > 0 ? Bounds.Height : BoxConstraints.Unbounded;
			Measure(new BoxConstraints(Bounds.Width, maxW, Bounds.Height, maxH));
			layoutDirty = false;

			foreach (var child in Children)
				child.PerformLayoutIfNeeded();
		}
	}
}
