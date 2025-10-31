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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorTilingPathBrush : IEditorBrush
	{
		readonly TilingPathTool tool;
		readonly WorldRenderer worldRenderer;
		readonly EditorActionManager editorActionManager;

		MouseInput? startingMouseInput = null;
		bool isDragging = false;
		TilingPathTool.PathPlan previewPlan = null;

		public EditorTilingPathBrush(TilingPathTool tool)
		{
			this.tool = tool;
			worldRenderer = tool.WorldRenderer;
			editorActionManager = worldRenderer.World.WorldActor.Trait<EditorActionManager>();
		}

		public bool HandleMouseInput(MouseInput mouseInput)
		{
			if (mouseInput.Button != MouseButton.Left)
				return false;

			var isFinal = false;
			if (mouseInput.Event == MouseInputEvent.Down)
			{
				startingMouseInput = mouseInput;
				isDragging = false;
			}
			else if (startingMouseInput != null)
			{
				if (mouseInput.Event == MouseInputEvent.Up)
				{
					isFinal = true;
				}
			}
			else
			{
				return false;
			}

			CPos ViewToWorldCorner(int2 xy) =>
				CellLayerUtils.WPosToCorner(
				worldRenderer.ProjectedPosition(
					worldRenderer.Viewport.ViewToWorldPx(xy)),
				worldRenderer.World.Map.Grid.Type);

			var from = ViewToWorldCorner(startingMouseInput.Value.Location);
			var to = ViewToWorldCorner(mouseInput.Location);

			void UpdatePlan(TilingPathTool.PathPlan newPlan, bool preview)
			{
				if (isFinal)
				{
					editorActionManager.Add(
						new UpdateTilingPathPlanEditorAction(tool, newPlan));
				}
				else if (preview)
				{
					previewPlan = newPlan;
				}
			}

			if (isFinal)
			{
				previewPlan = null;
				startingMouseInput = null;
			}

			isDragging |= to != from;
			var plan = tool.Plan;

			if (plan == null)
			{
				UpdatePlan(new TilingPathTool.PathPlan(to), true);
				return true;
			}

			var points = plan.PointsWithRallyIndex();

			(bool IsInside, bool IsRally, int RallyIndex, bool IsStartDirector, bool IsEndDirector)
			AssessCPos(CPos cpos)
			{
				var isInside = points.Select(p => p.CPos).Contains(cpos);
				var isRally = plan.Rallies.Contains(cpos);
				var rallyIndex =
					isRally
						? plan.Rallies.TakeWhile(r => r != cpos).Count()
						: points
							.Where(p => p.CPos == cpos)
							.Select(p => p.RallyIndex)
							.FirstOrDefault(0);
				var isStartDirector =
					plan.AutoStart != Direction.None
						&& cpos == plan.FirstPoint - plan.AutoStart.ToCVec();
				var isEndDirector =
					plan.AutoEnd != Direction.None
						&& cpos == plan.LastPoint + plan.AutoEnd.ToCVec();
				return (isInside, isRally, rallyIndex, isStartDirector, isEndDirector);
			}

			var (fromIsInside, fromIsRally, fromRallyIndex, fromIsStartDirector, fromIsEndDirector) =
				AssessCPos(from);
			var (toIsInside, toIsRally, toRallyIndex, toIsStartDirector, toIsEndDirector) =
				AssessCPos(to);

			if (isDragging)
			{
				if (fromIsStartDirector)
				{
					var offset = plan.FirstPoint - to;
					var direction =
						offset != CVec.Zero
							? DirectionExts.ClosestFromCVec(offset)
							: Direction.None;
					UpdatePlan(plan.WithStart(direction), true);
				}
				else if (fromIsEndDirector)
				{
					var offset = to - plan.LastPoint;
					var direction =
						offset != CVec.Zero
							? DirectionExts.ClosestFromCVec(offset)
							: Direction.None;
					UpdatePlan(plan.WithEnd(direction), true);
				}
				else if (fromIsInside)
				{
					if (fromIsRally)
					{
						if (!toIsRally || to == from)
						{
							UpdatePlan(plan.WithRallyReplaced(fromRallyIndex, to), true);
						}
					}
					else
					{
						UpdatePlan(plan.Moved(to - from), true);
					}
				}
				else
				{
					if (!toIsRally)
					{
						UpdatePlan(plan.WithRallyAppended(to), true);
					}
				}
			}
			else
			{
				if (toIsInside)
				{
					if (toIsRally)
					{
						if (toRallyIndex == 0)
						{
							UpdatePlan(plan.WithLoop(!plan.Loop), false);
						}
						else
						{
							UpdatePlan(plan.WithRallyRemoved(toRallyIndex), false);
						}
					}
					else
					{
						UpdatePlan(plan.WithRallyInserted(toRallyIndex, to), false);
					}
				}
				else
				{
					UpdatePlan(plan.WithRallyAppended(to), true);
				}
			}

			return true;
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self) { }
		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (tool.EditorBlitSource == null)
				yield break;

			var preview = EditorBlit.PreviewBlitSource(
				tool.EditorBlitSource.Value,
				MapBlitFilters.Terrain | MapBlitFilters.Actors,
				CVec.Zero,
				wr);
			foreach (var renderable in preview)
				yield return renderable;
		}

		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			var plan = previewPlan ?? tool.Plan;
			if (plan == null)
				yield break;

			var mainColor = tool.EditorBlitSource != null ? Color.Cyan : Color.Red;

			var gridType = worldRenderer.World.Map.Grid.Type;
			WPos CornerOfCell(CPos cpos) => CellLayerUtils.CornerToWPos(cpos, gridType);

			var points = plan.Points();
			for (var i = 1; i < points.Length; i++)
			{
				yield return new CircleAnnotationRenderable(
					CornerOfCell(points[i]), new WDist(128), 1, Color.Yellow, false);
				yield return new LineAnnotationRenderable(
					CornerOfCell(points[i - 1]),
					CornerOfCell(points[i]),
					1,
					Color.Yellow,
					Color.Yellow);
			}

			for (var i = 1; i < plan.Rallies.Length; i++)
			{
				yield return new CircleAnnotationRenderable(
					CornerOfCell(plan.Rallies[i]), new WDist(512), 1, mainColor, false);
				yield return new LineAnnotationRenderable(
					CornerOfCell(plan.Rallies[i - 1]),
					CornerOfCell(plan.Rallies[i]),
					1,
					mainColor,
					mainColor);
			}

			if (plan.AutoEnd != Direction.None)
				yield return new CircleAnnotationRenderable(
					CornerOfCell(plan.LastPoint) + plan.AutoEnd.ToWVec() * 768,
					new WDist(256),
					2,
					plan.End != Direction.None ? Color.Magenta : Color.Gray,
					false);

			if (plan.AutoStart != Direction.None)
				yield return new CircleAnnotationRenderable(
					CornerOfCell(plan.FirstPoint) - plan.AutoStart.ToWVec() * 768,
					new WDist(256),
					2,
					plan.Start != Direction.None ? Color.Magenta : Color.Gray,
					true);

			yield return new CircleAnnotationRenderable(
				CornerOfCell(plan.Rallies[0]), new WDist(512), 1, mainColor, true);
		}

		public void Tick() { }

		public void Dispose() { }
	}

	sealed class UpdateTilingPathPlanEditorAction : IEditorAction
	{
		[FluentReference]
		const string StartedPlan = "notification-tiling-path-started";
		[FluentReference]
		const string UpdatedPlan = "notification-tiling-path-updated";
		[FluentReference]
		const string ResetPlan = "notification-tiling-path-reset";

		public string Text { get; }

		readonly TilingPathTool tool;
		readonly TilingPathTool.PathPlan oldPlan;
		readonly TilingPathTool.PathPlan newPlan;

		public UpdateTilingPathPlanEditorAction(
			TilingPathTool tool,
			TilingPathTool.PathPlan newPlan)
		{
			this.tool = tool;
			oldPlan = tool.Plan;
			this.newPlan = newPlan;
			if (oldPlan == null && newPlan == null)
				throw new ArgumentException("oldPlan and newPlan cannot both be null");
			else if (oldPlan == null)
				Text = FluentProvider.GetMessage(StartedPlan);
			else if (newPlan == null)
				Text = FluentProvider.GetMessage(ResetPlan);
			else
				Text = FluentProvider.GetMessage(UpdatedPlan);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			tool.SetPlan(newPlan);
		}

		public void Undo()
		{
			tool.SetPlan(oldPlan);
		}
	}

	sealed class PaintTilingPathEditorAction : IEditorAction
	{
		[FluentReference]
		const string Painted = "notification-tiling-path-painted";

		public string Text { get; }

		readonly TilingPathTool tool;
		readonly TilingPathTool.PathPlan plan;
		readonly EditorBlit editorBlit;

		public PaintTilingPathEditorAction(TilingPathTool tool)
		{
			this.tool = tool;
			plan = tool.Plan;
			Text = FluentProvider.GetMessage(Painted);

			var world = tool.World;
			var editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();

			var blitSource = tool.EditorBlitSource.Value;

			editorBlit = new EditorBlit(
				MapBlitFilters.Terrain | MapBlitFilters.Actors,
				null,
				blitSource.CellRegion.TopLeft,
				world.Map,
				blitSource,
				editorActorLayer,
				false);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			tool.SetPlan(null);
			editorBlit.Commit();
		}

		public void Undo()
		{
			editorBlit.Revert();
			tool.SetPlan(plan);
		}
	}
}
