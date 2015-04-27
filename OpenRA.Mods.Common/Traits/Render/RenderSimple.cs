#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
	public class RenderSimpleInfo : RenderSpritesInfo, IRenderActorPreviewSpritesInfo, IQuantizeBodyOrientationInfo, Requires<IBodyOrientationInfo>
	{
		public readonly string Sequence = "idle";

		public override object Create(ActorInitializer init) { return new RenderSimple(init, this); }

		public virtual IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var ifacing = init.Actor.Traits.GetOrDefault<IFacingInfo>();
			var facing = ifacing != null ? init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : ifacing.GetInitialFacing() : 0;

			var anim = new Animation(init.World, image, () => facing);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			yield return new SpriteActorPreview(anim, WVec.Zero, 0, p, rs.Scale);
		}

		public virtual int QuantizedBodyFacings(ActorInfo ai, SequenceProvider sequenceProvider, string race)
		{
			return sequenceProvider.GetSequence(GetImage(ai, sequenceProvider, race), Sequence).Facings;
		}
	}

	public class RenderSimple : RenderSprites, IAutoSelectionSize
	{
		public readonly Animation DefaultAnimation;

		readonly RenderSimpleInfo info;

		public RenderSimple(ActorInitializer init, RenderSimpleInfo info, Func<int> baseFacing)
			: base(init, info)
		{
			this.info = info;

			DefaultAnimation = new Animation(init.World, GetImage(init.Self), baseFacing);
			Add(DefaultAnimation);
		}

		public RenderSimple(ActorInitializer init, RenderSimpleInfo info)
			: this(init, info, MakeFacingFunc(init.Self))
		{
			DefaultAnimation.PlayRepeating(NormalizeSequence(init.Self, info.Sequence));
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
					() => DefaultAnimation.PlayRepeating(NormalizeSequence(self, info.Sequence)));
		}
	}
}
