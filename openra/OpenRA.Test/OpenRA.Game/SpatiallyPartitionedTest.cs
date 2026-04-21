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
	sealed class SpatiallyPartitionedTest
	{
		[TestCase(TestName = "SpatiallyPartitioned.At works")]
		public void SpatiallyPartitionedAtTest()
		{
			var partition = new SpatiallyPartitioned<object>(5, 5, 2);

			var a = new object();
			partition.Add(a, new Rectangle(0, 0, 1, 1));
			Assert.That(partition.At(new int2(0, 0)), Does.Contain(a), "a is not present after add");
			Assert.That(partition.At(new int2(0, 1)), Does.Not.Contain(a), "a is present in the wrong location");
			Assert.That(partition.At(new int2(1, 0)), Does.Not.Contain(a), "a is present in the wrong location");

			var b = new object();
			partition.Add(b, new Rectangle(1, 1, 2, 2));
			Assert.That(partition.At(new int2(0, 1)), Does.Not.Contain(b), "b is present in the wrong location");
			Assert.That(partition.At(new int2(1, 0)), Does.Not.Contain(b), "b is present in the wrong location");
			Assert.That(partition.At(new int2(1, 1)), Does.Contain(b), "b is not present after add");
			Assert.That(partition.At(new int2(2, 2)), Does.Contain(b), "b is not present after add");
			Assert.That(partition.At(new int2(2, 3)), Does.Not.Contain(b), "b is present in the wrong location");
			Assert.That(partition.At(new int2(3, 2)), Does.Not.Contain(b), "b is present in the wrong location");
			Assert.That(partition.At(new int2(3, 3)), Does.Not.Contain(b), "b is present in the wrong location");

			partition[b] = new Rectangle(4, 4, 1, 1);
			Assert.That(partition.At(new int2(0, 0)), Does.Contain(a), "a wrongly changed location when b was updated");
			Assert.That(partition.At(new int2(4, 4)), Does.Contain(b), "b is not present at the new location in the extreme corner of the partition");
			Assert.That(partition.At(new int2(1, 1)), Does.Not.Contain(b), "b is still present at the old location after update");

			partition.Remove(a);
			Assert.That(partition.At(new int2(0, 0)), Does.Not.Contain(b), "a is still present after removal");
			Assert.That(partition.At(new int2(4, 4)), Does.Contain(b), "b wrongly changed location when a was removed");
		}

		[TestCase(TestName = "SpatiallyPartitioned.InBox works")]
		public void SpatiallyPartitionedInBoxTest()
		{
			var partition = new SpatiallyPartitioned<object>(5, 5, 2);

			var a = new object();
			partition.Add(a, new Rectangle(0, 0, 1, 1));
			Assert.That(partition.InBox(new Rectangle(0, 0, 0, 0)), Does.Not.Contain(a), "Searching an empty area should not return a");
			Assert.That(partition.InBox(new Rectangle(0, 0, 1, 1)), Does.Contain(a), "a is not present after add");
			Assert.That(partition.InBox(new Rectangle(0, 1, 1, 1)), Does.Not.Contain(a), "a is present in the wrong location");
			Assert.That(partition.InBox(new Rectangle(1, 0, 1, 1)), Does.Not.Contain(a), "a is present in the wrong location");

			var b = new object();
			partition.Add(b, new Rectangle(1, 1, 2, 2));
			Assert.That(partition.InBox(new Rectangle(0, 1, 1, 1)), Does.Not.Contain(b), "b is present in the wrong location");
			Assert.That(partition.InBox(new Rectangle(1, 0, 1, 1)), Does.Not.Contain(b), "b is present in the wrong location");
			Assert.That(partition.InBox(new Rectangle(1, 1, 1, 1)), Does.Contain(b), "b is not present after add");
			Assert.That(partition.InBox(new Rectangle(2, 2, 1, 1)), Does.Contain(b), "b is not present after add");
			Assert.That(partition.InBox(new Rectangle(2, 3, 1, 1)), Does.Not.Contain(b), "b is present in the wrong location");
			Assert.That(partition.InBox(new Rectangle(3, 2, 1, 1)), Does.Not.Contain(b), "b is present in the wrong location");
			Assert.That(partition.InBox(new Rectangle(3, 3, 1, 1)), Does.Not.Contain(b), "b is present in the wrong location");

			Assert.That(new[] { b }, Is.EquivalentTo(partition.InBox(new Rectangle(1, 1, 1, 1))),
				"Searching within a single partition bin did not return the correct result");
			Assert.That(partition.InBox(new Rectangle(0, 0, 5, 5)), Is.Unique,
				"Searching the whole partition returned duplicates of some items");
			Assert.That(new[] { a, b }, Is.EquivalentTo(partition.InBox(new Rectangle(0, 0, 5, 5))),
				"Searching the whole partition did not return all items");
			Assert.That(new[] { a, b }, Is.EquivalentTo(partition.InBox(new Rectangle(-10, -10, 25, 25))),
				"Searching an area larger than the partition did not return all items");

			partition[b] = new Rectangle(4, 4, 1, 1);
			Assert.That(partition.InBox(new Rectangle(0, 0, 1, 1)), Does.Contain(a), "a wrongly changed location when b was updated");
			Assert.That(partition.InBox(new Rectangle(4, 4, 1, 1)), Does.Contain(b), "b is not present at the new location in the extreme corner of the partition");
			Assert.That(partition.InBox(new Rectangle(1, 1, 1, 1)), Does.Not.Contain(b), "b is still present at the old location after update");

			partition.Remove(a);
			Assert.That(partition.InBox(new Rectangle(0, 0, 1, 1)), Does.Not.Contain(a), "a is still present after removal");
			Assert.That(partition.InBox(new Rectangle(4, 4, 1, 1)), Does.Contain(b), "b wrongly changed location when a was removed");
		}
	}
}
