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
	/// Unit tests for the Flutter-style Row/Column layout system.
	/// Verifies the "constraints go down / sizes go up / parent sets position" protocol.
	/// </summary>
	[TestFixture]
	sealed class RowColumnLayoutTest
	{
		static RowWidget MakeRow(int width, int height)
			=> new() { Bounds = new WidgetBounds(0, 0, width, height) };

		static ColumnWidget MakeColumn(int width, int height)
			=> new() { Bounds = new WidgetBounds(0, 0, width, height) };

		static ContainerWidget MakeChild(int width, int height)
			=> new() { Bounds = new WidgetBounds(0, 0, width, height) };

		static ExpandedWidget MakeExpanded(float flex = 1f)
			=> new() { Bounds = new WidgetBounds(0, 0, 0, 0), FlexFactor = flex };

		[Test]
		public void Row_StacksChildrenHorizontally()
		{
			var row = MakeRow(400, 100);
			var a = MakeChild(100, 100);
			var b = MakeChild(150, 100);
			row.AddChild(a);
			row.AddChild(b);

			row.Measure(BoxConstraints.Tight(400, 100));

			Assert.That(a.Bounds.X, Is.EqualTo(0));
			Assert.That(b.Bounds.X, Is.EqualTo(100));
		}

		[Test]
		public void Row_Spacing_AddsGapBetweenChildren()
		{
			var row = MakeRow(400, 100);
			row.Spacing = 20;
			var a = MakeChild(100, 100);
			var b = MakeChild(100, 100);
			row.AddChild(a);
			row.AddChild(b);

			row.Measure(BoxConstraints.Tight(400, 100));

			Assert.That(a.Bounds.X, Is.EqualTo(0));
			Assert.That(b.Bounds.X, Is.EqualTo(120)); // 100 + 20 gap
		}

		[Test]
		public void Row_MainAxisAlignmentCenter_CentersChildren()
		{
			var row = MakeRow(400, 100);
			row.MainAxisAlignment = MainAxisAlignment.Center;
			var a = MakeChild(100, 100);
			row.AddChild(a);

			row.Measure(BoxConstraints.Tight(400, 100));

			Assert.That(a.Bounds.X, Is.EqualTo(150)); // (400 - 100) / 2
		}

		[Test]
		public void Row_MainAxisAlignmentEnd_AlignsToEnd()
		{
			var row = MakeRow(400, 100);
			row.MainAxisAlignment = MainAxisAlignment.End;
			var a = MakeChild(100, 100);
			row.AddChild(a);

			row.Measure(BoxConstraints.Tight(400, 100));

			Assert.That(a.Bounds.X, Is.EqualTo(300)); // 400 - 100
		}

		[Test]
		public void Row_MainAxisAlignmentSpaceBetween_SpacesEvenly()
		{
			var row = MakeRow(400, 100);
			row.MainAxisAlignment = MainAxisAlignment.SpaceBetween;
			var a = MakeChild(50, 100);
			var b = MakeChild(50, 100);
			row.AddChild(a);
			row.AddChild(b);

			row.Measure(BoxConstraints.Tight(400, 100));

			Assert.That(a.Bounds.X, Is.EqualTo(0));
			Assert.That(b.Bounds.X, Is.EqualTo(350)); // 400 - 50
		}

		[Test]
		public void Row_CrossAxisAlignmentCenter_CentersVertically()
		{
			var row = MakeRow(400, 200);
			row.CrossAxisAlignment = CrossAxisAlignment.Center;
			var a = MakeChild(100, 50);
			row.AddChild(a);

			row.Measure(BoxConstraints.Tight(400, 200));

			Assert.That(a.Bounds.Y, Is.EqualTo(75)); // (200 - 50) / 2
		}

		[Test]
		public void Row_CrossAxisAlignmentEnd_AlignsToBottom()
		{
			var row = MakeRow(400, 200);
			row.CrossAxisAlignment = CrossAxisAlignment.End;
			var a = MakeChild(100, 50);
			row.AddChild(a);

			row.Measure(BoxConstraints.Tight(400, 200));

			Assert.That(a.Bounds.Y, Is.EqualTo(150)); // 200 - 50
		}

		[Test]
		public void Row_CrossAxisAlignmentStretch_StretchesChildHeight()
		{
			var row = MakeRow(400, 200);
			row.CrossAxisAlignment = CrossAxisAlignment.Stretch;
			var a = MakeChild(100, 50);
			row.AddChild(a);

			row.Measure(BoxConstraints.Tight(400, 200));

			Assert.That(a.Bounds.Height, Is.EqualTo(200));
		}

		[Test]
		public void Row_SingleExpanded_TakesRemainingSpace()
		{
			var row = MakeRow(400, 100);
			var a = MakeChild(100, 100);
			var exp = MakeExpanded();
			exp.AddChild(MakeChild(0, 100));
			var b = MakeChild(100, 100);
			row.AddChild(a);
			row.AddChild(exp);
			row.AddChild(b);

			row.Measure(BoxConstraints.Tight(400, 100));

			Assert.That(a.Bounds.X, Is.EqualTo(0));
			Assert.That(exp.Bounds.Width, Is.EqualTo(200));
			Assert.That(exp.Bounds.X, Is.EqualTo(100));
			Assert.That(b.Bounds.X, Is.EqualTo(300));
		}

		[Test]
		public void Row_TwoExpanded_ShareSpaceByFlexFactor()
		{
			var row = MakeRow(300, 100);
			var exp1 = MakeExpanded(flex: 1f);
			var exp2 = MakeExpanded(flex: 2f);
			row.AddChild(exp1);
			row.AddChild(exp2);

			row.Measure(BoxConstraints.Tight(300, 100));

			Assert.That(exp1.Bounds.Width, Is.EqualTo(100));
			Assert.That(exp2.Bounds.Width, Is.EqualTo(200));
		}

		[Test]
		public void Column_StacksChildrenVertically()
		{
			var col = MakeColumn(200, 400);
			var a = MakeChild(200, 50);
			var b = MakeChild(200, 80);
			col.AddChild(a);
			col.AddChild(b);

			col.Measure(BoxConstraints.Tight(200, 400));

			Assert.That(a.Bounds.Y, Is.EqualTo(0));
			Assert.That(b.Bounds.Y, Is.EqualTo(50));
		}

		[Test]
		public void Column_Spacing_AddsGapBetweenChildren()
		{
			var col = MakeColumn(200, 400);
			col.Spacing = 10;
			var a = MakeChild(200, 50);
			var b = MakeChild(200, 50);
			col.AddChild(a);
			col.AddChild(b);

			col.Measure(BoxConstraints.Tight(200, 400));

			Assert.That(a.Bounds.Y, Is.EqualTo(0));
			Assert.That(b.Bounds.Y, Is.EqualTo(60)); // 50 + 10 gap
		}

		[Test]
		public void Column_SingleExpanded_TakesRemainingHeight()
		{
			var col = MakeColumn(200, 400);
			var a = MakeChild(200, 50);
			var exp = MakeExpanded();
			var b = MakeChild(200, 50);
			col.AddChild(a);
			col.AddChild(exp);
			col.AddChild(b);

			col.Measure(BoxConstraints.Tight(200, 400));

			Assert.That(exp.Bounds.Height, Is.EqualTo(300));
			Assert.That(exp.Bounds.Y, Is.EqualTo(50));
			Assert.That(b.Bounds.Y, Is.EqualTo(350));
		}

		[Test]
		public void Padding_OffsetsSingleChild()
		{
			var pad = new PaddingWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100),
				PaddingInsets = new EdgeInsets(10, 20, 10, 20)
			};
			var child = MakeChild(160, 80);
			pad.AddChild(child);

			pad.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(child.Bounds.X, Is.EqualTo(0)); // ChildOrigin handles PaddingInsets.Left
			Assert.That(child.Bounds.Y, Is.EqualTo(0)); // ChildOrigin handles PaddingInsets.Top
		}

		[Test]
		public void Padding_ShrinkwrapsAroundChild()
		{
			var pad = new PaddingWidget
			{
				Bounds = new WidgetBounds(0, 0, 0, 0),
				PaddingInsets = new EdgeInsets(5)
			};
			var child = MakeChild(100, 40);
			pad.AddChild(child);

			pad.Measure(BoxConstraints.Loose(500, 500));

			Assert.That(pad.Bounds.Width, Is.EqualTo(110));
			Assert.That(pad.Bounds.Height, Is.EqualTo(50));
		}

		[Test]
		public void SizedBox_ForcesChildToExactSize()
		{
			var box = new SizedBoxWidget
			{
				Bounds = new WidgetBounds(0, 0, 120, 60)
			};
			var child = MakeChild(300, 300);
			box.AddChild(child);

			box.Measure(BoxConstraints.Tight(120, 60));

			Assert.That(child.Bounds.Width, Is.EqualTo(120));
			Assert.That(child.Bounds.Height, Is.EqualTo(60));
		}

		[Test]
		public void SizedBox_NoChild_ReportsOwnSize()
		{
			var box = new SizedBoxWidget
			{
				Bounds = new WidgetBounds(0, 0, 80, 30)
			};

			box.Measure(BoxConstraints.Tight(80, 30));

			Assert.That(box.Bounds.Width, Is.EqualTo(80));
			Assert.That(box.Bounds.Height, Is.EqualTo(30));
		}

		[Test]
		public void BoxConstraints_Deflate_SubtractsInsets()
		{
			var c = new BoxConstraints(10, 400, 10, 200);
			var result = c.Deflate(new EdgeInsets(5, 10, 5, 10));

			Assert.That(result.MinWidth, Is.EqualTo(0));
			Assert.That(result.MaxWidth, Is.EqualTo(380));
			Assert.That(result.MinHeight, Is.EqualTo(0));
			Assert.That(result.MaxHeight, Is.EqualTo(190));
		}

		[Test]
		public void BoxConstraints_Constrain_ClampsValues()
		{
			var c = BoxConstraints.Tight(100, 50);
			var (w, h) = c.Constrain(200, 10);

			Assert.That(w, Is.EqualTo(100));
			Assert.That(h, Is.EqualTo(50));
		}

		// -----------------------------------------------------------------------
		// ContainerWidget tests
		// -----------------------------------------------------------------------
		[Test]
		public void Container_ShrinkwrapsChildPlusPadding()
		{
			var container = new ContainerWidget
			{
				ContainerPadding = new EdgeInsets(10)
			};
			var child = MakeChild(100, 40);
			container.AddChild(child);

			container.Measure(BoxConstraints.Loose(500, 500));

			Assert.That(container.Bounds.Width, Is.EqualTo(120));  // 100 + 10*2
			Assert.That(container.Bounds.Height, Is.EqualTo(60));  // 40 + 10*2
		}

		[Test]
		public void Container_ForcesExactSizeWhenDeclared()
		{
			var container = new ContainerWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100)
			};
			var child = MakeChild(50, 30);
			container.AddChild(child);

			container.Measure(BoxConstraints.Loose(500, 500));

			Assert.That(container.Bounds.Width, Is.EqualTo(200));
			Assert.That(container.Bounds.Height, Is.EqualTo(100));
		}

		[Test]
		public void Container_AlignsCenterCenter()
		{
			var container = new ContainerWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			var child = MakeChild(60, 40);
			container.AddChild(child);

			container.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(child.Bounds.X, Is.EqualTo(70));  // (200 - 60) / 2
			Assert.That(child.Bounds.Y, Is.EqualTo(30));  // (100 - 40) / 2
		}

		[Test]
		public void Container_AlignsBottomRight()
		{
			var container = new ContainerWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom
			};
			var child = MakeChild(60, 40);
			container.AddChild(child);

			container.Measure(BoxConstraints.Tight(200, 100));

			Assert.That(child.Bounds.X, Is.EqualTo(140)); // 200 - 60
			Assert.That(child.Bounds.Y, Is.EqualTo(60));  // 100 - 40
		}

		[Test]
		public void Container_PaddingAndAlignmentCenterCombined()
		{
			var container = new ContainerWidget
			{
				Bounds = new WidgetBounds(0, 0, 200, 100),
				ContainerPadding = new EdgeInsets(10),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			var child = MakeChild(60, 40);
			container.AddChild(child);

			container.Measure(BoxConstraints.Tight(200, 100));

			// Content area: 200 - 20 = 180 wide, 100 - 20 = 80 tall
			Assert.That(child.Bounds.X, Is.EqualTo(60));  // (180 - 60) / 2
			Assert.That(child.Bounds.Y, Is.EqualTo(20));  // (80 - 40) / 2
		}

		[Test]
		public void Container_NoChild_ReportsInsetsSize()
		{
			var container = new ContainerWidget
			{
				ContainerPadding = new EdgeInsets(8)
			};

			container.Measure(BoxConstraints.Loose(500, 500));

			Assert.That(container.Bounds.Width, Is.EqualTo(16));  // 8*2
			Assert.That(container.Bounds.Height, Is.EqualTo(16)); // 8*2
		}

		[Test]
		public void Container_InsideRow_ParticipatesInLayout()
		{
			var row = MakeRow(300, 100);
			var c1 = new ContainerWidget { Bounds = new WidgetBounds(0, 0, 80, 60) };
			var c2 = new ContainerWidget { Bounds = new WidgetBounds(0, 0, 120, 60) };
			row.AddChild(c1);
			row.AddChild(c2);

			row.Measure(BoxConstraints.Tight(300, 100));

			Assert.That(c1.Bounds.X, Is.EqualTo(0));
			Assert.That(c2.Bounds.X, Is.EqualTo(80));
		}
	}
}
