using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System.Collections.Generic;

namespace OpenRA.Mods.AS.Traits
{
    public class WithIdleOverlayASInfo : WithIdleOverlayInfo
    {
        [Desc("Image name to use, if null, it falls back to default.")]
        public readonly string Image = "";

        public override object Create(ActorInitializer init) { return new WithIdleOverlayAS(init.Self, this); }

        public new IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
        {
            if (UpgradeMinEnabledLevel > 0)
                yield break;

            if (Palette != null)
                p = init.WorldRenderer.Palette(Palette);

            var idleImage = Image != null ? Image : image;

            var body = init.Actor.TraitInfo<BodyOrientationInfo>();
            var facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : 0;
            var anim = new Animation(init.World, idleImage, () => facing);
            anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

            var orientation = body.QuantizeOrientation(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing)), facings);
            var offset = body.LocalToWorld(Offset.Rotate(orientation));
            yield return new SpriteActorPreview(anim, offset, offset.Y + offset.Z + 1, p, rs.Scale);
        }
    }

    public class WithIdleOverlayAS : UpgradableTrait<WithIdleOverlayASInfo>, INotifyDamageStateChanged, INotifyBuildComplete, INotifySold, INotifyTransform
    {
        readonly Animation overlay;
		bool buildComplete;

		public WithIdleOverlayAS(Actor self, WithIdleOverlayASInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

            var image = info.Image != null ? info.Image : rs.GetImage(self);

			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units
			overlay = new Animation(self.World, image,
				() => (info.PauseOnLowPower && self.IsDisabled()) || !buildComplete);
			if (info.StartSequence != null)
				overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.StartSequence),
					() => overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence)));
			else
				overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence));

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => IsTraitDisabled || !buildComplete,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		public void Sold(Actor self) { }
		public void Selling(Actor self)
		{
			buildComplete = false;
		}

		public void BeforeTransform(Actor self)
		{
			buildComplete = false;
		}

		public void OnTransform(Actor self) { }
		public void AfterTransform(Actor self) { }

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}
    }
}
