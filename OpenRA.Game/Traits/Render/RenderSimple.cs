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
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class RenderSimpleInfo : RenderSpritesInfo, IBodyOrientationInfo
	{
		[Desc("Number of facings for gameplay calculations. -1 indiciates auto-detection from sequence")]
		public readonly int QuantizedFacings = -1;

		[Desc("Camera pitch the sprite was rendered with. Used to determine rotation ellipses")]
		public readonly WAngle CameraPitch = WAngle.FromDegrees(40);
		public override object Create(ActorInitializer init) { return new RenderSimple(init.self); }

		public virtual IEnumerable<IRenderable> RenderPreview(ActorInfo ai, PaletteReference pr)
		{
			var anim = new Animation(RenderSimple.GetImage(ai), () => 0);
			anim.PlayRepeating("idle");

			yield return new SpriteRenderable(anim.Image, WPos.Zero, 0, pr, 1f);
		}
	}

	public class RenderSimple : RenderSprites, IBodyOrientation, IAutoSelectionSize
	{
		RenderSimpleInfo Info;

		public RenderSimple(Actor self, Func<int> baseFacing)
			: base(self, baseFacing)
		{
			anims.Add("", new Animation(GetImage(self), baseFacing));
			Info = self.Info.Traits.Get<RenderSimpleInfo>();
		}

		public RenderSimple(Actor self)
			: this(self, MakeFacingFunc(self))
		{
			anim.PlayRepeating("idle");
		}

		public int2 SelectionSize(Actor self)
		{
			return anims.Values.Where(b => (b.DisableFunc == null || !b.DisableFunc())
			                                && b.Animation.CurrentSequence != null)
				.Select(a => (a.Animation.Image.size*Info.Scale).ToInt2())
				.FirstOrDefault();
		}

		protected virtual string NormalizeSequence(Actor self, string baseSequence)
		{
			string damageState = self.GetDamageState() >= DamageState.Heavy ? "damaged-" : "";
			if (anim.HasSequence(damageState + baseSequence))
				return damageState + baseSequence;
			else
				return baseSequence;
		}

		public void PlayCustomAnim(Actor self, string name)
		{
			if (anim.HasSequence(name))
				anim.PlayThen(NormalizeSequence(self, name),
					() => anim.PlayRepeating(NormalizeSequence(self, "idle")));
		}

		public WVec LocalToWorld(WVec vec)
		{
			// RA's 2d perspective doesn't correspond to an orthonormal 3D
			// coordinate system, so fudge the y axis to make things look good
			return new WVec(vec.Y, -Info.CameraPitch.Sin()*vec.X/1024, vec.Z);
		}

		public WRot QuantizeOrientation(Actor self, WRot orientation)
		{
			// Map yaw to the closest facing
			var numDirs = Info.QuantizedFacings == -1 ? anim.CurrentSequence.Facings : Info.QuantizedFacings;
			var facing = Util.QuantizeFacing(orientation.Yaw.Angle / 4, numDirs) * (256 / numDirs);

			// Roll and pitch are always zero
			return new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing));
		}
	}
}
