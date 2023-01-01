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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Placeholder actor used for dead segments and bridge end ramps.")]
	class BridgePlaceholderInfo : TraitInfo
	{
		public readonly string Type = "GroundLevelBridge";

		public readonly DamageState DamageState = DamageState.Undamaged;

		[ActorReference]
		[Desc("Actor type to replace with on repair.")]
		public readonly string ReplaceWithActor = null;

		public readonly CVec[] NeighbourOffsets = Array.Empty<CVec>();

		public override object Create(ActorInitializer init) { return new BridgePlaceholder(init.Self, this); }
	}

	class BridgePlaceholder : IBridgeSegment, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		public readonly BridgePlaceholderInfo Info;
		readonly Actor self;
		readonly BridgeLayer bridgeLayer;

		public BridgePlaceholder(Actor self, BridgePlaceholderInfo info)
		{
			Info = info;
			this.self = self;
			bridgeLayer = self.World.WorldActor.Trait<BridgeLayer>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			bridgeLayer.Add(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			bridgeLayer.Remove(self);
		}

		void IBridgeSegment.Repair(Actor repairer)
		{
			if (Info.ReplaceWithActor == null)
				return;

			self.World.AddFrameEndTask(w =>
			{
				self.Dispose();

				w.CreateActor(Info.ReplaceWithActor, new TypeDictionary
				{
					new LocationInit(self.Location),
					new OwnerInit(self.Owner),
				});
			});
		}

		void IBridgeSegment.Demolish(Actor saboteur, BitSet<DamageType> damageTypes)
		{
			// Do nothing
		}

		string IBridgeSegment.Type => Info.Type;
		DamageState IBridgeSegment.DamageState => Info.DamageState;
		bool IBridgeSegment.Valid => self.IsInWorld;
		CVec[] IBridgeSegment.NeighbourOffsets => Info.NeighbourOffsets;
		CPos IBridgeSegment.Location => self.Location;
	}
}
