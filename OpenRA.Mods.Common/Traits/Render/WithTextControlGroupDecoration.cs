#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders Ctrl groups using typeface.")]
	public class WithTextControlGroupDecorationInfo : ITraitInfo, IRulesetLoaded, Requires<IDecorationBoundsInfo>
	{
		public readonly string Font = "TinyBold";

		[Desc("Display in this color when not using the player color.")]
		public readonly Color Color = Color.White;

		[Desc("Use the player color of the current owner.")]
		public readonly bool UsePlayerColor = false;

		[Desc("The Z offset to apply when rendering this decoration.")]
		public readonly int ZOffset = 1;

		[Desc("Point in the actor's selection box used as reference for offsetting the decoration image. " +
			"Possible values are combinations of Center, Top, Bottom, Left, Right.")]
		public readonly ReferencePoints ReferencePoint = ReferencePoints.Bottom | ReferencePoints.Left;

		[Desc("Manual offset in screen pixel.")]
		public readonly int2 ScreenOffset = new int2(2, -2);

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (!Game.ModData.Manifest.Fonts.ContainsKey(Font))
				throw new YamlException("Font '{0}' is not listed in the mod.yaml's Fonts section".F(Font));
		}

		public object Create(ActorInitializer init) { return new WithTextControlGroupDecoration(init.Self, this); }
	}

	public class WithTextControlGroupDecoration : IRenderAboveShroudWhenSelected, INotifyOwnerChanged
	{
		readonly WithTextControlGroupDecorationInfo info;
		readonly IDecorationBounds[] decorationBounds;
		readonly SpriteFont font;

		Color color;

		public WithTextControlGroupDecoration(Actor self, WithTextControlGroupDecorationInfo info)
		{
			this.info = info;

			if (!Game.Renderer.Fonts.TryGetValue(info.Font, out font))
				throw new YamlException("Font '{0}' is not listed in the mod.yaml's Fonts section".F(info.Font));

			decorationBounds = self.TraitsImplementing<IDecorationBounds>().ToArray();
			color = info.UsePlayerColor ? self.Owner.Color : info.Color;
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (self.Owner != wr.World.LocalPlayer)
				yield break;

			if (self.World.FogObscures(self))
				yield break;

			foreach (var r in DrawControlGroup(self, wr))
				yield return r;
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable { get { return true; } }

		IEnumerable<IRenderable> DrawControlGroup(Actor self, WorldRenderer wr)
		{
			var group = self.World.Selection.GetControlGroupForActor(self);
			if (group == null)
				yield break;

			var bounds = decorationBounds.FirstNonEmptyBounds(self, wr);
			var number = group.Value.ToString();
			var halfSize = font.Measure(number) / 2;

			var boundsOffset = new int2(bounds.Left + bounds.Right, bounds.Top + bounds.Bottom) / 2;
			var sizeOffset = new int2();
			if (info.ReferencePoint.HasFlag(ReferencePoints.Top))
			{
				boundsOffset -= new int2(0, bounds.Height / 2);
				sizeOffset += new int2(0, halfSize.Y);
			}
			else if (info.ReferencePoint.HasFlag(ReferencePoints.Bottom))
			{
				boundsOffset += new int2(0, bounds.Height / 2);
				sizeOffset -= new int2(0, halfSize.Y);
			}

			if (info.ReferencePoint.HasFlag(ReferencePoints.Left))
			{
				boundsOffset -= new int2(bounds.Width / 2, 0);
				sizeOffset += new int2(halfSize.X, 0);
			}
			else if (info.ReferencePoint.HasFlag(ReferencePoints.Right))
			{
				boundsOffset += new int2(bounds.Width / 2, 0);
				sizeOffset -= new int2(halfSize.X, 0);
			}

			var screenPos = boundsOffset + sizeOffset + info.ScreenOffset;

			yield return new TextRenderable(font, wr.ProjectedPosition(screenPos), info.ZOffset, color, number);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (info.UsePlayerColor)
				color = newOwner.Color;
		}
	}
}
