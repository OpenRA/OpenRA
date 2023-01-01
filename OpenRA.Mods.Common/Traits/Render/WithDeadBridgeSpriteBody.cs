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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	class WithDeadBridgeSpriteBodyInfo : WithSpriteBodyInfo
	{
		[ActorReference]
		public readonly string[] RampActors = Array.Empty<string>();

		[Desc("Offset to search for the 'A' neighbour")]
		public readonly CVec AOffset = CVec.Zero;

		[Desc("Position to search for the 'B' neighbour")]
		public readonly CVec BOffset = CVec.Zero;

		[SequenceReference]
		public readonly string[] ARampSequences = { "aramp" };

		[SequenceReference]
		public readonly string[] BRampSequences = { "bramp" };

		[SequenceReference]
		public readonly string[] ABRampSequences = { "abramp" };

		[SequenceReference]
		[Desc("Placeholder sequence to use in the map editor.")]
		public readonly string EditorSequence = "editor";

		[PaletteReference]
		[Desc("Palette to use for the editor placeholder.")]
		public readonly string EditorPalette = "terrainalpha";

		public override object Create(ActorInitializer init) { return new WithDeadBridgeSpriteBody(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var anim = new Animation(init.World, image);
			var sequence = init.World.Type == WorldType.Editor ? EditorSequence : Sequence;
			var palette = init.World.Type == WorldType.Editor ? init.WorldRenderer.Palette(EditorPalette) : p;
			anim.PlayFetchIndex(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), sequence), () => 0);
			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, palette);
		}
	}

	class WithDeadBridgeSpriteBody : WithSpriteBody
	{
		readonly WithDeadBridgeSpriteBodyInfo bridgeInfo;
		readonly BridgeLayer bridgeLayer;

		public WithDeadBridgeSpriteBody(ActorInitializer init, WithDeadBridgeSpriteBodyInfo info)
			: base(init, info)
		{
			bridgeInfo = info;
			bridgeLayer = init.World.WorldActor.Trait<BridgeLayer>();
		}

		bool RampExists(Actor self, CVec offset)
		{
			var neighbour = bridgeLayer[self.Location + offset];
			if (neighbour == null)
				return false;

			return bridgeInfo.RampActors.Contains(neighbour.Info.Name);
		}

		protected override void TraitEnabled(Actor self)
		{
			base.TraitEnabled(self);

			self.World.AddFrameEndTask(w =>
			{
				var aRamp = bridgeInfo.AOffset != CVec.Zero && RampExists(self, bridgeInfo.AOffset);
				var bRamp = bridgeInfo.BOffset != CVec.Zero && RampExists(self, bridgeInfo.BOffset);

				var sequence = DefaultAnimation.CurrentSequence.Name;
				if (aRamp && bRamp && bridgeInfo.ABRampSequences.Length > 0)
					sequence = bridgeInfo.ABRampSequences.Random(Game.CosmeticRandom);
				else if (aRamp && bridgeInfo.ARampSequences.Length > 0)
					sequence = bridgeInfo.ARampSequences.Random(Game.CosmeticRandom);
				else if (bRamp && bridgeInfo.BRampSequences.Length > 0)
					sequence = bridgeInfo.BRampSequences.Random(Game.CosmeticRandom);

				DefaultAnimation.PlayRepeating(NormalizeSequence(self, sequence));
			});
		}
	}
}
