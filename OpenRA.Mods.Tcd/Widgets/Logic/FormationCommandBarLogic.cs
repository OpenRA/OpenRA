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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Tcd.Formations;
using OpenRA.Mods.Tcd.Orders;
using OpenRA.Mods.Tcd.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Tcd.Widgets.Logic
{
	public sealed class FormationCommandBarLogic : ChromeLogic
	{
		static readonly (string Button, FormationShape Shape)[] Shapes =
		[
			("FORMATION_GRID", FormationShape.Grid),
			("FORMATION_WEDGE", FormationShape.Wedge),
		];

		bool expanded;

		[ObjectCreator.UseCtor]
		public FormationCommandBarLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			var settings = world.WorldActor.TraitOrDefault<SquadManager>();
			var spacing = settings?.FormationSpacingCells ?? 1;
			var maxRowWidth = settings?.FormationMaxRowWidth ?? 8;

			var tray = widget.GetOrNull("TCD_TRAY");
			if (tray != null)
				tray.IsVisible = () => expanded;

			var toggle = widget.GetOrNull<ButtonWidget>("TCD_TOGGLE");
			if (toggle != null)
				toggle.OnClick = () => expanded = !expanded;

			foreach (var (name, shape) in Shapes)
			{
				var button = widget.GetOrNull<ButtonWidget>(name);
				if (button == null)
					continue;

				var applied = shape;
				button.IsDisabled = () => world.Selection.Actors.Count == 0;
				button.OnClick = () =>
				{
					// Point the formation at wherever the player is looking rather than at
					// whatever direction the units happened to already be facing.
					var cursorCell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
					var faceToward = world.Map.CenterOfCell(cursorCell);

					var ordered = FormationPlanner.Apply(world, world.Selection.Actors, applied,
						spacing, maxRowWidth, faceToward);

					if (ordered > 0)
						TextNotificationsManager.Debug($"{applied} formation: {ordered} units.");
				};
			}

			var draw = widget.GetOrNull<ButtonWidget>("FORMATION_LINE");
			if (draw != null)
			{
				draw.IsDisabled = () => world.Selection.Actors.Count == 0;
				draw.IsHighlighted = () => world.OrderGenerator is LineFormationOrderGenerator;
				draw.OnClick = () => world.OrderGenerator = new LineFormationOrderGenerator(world);
			}

			var mark = widget.GetOrNull<ButtonWidget>("FORMATION_SHAPE");
			if (mark == null)
				return;

			var capture = world.WorldActor.TraitOrDefault<FormationCapture>();
			mark.IsDisabled = () => capture == null || world.Selection.Actors.Count == 0;
			mark.IsHighlighted = () => capture != null && capture.Mode != FormationCaptureMode.None;
			mark.OnClick = () =>
			{
				// First click starts marking, second click closes the shape and places
				// the units - the button stands in for pressing and releasing the key.
				if (capture.Mode == FormationCaptureMode.None)
				{
					capture.Begin(FormationCaptureMode.Points);
					TextNotificationsManager.Debug("Right click to mark the shape, then press the button again.");
					return;
				}

				var placed = capture.Commit();
				TextNotificationsManager.Debug(placed > 0
					? $"Shape formation: {placed} units."
					: "Shape cancelled: no points marked.");
			};
		}
	}
}
