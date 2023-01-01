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

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckOwners : ILintMapPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			var playerNames = new MapPlayers(map.PlayerDefinitions).Players.Values
				.Select(p => p.Name)
				.ToHashSet();

			// Check for actors that require specific owners
			var actorsWithRequiredOwner = map.Rules.Actors
				.Where(a => a.Value.HasTraitInfo<RequiresSpecificOwnersInfo>())
				.ToDictionary(a => a.Key, a => a.Value.TraitInfo<RequiresSpecificOwnersInfo>());

			foreach (var kv in map.ActorDefinitions)
			{
				var actorReference = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
				var ownerInit = actorReference.GetOrDefault<OwnerInit>();
				if (ownerInit == null)
					emitError($"Actor {kv.Key} is not owned by any player.");
				else
				{
					var ownerName = ownerInit.InternalName;
					if (!playerNames.Contains(ownerName))
						emitError($"Actor {kv.Key} is owned by unknown player {ownerName}.");

					if (actorsWithRequiredOwner.TryGetValue(kv.Value.Value, out var info))
						if (!info.ValidOwnerNames.Contains(ownerName))
							emitError($"Actor {kv.Key} owner {ownerName} is not one of ValidOwnerNames: {info.ValidOwnerNames.JoinWith(", ")}");
				}
			}
		}
	}
}
