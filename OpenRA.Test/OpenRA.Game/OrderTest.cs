#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	public class OrderTest
	{
		ObjectCreator oc;
		Order order;
		Order immediateOrder;

		[SetUp]
		public void SetUp()
		{
			oc = new ObjectCreator(new Assembly[] { Assembly.GetAssembly(typeof(Order)) });

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
			var serializedData = new MemoryStream(order.Serialize(oc));
			var result = Order.Deserialize(null, new BinaryReader(serializedData), oc);

			Assert.That(result.ToString(), Is.EqualTo(order.ToString()));
		}

		[TestCase(TestName = "Data persists over serialization immediate")]
		public void SerializeB()
		{
			var serializedData = new MemoryStream(immediateOrder.Serialize(oc));
			var result = Order.Deserialize(null, new BinaryReader(serializedData), oc);

			Assert.That(result.ToString(), Is.EqualTo(immediateOrder.ToString()));
		}
	}
}
