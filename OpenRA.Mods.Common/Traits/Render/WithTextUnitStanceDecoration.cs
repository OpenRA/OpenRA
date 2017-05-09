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
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders unit stance using typeface.")]
	public class WithTextUnitStanceDecorationInfo : ConditionalTraitInfo, IRulesetLoaded, Requires<AutoTargetInfo>
	{
		public readonly string Font = "TinyBold";

		[Desc("Display this text for hold fire.")]
		public readonly string HoldFireText = null;

		[Desc("Display hold fire text in this color.")]
		public readonly Color HoldFireColor = Color.Blue;

		[Desc("Display this text for return fire.")]
		public readonly string ReturnFireText = null;

		[Desc("Display return fire text in this color.")]
		public readonly Color ReturnFireColor = Color.Yellow;

		[Desc("Display this text for defend.")]
		public readonly string DefendText = null;

		[Desc("Display defend text in this color.")]
		public readonly Color DefendColor = Color.Orange;

		[Desc("Display this text for attack anything.")]
		public readonly string AttackAnythingText = null;

		[Desc("Display attack anything text in this color.")]
		public readonly Color AttackAnythingColor = Color.Red;

		[Desc("The Z offset to apply when rendering this decoration.")]
		public readonly int ZOffset = 1;

		[Desc("Point in the actor's selection box used as reference for offsetting the decoration image. " +
			"Possible values are combinations of Center, Top, Bottom, Left, Right.")]
		public readonly ReferencePoints ReferencePoint = ReferencePoints.Top | ReferencePoints.Left;

		[Desc("Manual offset in screen pixel.")]
		public readonly int2 ScreenOffset = new int2(2, -2);

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (!Game.ModData.Manifest.Fonts.ContainsKey(Font))
				throw new YamlException("Font '{0}' is not listed in the mod.yaml's Fonts section".F(Font));
		}

		public override object Create(ActorInitializer init) { return new WithTextUnitStanceDecoration(init.Self, this); }
	}

	public class WithTextUnitStanceDecoration : ConditionalTrait<WithTextUnitStanceDecorationInfo>, IRenderAboveShroudWhenSelected
	{
		readonly SpriteFont font;
		readonly AutoTarget autoTarget;

		public WithTextUnitStanceDecoration(Actor self, WithTextUnitStanceDecorationInfo info)
			: base(info)
		{
			if (!Game.Renderer.Fonts.TryGetValue(info.Font, out font))
				throw new YamlException("Font '{0}' is not listed in the mod.yaml's Fonts section".F(info.Font));

			autoTarget = self.Trait<AutoTarget>();
		}

		string GetText(UnitStance stance)
		{
			switch (stance)
			{
				case UnitStance.HoldFire:
					return Info.HoldFireText;

				case UnitStance.ReturnFire:
					return Info.ReturnFireText;

				case UnitStance.Defend:
					return Info.DefendText;

				case UnitStance.AttackAnything:
					return Info.AttackAnythingText;

				default:
					return null;
			}
		}

		Color GetColor(UnitStance stance)
		{
			switch (stance)
			{
				case UnitStance.HoldFire:
					return Info.HoldFireColor;

				case UnitStance.ReturnFire:
					return Info.ReturnFireColor;

				case UnitStance.Defend:
					return Info.DefendColor;

				case UnitStance.AttackAnything:
					return Info.AttackAnythingColor;

				default:
					return Color.White;
			}
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled || autoTarget.IsTraitDisabled || self.Owner != wr.World.LocalPlayer || self.World.FogObscures(self))
				yield break;

			var text = GetText(autoTarget.PredictedStance);
			if (string.IsNullOrEmpty(text))
				yield break;

			var bounds = self.VisualBounds;
			var color = GetColor(autoTarget.PredictedStance);
			var halfSize = font.Measure(text) / 2;

			var boundsOffset = new int2(bounds.Left + bounds.Right, bounds.Top + bounds.Bottom) / 2;
			var sizeOffset = new int2();
			if (Info.ReferencePoint.HasFlag(ReferencePoints.Top))
			{
				boundsOffset -= new int2(0, bounds.Height / 2);
				sizeOffset += new int2(0, halfSize.Y);
			}
			else if (Info.ReferencePoint.HasFlag(ReferencePoints.Bottom))
			{
				boundsOffset += new int2(0, bounds.Height / 2);
				sizeOffset -= new int2(0, halfSize.Y);
			}

			if (Info.ReferencePoint.HasFlag(ReferencePoints.Left))
			{
				boundsOffset -= new int2(bounds.Width / 2, 0);
				sizeOffset += new int2(halfSize.X, 0);
			}
			else if (Info.ReferencePoint.HasFlag(ReferencePoints.Right))
			{
				boundsOffset += new int2(bounds.Width / 2, 0);
				sizeOffset -= new int2(halfSize.X, 0);
			}

			var screenPos = wr.ScreenPxPosition(self.CenterPosition) + boundsOffset + sizeOffset + Info.ScreenOffset;

			yield return new TextRenderable(font, wr.ProjectedPosition(screenPos), Info.ZOffset, color, text);
		}
	}
}
