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

using System.Linq;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	static class EditorWalkability
	{
		public static bool IsCellWalkable(World world, Locomotor locomotor, CPos cell)
		{
			if (!world.Map.Contains(cell) || locomotor == null)
				return false;

			if (locomotor.MovementCostForCell(cell) == PathGraph.MovementCostForUnreachableCell)
				return false;

			if (!locomotor.CanStayInCell(cell))
				return false;

			if (world.Type == WorldType.Editor)
			{
				var editorLayer = world.WorldActor.TraitOrDefault<EditorActorLayer>();
				if (editorLayer != null && IsCellBlockedByEditorActors(world, locomotor, cell, editorLayer))
					return false;
			}

			return locomotor.MovementCostToEnterCell(null, cell, BlockedByActor.All, null)
				!= PathGraph.MovementCostForUnreachableCell;
		}

		static bool IsCellBlockedByEditorActors(World world, Locomotor locomotor, CPos cell, EditorActorLayer editorLayer)
		{
			var previews = editorLayer.PreviewsAtCell(cell).ToArray();
			if (previews.Length == 0)
				return false;

			if (!locomotor.Info.SharesCell)
				return true;

			foreach (var preview in previews)
			{
				if (EditorPreviewBlocksFootMovement(preview))
					return true;
			}

			var map = world.Map;
			for (var i = (byte)SubCell.First; i < map.Grid.SubCellOffsets.Length; i++)
			{
				var subCell = (SubCell)i;
				if (!previews.Any(p => p.Footprint.TryGetValue(cell, out var s) && (s == SubCell.FullCell || s == subCell)))
					return false;
			}

			return true;
		}

		static bool EditorPreviewBlocksFootMovement(EditorActorPreview preview)
		{
			var ios = preview.Info.TraitInfoOrDefault<IOccupySpaceInfo>();
			if (ios == null)
				return false;

			return !ios.SharesCell;
		}
	}
}
