#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	// TODO: remove all the Render*Circle duplication
	class RenderJammerCircleInfo : ConditionalTraitInfo, IPlaceBuildingDecorationInfo
	{
		[Desc("Range circle color.")]
		public readonly Color Color = Color.FromArgb(128, Color.Red);

		[Desc("Range circle line width.")]
		public readonly float Width = 1;

		[Desc("Range circle border color.")]
		public readonly Color BorderColor = Color.FromArgb(96, Color.Black);

		[Desc("Range circle border width.")]
		public readonly float BorderWidth = 3;

		public IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (!EnabledByDefault)
				yield break;

			var jamsMissiles = ai.TraitInfoOrDefault<JamsMissilesInfo>();
			if (jamsMissiles != null)
			{
				yield return new RangeCircleAnnotationRenderable(
					centerPosition,
					jamsMissiles.Range,
					0,
					Color,
					Width,
					BorderColor,
					BorderWidth);
			}

			foreach (var a in w.ActorsWithTrait<RenderJammerCircle>())
				if (a.Actor.Owner.IsAlliedWith(w.RenderPlayer))
					foreach (var r in a.Trait.RenderAnnotations(a.Actor, wr))
						yield return r;
		}

		public override object Create(ActorInitializer init) { return new RenderJammerCircle(this); }
	}

	class RenderJammerCircle : ConditionalTrait<RenderJammerCircleInfo>, IRenderAnnotationsWhenSelected
	{
		readonly RenderJammerCircleInfo info;

		public RenderJammerCircle(RenderJammerCircleInfo info)
			: base(info)
		{
			this.info = info;
		}

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled)
				yield break;

			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var jamsMissiles = self.Info.TraitInfoOrDefault<JamsMissilesInfo>();
			if (jamsMissiles != null)
			{
				yield return new RangeCircleAnnotationRenderable(
					self.CenterPosition,
					jamsMissiles.Range,
					0,
					info.Color,
					info.Width,
					info.BorderColor,
					info.BorderWidth);
			}
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;
	}
}
