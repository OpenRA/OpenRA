using System;
using System.Collections.Generic;
using OpenRA.Primitives;
using Xunit;

namespace OpenRA.XUnitTests
{
    public class ActionQueueXUnitTest
    {
		[Fact]

		public void XUnitActionsArePerformedOrderedByTimeThenByInsertionOrder()
		{
			var list = new List<int>();
			var queue = new ActionQueue();
			queue.Add(() => list.Add(1), 3);
			queue.Add(() => list.Add(7), 2);
			queue.Add(() => list.Add(8), 2);
			queue.Add(() => list.Add(4), 1);
			queue.Add(() => list.Add(2), 0);
			queue.Add(() => list.Add(3), 0);
			queue.Add(() => list.Add(9), 2);
			queue.Add(() => list.Add(5), 1);
			queue.Add(() => list.Add(6), 1);
			queue.PerformActions(1);
			queue.PerformActions(2);
			queue.PerformActions(4);

			var expected = new List<int> { 2, 3, 4, 5, 6, 7, 8, 9, 1 };
			Assert.NotNull(queue);
			Assert.Equal(expected, list);
		}
	}
}
