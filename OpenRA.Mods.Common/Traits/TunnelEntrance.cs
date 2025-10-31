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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides a target for players to issue orders for units to move through a TerrainTunnel.",
		"The host actor should be placed so that the Sensor position overlaps one of the TerrainTunnel portal cells.")]
	public class TunnelEntranceInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("Offset to use as a staging point for actors entering or exiting the tunnel.",
			"Should be at least Margin cells away from the actual entrance.")]
		public readonly CVec RallyPoint = CVec.Zero;

		[Desc("Cell radius to use as a staging area around the RallyPoint.")]
		public readonly int Margin = 2;

		[Desc("Offset to check for the corresponding TerrainTunnel portal cell(s).")]
		public readonly CVec Sensor = CVec.Zero;

		public override object Create(ActorInitializer init) { return new TunnelEntrance(init.Self, this); }
	}

	public class TunnelEntrance : INotifyCreated
	{
		readonly TunnelEntranceInfo info;

		public readonly CPos Entrance;
		public CPos? Exit { get; private set; }
		public int NearEnough => info.Margin;

		public TunnelEntrance(Actor self, TunnelEntranceInfo info)
		{
			this.info = info;

			Entrance = self.Location + info.RallyPoint;
		}

		void INotifyCreated.Created(Actor self)
		{
			// Find the map tunnel associated with this entrance
			var sensor = self.Location + info.Sensor;
			var tunnel = self.World.WorldActor.Info.TraitInfos<TerrainTunnelInfo>()
				.FirstOrDefault(tti => tti.PortalCells().Contains(sensor));

			if (tunnel != null)
			{
				// Find the matching entrance at the other end of the tunnel
				// Run at the end of the tick to make sure that all the entrances exist in the world
				self.World.AddFrameEndTask(w =>
				{
					var portalCells = tunnel.PortalCells().ToList();
					var other = self.World.ActorsWithTrait<TunnelEntrance>()
						.FirstOrDefault(x => x.Actor != self && portalCells.Contains(x.Actor.Location + x.Trait.info.Sensor));

					if (other.Trait != null)
						Exit = other.Trait.Entrance;
				});
			}
		}
	}
}
