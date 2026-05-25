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
	[TestFixture]
	sealed class EdgeInsetsTest
	{
		[Test]
		public void ConstructorAll()
		{
			var e = new EdgeInsets(10);
			Assert.That(e.Top, Is.EqualTo(10));
			Assert.That(e.Right, Is.EqualTo(10));
			Assert.That(e.Bottom, Is.EqualTo(10));
			Assert.That(e.Left, Is.EqualTo(10));
		}

		[Test]
		public void ConstructorVerticalHorizontal()
		{
			var e = new EdgeInsets(5, 10);
			Assert.That(e.Top, Is.EqualTo(5));
			Assert.That(e.Bottom, Is.EqualTo(5));
			Assert.That(e.Left, Is.EqualTo(10));
			Assert.That(e.Right, Is.EqualTo(10));
		}

		[Test]
		public void ConstructorFourValues()
		{
			var e = new EdgeInsets(1, 2, 3, 4);
			Assert.That(e.Top, Is.EqualTo(1));
			Assert.That(e.Right, Is.EqualTo(2));
			Assert.That(e.Bottom, Is.EqualTo(3));
			Assert.That(e.Left, Is.EqualTo(4));
		}

		[Test]
		public void HorizontalAndVertical()
		{
			var e = new EdgeInsets(10, 20, 30, 40);
			Assert.That(e.Horizontal, Is.EqualTo(60)); // Left + Right = 40 + 20
			Assert.That(e.Vertical, Is.EqualTo(40)); // Top + Bottom = 10 + 30
		}

		[TestCase("10", 10, 10, 10, 10)]
		[TestCase("5, 10", 5, 10, 5, 10)]
		[TestCase("1, 2, 3, 4", 1, 2, 3, 4)]
		[TestCase(" 10 ", 10, 10, 10, 10)]
		[TestCase(" 5 , 10 ", 5, 10, 5, 10)]
		public void TryParseValid(string input, int top, int right, int bottom, int left)
		{
			Assert.That(EdgeInsets.TryParse(input, out var result), Is.True);
			Assert.That(result.Top, Is.EqualTo(top));
			Assert.That(result.Right, Is.EqualTo(right));
			Assert.That(result.Bottom, Is.EqualTo(bottom));
			Assert.That(result.Left, Is.EqualTo(left));
		}

		[TestCase("")]
		[TestCase("   ")]
		[TestCase("abc")]
		[TestCase("1, 2, 3")]
		[TestCase("1, 2, 3, 4, 5")]
		public void TryParseInvalid(string input)
		{
			Assert.That(EdgeInsets.TryParse(input, out _), Is.False);
		}

		[Test]
		public void TryParseNullReturnsFalse()
		{
			Assert.That(EdgeInsets.TryParse(null, out _), Is.False);
		}

		[Test]
		public void Equality()
		{
			var a = new EdgeInsets(1, 2, 3, 4);
			var b = new EdgeInsets(1, 2, 3, 4);
			var c = new EdgeInsets(5, 6, 7, 8);

			Assert.That(a, Is.EqualTo(b));
			Assert.That(a == b, Is.True);
			Assert.That(a != c, Is.True);
		}

		[Test]
		public void ToStringFormats()
		{
			Assert.That(new EdgeInsets(10).ToString(), Is.EqualTo("10"));
			Assert.That(new EdgeInsets(5, 10).ToString(), Is.EqualTo("5, 10"));
			Assert.That(new EdgeInsets(1, 2, 3, 4).ToString(), Is.EqualTo("1, 2, 3, 4"));
		}

		[Test]
		public void ZeroIsDefault()
		{
			var z = EdgeInsets.Zero;
			Assert.That(z.Top, Is.EqualTo(0));
			Assert.That(z.Right, Is.EqualTo(0));
			Assert.That(z.Bottom, Is.EqualTo(0));
			Assert.That(z.Left, Is.EqualTo(0));
		}
	}
}
