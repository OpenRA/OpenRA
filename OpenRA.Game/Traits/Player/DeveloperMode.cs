#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Traits
{
	[Desc("Attach this to the player actor.")]
	public class DeveloperModeInfo : ITraitInfo
	{
		[Desc("Default value of the developer mode checkbox in the lobby.")]
		public bool Enabled = false;

		[Desc("Prevent the developer mode state from being changed in the lobby.")]
		public bool Locked = false;

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

		[Desc("Enable the combat geometry overlay by default.")]
		public bool ShowCombatGeometry;

		[Desc("Enable the debug geometry overlay by default.")]
		public bool ShowDebugGeometry;

		[Desc("Enable the depth buffer overlay by default.")]
		public bool ShowDepthPreview;

		[Desc("Enable the actor tags overlay by default.")]
		public bool ShowActorTags;

		public object Create(ActorInitializer init) { return new DeveloperMode(this); }
	}

	public class DeveloperMode : IResolveOrder, ISync
	{
		DeveloperModeInfo info;
		[Sync] public bool FastCharge;
		[Sync] public bool AllTech;
		[Sync] public bool FastBuild;
		[Sync] public bool DisableShroud;
		[Sync] public bool PathDebug;
		[Sync] public bool UnlimitedPower;
		[Sync] public bool BuildAnywhere;

		// Client side only
		public bool ShowCombatGeometry;
		public bool ShowDebugGeometry;
		public bool ShowDepthPreview;
		public bool ShowActorTags;

		public bool EnableAll;

		public DeveloperMode(DeveloperModeInfo info)
		{
			this.info = info;
			FastBuild = info.FastBuild;
			FastCharge = info.FastCharge;
			DisableShroud = info.DisableShroud;
			PathDebug = info.PathDebug;
			UnlimitedPower = info.UnlimitedPower;
			BuildAnywhere = info.BuildAnywhere;
			ShowCombatGeometry = info.ShowCombatGeometry;
			ShowDebugGeometry = info.ShowDebugGeometry;
			ShowDepthPreview = info.ShowDepthPreview;
			ShowActorTags = info.ShowActorTags;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!self.World.AllowDevCommands)
				return;

			switch (order.OrderString)
			{
				case "DevAll":
					{
						EnableAll ^= true;
						AllTech = FastCharge = FastBuild = DisableShroud = UnlimitedPower = BuildAnywhere = EnableAll;

						if (EnableAll)
						{
							self.Owner.Shroud.ExploreAll();

							var amount = order.ExtraData != 0 ? (int)order.ExtraData : info.Cash;
							self.Trait<PlayerResources>().GiveCash(amount);
						}
						else
						{
							self.Owner.Shroud.ResetExploration();
						}

						self.Owner.Shroud.Disabled = DisableShroud;
						if (self.World.LocalPlayer == self.Owner)
							self.World.RenderPlayer = DisableShroud ? null : self.Owner;

						break;
					}

				case "DevEnableTech":
					{
						AllTech ^= true;
						break;
					}

				case "DevFastCharge":
					{
						FastCharge ^= true;
						break;
					}

				case "DevFastBuild":
					{
						FastBuild ^= true;
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
						{
							for (var i = 0; i < info.ResourceGrowth; i++)
								a.Trait.Seed(a.Actor);
						}

						break;
					}

				case "DevVisibility":
					{
						DisableShroud ^= true;
						self.Owner.Shroud.Disabled = DisableShroud;
						if (self.World.LocalPlayer == self.Owner)
							self.World.RenderPlayer = DisableShroud ? null : self.Owner;
						break;
					}

				case "DevPathDebug":
					{
						PathDebug ^= true;
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
						UnlimitedPower ^= true;
						break;
					}

				case "DevBuildAnywhere":
					{
						BuildAnywhere ^= true;
						break;
					}

				default:
					return;
			}

			Game.Debug("Cheat used: {0} by {1}", order.OrderString, self.Owner.PlayerName);
		}
	}
}
