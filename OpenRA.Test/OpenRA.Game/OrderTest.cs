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

using System.IO;
using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	public class OrderTest
	{
		Order order;
		Order immediateOrder;

		[SetUp]
		public void SetUp()
		{
			order = new Order("TestOrder", null, false)
			{
				TargetString = "TestTarget",
				TargetLocation = new CPos(1234, 5678),
				ExtraData = 1234,
				ExtraLocation = new CPos(555, 555)
			};

			immediateOrder = new Order("TestOrderImmediate", null, false)
			{
				IsImmediate = true,
				TargetString = "TestTarget"
			};
		}

		[TestCase(TestName = "Data persists over serialization")]
		public void SerializeA()
		{
			var serializedData = new MemoryStream(order.Serialize());
			var result = Order.Deserialize(null, new BinaryReader(serializedData));

			Assert.That(result.ToString(), Is.EqualTo(order.ToString()));
		}

		[TestCase(TestName = "Data persists over serialization immediate")]
		public void SerializeB()
		{
			var serializedData = new MemoryStream(immediateOrder.Serialize());
			var result = Order.Deserialize(null, new BinaryReader(serializedData));

			Assert.That(result.ToString(), Is.EqualTo(immediateOrder.ToString()));
		}
	}
}
