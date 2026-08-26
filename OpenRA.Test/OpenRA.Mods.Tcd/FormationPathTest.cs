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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Tcd.Formations;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class FormationPathTest
	{
		static WPos P(int x, int y) => new(x, y, 0);

		static double Gap(WPos a, WPos b)
		{
			double dx = b.X - a.X;
			double dy = b.Y - a.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}

		[TestCase(TestName = "Units are evenly spaced along a straight drawn line")]
		public void StraightLineIsEvenlySpaced()
		{
			List<WPos> path = [P(0, 0), P(9000, 0)];
			var slots = FormationPath.Distribute(path, 4, closed: false);

			Assert.That(slots.Length, Is.EqualTo(4));
			Assert.That(slots[0], Is.EqualTo(P(0, 0)), "the first unit should sit on the start");
			Assert.That(slots[^1], Is.EqualTo(P(9000, 0)), "the last unit should sit on the end");

			var gaps = Enumerable.Range(1, 3).Select(i => Gap(slots[i - 1], slots[i])).ToArray();
			Assert.That(gaps.Max() - gaps.Min(), Is.LessThan(2), "gaps along the line are uneven");
		}

		[TestCase(TestName = "Spacing stays even across a corner")]
		public void SpacingIsEvenAcrossSegments()
		{
			// An L: 4000 across, then 4000 down.
			List<WPos> path = [P(0, 0), P(4000, 0), P(4000, 4000)];
			var slots = FormationPath.Distribute(path, 5, closed: false);

			var gaps = Enumerable.Range(1, 4).Select(i => Gap(slots[i - 1], slots[i])).ToArray();
			Assert.That(gaps.Max() - gaps.Min(), Is.LessThan(2), "the corner broke the even spacing");
		}

		[TestCase(TestName = "A closed shape leaves no double gap where it joins")]
		public void ClosedShapeWrapsEvenly()
		{
			// A square drawn as four corner points.
			List<WPos> path = [P(0, 0), P(4000, 0), P(4000, 4000), P(0, 4000)];
			var slots = FormationPath.Distribute(path, 8, closed: true);

			var gaps = Enumerable.Range(1, 7).Select(i => Gap(slots[i - 1], slots[i])).ToArray();
			var closingGap = Gap(slots[^1], slots[0]);

			Assert.That(gaps.Max() - gaps.Min(), Is.LessThan(2), "gaps around the shape are uneven");
			Assert.That(Math.Abs(closingGap - gaps[0]), Is.LessThan(2), "the seam gap does not match the rest");
		}

		[TestCase(TestName = "More units than corners still spread over the whole shape")]
		public void ManyUnitsCoverTheWholePath()
		{
			List<WPos> path = [P(0, 0), P(10000, 0)];
			var slots = FormationPath.Distribute(path, 12, closed: false);

			Assert.That(slots.Distinct().Count(), Is.EqualTo(12), "units are stacking on top of each other");
			Assert.That(slots.Max(s => s.X), Is.EqualTo(10000));
		}

		[TestCase(TestName = "Degenerate paths do not throw")]
		public void DegeneratePathsAreSafe()
		{
			var single = FormationPath.Distribute([P(5, 5)], 3, closed: false);
			Assert.That(single.Length, Is.EqualTo(3));
			Assert.That(single.All(s => s == P(5, 5)), Is.True);

			var zeroLength = FormationPath.Distribute([P(1, 1), P(1, 1)], 3, closed: false);
			Assert.That(zeroLength.Length, Is.EqualTo(3));

			Assert.That(FormationPath.Distribute([P(0, 0), P(1, 0)], 0, closed: false), Is.Empty);
			Assert.Throws<ArgumentNullException>(() => FormationPath.Distribute(null, 3, closed: false));
			Assert.Throws<ArgumentException>(() => FormationPath.Distribute([], 3, closed: false));
		}
	}
}
