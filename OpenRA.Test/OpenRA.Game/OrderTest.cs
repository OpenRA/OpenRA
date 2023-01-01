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

using System.IO;
using NUnit.Framework;
using OpenRA.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	public class OrderTest
	{
		byte[] RoundTripOrder(byte[] bytes)
		{
			return Order.Deserialize(null, new BinaryReader(new MemoryStream(bytes))).Serialize();
		}

		[TestCase(TestName = "Order data persists over serialization (empty)")]
		public void SerializeEmpty()
		{
			var o = new Order().Serialize();
			Assert.That(RoundTripOrder(o), Is.EqualTo(o));
		}

		[TestCase(TestName = "Order data persists over serialization (unqueued)")]
		public void SerializeUnqueued()
		{
			var o = new Order("Test", null, false).Serialize();
			Assert.That(RoundTripOrder(o), Is.EqualTo(o));
		}

		[TestCase(TestName = "Order data persists over serialization (queued)")]
		public void SerializeQueued()
		{
			var o = new Order("Test", null, true).Serialize();
			Assert.That(RoundTripOrder(o), Is.EqualTo(o));
		}

		[TestCase(TestName = "Order data persists over serialization (pos target)")]
		public void SerializePos()
		{
			var o = new Order("Test", null, Target.FromPos(new WPos(int.MinValue, 0, int.MaxValue)), false).Serialize();
			Assert.That(RoundTripOrder(o), Is.EqualTo(o));
		}

		[TestCase(TestName = "Order data persists over serialization (invalid target)")]
		public void SerializeInvalid()
		{
			var o = new Order("Test", null, Target.Invalid, false).Serialize();
			Assert.That(RoundTripOrder(o), Is.EqualTo(o));
		}

		[TestCase(TestName = "Order data persists over serialization (extra fields)")]
		public void SerializeExtra()
		{
			var o = new Order("Test", null, Target.Invalid, true)
			{
				TargetString = "TargetString",
				ExtraLocation = new CPos(2047, 2047, 128),
				ExtraData = uint.MaxValue,
				IsImmediate = true,
			}.Serialize();
			Assert.That(RoundTripOrder(o), Is.EqualTo(o));
		}
	}
}
