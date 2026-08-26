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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Tcd.Formations;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class FormationShapesTest
	{
		const int Spacing = 1024;

		[TestCase(TestName = "Every unit gets exactly one distinct slot")]
		public void EveryUnitGetsADistinctSlot()
		{
			foreach (var shape in Enum.GetValues<FormationShape>())
			{
				int[] ranks = [0, 0, 1, 1, 1, 3, 3];
				var offsets = FormationShapes.Offsets(shape, ranks, Spacing);

				Assert.That(offsets.Length, Is.EqualTo(ranks.Length), $"{shape}: wrong slot count");
				Assert.That(offsets.Distinct().Count(), Is.EqualTo(ranks.Length), $"{shape}: two units share a slot");
			}
		}

		[TestCase(TestName = "Rectangle shapes itself from the unit count")]
		public void GridIsAsSquareAsTheCountAllows()
		{
			var six = FormationShapes.Offsets(FormationShape.Grid, [1, 1, 1, 1, 1, 1], Spacing);
			Assert.That(six.Select(o => o.Y).Distinct().Count(), Is.EqualTo(2), "six units should make two rows");
			Assert.That(six.Select(o => o.X).Distinct().Count(), Is.EqualTo(3), "six units should make three columns");

			var twelve = FormationShapes.Offsets(FormationShape.Grid, Enumerable.Repeat(1, 12).ToArray(), Spacing);
			Assert.That(twelve.Select(o => o.Y).Distinct().Count(), Is.EqualTo(3), "twelve units should make three rows");
		}

		[TestCase(TestName = "Rectangle rows never exceed the width cap")]
		public void GridRespectsTheRowLimit()
		{
			var offsets = FormationShapes.Offsets(FormationShape.Grid, Enumerable.Repeat(1, 20).ToArray(), Spacing, maxRowWidth: 4);

			foreach (var row in offsets.GroupBy(o => o.Y))
				Assert.That(row.Count(), Is.LessThanOrEqualTo(4), "a row is wider than the limit");
		}

		[TestCase(TestName = "Triangle puts one unit at the tip and widens behind it")]
		public void WedgeWidensBackwards()
		{
			int[] ranks = [0, 1, 1, 2, 2];
			var offsets = FormationShapes.Offsets(FormationShape.Wedge, ranks, Spacing);

			var tip = offsets.OrderBy(o => o.Y).First();
			Assert.That(offsets.Count(o => o.Y == tip.Y), Is.EqualTo(1), "the tip should be a single unit");

			var widths = offsets.GroupBy(o => o.Y).OrderBy(g => g.Key).Select(g => g.Max(o => Math.Abs(o.X))).ToArray();
			for (var i = 1; i < widths.Length; i++)
				Assert.That(widths[i], Is.GreaterThanOrEqualTo(widths[i - 1]), "the wedge narrows towards the back");
		}

		[TestCase(TestName = "Triangle arms keep neighbours one spacing apart")]
		public void WedgeArmsAreTight()
		{
			var offsets = FormationShapes.Offsets(FormationShape.Wedge, [0, 1, 1, 2, 2], Spacing);

			// Second unit on an arm sits one step further along it, not one step on each axis.
			var arm = offsets.OrderBy(o => o.Y).Skip(1).First();
			var tip = offsets.OrderBy(o => o.Y).First();
			var step = Math.Sqrt(Math.Pow(arm.X - tip.X, 2) + Math.Pow(arm.Y - tip.Y, 2));

			Assert.That(step, Is.LessThan(Spacing * 1.1), "wedge neighbours are further apart than one spacing");
		}

		[TestCase(TestName = "Formation is centred on the origin")]
		public void FormationIsCentred()
		{
			var offsets = FormationShapes.Offsets(FormationShape.Grid, [0, 0, 0, 2, 2], Spacing);

			Assert.That(Math.Abs(offsets.Sum(o => o.X)), Is.LessThanOrEqualTo(offsets.Length));
			Assert.That(Math.Abs(offsets.Sum(o => o.Y)), Is.LessThanOrEqualTo(offsets.Length));
		}

		[TestCase(TestName = "A single unit stands on the origin")]
		public void SingleUnitSitsOnTheOrigin()
		{
			var offsets = FormationShapes.Offsets(FormationShape.Grid, [4], Spacing);

			Assert.That(offsets.Length, Is.EqualTo(1));
			Assert.That(offsets[0], Is.EqualTo(WVec.Zero));
		}

		[TestCase(TestName = "No units produces no slots")]
		public void EmptyInputProducesNoSlots()
		{
			Assert.That(FormationShapes.Offsets(FormationShape.Grid, [], Spacing), Is.Empty);
		}

		[TestCase(TestName = "Invalid arguments are rejected")]
		public void InvalidArgumentsAreRejected()
		{
			Assert.Throws<ArgumentNullException>(() => FormationShapes.Offsets(FormationShape.Grid, null, Spacing));
			Assert.Throws<ArgumentOutOfRangeException>(() => FormationShapes.Offsets(FormationShape.Grid, [1], 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => FormationShapes.Offsets(FormationShape.Grid, [1], Spacing, 0));
		}
	}
}
