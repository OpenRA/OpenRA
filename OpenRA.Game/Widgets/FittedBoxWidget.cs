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
	/// Flutter-style widget that scales and positions its single child to fit within
	/// the available space according to a <see cref="BoxFit"/> strategy.
	/// Equivalent to Flutter's <c>FittedBox</c> widget.
	/// <para>
	/// The child is first measured with unconstrained (infinite) constraints to
	/// determine its natural size, then a uniform scale factor is computed and applied
	/// by setting the child's <c>Bounds</c> to the scaled dimensions. The child's
	/// origin is adjusted to align it within the available space.
	/// </para>
	/// </summary>
	public class FittedBoxWidget : Widget
	{
		/// <summary>How the child should be inscribed into the available space.</summary>
		public BoxFit Fit = BoxFit.Contain;

		/// <summary>How the child is aligned within the available space after scaling.</summary>
		public HorizontalAlignment HorizontalAlignment = HorizontalAlignment.Center;

		/// <summary>How the child is aligned vertically within the available space.</summary>
		public VerticalAlignment VerticalAlignment = VerticalAlignment.Center;

		public FittedBoxWidget() { }

		public FittedBoxWidget(FittedBoxWidget other)
			: base(other)
		{
			Fit = other.Fit;
			HorizontalAlignment = other.HorizontalAlignment;
			VerticalAlignment = other.VerticalAlignment;
		}

		public override FittedBoxWidget Clone() { return new FittedBoxWidget(this); }

		public override (int Width, int Height) Measure(BoxConstraints constraints)
		{
			// This widget always fills the parent's available space.
			var w = constraints.MaxWidth < BoxConstraints.Unbounded
				? constraints.MaxWidth
				: Bounds.Width > 0 ? Bounds.Width : 0;
			var h = constraints.MaxHeight < BoxConstraints.Unbounded
				? constraints.MaxHeight
				: Bounds.Height > 0 ? Bounds.Height : 0;

			(w, h) = constraints.Constrain(w, h);
			Bounds.Width = w;
			Bounds.Height = h;

			if (Children.Count > 0 && w > 0 && h > 0)
			{
				var child = Children[0];

				int scaledW, scaledH;
				if (Fit == BoxFit.Fill)
				{
					// Fill stretches the child to the exact available size — no natural measure needed.
					(scaledW, scaledH) = (w, h);
				}
				else
				{
					// Measure child at its natural (unconstrained) size, then apply the fit strategy.
					var (naturalW, naturalH) = child.Measure(BoxConstraints.Infinite());
					(scaledW, scaledH) = naturalW > 0 && naturalH > 0
						? ApplyFit(Fit, naturalW, naturalH, w, h)
						: (0, 0);
				}

				if (scaledW > 0 && scaledH > 0)
				{
					child.Measure(BoxConstraints.Tight(scaledW, scaledH));

					child.Bounds.X = HorizontalAlignment switch
					{
						HorizontalAlignment.Right => w - scaledW,
						HorizontalAlignment.Center => (w - scaledW) / 2,
						_ => 0
					};

					child.Bounds.Y = VerticalAlignment switch
					{
						VerticalAlignment.Bottom => h - scaledH,
						VerticalAlignment.Center => (h - scaledH) / 2,
						_ => 0
					};
				}
				else
				{
					child.Bounds.X = 0;
					child.Bounds.Y = 0;
				}
			}

			return (w, h);
		}

		static (int Width, int Height) ApplyFit(BoxFit fit, int srcW, int srcH, int dstW, int dstH)
		{
			switch (fit)
			{
				case BoxFit.None:
					return (Math.Min(srcW, dstW), Math.Min(srcH, dstH));

				case BoxFit.Cover:
				{
					var scale = Math.Max((float)dstW / srcW, (float)dstH / srcH);
					return (Math.Min((int)Math.Round(srcW * scale), dstW),
						Math.Min((int)Math.Round(srcH * scale), dstH));
				}

				case BoxFit.FitWidth:
				{
					var scale = (float)dstW / srcW;
					return (dstW, (int)Math.Round(srcH * scale));
				}

				case BoxFit.FitHeight:
				{
					var scale = (float)dstH / srcH;
					return ((int)Math.Round(srcW * scale), dstH);
				}

				case BoxFit.ScaleDown:
				{
					var scale = Math.Min(1f, Math.Min((float)dstW / srcW, (float)dstH / srcH));
					return ((int)Math.Round(srcW * scale), (int)Math.Round(srcH * scale));
				}

				default: // Contain
				{
					var scale = Math.Min((float)dstW / srcW, (float)dstH / srcH);
					return ((int)Math.Round(srcW * scale), (int)Math.Round(srcH * scale));
				}
			}
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
