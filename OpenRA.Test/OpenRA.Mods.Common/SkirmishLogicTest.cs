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
using OpenRA.Mods.Common.Server;
using OpenRA.Network;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class SkirmishLogicTest
	{
		[TestCase("Multi9", false, "Multi0", true)]
		[TestCase("Multi9", true, "Multi9", false)]
		[TestCase("Multi0", false, "Multi0", true)]
		[TestCase("Multi0", true, "Multi0", true)]
		[TestCase(null, false, null, true)]
		[TestCase(null, true, null, false)]
		public void RestoreSlot(string savedSlot, bool bot, string expectedSlot, bool accepted)
		{
			var client = new Session.Client { Slot = savedSlot, Bot = bot ? "normal" : null, SpawnPoint = 9 };
			var lobby = new Session();
			lobby.Slots.Add("Multi0", new Session.Slot { AllowBots = true });
			if (!bot)
				lobby.Clients.Add(client);

			Assert.That(SkirmishLogic.ValidateClientSlot(lobby, client), Is.EqualTo(accepted));
			Assert.That(client.Slot, Is.EqualTo(expectedSlot));
			if (savedSlot == "Multi9" && !bot)
				Assert.That(client.SpawnPoint, Is.Zero);
		}

		[TestCase(false, false)]
		[TestCase(true, false)]
		[TestCase(false, true)]
		public void RejectUnavailableBotSlot(bool closed, bool occupied)
		{
			var lobby = new Session();
			lobby.Slots.Add("Multi0", new Session.Slot { Closed = closed, AllowBots = closed || occupied });
			if (occupied)
				lobby.Clients.Add(new Session.Client { Slot = "Multi0" });

			var bot = new Session.Client { Slot = "Multi0", Bot = "normal" };
			Assert.That(SkirmishLogic.ValidateClientSlot(lobby, bot), Is.False);
		}

		[Test]
		public void MissingHumanSlotWithFullLobbyBecomesObserver()
		{
			var lobby = new Session();
			lobby.Slots.Add("Multi0", new Session.Slot());
			lobby.Clients.Add(new Session.Client { Slot = "Multi0" });
			var client = new Session.Client { Slot = "Multi9" };
			lobby.Clients.Add(client);

			Assert.That(SkirmishLogic.ValidateClientSlot(lobby, client), Is.True);
			Assert.That(client.IsObserver, Is.True);
		}
	}
}
