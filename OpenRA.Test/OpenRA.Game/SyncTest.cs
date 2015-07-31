#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	class SyncTest
	{
		class Complex : ISync
		{
			[Sync]
			public bool Bool = true;
			[Sync]
			public int Int = -123456789;
			[Sync]
			public int2 Int2 = new int2(123, -456);
			[Sync]
			public CPos CPos = new CPos(123, -456);
			[Sync]
			public CVec CVec = new CVec(123, -456);
			[Sync]
			public WDist WDist = new WDist(123);
			[Sync]
			public WPos WPos = new WPos(123, -456, int.MaxValue);
			[Sync]
			public WVec WVec = new WVec(123, -456, int.MaxValue);
			[Sync]
			public WAngle WAngle = new WAngle(123);
			[Sync]
			public WRot WRot = new WRot(new WAngle(123), new WAngle(-456), new WAngle(int.MaxValue));
			[Sync]
			public Target Target = Target.FromPos(new WPos(123, -456, int.MaxValue));
		}

		[TestCase(TestName = "Sync hashing has not accidentally changed")]
		public void ComplexHash()
		{
			// If you have intentionally changed the values used for sync hashing, just update the expected value.
			Assert.AreEqual(-2024026914, Sync.CalculateSyncHash(new Complex()));
		}

		class Flat : ISync
		{
			[Sync]
			int a = 123456789;
			[Sync]
			bool b = true;
		}

		class Base : ISync
		{
			[Sync]
			bool b = true;
		}

		class Derived : Base
		{
			[Sync]
			int a = 123456789;
		}

		[TestCase(TestName = "All sync members in inheritance hierarchy are hashed")]
		public void DerivedHash()
		{
			Assert.AreEqual(Sync.CalculateSyncHash(new Flat()), Sync.CalculateSyncHash(new Derived()));
		}
	}
}
