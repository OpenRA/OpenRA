#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[TraitLocation(SystemActors.World)]
	[Desc("Enables developer cheats via the chatbox. Attach this to the world actor.")]
	public class DevCommandsInfo : TraitInfo<DevCommands> { }

	public class DevCommands : IChatCommand, IWorldLoaded
	{
		[TranslationReference]
		static readonly string CheatsDisabled = "cheats-disabled";

		[TranslationReference]
		static readonly string InvalidCashAmount = "invalid-cash-amount";

		[TranslationReference]
		static readonly string ToggleVisiblityDescription = "toggle-visibility";

		[TranslationReference]
		static readonly string GiveCashDescription = "give-cash";

		[TranslationReference]
		static readonly string GiveCashAllDescription = "give-cash-all";

		[TranslationReference]
		static readonly string InstantBuildingDescription = "instant-building";

		[TranslationReference]
		static readonly string BuildAnywhereDescription = "build-anywhere";

		[TranslationReference]
		static readonly string UnlimitedPowerDescription = "unlimited-power";

		[TranslationReference]
		static readonly string EnableTechDescription = "enable-tech";

		[TranslationReference]
		static readonly string FastChargeDescription = "fast-charge";

		[TranslationReference]
		static readonly string DevCheatAllDescription = "dev-cheat-all";

		[TranslationReference]
		static readonly string DevCrashDescription = "dev-crash";

		[TranslationReference]
		static readonly string LevelUpActorDescription = "levelup-actor";

		[TranslationReference]
		static readonly string PlayerExperienceDescription = "player-experience";

		[TranslationReference]
		static readonly string PowerOutageDescription = "power-outage";

		[TranslationReference]
		static readonly string KillSelectedActorsDescription = "kill-selected-actors";

		[TranslationReference]
		static readonly string DisposeSelectedActorsDescription = "dispose-selected-actors";

		readonly IDictionary<string, (string Description, Action<string, World> Handler)> commandHandlers = new Dictionary<string, (string, Action<string, World>)>
		{
			{ "visibility", (ToggleVisiblityDescription, Visibility) },
			{ "give-cash", (GiveCashDescription, GiveCash) },
			{ "give-cash-all", (GiveCashAllDescription, GiveCashAll) },
			{ "instant-build", (InstantBuildingDescription, InstantBuild) },
			{ "build-anywhere", (BuildAnywhereDescription, BuildAnywhere) },
			{ "unlimited-power", (UnlimitedPowerDescription, UnlimitedPower) },
			{ "enable-tech", (EnableTechDescription, EnableTech) },
			{ "fast-charge", (FastChargeDescription, FastCharge) },
			{ "all", (DevCheatAllDescription, All) },
			{ "crash", (DevCrashDescription, Crash) },
			{ "levelup", (LevelUpActorDescription, LevelUp) },
			{ "player-experience", (PlayerExperienceDescription, PlayerExperience) },
			{ "power-outage", (PowerOutageDescription, PowerOutage) },
			{ "kill", (KillSelectedActorsDescription, Kill) },
			{ "dispose", (DisposeSelectedActorsDescription, Dispose) }
		};

		World world;
		DeveloperMode developerMode;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;

			if (world.LocalPlayer != null)
				developerMode = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();

			var console = world.WorldActor.Trait<ChatCommands>();
			var help = world.WorldActor.Trait<HelpCommand>();

			foreach (var command in commandHandlers)
			{
				console.RegisterCommand(command.Key, this);
				help.RegisterHelp(command.Key, command.Value.Description);
			}
		}

		public void InvokeCommand(string name, string arg)
		{
			if (world.LocalPlayer == null)
				return;

			if (!developerMode.Enabled)
			{
				TextNotificationsManager.Debug(Game.ModData.Translation.GetString(CheatsDisabled));
				return;
			}

			if (commandHandlers.TryGetValue(name, out var command))
				command.Handler(arg, world);
		}

		static void GiveCash(string arg, World world)
		{
			IssueCashDevCommand(world, "DevGiveCash", arg);
		}

		static void GiveCashAll(string arg, World world)
		{
			IssueCashDevCommand(world, "DevGiveCashAll", arg);
		}

		static void IssueCashDevCommand(World world, string command, string arg)
		{
			var giveCashOrder = new Order(command, world.LocalPlayer.PlayerActor, false);

			if (string.IsNullOrEmpty(arg))
				giveCashOrder.ExtraData = 0;
			else if (int.TryParse(arg, out var cash))
				giveCashOrder.ExtraData = (uint)cash;
			else
			{
				TextNotificationsManager.Debug(Game.ModData.Translation.GetString(InvalidCashAmount));
				return;
			}

			world.IssueOrder(giveCashOrder);
		}

		static void Visibility(string arg, World world)
		{
			IssueDevCommand(world, "DevVisibility");
		}

		static void InstantBuild(string arg, World world)
		{
			IssueDevCommand(world, "DevFastBuild");
		}

		static void BuildAnywhere(string arg, World world)
		{
			IssueDevCommand(world, "DevBuildAnywhere");
		}

		static void UnlimitedPower(string arg, World world)
		{
			IssueDevCommand(world, "DevUnlimitedPower");
		}

		static void EnableTech(string arg, World world)
		{
			IssueDevCommand(world, "DevEnableTech");
		}

		static void FastCharge(string arg, World world)
		{
			IssueDevCommand(world, "DevFastCharge");
		}

		static void All(string arg, World world)
		{
			IssueDevCommand(world, "DevAll");
		}

		static void Crash(string arg, World world)
		{
			throw new DevException();
		}

		static void LevelUp(string arg, World world)
		{
			foreach (var actor in world.Selection.Actors)
			{
				if (actor.IsDead)
					continue;

				var leveluporder = new Order("DevLevelUp", actor, false);
				if (int.TryParse(arg, out var level))
					leveluporder.ExtraData = (uint)level;

				if (actor.Info.HasTraitInfo<GainsExperienceInfo>())
					world.IssueOrder(leveluporder);
			}
		}

		static void PlayerExperience(string arg, World world)
		{
			if (!int.TryParse(arg, out var experience))
				return;

			foreach (var player in world.Selection.Actors.Select(a => a.Owner.PlayerActor).Distinct())
				world.IssueOrder(new Order("DevPlayerExperience", player, false) { ExtraData = (uint)experience });
		}

		static void PowerOutage(string arg, World world)
		{
			foreach (var player in world.Selection.Actors.Select(a => a.Owner.PlayerActor).Distinct())
				world.IssueOrder(new Order("PowerOutage", player, false) { ExtraData = 250 });
		}

		static void Kill(string arg, World world)
		{
			foreach (var actor in world.Selection.Actors)
			{
				if (actor.IsDead)
					continue;

				world.IssueOrder(new Order("DevKill", world.LocalPlayer.PlayerActor, Target.FromActor(actor), false) { TargetString = arg });
			}
		}

		static void Dispose(string arg, World world)
		{
			foreach (var actor in world.Selection.Actors)
			{
				if (actor.Disposed)
					continue;

				world.IssueOrder(new Order("DevDispose", world.LocalPlayer.PlayerActor, Target.FromActor(actor), false));
			}
		}

		static void IssueDevCommand(World world, string command)
		{
			world.IssueOrder(new Order(command, world.LocalPlayer.PlayerActor, false));
		}

		[Serializable]
		class DevException : Exception { }
	}
}
