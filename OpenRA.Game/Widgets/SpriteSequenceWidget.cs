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
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class SpriteSequenceWidget : SpriteWidget
	{
		public string Unit = null;
		public string Sequence = null;
		public int Frame = 0;
		public int Facing = 0;

		public Func<Animation> GetAnimation;
		public Func<int> GetFacing;

		[ObjectCreator.UseCtor]
		public SpriteSequenceWidget(WorldRenderer worldRenderer)
			: base(worldRenderer)
		{
			GetAnimation = () => null;
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			if (Unit != null && Sequence != null)
			{
				var anim = new Animation(Unit, () => Facing);
				anim.PlayFetchIndex(Sequence, () => Frame);
				GetAnimation = () => anim;
			}

			GetSprite = () =>
			{
				var anim = GetAnimation();
				return anim != null ? anim.Image : null;
			};
		}

		protected SpriteSequenceWidget(SpriteSequenceWidget other)
			: base(other)
		{
			Unit = other.Unit;
			Sequence = other.Sequence;
			Frame = other.Frame;
			Facing = other.Facing;

			GetAnimation = other.GetAnimation;
			GetFacing = other.GetFacing;
		}

		public override Widget Clone() { return new SpriteSequenceWidget(this); }
	}
}
