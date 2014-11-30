#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class RenderSimpleInfo : RenderSpritesInfo, IRenderActorPreviewSpritesInfo, IQuantizeBodyOrientationInfo, ILegacyEditorRenderInfo, Requires<IBodyOrientationInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderSimple(init.self); }

		public virtual IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var ifacing = init.Actor.Traits.GetOrDefault<IFacingInfo>();
			var facing = ifacing != null ? init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : ifacing.GetInitialFacing() : 0;

			var anim = new Animation(init.World, image, () => facing);
			anim.PlayRepeating("idle");
			yield return new SpriteActorPreview(anim, WVec.Zero, 0, p, rs.Scale);
		}

		public virtual int QuantizedBodyFacings(SequenceProvider sequenceProvider, ActorInfo ai)
		{
			return sequenceProvider.GetSequence(RenderSprites.GetImage(ai), "idle").Facings;
		}

		public string EditorPalette { get { return Palette; } }
		public string EditorImage(ActorInfo actor) { return RenderSimple.GetImage(actor); }
	}

	public class RenderSimple : RenderSprites, IAutoSelectionSize
	{
		public readonly Animation DefaultAnimation;

		public RenderSimple(Actor self, Func<int> baseFacing)
			: base(self)
		{
			DefaultAnimation = new Animation(self.World, GetImage(self), baseFacing);
			Add("", DefaultAnimation);
		}

		public RenderSimple(Actor self)
			: this(self, MakeFacingFunc(self))
		{
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, "idle"));
		}

		public int2 SelectionSize(Actor self) { return AutoSelectionSize(self); }

		public string NormalizeSequence(Actor self, string sequence)
		{
			return NormalizeSequence(DefaultAnimation, self.GetDamageState(), sequence);
		}

		public void PlayCustomAnim(Actor self, string name)
		{
			if (DefaultAnimation.HasSequence(name))
				DefaultAnimation.PlayThen(NormalizeSequence(self, name),
					() => DefaultAnimation.PlayRepeating(NormalizeSequence(self, "idle")));
		}
	}
}
