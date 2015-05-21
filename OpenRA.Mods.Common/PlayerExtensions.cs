#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common
{
	public static class PlayerExtensions
	{
		public static bool HasNoRequiredUnits(this Player player)
		{
			return !player.World.ActorsWithTrait<MustBeDestroyed>().Any(p =>
			{
				return p.Actor.Owner == player &&
					(player.World.LobbyInfo.GlobalSettings.ShortGame ? p.Trait.Info.RequiredForShortGame : p.Actor.IsInWorld);
			});
		}
	}
}