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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Renders the Tiberian Sun Vein resources.", "Attach this to the world actor")]
	public class TSVeinsRendererInfo : TraitInfo, Requires<IResourceLayerInfo>, IMapPreviewSignatureInfo
	{
		[FieldLoader.Require]
		[Desc("Resource type used for veins.")]
		public readonly string ResourceType = null;

		[Desc("Sequence image that holds the different variants.")]
		public readonly string Image = "resources";

		[SequenceReference(nameof(Image))]
		[Desc("Vein sequence name.")]
		public readonly string Sequence = "veins";

		[PaletteReference]
		[Desc("Palette used for rendering the resource sprites.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[FieldLoader.Require]
		[TranslationReference]
		[Desc("Resource name used by tooltips.")]
		public readonly string Name = null;

		[ActorReference]
		[Desc("Actor types that should be treated as veins for adjacency.")]
		public readonly HashSet<string> VeinholeActors = new();

		void IMapPreviewSignatureInfo.PopulateMapPreviewSignatureCells(Map map, ActorInfo ai, ActorReference s, List<(MPos Uv, Color Color)> destinationBuffer)
		{
			var resourceLayer = ai.TraitInfoOrDefault<IResourceLayerInfo>();
			if (resourceLayer == null)
				return;

			if (!resourceLayer.TryGetResourceIndex(ResourceType, out var resourceIndex) || !resourceLayer.TryGetTerrainType(ResourceType, out var terrainType))
				return;

			var veinholeCells = new HashSet<CPos>();

			foreach (var kv in map.ActorDefinitions)
			{
				var type = kv.Value.Value;
				if (!VeinholeActors.Contains(type))
					continue;

				var actorReference = new ActorReference(type, kv.Value.ToDictionary());
				var location = actorReference.Get<LocationInit>();
				var veinholeInfo = map.Rules.Actors[actorReference.Type];
				foreach (var cell in veinholeInfo.TraitInfo<IOccupySpaceInfo>().OccupiedCells(veinholeInfo, location.Value))
					veinholeCells.Add(cell.Key);
			}

			var terrainInfo = map.Rules.TerrainInfo;
			var info = terrainInfo.TerrainTypes[terrainInfo.GetTerrainIndex(terrainType)];

			for (var i = 0; i < map.MapSize.X; i++)
			{
				for (var j = 0; j < map.MapSize.Y; j++)
				{
					var uv = new MPos(i, j);

					// Cell contains veins
					if (map.Resources[uv].Type == resourceIndex)
					{
						destinationBuffer.Add((uv, info.Color));
						continue;
					}

					// Cell is a vein border if it is flat and adjacent to at least one cell
					// that is also flat and contains veins (borders are not drawn next to slope vein cells)
					var isBorder = map.Ramp[uv] == 0 && Common.Util.ExpandFootprint(uv.ToCPos(map), false).Any(c =>
					{
						if (!map.Resources.Contains(c))
							return false;

						if (veinholeCells.Contains(c))
							return true;

						return map.Resources[c].Type == resourceIndex && map.Ramp[c] == 0;
					});

					if (isBorder)
						destinationBuffer.Add((uv, info.Color));
				}
			}
		}

		public override object Create(ActorInitializer init) { return new TSVeinsRenderer(init.Self, this); }
	}

	public class TSVeinsRenderer : IResourceRenderer, IWorldLoaded, IRenderOverlay, ITickRender, INotifyActorDisposing, IRadarTerrainLayer
	{
		[Flags]
		enum Adjacency : byte
		{
			None = 0x0,
			MinusX = 0x1,
			PlusX = 0x2,
			MinusY = 0x4,
			PlusY = 0x8,
		}

		static readonly Dictionary<Adjacency, int[]> BorderIndices = new()
		{
			{ Adjacency.MinusY, new[] { 3, 4, 5 } },
			{ Adjacency.PlusX, new[] { 6, 7, 8 } },
			{ Adjacency.MinusY | Adjacency.PlusX, new[] { 9, 10, 11 } },
			{ Adjacency.PlusY, new[] { 12, 13, 14 } },
			{ Adjacency.MinusY | Adjacency.PlusY, new[] { 15, 16, 17 } },
			{ Adjacency.PlusY | Adjacency.PlusX, new[] { 18, 19, 20 } },
			{ Adjacency.MinusY | Adjacency.PlusY | Adjacency.PlusX, new[] { 21, 22, 23 } },
			{ Adjacency.MinusX, new[] { 24, 25, 26 } },
			{ Adjacency.MinusX | Adjacency.MinusY, new[] { 27, 28, 29 } },
			{ Adjacency.MinusX | Adjacency.PlusX, new[] { 30, 31, 32 } },
			{ Adjacency.MinusX | Adjacency.PlusX | Adjacency.MinusY, new[] { 33, 34, 35 } },
			{ Adjacency.MinusX | Adjacency.PlusY, new[] { 36, 37, 38 } },
			{ Adjacency.MinusX | Adjacency.MinusY | Adjacency.PlusY, new[] { 39, 40, 41 } },
			{ Adjacency.MinusX | Adjacency.PlusX | Adjacency.PlusY, new[] { 42, 43, 44 } },
			{ Adjacency.MinusX | Adjacency.PlusX | Adjacency.MinusY | Adjacency.PlusY, new[] { 45, 46, 47 } },
		};

		static readonly int[] HeavyIndices = { 48, 49, 50, 51 };
		static readonly int[] LightIndices = { 52 };
		static readonly int[] Ramp1Indices = { 53, 54 };
		static readonly int[] Ramp2Indices = { 55, 56 };
		static readonly int[] Ramp3Indices = { 57, 58 };
		static readonly int[] Ramp4Indices = { 59, 60 };

		readonly TSVeinsRendererInfo info;
		readonly World world;
		readonly IResourceLayer resourceLayer;
		readonly CellLayer<int[]> renderIndices;
		readonly CellLayer<Adjacency> borders;
		readonly HashSet<CPos> dirty = new();
		readonly Queue<CPos> cleanDirty = new();
		readonly HashSet<CPos> veinholeCells = new();
		readonly int maxDensity;
		readonly Color veinRadarColor;

		ISpriteSequence veinSequence;
		PaletteReference veinPalette;
		TerrainSpriteLayer spriteLayer;

		public TSVeinsRenderer(Actor self, TSVeinsRendererInfo info)
		{
			this.info = info;
			world = self.World;

			resourceLayer = self.Trait<IResourceLayer>();
			resourceLayer.CellChanged += AddDirtyCell;
			maxDensity = resourceLayer.GetMaxDensity(info.ResourceType);

			var terrainInfo = self.World.Map.Rules.TerrainInfo;
			resourceLayer.Info.TryGetTerrainType(info.ResourceType, out var terrainType);
			veinRadarColor = terrainInfo.TerrainTypes[terrainInfo.GetTerrainIndex(terrainType)].Color;

			renderIndices = new CellLayer<int[]>(world.Map);
			borders = new CellLayer<Adjacency>(world.Map);
		}

		void AddDirtyCell(CPos cell, string resourceType)
		{
			if (resourceType == null || resourceType == info.ResourceType)
				dirty.Add(cell);
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			// Cache locations of veinhole actors
			// TODO: Add support for monitoring actors placed in the map editor!
			w.ActorAdded += ActorAddedToWorld;
			w.ActorRemoved += ActorRemovedFromWorld;
			foreach (var a in w.Actors)
				ActorAddedToWorld(a);

			veinSequence = w.Map.Sequences.GetSequence(info.Image, info.Sequence);
			veinPalette = wr.Palette(info.Palette);

			var first = veinSequence.GetSprite(0);
			var emptySprite = new Sprite(first.Sheet, Rectangle.Empty, TextureChannel.Alpha);
			spriteLayer = new TerrainSpriteLayer(w, wr, emptySprite, first.BlendMode, wr.World.Type != WorldType.Editor);

			// Initialize the renderIndices with the initial map state so it is visible
			// through the fog with the Explored Map option enabled
			foreach (var cell in w.Map.AllCells)
			{
				var resource = resourceLayer.GetResource(cell);
				var cellIndices = CalculateCellIndices(resource, cell);
				if (cellIndices != null)
				{
					renderIndices[cell] = cellIndices;
					UpdateRenderedSprite(cell, cellIndices);
				}
			}
		}

		int[] CalculateCellIndices(ResourceLayerContents contents, CPos cell)
		{
			if (contents.Type != info.ResourceType || contents.Density == 0)
				return null;

			var ramp = world.Map.Ramp[cell];
			switch (ramp)
			{
				case 1: return Ramp1Indices;
				case 2: return Ramp2Indices;
				case 3: return Ramp3Indices;
				case 4: return Ramp4Indices;
				default: return contents.Density == maxDensity ? HeavyIndices : LightIndices;
			}
		}

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			spriteLayer.Draw(wr.Viewport);
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			foreach (var cell in dirty)
			{
				if (!resourceLayer.IsVisible(cell))
					continue;

				var contents = resourceLayer.GetResource(cell);
				var cellIndices = CalculateCellIndices(contents, cell);
				if (cellIndices != renderIndices[cell])
				{
					renderIndices[cell] = cellIndices;
					UpdateRenderedSprite(cell, cellIndices);
				}

				cleanDirty.Enqueue(cell);
			}

			while (cleanDirty.Count > 0)
				dirty.Remove(cleanDirty.Dequeue());
		}

		bool HasBorder(CPos cell)
		{
			if (!renderIndices.Contains(cell))
				return false;

			// Draw the vein border if this is a flat cell with veins, or a veinhole
			return (world.Map.Ramp[cell] == 0 && renderIndices[cell] != null) || veinholeCells.Contains(cell);
		}

		Adjacency CalculateBorders(CPos cell)
		{
			// Borders are only valid on flat cells
			if (world.Map.Ramp[cell] != 0)
				return Adjacency.None;

			var ret = Adjacency.None;
			if (HasBorder(cell + new CVec(0, -1)))
				ret |= Adjacency.MinusY;

			if (HasBorder(cell + new CVec(-1, 0)))
				ret |= Adjacency.MinusX;

			if (HasBorder(cell + new CVec(1, 0)))
				ret |= Adjacency.PlusX;

			if (HasBorder(cell + new CVec(0, 1)))
				ret |= Adjacency.PlusY;

			return ret;
		}

		void UpdateRenderedSprite(CPos cell, int[] indices)
		{
			borders[cell] = Adjacency.None;
			UpdateSpriteLayers(cell, indices);

			foreach (var c in Common.Util.ExpandFootprint(cell, false))
				UpdateBorderSprite(c);
		}

		void UpdateBorderSprite(CPos cell)
		{
			// Borders are never drawn on ramps or in cells that contain resources
			if (HasBorder(cell) || world.Map.Ramp[cell] != 0)
				return;

			var adjacency = CalculateBorders(cell);
			if (borders[cell] == adjacency)
				return;

			borders[cell] = adjacency;

			if (adjacency == Adjacency.None)
				UpdateSpriteLayers(cell, null);
			else if (BorderIndices.TryGetValue(adjacency, out var indices))
				UpdateSpriteLayers(cell, indices);
			else
				throw new InvalidOperationException($"SpriteMap does not contain an index for Adjacency type '{adjacency}'");
		}

		void UpdateSpriteLayers(CPos cell, int[] indices)
		{
			if (indices != null)
				spriteLayer.Update(cell, veinSequence, veinPalette, indices.Random(world.LocalRandom));
			else
				spriteLayer.Clear(cell);
		}

		void ActorAddedToWorld(Actor a)
		{
			if (info.VeinholeActors.Contains(a.Info.Name))
			{
				foreach (var cell in a.OccupiesSpace.OccupiedCells())
				{
					veinholeCells.Add(cell.Cell);
					AddDirtyCell(cell.Cell, info.ResourceType);
				}
			}
		}

		void ActorRemovedFromWorld(Actor a)
		{
			if (info.VeinholeActors.Contains(a.Info.Name))
			{
				foreach (var cell in a.OccupiesSpace.OccupiedCells())
				{
					veinholeCells.Remove(cell.Cell);
					AddDirtyCell(cell.Cell, null);
				}
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			resourceLayer.CellChanged -= AddDirtyCell;
			world.ActorAdded -= ActorAddedToWorld;
			world.ActorRemoved -= ActorRemovedFromWorld;
		}

		IEnumerable<string> IResourceRenderer.ResourceTypes { get { yield return info.ResourceType; } }

		string IResourceRenderer.GetRenderedResourceType(CPos cell)
		{
			if (renderIndices[cell] != null)
				return info.ResourceType;

			// This makes sure harvesters will get a harvest cursor but then move to the next actual resource cell to start harvesting
			return borders[cell] != Adjacency.None ? info.ResourceType : null;
		}

		string IResourceRenderer.GetRenderedResourceTooltip(CPos cell)
		{
			if (renderIndices[cell] != null)
				return info.Name;

			return borders[cell] != Adjacency.None ? info.Name : null;
		}

		IEnumerable<IRenderable> IResourceRenderer.RenderUIPreview(WorldRenderer wr, string resourceType, int2 origin, float scale)
		{
			if (resourceType != info.ResourceType)
				yield break;

			var sprite = veinSequence.GetSprite(HeavyIndices[0]);
			var palette = wr.Palette(info.Palette);

			yield return new UISpriteRenderable(sprite, WPos.Zero, origin, 0, palette, scale);
		}

		IEnumerable<IRenderable> IResourceRenderer.RenderPreview(WorldRenderer wr, string resourceType, WPos origin)
		{
			if (resourceType != info.ResourceType)
				yield break;

			var frame = HeavyIndices[0];
			var sprite = veinSequence.GetSprite(frame);
			var alpha = veinSequence.GetAlpha(frame);
			var palette = wr.Palette(info.Palette);

			var tintModifiers = veinSequence.IgnoreWorldTint ? TintModifiers.IgnoreWorldTint : TintModifiers.None;
			yield return new SpriteRenderable(sprite, origin, WVec.Zero, 0, palette, veinSequence.Scale, alpha, float3.Ones, tintModifiers, false);
		}

		event Action<CPos> IRadarTerrainLayer.CellEntryChanged
		{
			add
			{
				renderIndices.CellEntryChanged += value;
				borders.CellEntryChanged += value;
			}
			remove
			{
				renderIndices.CellEntryChanged -= value;
				borders.CellEntryChanged -= value;
			}
		}

		bool IRadarTerrainLayer.TryGetTerrainColorPair(MPos uv, out (Color Left, Color Right) value)
		{
			value = default;

			if (borders[uv] == Adjacency.None && renderIndices[uv] == null)
				return false;

			value = (veinRadarColor, veinRadarColor);
			return true;
		}
	}
}
