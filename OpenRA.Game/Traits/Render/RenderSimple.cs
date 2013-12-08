﻿#region Copyright & License Information
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

		public virtual IEnumerable<IRenderable> RenderPreview(ActorInfo ai, PaletteReference pr)
		{
			var anim = new Animation(RenderSimple.GetImage(ai), () => 0);
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
			Anims.Add("", new Animation(GetImage(self), baseFacing));
			Info = self.Info.Traits.Get<RenderSimpleInfo>();
		}

		public RenderSimple(Actor self)
			: this(self, MakeFacingFunc(self))
		{
			Anim.PlayRepeating("idle");
			self.Trait<IBodyOrientation>().SetAutodetectedFacings(Anim.CurrentSequence.Facings);
		}

		public int2 SelectionSize(Actor self)
		{
			return Anims.Values.Where(b => (b.DisableFunc == null || !b.DisableFunc())
			                                && b.Animation.CurrentSequence != null)
				.Select(a => (a.Animation.Image.size*Info.Scale).ToInt2())
				.FirstOrDefault();
		}

		public string NormalizeSequence(Actor self, string baseSequence)
		{
			return NormalizeSequence(Anim, self.GetDamageState(), baseSequence);
		}

		public void PlayCustomAnim(Actor self, string name)
		{
			if (Anim.HasSequence(name))
				Anim.PlayThen(NormalizeSequence(self, name),
					() => Anim.PlayRepeating(NormalizeSequence(self, "idle")));
		}
	}
}
