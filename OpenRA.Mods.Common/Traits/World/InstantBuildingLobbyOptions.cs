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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Lobby options for instant production/build times.")]
	public sealed class InstantBuildingLobbyOptionsInfo : TraitInfo, ILobbyOptions
	{
		[FluentReference]
		[Desc("Descriptive label for the instant building checkbox in the lobby.")]
		public readonly string CheckboxLabel = "checkbox-instant-building.label";

		[FluentReference]
		[Desc("Tooltip description for the instant building checkbox in the lobby.")]
		public readonly string CheckboxDescription = "checkbox-instant-building.description";

		[Desc("Default value of the instant building checkbox in the lobby.")]
		public readonly bool CheckboxEnabled = false;

		[Desc("Prevent the instant building state from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Whether to display the instant building checkbox in the lobby.")]
		public readonly bool CheckboxVisible = true;

		[Desc("Display order for the instant building checkbox in the lobby.")]
		public readonly int CheckboxDisplayOrder = 12;

		public override object Create(ActorInitializer init) { return new InstantBuilding(init, this); }

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption(map, InstantBuilding.MainOptionId, CheckboxLabel, CheckboxDescription,
				CheckboxVisible, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);

			foreach (var category in InstantBuildingCategory.All)
				yield return new LobbyBooleanOption(map, InstantBuilding.SubOptionId(category.Id), category.Label, category.Description,
					CheckboxVisible, category.DisplayOrder, category.DefaultEnabled, CheckboxLocked);
		}
	}

	public sealed class InstantBuilding : INotifyCreated
	{
		public const string MainOptionId = "instant-building";

		public static readonly FrozenSet<string> SubOptionIds = InstantBuildingCategory.All
			.Select(c => SubOptionId(c.Id))
			.ToFrozenSet();

		readonly InstantBuildingLobbyOptionsInfo info;
		readonly Dictionary<string, bool> categoryEnabled = [];

		bool enabled;

		public InstantBuilding(ActorInitializer init, InstantBuildingLobbyOptionsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var gs = self.World.LobbyInfo?.GlobalSettings;
			if (gs == null)
				return;

			enabled = gs.OptionOrDefault(MainOptionId, info.CheckboxEnabled);
			foreach (var category in InstantBuildingCategory.All)
				categoryEnabled[category.Id] = gs.OptionOrDefault(SubOptionId(category.Id), category.DefaultEnabled);
		}

		public bool IsEnabled(string queueType)
		{
			if (!enabled)
				return false;

			var category = CategorizeQueueType(queueType);
			return category != null && categoryEnabled.GetValueOrDefault(category, true);
		}

		public bool IsBuildingsEnabled() => IsEnabled("Building");

		public static string SubOptionId(string category) => $"{MainOptionId}-{category}";

		public static string CategorizeQueueType(string queueType)
		{
			if (queueType.StartsWith("Building", StringComparison.OrdinalIgnoreCase))
				return "buildings";

			if (queueType.Equals("Defense", StringComparison.OrdinalIgnoreCase)
				|| queueType.StartsWith("Support", StringComparison.OrdinalIgnoreCase))
				return "defense";

			if (queueType.StartsWith("Infantry", StringComparison.OrdinalIgnoreCase))
				return "infantry";

			if (queueType.StartsWith("Vehicle", StringComparison.OrdinalIgnoreCase)
				|| queueType.Equals("Armor", StringComparison.OrdinalIgnoreCase))
				return "vehicles";

			if (queueType.StartsWith("Aircraft", StringComparison.OrdinalIgnoreCase)
				|| queueType.Equals("Air", StringComparison.OrdinalIgnoreCase)
				|| queueType.Equals("Plane", StringComparison.OrdinalIgnoreCase))
				return "aircraft";

			if (queueType.Equals("Ship", StringComparison.OrdinalIgnoreCase)
				|| queueType.Equals("Naval", StringComparison.OrdinalIgnoreCase))
				return "naval";

			return null;
		}

	}

	readonly record struct InstantBuildingCategory(string Id, string Label, string Description, int DisplayOrder, bool DefaultEnabled = true)
	{
		public static readonly InstantBuildingCategory[] All =
		[
			new("buildings", "checkbox-instant-building-buildings.label", "checkbox-instant-building-buildings.description", 13),
			new("defense", "checkbox-instant-building-defense.label", "checkbox-instant-building-defense.description", 14),
			new("infantry", "checkbox-instant-building-infantry.label", "checkbox-instant-building-infantry.description", 15),
			new("vehicles", "checkbox-instant-building-vehicles.label", "checkbox-instant-building-vehicles.description", 16),
			new("aircraft", "checkbox-instant-building-aircraft.label", "checkbox-instant-building-aircraft.description", 17),
			new("naval", "checkbox-instant-building-naval.label", "checkbox-instant-building-naval.description", 18),
		];
	}
}
