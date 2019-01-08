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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	class WithGateSpriteBodyInfo : WithSpriteBodyInfo, IWallConnectorInfo, Requires<GateInfo>
	{
		[Desc("Cells (outside the gate footprint) that contain wall cells that can connect to the gate")]
		public readonly CVec[] WallConnections = { };

		[Desc("Wall type for connections")]
		public readonly string Type = "wall";

		[Desc("Override sequence to use when fully open.")]
		public readonly string OpenSequence = null;

		public override object Create(ActorInitializer init) { return new WithGateSpriteBody(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var anim = new Animation(init.World, image);
			anim.PlayFetchIndex(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence), () => 0);

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p, rs.Scale);
		}

		string IWallConnectorInfo.GetWallConnectionType()
		{
			return Type;
		}
	}

	class WithGateSpriteBody : WithSpriteBody, INotifyRemovedFromWorld, IWallConnector, ITick
	{
		readonly WithGateSpriteBodyInfo gateBodyInfo;
		readonly Gate gate;
		bool renderOpen;

		public WithGateSpriteBody(ActorInitializer init, WithGateSpriteBodyInfo info)
			: base(init, info, () => 0)
		{
			gateBodyInfo = info;
			gate = init.Self.Trait<Gate>();
		}

		void UpdateState(Actor self)
		{
			if (renderOpen || IsTraitPaused)
				DefaultAnimation.PlayRepeating(NormalizeSequence(self, gateBodyInfo.OpenSequence));
			else
				DefaultAnimation.PlayFetchIndex(NormalizeSequence(self, Info.Sequence), GetGateFrame);
		}

		void ITick.Tick(Actor self)
		{
			if (gateBodyInfo.OpenSequence == null)
				return;

			if (gate.Position == gate.OpenPosition ^ renderOpen)
			{
				renderOpen = gate.Position == gate.OpenPosition;
				UpdateState(self);
			}
		}

		int GetGateFrame()
		{
			return int2.Lerp(0, DefaultAnimation.CurrentSequence.Length - 1, gate.Position, gate.OpenPosition);
		}

		protected override void DamageStateChanged(Actor self)
		{
			UpdateState(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			base.TraitEnabled(self);

			UpdateState(self);
			UpdateNeighbours(self);
		}

		void UpdateNeighbours(Actor self)
		{
			var footprint = gate.Footprint.ToArray();
			var adjacent = Util.ExpandFootprint(footprint, true).Except(footprint)
				.Where(self.World.Map.Contains).ToList();

			var adjacentActorTraits = adjacent.SelectMany(self.World.ActorMap.GetActorsAt)
				.SelectMany(a => a.TraitsImplementing<IWallConnector>());

			foreach (var rb in adjacentActorTraits)
				rb.SetDirty();
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			UpdateNeighbours(self);
		}

		bool IWallConnector.AdjacentWallCanConnect(Actor self, CPos wallLocation, string wallType, out CVec facing)
		{
			facing = wallLocation - self.Location;
			return wallType == gateBodyInfo.Type && gateBodyInfo.WallConnections.Contains(facing);
		}

		void IWallConnector.SetDirty() { }
	}
}
