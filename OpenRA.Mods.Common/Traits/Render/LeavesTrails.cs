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

using System.Collections.Generic;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public enum TrailType { Cell, CenterPosition }

	[Desc("Renders a sprite effect when leaving a cell.")]
	public class LeavesTrailsInfo : UpgradableTraitInfo
	{
		public readonly string Image = null;

		[SequenceReference("Image")]
		public readonly string[] Sequences = { "idle" };

		[PaletteReference] public readonly string Palette = "effect";

		[Desc("Only do so when the terrain types match with the previous cell.")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();

		[Desc("Accepts values: Cell to draw the trail sprite in the center of the current cell,",
			"CenterPosition to draw the trail sprite at the current position.")]
		public readonly TrailType Type = TrailType.Cell;

		[Desc("Should the trail be visible through fog.")]
		public readonly bool VisibleThroughFog = false;

		[Desc("Display a trail while stationary.")]
		public readonly bool TrailWhileStationary = false;

		[Desc("Delay between trail updates when stationary.")]
		public readonly int StationaryInterval = 0;

		[Desc("Display a trail while moving.")]
		public readonly bool TrailWhileMoving = true;

		[Desc("Delay between trail updates when moving.")]
		public readonly int MovingInterval = 0;

		[Desc("Delay before first trail.")]
		public readonly int StartDelay = 0;

		[Desc("Trail spawn positions relative to actor position. (forward, right, up) triples")]
		public readonly WVec[] Offsets = { WVec.Zero };

		[Desc("Should the trail spawn relative to last position or current position?")]
		public readonly bool SpawnAtLastPosition = true;

		public override object Create(ActorInitializer init) { return new LeavesTrails(init.Self, this); }
	}

	public class LeavesTrails : UpgradableTrait<LeavesTrailsInfo>, ITick, INotifyCreated
	{
		BodyOrientation body;
		IFacing facing;
		int cachedFacing;
		int cachedInterval;

		public LeavesTrails(Actor self, LeavesTrailsInfo info)
			: base(info)
		{
			cachedInterval = Info.StartDelay;
		}

		WPos cachedPosition;
		public void Created(Actor self)
		{
			body = self.Trait<BodyOrientation>();
			facing = self.TraitOrDefault<IFacing>();
			cachedFacing = facing != null ? facing.Facing : 0;
			cachedPosition = self.CenterPosition;
		}

		int ticks;
		int offset;
		bool wasStationary;
		bool isMoving;

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			wasStationary = !isMoving;
			isMoving = self.CenterPosition != cachedPosition;
			if ((isMoving && !Info.TrailWhileMoving) || (!isMoving && !Info.TrailWhileStationary))
				return;

			if (isMoving == wasStationary)
			{
				cachedInterval = Info.StartDelay;
				ticks = 0;
			}

			if (++ticks >= cachedInterval)
			{
				var spawnCell = Info.SpawnAtLastPosition ? self.World.Map.CellContaining(cachedPosition) : self.World.Map.CellContaining(self.CenterPosition);
				var type = self.World.Map.GetTerrainInfo(spawnCell).Type;

				if (++offset >= Info.Offsets.Length)
					offset = 0;

				var offsetRotation = Info.Offsets[offset].Rotate(body.QuantizeOrientation(self, self.Orientation));
				var spawnPosition = Info.SpawnAtLastPosition ? cachedPosition : self.CenterPosition;

				var pos = Info.Type == TrailType.CenterPosition ? spawnPosition + body.LocalToWorld(offsetRotation) :
					self.World.Map.CenterOfCell(spawnCell);

				var spawnFacing = Info.SpawnAtLastPosition ? cachedFacing : (facing != null ? facing.Facing : 0);

				if (Info.TerrainTypes.Contains(type) && !string.IsNullOrEmpty(Info.Image))
					self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, self.World, Info.Image,
						Info.Sequences.Random(Game.CosmeticRandom), Info.Palette, Info.VisibleThroughFog, false, spawnFacing)));

				cachedPosition = self.CenterPosition;
				cachedFacing = facing != null ? facing.Facing : 0;
				ticks = 0;

				cachedInterval = isMoving ? Info.MovingInterval : Info.StationaryInterval;
			}
		}

		protected override void UpgradeEnabled(Actor self)
		{
			cachedPosition = self.CenterPosition;
		}
	}
}
