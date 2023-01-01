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

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Primitives;

namespace OpenRA.Test
{
	[TestFixture]
	class ActionQueueTest
	{
		[TestCase(TestName = "ActionQueue performs actions in order of time, then insertion order.")]
		public void ActionsArePerformedOrderedByTimeThenByInsertionOrder()
		{
			var list = new List<int>();
			var queue = new ActionQueue();
			queue.Add(() => list.Add(1), 0);
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
			queue.PerformActions(3);
			if (!list.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }))
				Assert.Fail("Actions were not performed in the correct order. Actual order was: " + string.Join(", ", list));
		}
	}
}
