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

using System.Collections.Generic;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Controls the attack range circles lobby option and hotkey state. Attach this to the world actor.")]
	public class AttackRangeCirclesOptionsInfo : TraitInfo, ILobbyOptions
	{
		[FluentReference]
		[Desc("Descriptive label for the attack range circles checkbox in the lobby.")]
		public readonly string CheckboxLabel = "checkbox-attack-range-circles.label";

		[FluentReference]
		[Desc("Tooltip description for the attack range circles checkbox in the lobby.")]
		public readonly string CheckboxDescription = "checkbox-attack-range-circles.description";

		[Desc("Default value of the attack range circles checkbox in the lobby.")]
		public readonly bool CheckboxEnabled = false;

		[Desc("Prevent the attack range circles state from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Whether to display the attack range circles checkbox in the lobby.")]
		public readonly bool CheckboxVisible = true;

		[Desc("Display order for the attack range circles checkbox in the lobby.")]
		public readonly int CheckboxDisplayOrder = 11;

		[Desc("Color of the range circle.")]
		public readonly Color CircleColor = Color.FromArgb(128, Color.Yellow);

		[Desc("Range circle line width.")]
		public readonly float CircleWidth = 1;

		[Desc("Color of the range circle border.")]
		public readonly Color CircleBorderColor = Color.FromArgb(96, Color.Black);

		[Desc("Range circle border width.")]
		public readonly float CircleBorderWidth = 3;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption(map, "attackrangecircles",
				CheckboxLabel, CheckboxDescription,
				CheckboxVisible, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public override object Create(ActorInitializer init) { return new AttackRangeCirclesOptions(this); }
	}

	public class AttackRangeCirclesOptions : INotifyCreated
	{
		readonly AttackRangeCirclesOptionsInfo info;
		World world;

		public bool FeatureEnabled { get; private set; }
		public bool HotkeyHeld { get; set; }

		public Color CircleColor => info.CircleColor;
		public float CircleWidth => info.CircleWidth;
		public Color CircleBorderColor => info.CircleBorderColor;
		public float CircleBorderWidth => info.CircleBorderWidth;

		public bool ShouldShowCircles => HotkeyHeld && (FeatureEnabled || world.IsReplay);

		public AttackRangeCirclesOptions(AttackRangeCirclesOptionsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			world = self.World;
			FeatureEnabled = world.LobbyInfo.GlobalSettings
				.OptionOrDefault("attackrangecircles", info.CheckboxEnabled);
		}
	}
}
