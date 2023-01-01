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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders crates with both water and land variants.")]
	class WithCrateBodyInfo : TraitInfo, Requires<RenderSpritesInfo>, IRenderActorPreviewSpritesInfo
	{
		[Desc("Easteregg sequences to use in December.")]
		public readonly string[] XmasImages = Array.Empty<string>();

		[Desc("Terrain types on which to display WaterSequence.")]
		public readonly HashSet<string> WaterTerrainTypes = new HashSet<string> { "Water" };

		[SequenceReference]
		public readonly string IdleSequence = "idle";

		[SequenceReference]
		public readonly string WaterSequence = null;

		[SequenceReference]
		public readonly string LandSequence = null;

		public override object Create(ActorInitializer init) { return new WithCrateBody(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			var anim = new Animation(init.World, image);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), IdleSequence));
			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p);
		}
	}

	class WithCrateBody : INotifyParachute, INotifyAddedToWorld
	{
		readonly Actor self;
		readonly Animation anim;
		readonly WithCrateBodyInfo info;

		public WithCrateBody(Actor self, WithCrateBodyInfo info)
		{
			this.self = self;
			this.info = info;

			var rs = self.Trait<RenderSprites>();
			var image = rs.GetImage(self);
			var images = info.XmasImages.Length > 0 && DateTime.Today.Month == 12 ? info.XmasImages : new[] { image };

			anim = new Animation(self.World, images.Random(Game.CosmeticRandom));
			anim.Play(info.IdleSequence);
			rs.Add(anim);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			// Run in a frame end task to give Parachute a chance to set the actor position
			self.World.AddFrameEndTask(w =>
			{
				// Don't change animations while still in air
				if (!self.IsAtGroundLevel())
					return;

				PlaySequence();
			});
		}

		void INotifyParachute.OnParachute(Actor self) { }

		void INotifyParachute.OnLanded(Actor self)
		{
			PlaySequence();
		}

		void PlaySequence()
		{
			var onWater = info.WaterTerrainTypes.Contains(self.World.Map.GetTerrainInfo(self.Location).Type);
			var sequence = onWater ? info.WaterSequence : info.LandSequence;
			if (!string.IsNullOrEmpty(sequence))
				anim.PlayRepeating(sequence);
		}
	}
}
