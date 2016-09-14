#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	/// <summary> Contains all functions that are valid for players and observers/spectators. </summary>
	public class WorldCommandWidget : Widget
	{
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly RadarPings radarPings;

		[ObjectCreator.UseCtor]
		public WorldCommandWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			radarPings = world.WorldActor.TraitOrDefault<RadarPings>();
		}

		public override string GetCursor(int2 pos) { return null; }
		public override Rectangle GetEventBounds() { return Rectangle.Empty; }

		public override bool HandleKeyPress(KeyInput e)
		{
			if (world == null)
				return false;

			return ProcessInput(e);
		}

		bool ProcessInput(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				var key = Hotkey.FromKeyInput(e);
				var ks = Game.Settings.Keys;

				if (key == ks.CycleBaseKey)
					return CycleBases();

				if (key == ks.CycleProductionBuildingsKey)
					return CycleProductionBuildings();

				if (key == ks.ToLastEventKey)
					return ToLastEvent();

				if (key == ks.ToSelectionKey)
					return ToSelection();
			}

			return false;
		}

		bool CycleBases()
		{
			var player = world.RenderPlayer ?? world.LocalPlayer;

			var bases = world.ActorsHavingTrait<BaseBuilding>()
				.Where(a => a.Owner == player)
				.ToList();

			// If no BaseBuilding exist pick the first selectable Building.
			if (!bases.Any())
			{
				var building = world.ActorsHavingTrait<Building>()
					.FirstOrDefault(a => a.Owner == player && a.Info.HasTraitInfo<SelectableInfo>());

				// No buildings left
				if (building == null)
					return true;

				world.Selection.Combine(world, new Actor[] { building }, false, true);
				return ToSelection();
			}

			var next = bases
				.SkipWhile(b => !world.Selection.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = bases.First();

			world.Selection.Combine(world, new Actor[] { next }, false, true);

			return ToSelection();
		}

		bool CycleProductionBuildings()
		{
			var player = world.RenderPlayer ?? world.LocalPlayer;

			var facilities = world.ActorsHavingTrait<Production>()
				.Where(a => a.Owner == player && !a.Info.HasTraitInfo<BaseBuildingInfo>())
				.OrderBy(f => f.Info.TraitInfo<ProductionInfo>().Produces.First())
				.ToList();

			if (!facilities.Any())
				return true;

			var next = facilities
				.SkipWhile(b => !world.Selection.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = facilities.First();

			world.Selection.Combine(world, new Actor[] { next }, false, true);

			Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", "ClickSound", null);

			return ToSelection();
		}

		bool ToLastEvent()
		{
			if (radarPings == null || radarPings.LastPingPosition == null)
				return true;

			worldRenderer.Viewport.Center(radarPings.LastPingPosition.Value);
			return true;
		}

		bool ToSelection()
		{
			worldRenderer.Viewport.Center(world.Selection.Actors);
			return true;
		}
	}
}
