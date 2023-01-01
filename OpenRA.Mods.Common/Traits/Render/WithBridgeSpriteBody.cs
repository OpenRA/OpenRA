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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	class WithBridgeSpriteBodyInfo : WithSpriteBodyInfo
	{
		public readonly string Type = "GroundLevelBridge";

		[Desc("Offset to search for the 'A' neighbour")]
		public readonly CVec AOffset = CVec.Zero;

		[Desc("Position to search for the 'B' neighbour")]
		public readonly CVec BOffset = CVec.Zero;

		[SequenceReference]
		[Desc("Sequences to use when both neighbours are alive.")]
		public readonly string[] Sequences = { "idle" };

		[SequenceReference]
		public readonly string[] ADestroyedSequences = { "adestroyed" };

		[SequenceReference]
		public readonly string[] BDestroyedSequences = { "bdestroyed" };

		[SequenceReference]
		public readonly string[] ABDestroyedSequences = { "abdestroyed" };

		public override object Create(ActorInitializer init) { return new WithBridgeSpriteBody(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var anim = new Animation(init.World, image);
			anim.PlayFetchIndex(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequences.First()), () => 0);

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p);
		}
	}

	class WithBridgeSpriteBody : WithSpriteBody, INotifyRemovedFromWorld
	{
		readonly WithBridgeSpriteBodyInfo bridgeInfo;
		readonly BridgeLayer bridgeLayer;
		readonly Actor self;

		public WithBridgeSpriteBody(ActorInitializer init, WithBridgeSpriteBodyInfo info)
			: base(init, info)
		{
			self = init.Self;
			bridgeInfo = info;
			bridgeLayer = init.World.WorldActor.Trait<BridgeLayer>();
		}

		protected override void TraitEnabled(Actor self)
		{
			base.TraitEnabled(self);

			if (bridgeInfo.AOffset != CVec.Zero)
				UpdateNeighbour(bridgeInfo.AOffset);

			if (bridgeInfo.BOffset != CVec.Zero)
				UpdateNeighbour(bridgeInfo.BOffset);

			SetDirty();
		}

		void UpdateNeighbour(CVec offset)
		{
			var neighbour = bridgeLayer[self.Location + offset];
			if (neighbour == null)
				return;

			var body = neighbour.TraitOrDefault<WithBridgeSpriteBody>();
			if (body != null && body.bridgeInfo.Type == bridgeInfo.Type)
				body.SetDirty();
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			UpdateNeighbour(bridgeInfo.AOffset);
			UpdateNeighbour(bridgeInfo.BOffset);
		}

		void SetDirty()
		{
			self.World.AddFrameEndTask(w =>
			{
				var aDestroyed = bridgeInfo.AOffset != CVec.Zero && NeighbourIsDestroyed(bridgeInfo.AOffset);
				var bDestroyed = bridgeInfo.BOffset != CVec.Zero && NeighbourIsDestroyed(bridgeInfo.BOffset);

				var sequence = DefaultAnimation.CurrentSequence.Name;
				if (aDestroyed && bDestroyed && bridgeInfo.ABDestroyedSequences.Length > 0)
					sequence = bridgeInfo.ABDestroyedSequences.Random(Game.CosmeticRandom);
				else if (aDestroyed && bridgeInfo.ADestroyedSequences.Length > 0)
					sequence = bridgeInfo.ADestroyedSequences.Random(Game.CosmeticRandom);
				else if (bDestroyed && bridgeInfo.BDestroyedSequences.Length > 0)
					sequence = bridgeInfo.BDestroyedSequences.Random(Game.CosmeticRandom);
				else
					sequence = bridgeInfo.Sequences.Random(Game.CosmeticRandom);

				DefaultAnimation.PlayRepeating(NormalizeSequence(self, sequence));
			});
		}

		bool NeighbourIsDestroyed(CVec offset)
		{
			var neighbour = bridgeLayer[self.Location + offset];
			if (neighbour == null)
				return false;

			var segment = neighbour.TraitOrDefault<IBridgeSegment>();
			if (segment == null)
				return false;

			return segment.DamageState == DamageState.Dead;
		}
	}
}
