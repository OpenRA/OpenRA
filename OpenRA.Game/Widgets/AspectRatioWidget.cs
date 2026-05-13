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

namespace OpenRA.Widgets
{
	/// <summary>
	/// Flutter-style widget that sizes its single child to a specific aspect ratio.
	/// Equivalent to Flutter's <c>AspectRatio</c> widget.
	/// <para>
	/// The widget first tries to use the parent's width to derive the height
	/// (height = width / <see cref="Ratio"/>). If that violates the constraints
	/// it tries from the height. Falls back to the largest fitting box.
	/// </para>
	/// </summary>
	public class AspectRatioWidget : Widget
	{
		/// <summary>
		/// Width-to-height ratio (e.g. 16/9 ≈ 1.778 for widescreen).
		/// Must be greater than zero.
		/// </summary>
		public float Ratio = 1f;

		public AspectRatioWidget() { }

		public AspectRatioWidget(AspectRatioWidget other)
			: base(other)
		{
			Ratio = other.Ratio;
		}

		public override AspectRatioWidget Clone() { return new AspectRatioWidget(this); }

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);
			if (Ratio <= 0)
				throw new InvalidOperationException($"AspectRatioWidget '{Id}': Ratio must be greater than zero, got {Ratio}.");
		}

		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			var ratio = Ratio;

			// Try width-first: derive height from available width.
			var maxW = constraints.MaxWidth < BoxConstraints.Unbounded
				? constraints.MaxWidth
				: constraints.MinWidth;
			var derivedH = (int)Math.Round(maxW / ratio);

			int w, h;
			if (derivedH >= constraints.MinHeight && derivedH <= constraints.MaxHeight)
			{
				w = maxW;
				h = derivedH;
			}
			else
			{
				// Try height-first.
				var maxH = constraints.MaxHeight < BoxConstraints.Unbounded
					? constraints.MaxHeight
					: constraints.MinHeight;
				var derivedW = (int)Math.Round(maxH * ratio);

				if (derivedW >= constraints.MinWidth && derivedW <= constraints.MaxWidth)
				{
					w = derivedW;
					h = maxH;
				}
				else
				{
					// Fallback: clamp within constraints.
					(w, h) = constraints.Constrain((int)Math.Round(maxH * ratio), maxH);
				}
			}

			Bounds.Width = w;
			Bounds.Height = h;

			if (Children.Count > 0)
			{
				var child = Children[0];
				child.Measure(BoxConstraints.Tight(w, h));
				child.Bounds.X = 0;
				child.Bounds.Y = 0;
			}

			return (w, h);
		}

		public override void PerformLayoutIfNeeded()
		{
			if (!layoutDirty)
				return;

			Measure(BoxConstraints.Tight(Bounds.Width, Bounds.Height));
			layoutDirty = false;

			foreach (var child in Children)
				child.PerformLayoutIfNeeded();
		}
	}
}
