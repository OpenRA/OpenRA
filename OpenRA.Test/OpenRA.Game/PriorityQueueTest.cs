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
using OpenRA.Mods.Common;
using OpenRA.Support;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class PriorityQueueTest
	{
		readonly struct Int32Comparer : IComparer<int>
		{
			public int Compare(int x, int y) => x.CompareTo(y);
		}

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
			var values = Enumerable.Range(0, count).ToList();
			var shuffledValues = values.Shuffle(mt).ToArray();

			var queue = new Primitives.PriorityQueue<int, Int32Comparer>(default);

			Assert.That(queue.Empty, Is.True, "New queue should start out empty.");
			Assert.Throws<InvalidOperationException>(() => queue.Peek(), "Peeking at an empty queue should throw.");
			Assert.Throws<InvalidOperationException>(() => queue.Pop(), "Popping an empty queue should throw.");

			foreach (var value in shuffledValues)
			{
				queue.Add(value);
				Assert.That(queue.Empty, Is.False, "Queue should not be empty - items have been added.");
			}

			foreach (var value in values)
			{
				Assert.That(value, Is.EqualTo(queue.Peek()), "Peek returned the wrong item - should be in order.");
				Assert.That(queue.Empty, Is.False, "Queue should not be empty yet.");
				Assert.That(value, Is.EqualTo(queue.Pop()), "Pop returned the wrong item - should be in order.");
			}

			Assert.That(queue.Empty, Is.True, "Queue should now be empty.");
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

			var queue = new Primitives.PriorityQueue<int, Int32Comparer>(default);

			Assert.That(queue.Empty, Is.True, "New queue should start out empty.");
			Assert.Throws<InvalidOperationException>(() => queue.Peek(), "Peeking at an empty queue should throw.");
			Assert.Throws<InvalidOperationException>(() => queue.Pop(), "Popping an empty queue should throw.");

			foreach (var value in shuffledValues.Take(10))
			{
				queue.Add(value);
				Assert.That(queue.Empty, Is.False, "Queue should not be empty - items have been added.");
			}

			foreach (var value in shuffledValues.Take(10).Order().Take(5))
			{
				Assert.That(value, Is.EqualTo(queue.Peek()), "Peek returned the wrong item - should be in order.");
				Assert.That(queue.Empty, Is.False, "Queue should not be empty yet.");
				Assert.That(value, Is.EqualTo(queue.Pop()), "Pop returned the wrong item - should be in order.");
			}

			foreach (var value in shuffledValues.Skip(10).Take(5))
			{
				queue.Add(value);
				Assert.That(queue.Empty, Is.False, "Queue should not be empty - items have been added.");
			}

			foreach (var value in shuffledValues.Take(10).Order().Skip(5)
				.Concat(shuffledValues.Skip(10).Take(5)).Order().Take(5))
			{
				Assert.That(value, Is.EqualTo(queue.Peek()), "Peek returned the wrong item - should be in order.");
				Assert.That(queue.Empty, Is.False, "Queue should not be empty yet.");
				Assert.That(value, Is.EqualTo(queue.Pop()), "Pop returned the wrong item - should be in order.");
			}

			foreach (var value in shuffledValues.Skip(15))
			{
				queue.Add(value);
				Assert.That(queue.Empty, Is.False, "Queue should not be empty - items have been added.");
			}

			foreach (var value in shuffledValues.Take(10).Order().Skip(5)
				.Concat(shuffledValues.Skip(10).Take(5)).Order().Skip(5)
				.Concat(shuffledValues.Skip(15)).Order())
			{
				Assert.That(value, Is.EqualTo(queue.Peek()), "Peek returned the wrong item - should be in order.");
				Assert.That(queue.Empty, Is.False, "Queue should not be empty yet.");
				Assert.That(value, Is.EqualTo(queue.Pop()), "Pop returned the wrong item - should be in order.");
			}

			Assert.That(queue.Empty, Is.True, "Queue should now be empty.");
			Assert.Throws<InvalidOperationException>(() => queue.Peek(), "Peeking at an empty queue should throw.");
			Assert.Throws<InvalidOperationException>(() => queue.Pop(), "Popping an empty queue should throw.");
		}
	}
}
