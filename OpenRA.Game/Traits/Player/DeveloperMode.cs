#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Traits
{
	public class DeveloperModeInfo : ITraitInfo
	{
		public int Cash = 20000;
		public int ResourceGrowth = 100;
		public bool FastBuild;
		public bool FastCharge;
		public bool DisableShroud;
		public bool PathDebug;
		public bool UnlimitedPower;
		public bool BuildAnywhere;
		public bool ShowCombatGeometry;
		public bool ShowDebugGeometry;

		public object Create(ActorInitializer init) { return new DeveloperMode(this); }
	}

	public class DeveloperMode : IResolveOrder, ISync
	{
		DeveloperModeInfo Info;
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

		public DeveloperMode(DeveloperModeInfo info)
		{
			Info = info;
			FastBuild = info.FastBuild;
			FastCharge = info.FastCharge;
			DisableShroud = info.DisableShroud;
			PathDebug = info.PathDebug;
			UnlimitedPower = info.UnlimitedPower;
			BuildAnywhere = info.BuildAnywhere;
			ShowCombatGeometry = info.ShowCombatGeometry;
			ShowDebugGeometry = info.ShowDebugGeometry;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!self.World.AllowDevCommands)
				return;

			switch(order.OrderString)
			{
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
						var amount = order.ExtraData != 0 ? (int)order.ExtraData : Info.Cash;
						self.Trait<PlayerResources>().GiveCash(amount);
						break;
					}

				case "DevGrowResources":
					{
						foreach (var a in self.World.ActorsWithTrait<ISeedableResource>())
						{
							for (var i = 0; i < Info.ResourceGrowth; i++)
								a.Trait.Seed(a.Actor);
						}
						break;
					}

				case "DevShroudDisable":
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
						self.Owner.Shroud.ExploreAll(self.World);
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

			Game.Debug("Cheat used: {0} by {1}".F(order.OrderString, self.Owner.PlayerName));
		}
	}
}
