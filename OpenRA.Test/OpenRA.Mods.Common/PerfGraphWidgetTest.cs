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

using System.Text;
using NUnit.Framework;
using OpenRA.Mods.Common.Widgets;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class PerfGraphWidgetTest
	{
		readonly PerfGraphWidget widget = new();
		readonly StringBuilder output = new(6);

		void AssertThat(double input, string expected)
		{
			output.Clear();
			widget.SixCharacterFormatFloat(output, input);
			Assert.That(output.ToString(), Is.EqualTo(expected));
		}

		[TestCase(TestName = "NaN is reported with 3 padding spaces after.")]
		public void NaN()
		{
			AssertThat(double.NaN, "NaN   ");
		}

		[TestCase(TestName = "Infinities are reported with 2 padding spaces after.")]
		public void Infinity()
		{
			AssertThat(double.PositiveInfinity, "+Inf  ");
			AssertThat(double.NegativeInfinity, "-Inf  ");
		}

		[TestCase(TestName = "Negative numbers are discarded as \"<0\" with 4 padding spaces after.")]
		public void Negative()
		{
			AssertThat(-0.000001, "<0    ");
		}

		[TestCase(TestName = "Numbers < 1 start with a decimal point ('.') and show 5 decimal places after the decimal point.")]
		public void Fractional()
		{
			AssertThat(0, ".00000");
			AssertThat(0.000001, ".00000");
			AssertThat(0.000004, ".00000");
			AssertThat(0.0000049, ".00000");
			AssertThat(0.000005, ".00001");
			AssertThat(0.000009, ".00001");
			AssertThat(0.00001, ".00001");
			AssertThat(0.00004, ".00004");
			AssertThat(0.00005, ".00005");
			AssertThat(0.00009, ".00009");
			AssertThat(0.0001, ".00010");
			AssertThat(0.0004, ".00040");
			AssertThat(0.0005, ".00050");
			AssertThat(0.0009, ".00090");
			AssertThat(0.001, ".00100");
			AssertThat(0.005, ".00500");
			AssertThat(0.01, ".01000");
			AssertThat(0.05, ".05000");
			AssertThat(0.1, ".10000");
			AssertThat(0.5, ".50000");
		}

		[TestCase(TestName = "Numbers >= 1 and < 100000 show the 5 most signifcant digits with a decimal point ('.').")]
		public void FiveSignificantDigits()
		{
			AssertThat(1, "1.0000");
			AssertThat(9, "9.0000");
			AssertThat(9.00005, "9.0001");
			AssertThat(10, "10.000");
			AssertThat(99, "99.000");
			AssertThat(99.0005, "99.001");
			AssertThat(100, "100.00");
			AssertThat(123.4321, "123.43");
			AssertThat(999.005, "999.01");
			AssertThat(1000, "1000.0");
			AssertThat(9999.04999999, "9999.0");
			AssertThat(9999.05000001, "9999.1"); // Binary conversion causes 9999.05 + 0.05 => 9999.099999999999.
			AssertThat(10000, "10000.");
		}

		[TestCase(TestName = "Numbers >= 100000 and < 1000000 show the 5 most signifcant digits with a decimal point ('.').")]
		public void SixSignificantDigits()
		{
			AssertThat(99999.9, "100000");
			AssertThat(100000, "100000");
			AssertThat(999999, "999999");
		}

		[TestCase(TestName = "Numbers >= 1000000 are very unlikely for perf measurements and would require different formatting.")]
		public void TooBig()
		{
			AssertThat(1000000, "TooBig");
			AssertThat(1234321, "TooBig");
			AssertThat(10000000, "TooBig");
			AssertThat(100000000, "TooBig");
		}
	}
}
