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

using NUnit.Framework;
using OpenRA.Widgets;

namespace OpenRA.Test
{
	[TestFixture]
	public class MediatorTest
	{
		[TestCase(TestName = "Mediator test")]
		public void Test()
		{
			var mediator = new Mediator();
			var testHandler = new TestHandler();
			mediator.Subscribe(testHandler);

			mediator.Send(new TestNotificaton());

			Assert.IsTrue(testHandler.WasNotified);
		}
	}

	public class TestHandler : INotificationHandler<TestNotificaton>
	{
		public bool WasNotified { get; set; }

		public void Handle(TestNotificaton notification)
		{
			WasNotified = true;
		}
	}

	public class TestNotificaton { }
}
