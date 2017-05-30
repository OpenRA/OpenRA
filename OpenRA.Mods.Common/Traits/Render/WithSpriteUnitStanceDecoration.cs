#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders unit stance indicator.")]
	public class WithSpriteUnitStanceDecorationInfo : ConditionalTraitInfo, Requires<AutoTargetInfo>
	{
		[PaletteReference] public readonly string Palette = "chrome";

		public readonly string Image = "pips";

		[Desc("Sprite sequence used for hold fire stance.")]
		[SequenceReference("Image")] public readonly string HoldFireSequence = null;

		[Desc("Sprite sequence used for return fire stance.")]
		[SequenceReference("Image")] public readonly string ReturnFireSequence = null;

		[Desc("Sprite sequence used for defend stance.")]
		[SequenceReference("Image")] public readonly string DefendSequence = null;

		[Desc("Sprite sequence used for attack anything stance.")]
		[SequenceReference("Image")] public readonly string AttackAnythingSequence = null;

		[Desc("Starting sprite sequence used for hold fire stance.")]
		[SequenceReference("Image")] public readonly string HoldFireStartSequence = null;

		[Desc("Starting sprite sequence used for return fire stance.")]
		[SequenceReference("Image")] public readonly string ReturnFireStartSequence = null;

		[Desc("Starting sprite sequence used for defend stance.")]
		[SequenceReference("Image")] public readonly string DefendStartSequence = null;

		[Desc("Starting sprite sequence used for attack anything stance.")]
		[SequenceReference("Image")] public readonly string AttackAnythingStartSequence = null;

		[Desc("Point in the actor's selection box used as reference for offsetting the decoration image. " +
			"Possible values are combinations of Center, Top, Bottom, Left, Right.")]
		public readonly ReferencePoints ReferencePoint = ReferencePoints.Top | ReferencePoints.Left;

		[Desc("Manual offset in screen pixel.")]
		public readonly int2 ScreenOffset = new int2(0, 0);

		public override object Create(ActorInitializer init) { return new WithSpriteUnitStanceDecoration(init.Self, this); }
	}

	public class WithSpriteUnitStanceDecoration : ConditionalTrait<WithSpriteUnitStanceDecorationInfo>, IRenderAboveShroudWhenSelected
	{
		readonly Animation pipImages;
		readonly AutoTarget autoTarget;
		PaletteReference palette = null;
		UnitStance lastStance = (UnitStance)(-1);
		bool playing = false;

		string GetSequence(UnitStance stance, bool start = false)
		{
			switch (stance)
			{
				case UnitStance.HoldFire:
					return start ? Info.HoldFireStartSequence : Info.HoldFireSequence;

				case UnitStance.ReturnFire:
					return start ? Info.ReturnFireStartSequence : Info.ReturnFireSequence;

				case UnitStance.Defend:
					return start ? Info.DefendStartSequence : Info.DefendSequence;

				case UnitStance.AttackAnything:
					return start ? Info.AttackAnythingStartSequence : Info.AttackAnythingSequence;

				default:
					return null;
			}
		}

		bool Play()
		{
			var predictedStance = autoTarget.PredictedStance;
			if (lastStance == predictedStance)
				return playing;

			lastStance = predictedStance;
			var sequence = GetSequence(predictedStance);
			var startSequence = GetSequence(predictedStance, true);
			playing = !string.IsNullOrEmpty(sequence) || !string.IsNullOrEmpty(startSequence);
			if (!playing)
				return false;

			if (string.IsNullOrEmpty(sequence))
				pipImages.Play(startSequence);
			else if (string.IsNullOrEmpty(startSequence))
				pipImages.PlayRepeating(sequence);
			else
				pipImages.PlayThen(startSequence, () => pipImages.PlayRepeating(sequence));

			return true;
		}

		public WithSpriteUnitStanceDecoration(Actor self, WithSpriteUnitStanceDecorationInfo info)
			: base(info)
		{
			autoTarget = self.Trait<AutoTarget>();
			pipImages = new Animation(self.World, info.Image, () => !playing || self.World.Paused);
		}

		public void Reset()
		{
			lastStance = (UnitStance)(-1);
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled || autoTarget.IsTraitDisabled || self.Owner != wr.World.LocalPlayer || self.World.FogObscures(self) || !Play())
				yield break;

			var bounds = self.VisualBounds;
			var boundsOffset = 0.5f * new float2(bounds.Left + bounds.Right, bounds.Top + bounds.Bottom);
			if (Info.ReferencePoint.HasFlag(ReferencePoints.Top))
				boundsOffset -= new float2(0, 0.5f * bounds.Height);
			else if (Info.ReferencePoint.HasFlag(ReferencePoints.Bottom))
				boundsOffset += new float2(0, 0.5f * bounds.Height);

			if (Info.ReferencePoint.HasFlag(ReferencePoints.Left))
				boundsOffset -= new float2(0.5f * bounds.Width, 0);
			else if (Info.ReferencePoint.HasFlag(ReferencePoints.Right))
				boundsOffset += new float2(0.5f * bounds.Width, 0);

			if (palette == null)
				palette = wr.Palette(Info.Palette);

			var pxPos = wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(self.CenterPosition) + boundsOffset.ToInt2()) + Info.ScreenOffset;
			pxPos -= (0.5f * pipImages.Image.Size.XY).ToInt2();
			yield return new UISpriteRenderable(pipImages.Image, self.CenterPosition, pxPos, 0, palette, 1f);
			pipImages.Tick();
		}
	}
}
