#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the player actor.")]
	public class DeveloperModeInfo : ITraitInfo, ILobbyOptions
	{
		[Desc("Default value of the developer mode checkbox in the lobby.")]
		public bool Enabled = false;

		[Desc("Prevent the developer mode state from being changed in the lobby.")]
		public bool Locked = false;

		[Desc("Whether to display the developer mode checkbox in the lobby.")]
		public bool Visible = true;

		[Desc("Display order for the developer mode checkbox in the lobby.")]
		public int DisplayOrder = 0;

		[Desc("Default cash bonus granted by the give cash cheat.")]
		public int Cash = 20000;

		[Desc("Growth steps triggered by the grow resources button.")]
		public int ResourceGrowth = 100;

		[Desc("Enable the fast build cheat by default.")]
		public bool FastBuild;

		[Desc("Enable the fast support powers cheat by default.")]
		public bool FastCharge;

		[Desc("Enable the disable visibility cheat by default.")]
		public bool DisableShroud;

		[Desc("Enable the unlimited power cheat by default.")]
		public bool UnlimitedPower;

		[Desc("Enable the build anywhere cheat by default.")]
		public bool BuildAnywhere;

		[Desc("Enable the path debug overlay by default.")]
		public bool PathDebug;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("cheats", "Debug Menu",
				"Enables cheats and developer commands",
				Visible, DisplayOrder, Enabled, Locked);
		}

		public object Create(ActorInitializer init) { return new DeveloperMode(this); }
	}

	public class DeveloperMode : IResolveOrder, ISync, INotifyCreated
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
				.OptionOrDefault("cheats", info.Enabled);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!Enabled)
				return;

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
						self.Trait<PlayerResources>().GiveCash(amount);
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
					self.Trait<PlayerResources>().GiveCash(amount);
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

				default:
					return;
			}

			Game.Debug("Cheat used: {0} by {1}", order.OrderString, self.Owner.PlayerName);
		}
	}
}
