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
			DefaultAnimation.PlayRepeating("idle");
			self.Trait<IBodyOrientation>().SetAutodetectedFacings(DefaultAnimation.CurrentSequence.Facings);
		}

		public int2 SelectionSize(Actor self) { return AutoSelectionSize(self); }

		public string NormalizeSequence(Actor self, string baseSequence)
		{
			return NormalizeSequence(DefaultAnimation, self.GetDamageState(), baseSequence);
		}

		public void PlayCustomAnim(Actor self, string name)
		{
			if (DefaultAnimation.HasSequence(name))
				DefaultAnimation.PlayThen(NormalizeSequence(self, name),
					() => DefaultAnimation.PlayRepeating(NormalizeSequence(self, "idle")));
		}
	}
}
