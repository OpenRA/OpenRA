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
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class WAngleTest
	{
		[TestCase(0, ExpectedResult = 0, TestName = "Zero")]
		[TestCase(512, ExpectedResult = 512, TestName = "Pi")]
		[TestCase(1023, ExpectedResult = 1023, TestName = "Max Bound")]
		[TestCase(1024, ExpectedResult = 0, TestName = "Positive Wrap 360")]
		[TestCase(2048, ExpectedResult = 0, TestName = "Positive Wrap 720")]
		[TestCase(-1, ExpectedResult = 1023, TestName = "Negative Wrap 1")]
		[TestCase(-1025, ExpectedResult = 1023, TestName = "Negative Wrap Multi")]
		public int ConstructorMaskingAndEquality(int input)
		{
			var angle = new WAngle(input);

			Assert.That(angle.Angle, Is.EqualTo(new WAngle(input + 1024).Angle), "Adding 360 degrees must result in identical internal state.");
			Assert.That(angle.Angle, Is.EqualTo(new WAngle(input - 1024).Angle), "Subtracting 360 degrees must result in identical internal state.");

			return angle.Angle;
		}

		[Test]
		public void ArithmeticAndDivision()
		{
			var angle = new WAngle(1000);

			Assert.That((angle * 10000).Angle, Is.EqualTo((1000 * 10000) & 1023), "Multiplication must wrap within the 10-bit mask.");
			Assert.That((angle / 10).Angle, Is.EqualTo(100), "Integer division of WAngle returned unexpected value.");
		}

		[Test]
		public void ShortestPathInterpolationExhaustive()
		{
			for (var i = 0; i < 1024; i += 64)
			{
				var start = new WAngle(i);
				var end = new WAngle((i + 512) & 1023);

				Assert.That(WAngle.Lerp(start, end, 0, 1).Angle, Is.EqualTo(start.Angle), $"Lerp start bound (mul=0) failed at angle {i}.");
				Assert.That(WAngle.Lerp(start, end, 1, 1).Angle, Is.EqualTo(end.Angle), $"Lerp end bound (mul=div) failed at angle {i}.");
			}

			// Crossing the zero-wrap seam
			var nearMax = new WAngle(1014);
			var nearZero = new WAngle(10);

			Assert.That(WAngle.Lerp(nearMax, nearZero, 1, 2).Angle, Is.EqualTo(0), "Lerp failed to take the positive shortest path across the 1024/0 seam.");
			Assert.That(WAngle.Lerp(nearZero, nearMax, 1, 2).Angle, Is.EqualTo(0), "Lerp failed to take the negative shortest path across the 1024/0 seam.");

			var start2 = new WAngle(0);
			var end2 = new WAngle(512);

			// At exactly 180 degrees apart, the shortest path is ambiguous.
			// This verifies the mask1/mask2 logic resolves to a valid perpendicular axis.
			var midpoint = WAngle.Lerp(start2, end2, 1, 2);

			Assert.That(midpoint.Angle, Is.AnyOf(256, 768),
				"Lerp failed to resolve the 180-degree midpoint tie-break to a 90 or 270 degree axis.");
		}

		[Test]
		public void TrigonometrySymmetryAndPythagorasExhaustive()
		{
			for (var i = -1024; i < 2048; i++)
			{
				var angle = new WAngle(i);
				var sin = angle.Sin();
				var cos = angle.Cos();

				// Even/Odd Identities
				Assert.That(cos, Is.EqualTo(new WAngle(-i).Cos()), $"Cosine symmetry failure: Cos({i}) != Cos({-i})");

				// Shift Identities
				Assert.That(sin, Is.EqualTo(new WAngle(i - 256).Cos()), $"Sine shift failure: Sin({i}) != Cos({i}-90deg)");

				// Pythagorean Identity: Radius^2 = 1024^2 = 1,048,576
				var squareSum = (long)cos * cos + (long)sin * sin;
				Assert.That(squareSum, Is.EqualTo(1048576).Within(4096), $"Trig table drift (Sin^2 + Cos^2) exceeded tolerance at angle {i}.");

				// Tangent Periodicity
				var wrapped = i & 1023;
				if (wrapped != 256 && wrapped != 768)
				{
					Assert.That(angle.Tan(), Is.EqualTo(new WAngle(i + 512).Tan()), $"Tangent periodicity failure at angle {i}.");
				}
			}
		}

		[Test]
		public void TangentAsymptoteAndSignTransitions()
		{
			Assert.That(new WAngle(256).Tan(), Is.EqualTo(int.MaxValue), "Tan(90deg) must return int.MaxValue (Infinity).");
			Assert.That(new WAngle(768).Tan(), Is.EqualTo(int.MaxValue), "Tan(270deg) must return int.MaxValue (Infinity).");

			Assert.That(new WAngle(255).Tan(), Is.GreaterThan(0), "Tangent must be positive approaching 90deg from below.");
			Assert.That(new WAngle(257).Tan(), Is.LessThan(0), "Tangent must be negative approaching 90deg from above.");
		}

		[Test]
		public void InverseTrigFullCircleRoundTripExhaustive()
		{
			for (var i = 0; i < 1024; i++)
			{
				var original = new WAngle(i);
				var s = original.Sin();
				var c = original.Cos();

				// ArcTan Round-trip
				var resultAtan = WAngle.ArcTan(s, c);
				var deltaAtan = Math.Abs(original.Angle - resultAtan.Angle);
				if (deltaAtan > 512) deltaAtan = 1024 - deltaAtan;

				Assert.That(deltaAtan, Is.LessThanOrEqualTo(1), $"ArcTan precision loss at {i} units. Input ({c}, {s}) produced {resultAtan.Angle}.");

				// ArcSin/ArcCos range and identity checks
				var asin = WAngle.ArcSin(s);
				var acos = WAngle.ArcCos(c);

				Assert.That(Math.Abs(asin.Sin() - s), Is.LessThanOrEqualTo(3), $"ArcSin round-trip precision failure for value {s}.");
				Assert.That(Math.Abs(acos.Cos() - c), Is.LessThanOrEqualTo(3), $"ArcCos round-trip precision failure for value {c}.");
			}
		}

		[TestCase(1, 1, 128, "Q1")]
		[TestCase(1, -1, 384, "Q2")]
		[TestCase(-1, -1, 640, "Q3")]
		[TestCase(-1, 1, 896, "Q4")]
		public void InverseTrigQuadrantAlignment(int y, int x, int expected, string label)
		{
			Assert.That(WAngle.ArcTan(y, x).Angle, Is.EqualTo(expected).Within(2), $"ArcTan quadrant multiplexer failed for {label}.");
		}

		[Test]
		public void InverseTrigMemorySafetyAndGuards()
		{
			// Safe domain
			Assert.DoesNotThrow(() => WAngle.ArcSin(1024), "ArcSin max boundary lookup crashed.");
			Assert.DoesNotThrow(() => WAngle.ArcSin(-1024), "ArcSin min boundary lookup crashed.");
			Assert.DoesNotThrow(() => WAngle.ArcSin(0), "ArcSin zero-point lookup crashed.");
			Assert.DoesNotThrow(() => WAngle.ArcTan(166, 1), "ArcTan high-ratio precision limit lookup crashed.");

			// Out of range domain
			Assert.Throws<ArgumentOutOfRangeException>(() => WAngle.ArcSin(1025), "ArcSin failed to guard against value > 1024.");
			Assert.Throws<ArgumentOutOfRangeException>(() => WAngle.ArcSin(-1025), "ArcSin failed to guard against value < -1024.");

			// Fuzzing extreme integers for overflow in internal logic
			Assert.DoesNotThrow(() => WAngle.ArcTan(int.MinValue, 1), "ArcTan failed to handle int.MinValue safely.");
			Assert.DoesNotThrow(() => WAngle.ArcTan(int.MaxValue, int.MaxValue), "ArcTan failed to handle int.MaxValue safely.");
		}

		[TestCase(0, ExpectedResult = 0, TestName = "0 deg")]
		[TestCase(90, ExpectedResult = 256, TestName = "90 deg")]
		[TestCase(180, ExpectedResult = 512, TestName = "180 deg")]
		[TestCase(360, ExpectedResult = 0, TestName = "360 deg")]
		public int DegreesConversionIntegrity(int degrees)
		{
			var angle = WAngle.FromDegrees(degrees);
			Assert.That(angle.Angle, Is.InRange(0, 1023), "FromDegrees produced an internal angle outside the 10-bit mask range.");
			return angle.Angle;
		}

		[Test]
		public void InverseTrigTableAccessAndRounding()
		{
			// CosineTable[256] is 0. This forces the search to the end of the table.
			// We verify the neighbor check (index + 1) is clamped or safe.
			Assert.DoesNotThrow(() => WAngle.ArcSin(0), "ArcSin(0) caused an out-of-bounds neighbor check.");
			Assert.That(WAngle.ArcSin(0).Angle, Is.EqualTo(0), "ArcSin(0) should return 0 units.");

			Assert.DoesNotThrow(() => WAngle.ArcSin(1024), "ArcSin(1024) caused an out-of-bounds neighbor check.");
			Assert.That(WAngle.ArcSin(1024).Angle, Is.EqualTo(256), "ArcSin(1024) should return 256 units (90 degrees).");
		}

		[Test]
		public void ArcTanPrecisionLimitGuard()
		{
			// The ratio ay > ax * 167 triggers an early return to prevent table overflow.
			Assert.DoesNotThrow(() => WAngle.ArcTan(166, 1), "ArcTan failed precision lookup just below the guard threshold.");

			var result = WAngle.ArcTan(167, 1);
			Assert.That(result.Angle, Is.EqualTo(256), "ArcTan failed to snap to 90 degrees at/above the precision limit.");

			var extremeResult = WAngle.ArcTan(int.MaxValue, 1);
			Assert.That(extremeResult.Angle, Is.EqualTo(256), "ArcTan failed to snap extreme ratios to 90 degrees.");
		}
	}
}
