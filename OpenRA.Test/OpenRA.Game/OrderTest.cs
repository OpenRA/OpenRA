#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;
using NUnit.Framework;
using OpenRA.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	public class OrderTest
	{
		Order order;
		Order targetInvalid;
		Order immediateOrder;

		[SetUp]
		public void SetUp()
		{
			order = new Order("TestOrder", null, false)
			{
				TargetString = "TestTarget",
				ExtraData = 1234,
				ExtraLocation = new CPos(555, 555)
			};

			targetInvalid = new Order("TestOrder", null, Target.Invalid, false);

			immediateOrder = new Order("TestOrderImmediate", null, false)
			{
				IsImmediate = true,
				TargetString = "TestTarget"
			};
		}

		Order RoundTripOrder(Order o)
		{
			var serializedData = new MemoryStream(o.Serialize());
			return Order.Deserialize(null, new BinaryReader(serializedData));
		}

		[TestCase(TestName = "Data persists over serialization")]
		public void SerializeA()
		{
			Assert.That(RoundTripOrder(order).ToString(), Is.EqualTo(order.ToString()));
		}

		[TestCase(TestName = "Data persists over serialization (Immediate order)")]
		public void SerializeB()
		{
			Assert.That(RoundTripOrder(immediateOrder).ToString(), Is.EqualTo(immediateOrder.ToString()));
		}

		[TestCase(TestName = "Data persists over serialization (Invalid target)")]
		public void SerializeC()
		{
			Assert.That(RoundTripOrder(targetInvalid).ToString(), Is.EqualTo(targetInvalid.ToString()));
		}
	}
}
