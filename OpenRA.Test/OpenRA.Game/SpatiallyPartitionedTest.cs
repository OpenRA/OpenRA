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
	class SpatiallyPartitionedTest
	{
		[TestCase(TestName = "SpatiallyPartitioned.At works")]
		public void SpatiallyPartitionedAtTest()
		{
			var partition = new SpatiallyPartitioned<object>(5, 5, 2);

			var a = new object();
			partition.Add(a, new Rectangle(0, 0, 1, 1));
			CollectionAssert.Contains(partition.At(new int2(0, 0)), a, "a is not present after add");
			CollectionAssert.DoesNotContain(partition.At(new int2(0, 1)), a, "a is present in the wrong location");
			CollectionAssert.DoesNotContain(partition.At(new int2(1, 0)), a, "a is present in the wrong location");

			var b = new object();
			partition.Add(b, new Rectangle(1, 1, 2, 2));
			CollectionAssert.DoesNotContain(partition.At(new int2(0, 1)), b, "b is present in the wrong location");
			CollectionAssert.DoesNotContain(partition.At(new int2(1, 0)), b, "b is present in the wrong location");
			CollectionAssert.Contains(partition.At(new int2(1, 1)), b, "b is not present after add");
			CollectionAssert.Contains(partition.At(new int2(2, 2)), b, "b is not present after add");
			CollectionAssert.DoesNotContain(partition.At(new int2(2, 3)), b, "b is present in the wrong location");
			CollectionAssert.DoesNotContain(partition.At(new int2(3, 2)), b, "b is present in the wrong location");
			CollectionAssert.DoesNotContain(partition.At(new int2(3, 3)), b, "b is present in the wrong location");

			partition.Update(b, new Rectangle(4, 4, 1, 1));
			CollectionAssert.Contains(partition.At(new int2(0, 0)), a, "a wrongly changed location when b was updated");
			CollectionAssert.Contains(partition.At(new int2(4, 4)), b, "b is not present at the new location in the extreme corner of the partition");
			CollectionAssert.DoesNotContain(partition.At(new int2(1, 1)), b, "b is still present at the old location after update");

			partition.Remove(a);
			CollectionAssert.DoesNotContain(partition.At(new int2(0, 0)), a, "a is still present after removal");
			CollectionAssert.Contains(partition.At(new int2(4, 4)), b, "b wrongly changed location when a was removed");
		}

		[TestCase(TestName = "SpatiallyPartitioned.InBox works")]
		public void SpatiallyPartitionedInBoxTest()
		{
			var partition = new SpatiallyPartitioned<object>(5, 5, 2);

			var a = new object();
			partition.Add(a, new Rectangle(0, 0, 1, 1));
			CollectionAssert.DoesNotContain(partition.InBox(new Rectangle(0, 0, 0, 0)), a, "Searching an empty area should not return a");
			CollectionAssert.Contains(partition.InBox(new Rectangle(0, 0, 1, 1)), a, "a is not present after add");
			CollectionAssert.DoesNotContain(partition.InBox(new Rectangle(0, 1, 1, 1)), a, "a is present in the wrong location");
			CollectionAssert.DoesNotContain(partition.InBox(new Rectangle(1, 0, 1, 1)), a, "a is present in the wrong location");

			var b = new object();
			partition.Add(b, new Rectangle(1, 1, 2, 2));
			CollectionAssert.DoesNotContain(partition.InBox(new Rectangle(0, 1, 1, 1)), b, "b is present in the wrong location");
			CollectionAssert.DoesNotContain(partition.InBox(new Rectangle(1, 0, 1, 1)), b, "b is present in the wrong location");
			CollectionAssert.Contains(partition.InBox(new Rectangle(1, 1, 1, 1)), b, "b is not present after add");
			CollectionAssert.Contains(partition.InBox(new Rectangle(2, 2, 1, 1)), b, "b is not present after add");
			CollectionAssert.DoesNotContain(partition.InBox(new Rectangle(2, 3, 1, 1)), b, "b is present in the wrong location");
			CollectionAssert.DoesNotContain(partition.InBox(new Rectangle(3, 2, 1, 1)), b, "b is present in the wrong location");
			CollectionAssert.DoesNotContain(partition.InBox(new Rectangle(3, 3, 1, 1)), b, "b is present in the wrong location");

			CollectionAssert.AreEquivalent(new[] { b }, partition.InBox(new Rectangle(1, 1, 1, 1)),
				"Searching within a single partition bin did not return the correct result");
			CollectionAssert.AllItemsAreUnique(partition.InBox(new Rectangle(0, 0, 5, 5)),
				"Searching the whole partition returned duplicates of some items");
			CollectionAssert.AreEquivalent(new[] { a, b }, partition.InBox(new Rectangle(0, 0, 5, 5)),
				"Searching the whole partition did not return all items");
			CollectionAssert.AreEquivalent(new[] { a, b }, partition.InBox(new Rectangle(-10, -10, 25, 25)),
				"Searching an area larger than the partition did not return all items");

			partition.Update(b, new Rectangle(4, 4, 1, 1));
			CollectionAssert.Contains(partition.InBox(new Rectangle(0, 0, 1, 1)), a, "a wrongly changed location when b was updated");
			CollectionAssert.Contains(partition.InBox(new Rectangle(4, 4, 1, 1)), b, "b is not present at the new location in the extreme corner of the partition");
			CollectionAssert.DoesNotContain(partition.InBox(new Rectangle(1, 1, 1, 1)), b, "b is still present at the old location after update");

			partition.Remove(a);
			CollectionAssert.DoesNotContain(partition.InBox(new Rectangle(0, 0, 1, 1)), a, "a is still present after removal");
			CollectionAssert.Contains(partition.InBox(new Rectangle(4, 4, 1, 1)), b, "b wrongly changed location when a was removed");
		}
	}
}
