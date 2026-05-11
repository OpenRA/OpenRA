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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[IncludeStaticFluentReferences(typeof(DeveloperMode))]
	[Desc("Attach this to the player actor.")]
	public class DeveloperModeInfo : TraitInfo, ILobbyOptions
	{
		[FluentReference]
		[Desc("Descriptive label for the developer mode checkbox in the lobby.")]
		public readonly string CheckboxLabel = "checkbox-debug-menu.label";

		[FluentReference]
		[Desc("Tooltip description for the developer mode checkbox in the lobby.")]
		public readonly string CheckboxDescription = "checkbox-debug-menu.description";

		[Desc("Default value of the developer mode checkbox in the lobby.")]
		public readonly bool CheckboxEnabled = false;

		[Desc("Prevent the developer mode state from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Whether to display the developer mode checkbox in the lobby.")]
		public readonly bool CheckboxVisible = true;

		[Desc("Display order for the developer mode checkbox in the lobby.")]
		public readonly int CheckboxDisplayOrder = 0;

		[Desc("Default cash bonus granted by the give cash cheat.")]
		public readonly int Cash = 20000;

		[Desc("Growth steps triggered by the grow resources button.")]
		public readonly int ResourceGrowth = 100;

		[Desc("Enable the fast build cheat by default.")]
		public readonly bool FastBuild;

		[Desc("Enable the fast support powers cheat by default.")]
		public readonly bool FastCharge;

		[Desc("Enable the disable visibility cheat by default.")]
		public readonly bool DisableShroud;

		[Desc("Enable the unlimited power cheat by default.")]
		public readonly bool UnlimitedPower;

		[Desc("Enable the build anywhere cheat by default.")]
		public readonly bool BuildAnywhere;

		[Desc("Enable the path debug overlay by default.")]
		public readonly bool PathDebug;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption(map, "cheats",
				CheckboxLabel, CheckboxDescription, CheckboxVisible, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public override object Create(ActorInitializer init) { return new DeveloperMode(this); }
	}

	public class DeveloperMode : IResolveOrder, ISync, INotifyCreated, IUnlocksRenderPlayer
	{
		public static class Orders
		{
			public const string All = "DevAll";
			public const string EnableTech = "DevEnableTech";
			public const string FastCharge = "DevFastCharge";
			public const string FastBuild = "DevFastBuild";
			public const string GiveCash = "DevGiveCash";
			public const string GiveCashAll = "DevGiveCashAll";
			public const string GrowResources = "DevGrowResources";
			public const string Visibility = "DevVisibility";
			public const string GiveExploration = "DevGiveExploration";
			public const string ResetExploration = "DevResetExploration";
			public const string UnlimitedPower = "DevUnlimitedPower";
			public const string BuildAnywhere = "DevBuildAnywhere";
			public const string PlayerExperience = "DevPlayerExperience";
			public const string Heal = "DevHeal";
			public const string Kill = "DevKill";
			public const string Dispose = "DevDispose";
		}

		[FluentReference("cheat", "player", "suffix")]
		const string CheatUsed = "notification-cheat-used";

		[FluentReference("cheat", "player")]
		const string CheatEnabled = "notification-cheat-enabled";

		[FluentReference("cheat", "player")]
		const string CheatDisabled = "notification-cheat-disabled";

		readonly DeveloperModeInfo info;
		public bool Enabled { get; private set; }

		[VerifySync]
		bool fastCharge;

		[VerifySync]
		bool allTech;

		[VerifySync]
		bool fastBuild;

		[VerifySync]
		bool disableShroud;

		[VerifySync]
		bool pathDebug;

		[VerifySync]
		bool unlimitedPower;

		[VerifySync]
		bool buildAnywhere;

		public bool FastCharge => Enabled && fastCharge;
		public bool AllTech => Enabled && allTech;
		public bool FastBuild => Enabled && fastBuild;
		public bool DisableShroud => Enabled && disableShroud;
		public bool PathDebug => Enabled && pathDebug;
		public bool UnlimitedPower => Enabled && unlimitedPower;
		public bool BuildAnywhere => Enabled && buildAnywhere;

		bool enableAll;

		public DeveloperMode(DeveloperModeInfo info)
		{
			this.info = info;
			fastBuild = info.FastBuild;
			fastCharge = info.FastCharge;
			disableShroud = info.DisableShroud;
			pathDebug = info.PathDebug;
			unlimitedPower = info.UnlimitedPower;
			buildAnywhere = info.BuildAnywhere;
		}

		void INotifyCreated.Created(Actor self)
		{
			Enabled = self.World.LobbyInfo.NonBotPlayers.Count() == 1 || self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("cheats", info.CheckboxEnabled);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!Enabled)
				return;

			var debugSuffix = "";
			switch (order.OrderString)
			{
				case Orders.All:
				{
					enableAll ^= true;
					allTech = fastCharge = fastBuild = disableShroud = unlimitedPower = buildAnywhere = enableAll;

					if (enableAll)
					{
						var amount = order.ExtraData != 0 ? (int)order.ExtraData : info.Cash;
						self.Trait<PlayerResources>().ChangeCash(amount);
					}

					self.Owner.Shroud.Disabled = DisableShroud;
					if (self.World.LocalPlayer == self.Owner)
						self.World.RenderPlayer = DisableShroud ? null : self.Owner;

					break;
				}

				case Orders.EnableTech:
				{
					allTech ^= true;
					break;
				}

				case Orders.FastCharge:
				{
					fastCharge ^= true;
					break;
				}

				case Orders.FastBuild:
				{
					fastBuild ^= true;
					break;
				}

				case Orders.GiveCash:
				{
					var amount = order.ExtraData != 0 ? (int)order.ExtraData : info.Cash;
					self.Trait<PlayerResources>().ChangeCash(amount);

					debugSuffix = $" ({amount} credits)";
					break;
				}

				case Orders.GiveCashAll:
				{
					var amount = order.ExtraData != 0 ? (int)order.ExtraData : info.Cash;
					var receivingPlayers = self.World.Players.Where(p => p.Playable);

					foreach (var player in receivingPlayers)
						player.PlayerActor.Trait<PlayerResources>().ChangeCash(amount);

					debugSuffix = $" ({amount} credits)";
					break;
				}

				case Orders.GrowResources:
				{
					foreach (var a in self.World.ActorsWithTrait<ISeedableResource>())
						for (var i = 0; i < info.ResourceGrowth; i++)
							a.Trait.Seed(a.Actor);

					break;
				}

				case Orders.Visibility:
				{
					disableShroud ^= true;
					self.Owner.Shroud.Disabled = DisableShroud;
					if (self.World.LocalPlayer == self.Owner)
						self.World.RenderPlayer = DisableShroud ? null : self.Owner;

					break;
				}

				case PathFinderOverlay.OrderName:
				{
					pathDebug ^= true;
					break;
				}

				case Orders.GiveExploration:
				{
					self.Owner.Shroud.ExploreAll();
					break;
				}

				case Orders.ResetExploration:
				{
					self.Owner.Shroud.ResetExploration();
					break;
				}

				case Orders.UnlimitedPower:
				{
					unlimitedPower ^= true;
					break;
				}

				case Orders.BuildAnywhere:
				{
					buildAnywhere ^= true;
					break;
				}

				case Orders.PlayerExperience:
				{
					self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>()?.GiveExperience((int)order.ExtraData);
					break;
				}

				case Orders.Heal:
				{
					if (order.Target.Type != TargetType.Actor)
						break;

					var actor = order.Target.Actor;
					var health = actor.TraitOrDefault<IHealth>();
					health?.InflictDamage(actor, actor, new Damage(-health.MaxHP), true);
					break;
				}

				case Orders.Kill:
				{
					if (order.Target.Type != TargetType.Actor)
						break;

					var actor = order.Target.Actor;
					var args = order.TargetString.Split(' ');
					var damageTypes = BitSet<DamageType>.FromStringsNoAlloc(args);

					actor.Kill(actor, damageTypes);
					break;
				}

				case Orders.Dispose:
				{
					if (order.Target.Type != TargetType.Actor)
						break;

					order.Target.Actor.Dispose();
					break;
				}

				default:
					return;
			}

			var notification = order.OrderString switch
			{
				Orders.All => enableAll ? CheatEnabled : CheatDisabled,
				Orders.EnableTech => allTech ? CheatEnabled : CheatDisabled,
				Orders.FastCharge => fastCharge ? CheatEnabled : CheatDisabled,
				Orders.FastBuild => fastBuild ? CheatEnabled : CheatDisabled,
				Orders.Visibility => disableShroud ? CheatEnabled : CheatDisabled,
				PathFinderOverlay.OrderName => pathDebug ? CheatEnabled : CheatDisabled,
				Orders.UnlimitedPower => unlimitedPower ? CheatEnabled : CheatDisabled,
				Orders.BuildAnywhere => buildAnywhere ? CheatEnabled : CheatDisabled,
				_ => CheatUsed,
			};

			if (notification == CheatUsed)
				TextNotificationsManager.Debug(FluentProvider.GetMessage(CheatUsed,
					"cheat", order.OrderString,
					"player", self.Owner.ResolvedPlayerName,
					"suffix", debugSuffix));
			else
				TextNotificationsManager.Debug(FluentProvider.GetMessage(notification,
					"cheat", order.OrderString,
					"player", self.Owner.ResolvedPlayerName));
		}

		bool IUnlocksRenderPlayer.RenderPlayerUnlocked => Enabled;
	}
}
