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

using NUnit.Framework;
using OpenRA.Widgets;

namespace OpenRA.Test
{
	/// <summary>
	/// Unit tests for Flutter-style widgets added beyond the initial Row/Column set:
	/// FlexibleWidget, WrapWidget, StackWidget, PositionedWidget, AlignWidget,
	/// AspectRatioWidget, ConstrainedBoxWidget, FittedBoxWidget.
	/// Also covers the Spacing/SpaceX interaction fix in LinearWidget.
	/// </summary>
	[TestFixture]
	sealed class FlutterLayoutTest
	{
		static ContainerWidget MakeChild(int width, int height)
			=> new() { Bounds = new WidgetBounds(0, 0, width, height) };

		static RowWidget MakeRow(int width, int height)
			=> new() { Bounds = new WidgetBounds(0, 0, width, height) };

		static FlexibleWidget MakeFlexible(float flex = 1f)
			=> new() { Bounds = new WidgetBounds(0, 0, 0, 0), FlexFactor = flex };

		// -----------------------------------------------------------------------
		// FlexibleWidget (FlexFit.loose)
		// -----------------------------------------------------------------------
		[Test]
		public void Flexible_LooseFit_ChildSmallerThanAllocation()
		{
			// Row with 300px. One Flexible(flex=1) gets 200px allocation.
			// Its inner child is only 80px wide — with loose fit it should stay 80px.
			var row = MakeRow(300, 100);
			var fixedChild = MakeChild(100, 100);
			var flex = MakeFlexible(flex: 1f);
			var innerChild = MakeChild(80, 100);
			flex.AddChild(innerChild);
			row.AddChild(fixedChild);
			row.AddChild(flex);

			row.Measure(BoxConstraints.Tight(300, 100));

			Assert.That(flex.Bounds.Width, Is.EqualTo(80));
			Assert.That(flex.Bounds.X, Is.EqualTo(100));
		}

		[Test]
		public void Flexible_TightMode_EquivalentToExpanded()
		{
			var row = MakeRow(300, 100);
			var fixedChild = MakeChild(100, 100);
			var flex = new FlexibleWidget
			{
				Bounds = new WidgetBounds(0, 0, 0, 0),
				FlexFactor = 1f,
				Tight = true
			};
			row.AddChild(fixedChild);
			row.AddChild(flex);

			row.Measure(BoxConstraints.Tight(300, 100));

			Assert.That(flex.Bounds.Width, Is.EqualTo(200));
		}

		// -----------------------------------------------------------------------
		// WrapWidget
		// -----------------------------------------------------------------------
		[Test]
		public void Wrap_SingleRow_NoWrap()
		{
			var wrap = new WrapWidget { Bounds = new WidgetBounds(0, 0, 300, 0) };
			var a = MakeChild(80, 40);
			var b = MakeChild(80, 40);
			wrap.AddChild(a);
			wrap.AddChild(b);

			wrap.Measure(BoxConstraints.Loose(300, 500));

			Assert.That(a.Bounds.X, Is.EqualTo(0));
			Assert.That(b.Bounds.X, Is.EqualTo(80));
			Assert.That(a.Bounds.Y, Is.EqualTo(0));
			Assert.That(b.Bounds.Y, Is.EqualTo(0));
		}

		[Test]
		public void Wrap_OverflowWrapsToNextLine()
		{
			var wrap = new WrapWidget();
			var a = MakeChild(200, 40);
			var b = MakeChild(200, 40);
			wrap.AddChild(a);
			wrap.AddChild(b);

			wrap.Measure(BoxConstraints.Loose(300, 500));

			Assert.That(a.Bounds.Y, Is.EqualTo(0));

			// b wraps to second line
			Assert.That(b.Bounds.Y, Is.EqualTo(40));
			Assert.That(b.Bounds.X, Is.EqualTo(0));
		}

		[Test]
		public void Wrap_RunSpacing_AddsBetweenLines()
		{
			var wrap = new WrapWidget { RunSpacing = 10 };
			var a = MakeChild(200, 40);
			var b = MakeChild(200, 40);
			wrap.AddChild(a);
			wrap.AddChild(b);

			wrap.Measure(BoxConstraints.Loose(300, 500));

			Assert.That(b.Bounds.Y, Is.EqualTo(50));
		}

		[Test]
		public void Wrap_Spacing_AddsGapWithinRun()
		{
			var wrap = new WrapWidget { Spacing = 10 };
			var a = MakeChild(80, 40);
			var b = MakeChild(80, 40);
			wrap.AddChild(a);
			wrap.AddChild(b);

			wrap.Measure(BoxConstraints.Loose(300, 500));

			Assert.That(b.Bounds.X, Is.EqualTo(90));
		}

		// -----------------------------------------------------------------------
		// StackWidget
		// -----------------------------------------------------------------------
		[Test]
		public void Stack_NonPositioned_LayeredAtDefaultAlignment()
		{
			var stack = new StackWidget { Bounds = new WidgetBounds(0, 0, 200, 100) };
			var a = MakeChild(200, 100);
			var b = MakeChild(60, 30);
			stack.AddChild(a);
			stack.AddChild(b);

			stack.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(a.Bounds.X, Is.EqualTo(0));
			Assert.That(a.Bounds.Y, Is.EqualTo(0));

			// default HorizontalAlignment.Left, VerticalAlignment.Top
			Assert.That(b.Bounds.X, Is.EqualTo(0));
			Assert.That(b.Bounds.Y, Is.EqualTo(0));
		}

		[Test]
		public void Stack_NonPositioned_CenterAlignment()
		{
			var stack = new StackWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			var child = MakeChild(60, 40);
			stack.AddChild(child);

			stack.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(child.Bounds.X, Is.EqualTo(70));
			Assert.That(child.Bounds.Y, Is.EqualTo(30));
		}

		[Test]
		public void Stack_Positioned_LeftTop()
		{
			var stack = new StackWidget { Bounds = new WidgetBounds(0, 0, 200, 100) };
			var pos = new PositionedWidget
			{
				Bounds = new WidgetBounds(0, 0, 50, 30),
				PositionLeft = 10,
				PositionTop = 20
			};
			stack.AddChild(pos);

			stack.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(pos.Bounds.X, Is.EqualTo(10));
			Assert.That(pos.Bounds.Y, Is.EqualTo(20));
		}

		[Test]
		public void Stack_Positioned_RightBottom()
		{
			var stack = new StackWidget { Bounds = new WidgetBounds(0, 0, 200, 100) };
			var pos = new PositionedWidget
			{
				Bounds = new WidgetBounds(0, 0, 50, 30),
				PositionRight = 5,
				PositionBottom = 8
			};
			stack.AddChild(pos);

			stack.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(pos.Bounds.X, Is.EqualTo(145));
			Assert.That(pos.Bounds.Y, Is.EqualTo(62));
		}

		[Test]
		public void Stack_Positioned_BothEdges_SizesChild()
		{
			var stack = new StackWidget { Bounds = new WidgetBounds(0, 0, 200, 100) };
			var pos = new PositionedWidget
			{
				PositionLeft = 10,
				PositionRight = 10,
				PositionTop = 5,
				PositionBottom = 5
			};
			stack.AddChild(pos);

			stack.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(pos.Bounds.Width, Is.EqualTo(180));
			Assert.That(pos.Bounds.Height, Is.EqualTo(90));
			Assert.That(pos.Bounds.X, Is.EqualTo(10));
			Assert.That(pos.Bounds.Y, Is.EqualTo(5));
		}

		// -----------------------------------------------------------------------
		// AlignWidget
		// -----------------------------------------------------------------------
		[Test]
		public void Align_TopLeft_DefaultFactors()
		{
			var align = new AlignWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100),
				AlignX = -1f,
				AlignY = -1f
			};
			var child = MakeChild(60, 40);
			align.AddChild(child);

			align.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(child.Bounds.X, Is.EqualTo(0));
			Assert.That(child.Bounds.Y, Is.EqualTo(0));
		}

		[Test]
		public void Align_Center()
		{
			var align = new AlignWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100),
				AlignX = 0f,
				AlignY = 0f
			};
			var child = MakeChild(60, 40);
			align.AddChild(child);

			align.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(child.Bounds.X, Is.EqualTo(70));
			Assert.That(child.Bounds.Y, Is.EqualTo(30));
		}

		[Test]
		public void Align_BottomRight()
		{
			var align = new AlignWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100),
				AlignX = 1f,
				AlignY = 1f
			};
			var child = MakeChild(60, 40);
			align.AddChild(child);

			align.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(child.Bounds.X, Is.EqualTo(140));
			Assert.That(child.Bounds.Y, Is.EqualTo(60));
		}

		// -----------------------------------------------------------------------
		// AspectRatioWidget
		// -----------------------------------------------------------------------
		[Test]
		public void AspectRatio_WidthFirst_DerivesHeight()
		{
			// Loose: max 200x500. Ratio 2 => height = 200/2 = 100, fits in [0,500].
			var ar = new AspectRatioWidget { Ratio = 2f };
			ar.Measure(BoxConstraints.Loose(200, 500));

			Assert.That(ar.Bounds.Width, Is.EqualTo(200));
			Assert.That(ar.Bounds.Height, Is.EqualTo(100));
		}

		[Test]
		public void AspectRatio_HeightFirst_DerivesWidth()
		{
			// Loose: max 500x200. Ratio 2 => height = 500/2 = 250, exceeds max 200.
			// Falls back to height-first: width = 200 * 2 = 400, fits in [0,500].
			var ar = new AspectRatioWidget { Ratio = 2f };
			ar.Measure(BoxConstraints.Loose(500, 200));

			Assert.That(ar.Bounds.Width, Is.EqualTo(400));
			Assert.That(ar.Bounds.Height, Is.EqualTo(200));
		}

		// -----------------------------------------------------------------------
		// ConstrainedBoxWidget
		// -----------------------------------------------------------------------
		[Test]
		public void ConstrainedBox_ImposesMinimumsOnChild()
		{
			// SizedBox(0x0) with no declared size will fill to the min constraints.
			var box = new ConstrainedBoxWidget
			{
				AdditionalMinWidth = 150,
				AdditionalMinHeight = 80
			};
			var child = new SizedBoxWidget();
			box.AddChild(child);

			box.Measure(BoxConstraints.Loose(500, 500));

			Assert.That(child.Bounds.Width, Is.EqualTo(150));
			Assert.That(child.Bounds.Height, Is.EqualTo(80));
		}

		[Test]
		public void ConstrainedBox_ImposesMaximumsOnChild()
		{
			// SizedBox declared 300x200 will be clamped to 100x50 by ConstrainedBox.
			var box = new ConstrainedBoxWidget
			{
				AdditionalMaxWidth = 100,
				AdditionalMaxHeight = 50
			};
			var child = new SizedBoxWidget { Bounds = new WidgetBounds(0, 0, 300, 200) };
			box.AddChild(child);

			box.Measure(BoxConstraints.Loose(500, 500));

			Assert.That(child.Bounds.Width, Is.EqualTo(100));
			Assert.That(child.Bounds.Height, Is.EqualTo(50));
		}

		// -----------------------------------------------------------------------
		// FittedBoxWidget
		// -----------------------------------------------------------------------
		[Test]
		public void FittedBox_Contain_ScalesUniformlyToFit()
		{
			// Available: 200x100. Child natural size: 400x200. Scale = 0.5 => 200x100.
			var fitted = new FittedBoxWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100),
				Fit = BoxFit.Contain
			};
			var child = new SizedBoxWidget { Bounds = new WidgetBounds(0, 0, 400, 200) };
			fitted.AddChild(child);

			fitted.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(child.Bounds.Width, Is.EqualTo(200));
			Assert.That(child.Bounds.Height, Is.EqualTo(100));

			// Centered by default — no offset when scaled size equals available size.
			Assert.That(child.Bounds.X, Is.EqualTo(0));
			Assert.That(child.Bounds.Y, Is.EqualTo(0));
		}

		[Test]
		public void FittedBox_Fill_StretchesToExactSize()
		{
			var fitted = new FittedBoxWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100),
				Fit = BoxFit.Fill
			};
			var child = new SizedBoxWidget { Bounds = new WidgetBounds(0, 0, 50, 50) };
			fitted.AddChild(child);

			fitted.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(child.Bounds.Width, Is.EqualTo(200));
			Assert.That(child.Bounds.Height, Is.EqualTo(100));
		}

		[Test]
		public void FittedBox_ScaleDown_DoesNotUpscale()
		{
			// Child is 50x30 (smaller than 200x100). ScaleDown must not enlarge it.
			var fitted = new FittedBoxWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100),
				Fit = BoxFit.ScaleDown
			};
			var child = new SizedBoxWidget { Bounds = new WidgetBounds(0, 0, 50, 30) };
			fitted.AddChild(child);

			fitted.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(child.Bounds.Width, Is.EqualTo(50));
			Assert.That(child.Bounds.Height, Is.EqualTo(30));
		}

		// -----------------------------------------------------------------------
		// LinearWidget: Spacing ignored for SpaceX alignments
		// -----------------------------------------------------------------------
		[Test]
		public void Row_SpaceBetween_IgnoresSpacingProperty()
		{
			// 2 children of 50px in a 400px row — SpaceBetween places them at 0 and 350
			// regardless of the Spacing property.
			var row = MakeRow(400, 100);
			row.MainAxisAlignment = MainAxisAlignment.SpaceBetween;
			row.Spacing = 100;
			var a = MakeChild(50, 100);
			var b = MakeChild(50, 100);
			row.AddChild(a);
			row.AddChild(b);

			row.Measure(BoxConstraints.Tight(400, 100));

			Assert.That(a.Bounds.X, Is.EqualTo(0));
			Assert.That(b.Bounds.X, Is.EqualTo(350));
		}

		[Test]
		public void Row_SpaceEvenly_IgnoresSpacingProperty()
		{
			var row = MakeRow(400, 100);
			row.MainAxisAlignment = MainAxisAlignment.SpaceEvenly;
			row.Spacing = 50;
			var a = MakeChild(100, 100);
			var b = MakeChild(100, 100);
			row.AddChild(a);
			row.AddChild(b);

			row.Measure(BoxConstraints.Tight(400, 100));

			// SpaceEvenly: gap = 200 / 3 = 66.666... The algorithm accumulates as float so
			// b ends up at (int)(66.666 + 100 + 66.666) = (int)233.333 = 233.
			Assert.That(a.Bounds.X, Is.EqualTo(66));
			Assert.That(b.Bounds.X, Is.EqualTo(233));
		}

		[Test]
		public void Row_SpaceAround_IgnoresSpacingProperty()
		{
			var row = MakeRow(300, 100);
			row.MainAxisAlignment = MainAxisAlignment.SpaceAround;
			row.Spacing = 50;
			var a = MakeChild(50, 100);
			var b = MakeChild(50, 100);
			row.AddChild(a);
			row.AddChild(b);

			row.Measure(BoxConstraints.Tight(300, 100));

			// Free space = 200, split into 2 equal portions of 100.
			// Half-space before first child = 50. b at 50 + 50 + 100 = 200.
			Assert.That(a.Bounds.X, Is.EqualTo(50));
			Assert.That(b.Bounds.X, Is.EqualTo(200));
		}

		// -----------------------------------------------------------------------
		// Edge cases: degenerate constraints and counts
		// -----------------------------------------------------------------------
		[Test]
		public void Wrap_SpaceBetween_SingleChildPerRun_DoesNotDivideByZero()
		{
			// Each child is wider than the available width, so each ends up alone in a run.
			// SpaceBetween must degrade gracefully when r.Count == 1 (no inter-child gap to spread).
			var wrap = new WrapWidget { Alignment = WrapAlignment.SpaceBetween };
			var a = MakeChild(250, 40);
			var b = MakeChild(250, 40);
			wrap.AddChild(a);
			wrap.AddChild(b);

			Assert.DoesNotThrow(() => wrap.Measure(BoxConstraints.Loose(200, 500)));
			Assert.That(a.Bounds.Y, Is.EqualTo(0));
			Assert.That(b.Bounds.Y, Is.EqualTo(40));
		}

		[Test]
		public void Stack_Positioned_RightOnly_OversizedChild_ClampedToZero()
		{
			// Right=10 but child wants 300px wide in a 200px stack: anchoring would
			// put X at 200 - 10 - 300 = -110. Clamped to 0 instead.
			var stack = new StackWidget { Bounds = new WidgetBounds(0, 0, 200, 100) };
			var pos = new PositionedWidget
			{
				Bounds = new WidgetBounds(0, 0, 300, 50),
				PositionRight = 10
			};
			stack.AddChild(pos);

			stack.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(pos.Bounds.X, Is.GreaterThanOrEqualTo(0));
		}

		[Test]
		public void Row_ZeroWidthConstraint_DoesNotCrash()
		{
			// Zero-width constraint: children should still be measured without exceptions.
			var row = MakeRow(0, 100);
			row.AddChild(MakeChild(50, 50));
			row.AddChild(MakeChild(50, 50));

			Assert.DoesNotThrow(() => row.Measure(BoxConstraints.Tight(0, 100)));
		}

		[Test]
		public void Column_ZeroHeightConstraint_DoesNotCrash()
		{
			var col = new ColumnWidget { Bounds = new WidgetBounds(0, 0, 100, 0) };
			col.AddChild(MakeChild(50, 50));
			col.AddChild(MakeChild(50, 50));

			Assert.DoesNotThrow(() => col.Measure(BoxConstraints.Tight(100, 0)));
		}

		[Test]
		public void Container_NoChildren_StillReportsSize()
		{
			// A Container without children should still respect its declared size.
			var container = new ContainerWidget { Bounds = new WidgetBounds(0, 0, 80, 40) };
			var (w, h) = container.Measure(BoxConstraints.Loose(500, 500));

			Assert.That(w, Is.EqualTo(80));
			Assert.That(h, Is.EqualTo(40));
		}

		[Test]
		public void NestedFlexibles_DistributeProportionally()
		{
			// Row(300px) with two Flexibles, flex=1 and flex=2 → 100px / 200px split.
			var row = MakeRow(300, 100);
			var f1 = MakeFlexible(1f);
			var f2 = MakeFlexible(2f);
			f1.Tight = true;
			f2.Tight = true;
			row.AddChild(f1);
			row.AddChild(f2);

			row.Measure(BoxConstraints.Tight(300, 100));

			Assert.That(f1.Bounds.Width, Is.EqualTo(100));
			Assert.That(f2.Bounds.Width, Is.EqualTo(200));
		}
	}
}
