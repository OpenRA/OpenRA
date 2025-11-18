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
using System.Collections.ObjectModel;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using Color = OpenRA.Primitives.Color;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	public sealed class HeightToolInfo : TraitInfo, IEditorToolInfo
	{
		[FluentReference]
		const string Label = "label-tool-height";

		[Desc("The widget tree to open when the tool is selected.")]
		const string PanelWidget = "HEIGHT_TOOL_PANEL";

		public readonly int MinSize = 1;
		public readonly int MaxSize = 10;

		public override object Create(ActorInitializer init)
		{
			return new HeightTool(init.World, this);
		}

		string IEditorToolInfo.Label => Label;
		string IEditorToolInfo.PanelWidget => PanelWidget;
	}

	public sealed class HeightTool(World world, HeightToolInfo info) : IRenderAnnotations
	{
		[Flags]
		public enum Settings
		{
			Lower = 0x01,
			Circle = 0x02
		}

		public class Change
		{
			public byte Old;
			public byte New;
			public bool Extended;
		}

		public readonly HeightToolInfo Info = info;

		readonly Dictionary<CPos, Change> changes = [];
		CPos? currentCell;
		int currentSize;
		Settings currentSettings;

		public bool SpatiallyPartitionable => false;
		public IReadOnlyDictionary<CPos, Change> Changes => new ReadOnlyDictionary<CPos, Change>(changes);

		public void Update(CPos? cell, int size, Settings settings)
		{
			if (currentCell == cell && currentSize == size && currentSettings == settings)
				return;

			currentCell = cell;
			currentSize = size;
			currentSettings = settings;

			RebuildCache();
		}

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr)
		{
			foreach (var (cell, change) in changes)
			{
				var color = change.New > change.Old ? Color.FromArgb(0x00, 0xff, 0x00) :
					change.New < change.Old ? Color.FromArgb(0xff, 0x00, 0x00) :
					change.Extended ? Color.FromArgb(0x00, 0x00, 0xff) :
					Color.FromArgb(0x22, 0x22, 0x22);

				yield return new MarkerTileRenderable(cell, Color.FromArgb(0x22, color));
			}
		}

		void RebuildCache()
		{
			changes.Clear();

			if (currentCell == null || !world.Map.Contains(currentCell.Value))
				return;

			var brushCells = GetBrushCells(currentCell.Value);
			ApplyDesiredHeightChanges(brushCells);
		}

		HashSet<CPos> GetBrushCells(CPos centerCell)
		{
			var cells = new HashSet<CPos>();
			var center = world.Map.CenterOfCell(centerCell);
			var radius = currentSize / 2;

			for (var x = -radius; x <= radius; x++)
			{
				for (var y = -radius; y <= radius; y++)
				{
					var cell = centerCell - new CVec(x, y);

					if (!world.Map.Contains(cell))
						continue;

					if (currentSettings.HasFlag(Settings.Circle) && (world.Map.CenterOfCell(cell) - center).Length > radius * 1024)
						continue;

					cells.Add(cell);
				}
			}

			return cells;
		}

		void ApplyDesiredHeightChanges(HashSet<CPos> brushCells)
		{
			byte source;
			byte target;

			if (currentSettings.HasFlag(Settings.Lower))
			{
				source = brushCells.Max(cell => world.Map.Height[cell]);
				target = (byte)Math.Clamp(source - 1, byte.MinValue, world.Map.Grid.MaximumTerrainHeight);
			}
			else
			{
				source = brushCells.Min(cell => world.Map.Height[cell]);
				target = (byte)Math.Clamp(source + 1, byte.MinValue, world.Map.Grid.MaximumTerrainHeight);
			}

			if (source == target)
				return;

			foreach (var cell in brushCells.Where(cell => world.Map.Height[cell] == source))
			{
				changes.Add(cell, new Change { Old = source, New = target });
			}
		}
	}
}
