#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using NUnit.Framework;
using OpenRA.Primitives;

namespace OpenRA.Test
{
	[TestFixture]
	class PriorityQueueTest
	{
		[TestCase(TestName = "PriorityQueue maintains invariants when adding and removing items.")]
		public void PriorityQueueGeneralTest()
		{
			var queue = new PriorityQueue<int>();

			Assert.IsTrue(queue.Empty, "New queue should start out empty.");
			Assert.Throws<InvalidOperationException>(() => queue.Peek(), "Peeking at an empty queue should throw.");
			Assert.Throws<InvalidOperationException>(() => queue.Pop(), "Popping an empty queue should throw.");

			foreach (var value in new[] { 4, 3, 5, 1, 2 })
			{
				queue.Add(value);
				Assert.IsFalse(queue.Empty, "Queue should not be empty - items have been added.");
			}

			foreach (var value in new[] { 1, 2, 3, 4, 5 })
			{
				Assert.AreEqual(value, queue.Peek(), "Peek returned the wrong item - should be in order.");
				Assert.IsFalse(queue.Empty, "Queue should not be empty yet.");
				Assert.AreEqual(value, queue.Pop(), "Pop returned the wrong item - should be in order.");
			}

			Assert.IsTrue(queue.Empty, "Queue should now be empty.");
			Assert.Throws<InvalidOperationException>(() => queue.Peek(), "Peeking at an empty queue should throw.");
			Assert.Throws<InvalidOperationException>(() => queue.Pop(), "Popping an empty queue should throw.");
		}
	}
}