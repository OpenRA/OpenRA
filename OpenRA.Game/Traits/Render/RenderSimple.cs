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
	public class RenderSimpleInfo : ITraitInfo, LocalCoordinatesModelInfo
	{
		[Desc("Defaults to the actor name.")]
		public readonly string Image = null;
		[Desc("custom palette name")]
		public readonly string Palette = null;
		[Desc("custom PlayerColorPalette: BaseName")]
		public readonly string PlayerPalette = "player";
		[Desc("Change the sprite image size.")]
		public readonly float Scale = 1f;

		[Desc("Number of facings for gameplay calculations. -1 indiciates auto-detection from sequence")]
		public readonly int QuantizedFacings = -1;

		[Desc("Camera pitch the sprite was rendered with. Used to determine rotation ellipses")]
		public readonly WAngle CameraPitch = WAngle.FromDegrees(40);
		public virtual object Create(ActorInitializer init) { return new RenderSimple(init.self); }

		public virtual IEnumerable<Renderable> RenderPreview(ActorInfo ai, PaletteReference pr)
		{
			var anim = new Animation(RenderSimple.GetImage(ai), () => 0);
			anim.PlayRepeating("idle");

			yield return new Renderable(anim.Image, 0.5f*anim.Image.size, pr, 0, Scale);
		}
	}

	public class RenderSimple : IRender, ILocalCoordinatesModel, IAutoSelectionSize, ITick, INotifyOwnerChanged
	{
		public Dictionary<string, AnimationWithOffset> anims = new Dictionary<string, AnimationWithOffset>();

		public static Func<int> MakeFacingFunc(Actor self)
		{
			var facing = self.TraitOrDefault<IFacing>();
			if (facing == null) return () => 0;
			return () => facing.Facing;
		}

		public Animation anim
		{
			get { return anims[""].Animation; }
			protected set { anims[""].Animation = value; }
		}

		public static string GetImage(ActorInfo actor)
		{
			var Info = actor.Traits.Get<RenderSimpleInfo>();
			return Info.Image ?? actor.Name;
		}

		public string GetImage(Actor self)
		{
			if (cachedImage != null)
				return cachedImage;

			return cachedImage = GetImage(self.Info);
		}

		RenderSimpleInfo Info;
		string cachedImage = null;
		bool initializePalette = true;
		protected PaletteReference palette;

		public RenderSimple(Actor self, Func<int> baseFacing)
		{
			anims.Add("", new Animation(GetImage(self), baseFacing));
			Info = self.Info.Traits.Get<RenderSimpleInfo>();
		}

		public RenderSimple(Actor self) : this( self, MakeFacingFunc(self) )
		{
			anim.PlayRepeating("idle");
		}

		protected virtual string PaletteName(Actor self)
		{
			return Info.Palette ?? Info.PlayerPalette + self.Owner.InternalName;
		}

		protected void UpdatePalette() { initializePalette = true; }
		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { UpdatePalette(); }

		public virtual IEnumerable<Renderable> Render(Actor self, WorldRenderer wr)
		{
			if (initializePalette)
			{
				palette = wr.Palette(PaletteName(self));
				initializePalette = false;
			}

			foreach (var a in anims.Values)
				if (a.DisableFunc == null || !a.DisableFunc())
					yield return a.Image(self, wr, palette, Info.Scale);
		}

		public int2 SelectionSize(Actor self)
		{
			return anims.Values.Where(b => (b.DisableFunc == null || !b.DisableFunc())
			                                && b.Animation.CurrentSequence != null)
				.Select(a => (a.Animation.Image.size*Info.Scale).ToInt2())
				.FirstOrDefault();
		}

		public virtual void Tick(Actor self)
		{
			foreach (var a in anims.Values)
				a.Animation.Tick();
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
