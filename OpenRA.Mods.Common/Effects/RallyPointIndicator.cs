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
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Effects
{
	public class RallyPointIndicator : IEffect, IEffectAboveShroud, IEffectAnnotation
	{
		readonly Actor building;
		readonly RallyPoint rp;
		readonly Animation flag;
		readonly Animation circles;

		readonly List<WPos> targetLineNodes = new();
		List<CPos> cachedLocations;

		public RallyPointIndicator(Actor building, RallyPoint rp)
		{
			this.building = building;
			this.rp = rp;

			if (rp.Info.Image != null)
			{
				if (rp.Info.FlagSequence != null)
				{
					flag = new Animation(building.World, rp.Info.Image);
					flag.PlayRepeating(rp.Info.FlagSequence);
				}

				if (rp.Info.CirclesSequence != null)
				{
					circles = new Animation(building.World, rp.Info.Image);
					circles.Play(rp.Info.CirclesSequence);
				}
			}

			UpdateTargetLineNodes(building.World);
		}

		void IEffect.Tick(World world)
		{
			flag?.Tick();

			circles?.Tick();

			if (cachedLocations == null || !cachedLocations.SequenceEqual(rp.Path))
			{
				UpdateTargetLineNodes(world);

				circles?.Play(rp.Info.CirclesSequence);
			}

			if (!building.IsInWorld || building.IsDead)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		void UpdateTargetLineNodes(World world)
		{
			cachedLocations = new List<CPos>(rp.Path);
			targetLineNodes.Clear();
			foreach (var c in cachedLocations)
				targetLineNodes.Add(world.Map.CenterOfCell(c));

			if (targetLineNodes.Count == 0)
				return;

			var exit = building.NearestExitOrDefault(targetLineNodes[0]);
			targetLineNodes.Insert(0, building.CenterPosition + (exit?.Info.SpawnOffset ?? WVec.Zero));
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr) { return SpriteRenderable.None; }

		IEnumerable<IRenderable> IEffectAboveShroud.RenderAboveShroud(WorldRenderer wr)
		{
			if (!building.IsInWorld || !building.Owner.IsAlliedWith(building.World.LocalPlayer))
				return SpriteRenderable.None;

			if (!building.World.Selection.Contains(building))
				return SpriteRenderable.None;

			var renderables = SpriteRenderable.None;
			if (targetLineNodes.Count > 0 && (circles != null || flag != null))
			{
				var palette = wr.Palette(rp.PaletteName);
				if (circles != null)
					renderables = renderables.Concat(circles.Render(targetLineNodes.Last(), palette));

				if (flag != null)
					renderables = renderables.Concat(flag.Render(targetLineNodes.Last(), palette));
			}

			return renderables;
		}

		IEnumerable<IRenderable> IEffectAnnotation.RenderAnnotation(WorldRenderer wr)
		{
			if (Game.Settings.Game.TargetLines == TargetLinesType.Disabled)
				return SpriteRenderable.None;

			if (!building.IsInWorld || !building.Owner.IsAlliedWith(building.World.LocalPlayer))
				return SpriteRenderable.None;

			if (!building.World.Selection.Contains(building))
				return SpriteRenderable.None;

			if (targetLineNodes.Count == 0)
				return SpriteRenderable.None;

			return RenderInner();
		}

		IEnumerable<IRenderable> RenderInner()
		{
			var prev = targetLineNodes[0];
			foreach (var pos in targetLineNodes.Skip(1))
			{
				var targetLine = new[] { prev, pos };
				prev = pos;
				yield return new TargetLineRenderable(targetLine, building.OwnerColor(), rp.Info.LineWidth, 1);
			}
		}
	}
}
