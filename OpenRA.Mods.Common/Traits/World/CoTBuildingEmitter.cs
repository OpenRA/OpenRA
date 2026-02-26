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
	[Desc("Emits Cursor-on-Target (CoT) messages for building lifecycle events (placed, damage state changed, destroyed) and periodic heartbeats.")]
	public sealed class CoTBuildingEmitterInfo : CoTEmitterInfoBase
	{
		public CoTBuildingEmitterInfo()
		{
			Callsign = "OpenRA-Building";
			Ce = 50.0;
			Le = 50.0;
			UpdateIntervalTicks = 150;
			MaxIntervalTicks = 1500;
		}

		public override object Create(ActorInitializer init) { return new CoTBuildingEmitter(init, this); }
	}

	public sealed class CoTBuildingEmitter : CoTEmitterBase<CoTBuildingEmitterInfo>
	{
		readonly bool createdByPlacement;
		bool placedNotified;

		protected override CoTDomain Domain => CoTDomain.Building;

		public CoTBuildingEmitter(ActorInitializer init, CoTBuildingEmitterInfo info)
			: base(init, info)
		{
			createdByPlacement = init.Contains<PlaceBuildingInit>(info);
		}

		protected override void OnAddedToWorld(Actor self)
		{
			if (createdByPlacement && !placedNotified && !IsTraitDisabled && !IsTraitPaused)
			{
				if (!TryGetLatLon(self, out var lat, out var lon))
					return;
				SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), Info.StaleSeconds, "placed");
				placedNotified = true;
			}
		}
	}
}
