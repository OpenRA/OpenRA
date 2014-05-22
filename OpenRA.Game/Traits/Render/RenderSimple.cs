#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class RenderSimpleInfo : RenderSpritesInfo, Requires<IBodyOrientationInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderSimple(init.self); }

		public virtual IEnumerable<IRenderable> RenderPreview(World world, ActorInfo ai, PaletteReference pr)
		{
			var anim = new Animation(world, RenderSimple.GetImage(ai), () => 0);
			anim.PlayRepeating("idle");

			return anim.Render(WPos.Zero, WVec.Zero, 0, pr, Scale);
		}
	}

	public class RenderSimple : RenderSprites, IAutoSelectionSize
	{
		RenderSimpleInfo Info;

		public RenderSimple(Actor self, Func<int> baseFacing)
			: base(self)
		{
			Add("", new Animation(self.World, GetImage(self), baseFacing));
			Info = self.Info.Traits.Get<RenderSimpleInfo>();
		}

		public RenderSimple(Actor self)
			: this(self, MakeFacingFunc(self))
		{
			anim.PlayRepeating("idle");
			self.Trait<IBodyOrientation>().SetAutodetectedFacings(anim.CurrentSequence.Facings);
		}

		public int2 SelectionSize(Actor self)
		{
			return anims.Values.Where(b => (b.DisableFunc == null || !b.DisableFunc())
			                                && b.Animation.CurrentSequence != null)
				.Select(a => (a.Animation.Image.size*Info.Scale).ToInt2())
				.FirstOrDefault();
		}

		public string NormalizeSequence(Actor self, string baseSequence)
		{
			return NormalizeSequence(anim, self.GetDamageState(), baseSequence);
		}

		public void PlayCustomAnim(Actor self, string name)
		{
			if (anim.HasSequence(name))
				anim.PlayThen(NormalizeSequence(self, name),
					() => anim.PlayRepeating(NormalizeSequence(self, "idle")));
		}
	}
}
