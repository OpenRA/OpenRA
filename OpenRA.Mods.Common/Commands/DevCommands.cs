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
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[TraitLocation(SystemActors.World)]
	[IncludeStaticFluentReferences(typeof(DevCommands))]
	[Desc("Enables developer cheats via the chatbox. Attach this to the world actor.")]
	public class DevCommandsInfo : TraitInfo<DevCommands> { }

	public class DevCommands : IChatCommand, IWorldLoaded
	{
		[FluentReference]
		const string CheatsDisabled = "notification-cheats-disabled";

		[FluentReference]
		const string InvalidCashAmount = "notification-invalid-cash-amount";

		[FluentReference]
		const string ToggleVisiblityDescription = "description-toggle-visibility";

		[FluentReference]
		const string GiveCashDescription = "description-give-cash";

		[FluentReference]
		const string GiveCashAllDescription = "description-give-cash-all";

		[FluentReference]
		const string InstantBuildingDescription = "description-instant-building";

		[FluentReference]
		const string BuildAnywhereDescription = "description-build-anywhere";

		[FluentReference]
		const string UnlimitedPowerDescription = "description-unlimited-power";

		[FluentReference]
		const string EnableTechDescription = "description-enable-tech";

		[FluentReference]
		const string FastChargeDescription = "description-fast-charge";

		[FluentReference]
		const string DevCheatAllDescription = "description-dev-cheat-all";

		[FluentReference]
		const string DevCrashDescription = "description-dev-crash";

		[FluentReference]
		const string LevelUpActorDescription = "description-levelup-actor";

		[FluentReference]
		const string PlayerExperienceDescription = "description-player-experience";

		[FluentReference]
		const string PowerOutageDescription = "description-power-outage";

		[FluentReference]
		const string GrowResourcesDescription = "description-grow-resources";

		[FluentReference]
		const string GiveExplorationDescription = "description-clear-shroud";

		[FluentReference]
		const string ResetExplorationDescription = "description-reset-shroud";

		[FluentReference]
		const string HealSelectedActorsDescription = "description-heal-selected-actors";

		[FluentReference]
		const string KillSelectedActorsDescription = "description-kill-selected-actors";

		[FluentReference]
		const string DisposeSelectedActorsDescription = "description-dispose-selected-actors";

		public static class Commands
		{
			public const string Visibility = "visibility";
			public const string GiveCash = "give-cash";
			public const string GiveCashAll = "give-cash-all";
			public const string FastBuild = "instant-build";
			public const string BuildAnywhere = "build-anywhere";
			public const string UnlimitedPower = "unlimited-power";
			public const string EnableTech = "enable-tech";
			public const string FastCharge = "fast-charge";
			public const string All = "all";
			public const string Crash = "crash";
			public const string GrowResources = "grow-resources";
			public const string GiveExploration = "clear-shroud";
			public const string ResetExploration = "reset-shroud";
			public const string PlayerExperience = "player-experience";
			public const string Kill = "kill";
			public const string Heal = "heal";
			public const string Dispose = "dispose";
		}

		readonly IDictionary<string, (string Description, Action<string, World> Handler)> commandHandlers = new Dictionary<string, (string, Action<string, World>)>
		{
			{ Commands.Visibility, (ToggleVisiblityDescription, Visibility) },
			{ Commands.GiveCash, (GiveCashDescription, GiveCash) },
			{ Commands.GiveCashAll, (GiveCashAllDescription, GiveCashAll) },
			{ Commands.FastBuild, (InstantBuildingDescription, InstantBuild) },
			{ Commands.BuildAnywhere, (BuildAnywhereDescription, BuildAnywhere) },
			{ Commands.UnlimitedPower, (UnlimitedPowerDescription, UnlimitedPower) },
			{ Commands.EnableTech, (EnableTechDescription, EnableTech) },
			{ Commands.FastCharge, (FastChargeDescription, FastCharge) },
			{ Commands.All, (DevCheatAllDescription, All) },
			{ Commands.Crash, (DevCrashDescription, Crash) },
			{ Commands.GrowResources, (GrowResourcesDescription, GrowResources) },
			{ Commands.GiveExploration, (GiveExplorationDescription, GiveExploration) },
			{ Commands.ResetExploration, (ResetExplorationDescription, ResetExploration) },
			{ GainsExperience.CommandName, (LevelUpActorDescription, LevelUp) },
			{ Commands.PlayerExperience, (PlayerExperienceDescription, PlayerExperience) },
			{ PowerManager.CommandName, (PowerOutageDescription, PowerOutage) },
			{ Commands.Kill, (KillSelectedActorsDescription, Kill) },
			{ Commands.Heal, (HealSelectedActorsDescription, Heal) },
			{ Commands.Dispose, (DisposeSelectedActorsDescription, Dispose) }
		};

		World world;
		DeveloperMode developerMode;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;

			var console = world.WorldActor.Trait<ChatCommands>();
			var help = world.WorldActor.Trait<HelpCommand>();
			developerMode = world.LocalPlayer?.PlayerActor.Trait<DeveloperMode>();

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
				TextNotificationsManager.Debug(FluentProvider.GetMessage(CheatsDisabled));
				return;
			}

			if (commandHandlers.TryGetValue(name, out var command))
				command.Handler(arg, world);
		}

		static void GiveCash(string arg, World world)
		{
			IssueCashDevCommand(world, DeveloperMode.Orders.GiveCash, arg);
		}

		static void GiveCashAll(string arg, World world)
		{
			IssueCashDevCommand(world, DeveloperMode.Orders.GiveCashAll, arg);
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
				TextNotificationsManager.Debug(FluentProvider.GetMessage(InvalidCashAmount));
				return;
			}

			world.IssueOrder(giveCashOrder);
		}

		static void Visibility(string arg, World world)
		{
			IssueDevCommand(world, DeveloperMode.Orders.Visibility);
		}

		static void InstantBuild(string arg, World world)
		{
			IssueDevCommand(world, DeveloperMode.Orders.FastBuild);
		}

		static void BuildAnywhere(string arg, World world)
		{
			IssueDevCommand(world, DeveloperMode.Orders.BuildAnywhere);
		}

		static void UnlimitedPower(string arg, World world)
		{
			IssueDevCommand(world, DeveloperMode.Orders.UnlimitedPower);
		}

		static void EnableTech(string arg, World world)
		{
			IssueDevCommand(world, DeveloperMode.Orders.EnableTech);
		}

		static void FastCharge(string arg, World world)
		{
			IssueDevCommand(world, DeveloperMode.Orders.FastCharge);
		}

		static void All(string arg, World world)
		{
			IssueDevCommand(world, DeveloperMode.Orders.All);
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

				var leveluporder = new Order(GainsExperience.OrderName, actor, false);
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

			world.IssueOrder(new Order(DeveloperMode.Orders.PlayerExperience, world.LocalPlayer.PlayerActor, false) { ExtraData = (uint)experience });
		}

		static void PowerOutage(string arg, World world)
		{
			world.IssueOrder(new Order(PowerManager.OrderName, world.LocalPlayer.PlayerActor, false) { ExtraData = 250 });
		}

		static void Heal(string arg, World world)
		{
			foreach (var actor in world.Selection.Actors)
			{
				if (actor.IsDead)
					continue;

				world.IssueOrder(new Order(DeveloperMode.Orders.Heal, world.LocalPlayer.PlayerActor, Target.FromActor(actor), false));
			}
		}

		static void Kill(string arg, World world)
		{
			foreach (var actor in world.Selection.Actors)
			{
				if (actor.IsDead)
					continue;

				world.IssueOrder(new Order(DeveloperMode.Orders.Kill, world.LocalPlayer.PlayerActor, Target.FromActor(actor), false) { TargetString = arg });
			}
		}

		static void Dispose(string arg, World world)
		{
			foreach (var actor in world.Selection.Actors)
			{
				if (actor.Disposed)
					continue;

				world.IssueOrder(new Order(DeveloperMode.Orders.Dispose, world.LocalPlayer.PlayerActor, Target.FromActor(actor), false));
			}
		}

		static void GrowResources(string arg, World world)
		{
			IssueDevCommand(world, DeveloperMode.Orders.GrowResources);
		}

		static void GiveExploration(string arg, World world)
		{
			IssueDevCommand(world, DeveloperMode.Orders.GiveExploration);
		}

		static void ResetExploration(string arg, World world)
		{
			IssueDevCommand(world, DeveloperMode.Orders.ResetExploration);
		}

		static void IssueDevCommand(World world, string command)
		{
			world.IssueOrder(new Order(command, world.LocalPlayer.PlayerActor, false));
		}

		public class DevException : Exception { }
	}
}
