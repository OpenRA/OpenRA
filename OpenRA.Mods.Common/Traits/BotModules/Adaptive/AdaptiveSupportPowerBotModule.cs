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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Adaptive AI support power usage scaled by lobby settings.")]
	public sealed class AdaptiveSupportPowerBotModuleInfo : SupportPowerBotModuleInfo
	{
		public override object Create(ActorInitializer init) { return new AdaptiveSupportPowerBotModule(init.Self, this); }
	}

	public sealed class AdaptiveSupportPowerBotModule : SupportPowerBotModule
	{
		AdaptiveAILobbySettings lobbySettings;

		public AdaptiveSupportPowerBotModule(Actor self, AdaptiveSupportPowerBotModuleInfo info)
			: base(self, info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);
			lobbySettings = self.World.WorldActor.TraitOrDefault<AdaptiveAILobbySettings>();
		}

		protected override int MinimumAttractiveness(SupportPowerDecision powerDecision)
		{
			var multiplier = lobbySettings?.SupportPowerMultiplier ?? 1f;
			if (multiplier <= 0)
				return int.MaxValue;

			return (int)(powerDecision.MinimumAttractiveness / multiplier);
		}
	}
}
