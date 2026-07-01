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
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class AdvancedProductionRallyOrderGenerator : OrderGenerator
	{
		static readonly Color RallyColor = Color.FromArgb(255, 255, 140, 0);
		static readonly float3 RallyTint = new float3(RallyColor.R, RallyColor.G, RallyColor.B) / 255f;

		readonly ProductionQueue queue;
		readonly string itemName;
		readonly Animation flag;
		readonly Animation circles;

		protected override MouseActionType ActionType => MouseActionType.Contextual;

		public AdvancedProductionRallyOrderGenerator(World world, ProductionQueue queue, string itemName)
			: base(world)
		{
			this.queue = queue;
			this.itemName = itemName;

			flag = new Animation(world, "rallypoint");
			flag.PlayRepeating("flag");

			circles = new Animation(world, "rallypoint");
			circles.PlayRepeating("circles");
		}

		public bool Matches(ProductionQueue candidateQueue, string candidateItemName)
		{
			return candidateQueue != null
				&& queue != null
				&& candidateQueue.Actor.ActorID == queue.Actor.ActorID
				&& candidateItemName == itemName;
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (!IsValid(world) || !world.Map.Contains(cell))
				yield break;

			yield return new Order(AdvancedProductionRallyPoints.SetOrder, world.LocalPlayer.PlayerActor, Target.FromCell(world, cell), false)
			{
				TargetString = itemName,
				SuppressVisualFeedback = true
			};
		}

		protected override void Tick(World world)
		{
			if (!IsValid(world))
				world.CancelInputMode();

			flag.Tick();
			circles.Tick();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
		{
			yield break;
		}

		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
		{
			var rallyCell = GetRallyCell(world);
			if (rallyCell == null)
				yield break;

			var rallyPos = world.Map.CenterOfCell(rallyCell.Value);
			var effectPalette = wr.Palette("effect");

			foreach (var r in circles.Render(rallyPos, effectPalette))
				yield return ToOrange(r);
			foreach (var r in flag.Render(rallyPos, effectPalette))
				yield return ToOrange(r);
		}

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			if (!IsValid(world))
				yield break;

			var rallyCell = GetRallyCell(world);
			if (rallyCell == null)
				yield break;

			var rallyPath = new List<WPos>();
			var producer = queue.MostLikelyProducer().Actor;
			if (producer != null && producer.IsInWorld)
			{
				var rallyPos = world.Map.CenterOfCell(rallyCell.Value);
				var exit = producer.NearestExitOrDefault(rallyPos, queue.Info.Type);
				rallyPath.Add(producer.CenterPosition + (exit?.Info.SpawnOffset ?? WVec.Zero));
			}

			rallyPath.Add(world.Map.CenterOfCell(rallyCell.Value));
			yield return new TargetLineRenderable(rallyPath, RallyColor, 2, 2);
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) ? "ability" : "move-blocked";
		}

		CPos? GetRallyCell(World world)
		{
			if (!IsValid(world))
				return null;

			var advancedRally = world.LocalPlayer.PlayerActor.TraitOrDefault<AdvancedProductionRallyPoints>();
			if (advancedRally == null || !advancedRally.TryGetRallyPoint(itemName, out var rallyCell))
				return null;

			return rallyCell;
		}

		static IRenderable ToOrange(IRenderable renderable)
		{
			return renderable is IModifyableRenderable mr
				? mr.WithTint(RallyTint, mr.TintModifiers | TintModifiers.ReplaceColor | TintModifiers.IgnoreWorldTint)
				: renderable;
		}

		bool IsValid(World world)
		{
			return world.LocalPlayer != null
				&& queue != null
				&& queue.Actor != null
				&& queue.Actor.IsInWorld
				&& !queue.Actor.IsDead
				&& !string.IsNullOrEmpty(itemName);
		}
	}
}
