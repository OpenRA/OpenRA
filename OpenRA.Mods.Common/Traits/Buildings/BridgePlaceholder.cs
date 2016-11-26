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

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Placeholder actor used for dead segments and bridge end ramps.")]
	class BridgePlaceholderInfo : ITraitInfo
	{
		public readonly string Type = "GroundLevelBridge";

		public readonly DamageState DamageState = DamageState.Undamaged;

		[Desc("Actor type to replace with on repair.")]
		[ActorReference] public readonly string ReplaceWithActor = null;

		public readonly CVec[] NeighbourOffsets = { };

		public object Create(ActorInitializer init) { return new BridgePlaceholder(init.Self, this); }
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

		void IBridgeSegment.Demolish(Actor saboteur)
		{
			// Do nothing
		}

		string IBridgeSegment.Type { get { return Info.Type; } }
		DamageState IBridgeSegment.DamageState { get { return Info.DamageState; } }
		bool IBridgeSegment.Valid { get { return self.IsInWorld; } }
		CVec[] IBridgeSegment.NeighbourOffsets { get { return Info.NeighbourOffsets; } }
		CPos IBridgeSegment.Location { get { return self.Location; } }
	}
}