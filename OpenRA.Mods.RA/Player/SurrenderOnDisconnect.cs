#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SurrenderOnDisconnectInfo : TraitInfo<SurrenderOnDisconnect>
	{

	}

	class SurrenderOnDisconnect : ITick
	{
		private bool Disconnected = false;

		public void Tick(Actor self)
		{
			if (Disconnected) return;

			var p = self.Owner;

			if (p.WinState == WinState.Lost || p.WinState == WinState.Won) return; /* already won or lost */

			var client = p.World.LobbyInfo.ClientWithIndex(p.ClientIndex);
			if (client == null)
				return;

			if (client.State == Session.ClientState.Disconnected)
			{
				Disconnected = true; /* dont call this multiple times! */
				self.World.IssueOrder(new Order("Surrender", self));
			}
		}
	}
}
