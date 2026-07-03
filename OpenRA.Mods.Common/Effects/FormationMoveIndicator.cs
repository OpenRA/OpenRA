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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Effects
{
	public class FormationMoveIndicator : IEffect, IEffectAboveShroud, IEffectAnnotation
	{
		const string Image = "rallypoint";
		const string FlagSequence = "flag";
		const string CirclesSequence = "circles";
		static readonly float3 GreenTint = new(0.35f, 1.8f, 0.35f);
		static readonly Color LineColor = Color.Green;
		static readonly Color FormationOutlineColor = Color.FromArgb(255, 255, 140, 0);

		const int LifetimeTicks = 25;
		const int LineWidth = 1;

		readonly WPos destination;
		readonly WPos source;
		readonly CPos[] formationCells;
		readonly Animation flag;
		readonly Animation circles;
		int ticksRemaining = LifetimeTicks;

		public static void ShowAt(World world, CPos anchorCell, IEnumerable<Actor> actors, FormationType formation)
		{
			var actorList = actors.Where(a => a.IsInWorld && !a.IsDead).ToArray();
			if (actorList.Length == 0)
				return;

			var formationCells = FormationPreferences.OrangePreviewEnabled
				&& FormationResolver.ShouldApply(formation, actorList.Length)
				? FormationPreview.GetDestinationOccupiedCells(world, actorList, anchorCell, formation).ToArray()
				: [];

			world.RemoveAll(e => e is FormationMoveIndicator);
			world.Add(new FormationMoveIndicator(world, GetCentroid(actorList), world.Map.CenterOfCell(anchorCell), formationCells));
		}

		static WPos GetCentroid(Actor[] actors)
		{
			long x = 0;
			long y = 0;
			long z = 0;
			foreach (var a in actors)
			{
				var p = a.CenterPosition;
				x += p.X;
				y += p.Y;
				z += p.Z;
			}

			var count = actors.Length;
			return new WPos((int)(x / count), (int)(y / count), (int)(z / count));
		}

		FormationMoveIndicator(World world, WPos source, WPos destination, CPos[] formationCells)
		{
			this.source = source;
			this.destination = destination;
			this.formationCells = formationCells;

			flag = new Animation(world, Image);
			flag.PlayRepeating(FlagSequence);

			circles = new Animation(world, Image);
			circles.Play(CirclesSequence);
		}

		void IEffect.Tick(World world)
		{
			flag.Tick();
			circles.Tick();

			if (--ticksRemaining <= 0)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr) { return SpriteRenderable.None; }

		IEnumerable<IRenderable> IEffectAboveShroud.RenderAboveShroud(WorldRenderer wr)
		{
			var palette = wr.Palette("effect");
			foreach (var r in circles.Render(destination, palette))
				yield return Tint(r);

			foreach (var r in flag.Render(destination, palette))
				yield return Tint(r);
		}

		IEnumerable<IRenderable> IEffectAnnotation.RenderAnnotation(WorldRenderer wr)
		{
			if (!Ui.WidgetsVisible)
				yield break;

			if (formationCells.Length > 0)
				yield return new BorderedRegionRenderable(formationCells, FormationOutlineColor, 1, Color.Transparent, 0);

			yield return new TargetLineRenderable([source, destination], LineColor, LineWidth, 1);
		}

		static IRenderable Tint(IRenderable r)
		{
			if (r is IModifyableRenderable mr)
				return mr.WithTint(GreenTint, TintModifiers.None);

			return r;
		}
	}
}
