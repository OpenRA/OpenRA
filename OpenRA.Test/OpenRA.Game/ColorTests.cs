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
using OpenRA.Primitives;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class ColorTest
	{
		[TestCase(255, 255, 255, 255)]
		[TestCase(0, 0, 0, 255)]
		[TestCase(255, 0, 0, 255)]
		[TestCase(0, 255, 0, 255)]
		[TestCase(0, 0, 255, 255)]
		[TestCase(128, 64, 32, 16)]
		public void HashCodeIsNonZeroAndConsistentForColor(int a, int r, int g, int b)
		{
			var color = Color.FromArgb(a, r, g, b);
			var duplicate = Color.FromArgb(a, r, g, b);

			Assert.That(color.GetHashCode(), Is.Not.EqualTo(0), $"Hash code for color (A:{a}, R:{r}, G:{g}, B:{b}) must not be zero.");
			Assert.That(color.GetHashCode(), Is.EqualTo(duplicate.GetHashCode()), $"Hash code for identical colors (A:{a}, R:{r}, G:{g}, B:{b}) must be consistent.");
		}

		static readonly Color[] TestColors =
		[
			Color.Red,
			Color.Green,
			Color.Blue,
			Color.Black,
			Color.White,
			Color.Transparent,
			Color.FromArgb(100, 150, 200, 50),
			Color.FromArgb(250, 10, 20, 30)
		];

		[Test]
		public void DistinctColorsHaveUniqueHashCodes()
		{
			for (var i = 0; i < TestColors.Length; i++)
				for (var j = i + 1; j < TestColors.Length; j++)
					if (TestColors[i] != TestColors[j])
						Assert.That(TestColors[i].GetHashCode(), Is.Not.EqualTo(TestColors[j].GetHashCode()),
							$"Distinct colors at indices {i} and {j} produced a hash collision.");
		}

		[Test]
		public void EqualColorsShareSameHashCode()
		{
			var c1 = Color.FromArgb(123, 45, 67, 89);
			var c2 = Color.FromArgb(123, 45, 67, 89);

			Assert.That(c1 == c2, Is.True, "Identical color instances should be evaluated as equal.");
			Assert.That(c1.GetHashCode(), Is.EqualTo(c2.GetHashCode()), "Equal colors must share the same hash code.");
		}
	}
}
