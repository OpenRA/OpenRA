#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	[Desc("Attach this to the player actor.")]
	public class DeveloperModeInfo : ITraitInfo, ILobbyOptions
	{
		[Translate]
		[Desc("Descriptive label for the developer mode checkbox in the lobby.")]
		public readonly string CheckboxLabel = "Debug Menu";

		[Translate]
		[Desc("Tooltip description for the developer mode checkbox in the lobby.")]
		public readonly string CheckboxDescription = "Enables cheats and developer commands";

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

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("cheats", CheckboxLabel, CheckboxDescription, CheckboxVisible, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public object Create(ActorInitializer init) { return new DeveloperMode(this); }
	}

	public class DeveloperMode : IResolveOrder, ISync, INotifyCreated, IUnlocksRenderPlayer
	{
		readonly DeveloperModeInfo info;
		public bool Enabled { get; private set; }

		[Sync] bool fastCharge;
		[Sync] bool allTech;
		[Sync] bool fastBuild;
		[Sync] bool disableShroud;
		[Sync] bool pathDebug;
		[Sync] bool unlimitedPower;
		[Sync] bool buildAnywhere;

		public bool FastCharge { get { return Enabled && fastCharge; } }
		public bool AllTech { get { return Enabled && allTech; } }
		public bool FastBuild { get { return Enabled && fastBuild; } }
		public bool DisableShroud { get { return Enabled && disableShroud; } }
		public bool PathDebug { get { return Enabled && pathDebug; } }
		public bool UnlimitedPower { get { return Enabled && unlimitedPower; } }
		public bool BuildAnywhere { get { return Enabled && buildAnywhere; } }

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
				case "DevAll":
				{
					enableAll ^= true;
					allTech = fastCharge = fastBuild = disableShroud = unlimitedPower = buildAnywhere = enableAll;

					if (enableAll)
					{
						self.Owner.Shroud.ExploreAll();

						var amount = order.ExtraData != 0 ? (int)order.ExtraData : info.Cash;
						self.Trait<PlayerResources>().ChangeCash(amount);
					}
					else
						self.Owner.Shroud.ResetExploration();

					self.Owner.Shroud.Disabled = DisableShroud;
					if (self.World.LocalPlayer == self.Owner)
						self.World.RenderPlayer = DisableShroud ? null : self.Owner;

					break;
				}

				case "DevEnableTech":
				{
					allTech ^= true;
					break;
				}

				case "DevFastCharge":
				{
					fastCharge ^= true;
					break;
				}

				case "DevFastBuild":
				{
					fastBuild ^= true;
					break;
				}

				case "DevGiveCash":
				{
					var amount = order.ExtraData != 0 ? (int)order.ExtraData : info.Cash;
					self.Trait<PlayerResources>().ChangeCash(amount);

					debugSuffix = " ({0} credits)".F(amount);
					break;
				}

				case "DevGiveCashAll":
				{
					var amount = order.ExtraData != 0 ? (int)order.ExtraData : info.Cash;
					var receivingPlayers = self.World.Players.Where(p => p.Playable);

					foreach (var player in receivingPlayers)
						player.PlayerActor.Trait<PlayerResources>().ChangeCash(amount);

					debugSuffix = " ({0} credits)".F(amount);
					break;
				}

				case "DevGrowResources":
				{
					foreach (var a in self.World.ActorsWithTrait<ISeedableResource>())
						for (var i = 0; i < info.ResourceGrowth; i++)
							a.Trait.Seed(a.Actor);

					break;
				}

				case "DevVisibility":
				{
					disableShroud ^= true;
					self.Owner.Shroud.Disabled = DisableShroud;
					if (self.World.LocalPlayer == self.Owner)
						self.World.RenderPlayer = DisableShroud ? null : self.Owner;

					break;
				}

				case "DevPathDebug":
				{
					pathDebug ^= true;
					break;
				}

				case "DevGiveExploration":
				{
					self.Owner.Shroud.ExploreAll();
					break;
				}

				case "DevResetExploration":
				{
					self.Owner.Shroud.ResetExploration();
					break;
				}

				case "DevUnlimitedPower":
				{
					unlimitedPower ^= true;
					break;
				}

				case "DevBuildAnywhere":
				{
					buildAnywhere ^= true;
					break;
				}

				case "DevKill":
				{
					if (order.Target.Type != TargetType.Actor)
						break;

					var actor = order.Target.Actor;
					var args = order.TargetString.Split(' ');
					var damageTypes = BitSet<DamageType>.FromStringsNoAlloc(args);

					actor.Kill(actor, damageTypes);
					break;
				}

				case "DevDispose":
				{
					if (order.Target.Type != TargetType.Actor)
						break;

					order.Target.Actor.Dispose();
					break;
				}

				default:
					return;
			}

			Game.Debug("Cheat used: {0} by {1}{2}", order.OrderString, self.Owner.PlayerName, debugSuffix);
		}

		bool IUnlocksRenderPlayer.RenderPlayerUnlocked { get { return Enabled; } }
	}
}
