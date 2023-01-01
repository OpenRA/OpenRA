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
using OpenRA.Mods.Common;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Test
{
	[TestFixture]
	class PriorityQueueTest
	{
		[TestCase(1, 123)]
		[TestCase(1, 1234)]
		[TestCase(1, 12345)]
		[TestCase(2, 123)]
		[TestCase(2, 1234)]
		[TestCase(2, 12345)]
		[TestCase(10, 123)]
		[TestCase(10, 1234)]
		[TestCase(10, 12345)]
		[TestCase(15, 123)]
		[TestCase(15, 1234)]
		[TestCase(15, 12345)]
		[TestCase(16, 123)]
		[TestCase(16, 1234)]
		[TestCase(16, 12345)]
		[TestCase(17, 123)]
		[TestCase(17, 1234)]
		[TestCase(17, 12345)]
		[TestCase(100, 123)]
		[TestCase(100, 1234)]
		[TestCase(100, 12345)]
		[TestCase(1000, 123)]
		[TestCase(1000, 1234)]
		[TestCase(1000, 12345)]
		public void PriorityQueueAddThenRemoveTest(int count, int seed)
		{
			var mt = new MersenneTwister(seed);
			var values = Enumerable.Range(0, count);
			var shuffledValues = values.Shuffle(mt).ToArray();

			var queue = new PriorityQueue<int>();

			Assert.IsTrue(queue.Empty, "New queue should start out empty.");
			Assert.Throws<InvalidOperationException>(() => queue.Peek(), "Peeking at an empty queue should throw.");
			Assert.Throws<InvalidOperationException>(() => queue.Pop(), "Popping an empty queue should throw.");

			foreach (var value in shuffledValues)
			{
				queue.Add(value);
				Assert.IsFalse(queue.Empty, "Queue should not be empty - items have been added.");
			}

			foreach (var value in values)
			{
				Assert.AreEqual(value, queue.Peek(), "Peek returned the wrong item - should be in order.");
				Assert.IsFalse(queue.Empty, "Queue should not be empty yet.");
				Assert.AreEqual(value, queue.Pop(), "Pop returned the wrong item - should be in order.");
			}

			Assert.IsTrue(queue.Empty, "Queue should now be empty.");
			Assert.Throws<InvalidOperationException>(() => queue.Peek(), "Peeking at an empty queue should throw.");
			Assert.Throws<InvalidOperationException>(() => queue.Pop(), "Popping an empty queue should throw.");
		}

		[TestCase(15, 123)]
		[TestCase(15, 1234)]
		[TestCase(15, 12345)]
		[TestCase(16, 123)]
		[TestCase(16, 1234)]
		[TestCase(16, 12345)]
		[TestCase(17, 123)]
		[TestCase(17, 1234)]
		[TestCase(17, 12345)]
		[TestCase(100, 123)]
		[TestCase(100, 1234)]
		[TestCase(100, 12345)]
		[TestCase(1000, 123)]
		[TestCase(1000, 1234)]
		[TestCase(1000, 12345)]
		public void PriorityQueueAddAndRemoveInterleavedTest(int count, int seed)
		{
			var mt = new MersenneTwister(seed);
			var shuffledValues = Enumerable.Range(0, count).Shuffle(mt).ToArray();

			var queue = new PriorityQueue<int>();

			Assert.IsTrue(queue.Empty, "New queue should start out empty.");
			Assert.Throws<InvalidOperationException>(() => queue.Peek(), "Peeking at an empty queue should throw.");
			Assert.Throws<InvalidOperationException>(() => queue.Pop(), "Popping an empty queue should throw.");

			foreach (var value in shuffledValues.Take(10))
			{
				queue.Add(value);
				Assert.IsFalse(queue.Empty, "Queue should not be empty - items have been added.");
			}

			foreach (var value in shuffledValues.Take(10).OrderBy(x => x).Take(5))
			{
				Assert.AreEqual(value, queue.Peek(), "Peek returned the wrong item - should be in order.");
				Assert.IsFalse(queue.Empty, "Queue should not be empty yet.");
				Assert.AreEqual(value, queue.Pop(), "Pop returned the wrong item - should be in order.");
			}

			foreach (var value in shuffledValues.Skip(10).Take(5))
			{
				queue.Add(value);
				Assert.IsFalse(queue.Empty, "Queue should not be empty - items have been added.");
			}

			foreach (var value in shuffledValues.Take(10).OrderBy(x => x).Skip(5)
				.Concat(shuffledValues.Skip(10).Take(5)).OrderBy(x => x).Take(5))
			{
				Assert.AreEqual(value, queue.Peek(), "Peek returned the wrong item - should be in order.");
				Assert.IsFalse(queue.Empty, "Queue should not be empty yet.");
				Assert.AreEqual(value, queue.Pop(), "Pop returned the wrong item - should be in order.");
			}

			foreach (var value in shuffledValues.Skip(15))
			{
				queue.Add(value);
				Assert.IsFalse(queue.Empty, "Queue should not be empty - items have been added.");
			}

			foreach (var value in shuffledValues.Take(10).OrderBy(x => x).Skip(5)
				.Concat(shuffledValues.Skip(10).Take(5)).OrderBy(x => x).Skip(5)
				.Concat(shuffledValues.Skip(15)).OrderBy(x => x))
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
