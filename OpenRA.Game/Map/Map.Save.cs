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
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	public sealed partial class Map
	{
		public void Save(IReadWritePackage toPackage)
		{
			MapFormat = CurrentMapFormat;

			var root = new List<MiniYamlNode>();
			foreach (var field in YamlFields)
				field.Serialize(this, root);

			// HACK: map.yaml is expected to have empty lines between top-level blocks
			for (var i = root.Count - 1; i > 0; i--)
				root.Insert(i, new MiniYamlNode("", ""));

			// Saving to a new package: copy over all the content from the map
			if (Package != null && toPackage != Package)
				foreach (var file in Package.Contents)
					toPackage.Update(file, Package.GetStream(file).ReadAllBytes());

			void UpdatePackage(string filename, byte[] data)
			{
				if (Package != toPackage)
					toPackage.Update(filename, data);
				else
				{
					var stream = Package.GetStream(filename);
					if (stream == null || !Enumerable.SequenceEqual(data, stream.ReadAllBytes()))
						toPackage.Update(filename, data);
				}
			}

			if (!LockPreview)
				UpdatePackage("map.png", SavePreview());

			// Update the package with the new map data
			UpdatePackage("map.yaml", Encoding.UTF8.GetBytes(root.WriteToString()));
			UpdatePackage("map.bin", SaveBinaryData());

			Package = toPackage;

			// Update UID to match the newly saved data
			Uid = ComputeUID(toPackage, MapFormat);
		}

		public byte[] SaveBinaryData()
		{
			var dataStream = new MemoryStream();
			using (var writer = new BinaryWriter(dataStream))
			{
				// Binary data version
				writer.Write(TileFormat);

				// Size
				writer.Write((ushort)MapSize.Width);
				writer.Write((ushort)MapSize.Height);

				// Data offsets
				const int TilesOffset = 17;
				var heightsOffset = Grid.MaximumTerrainHeight > 0 ? 3 * MapSize.Width * MapSize.Height + 17 : 0;
				var resourcesOffset = (Grid.MaximumTerrainHeight > 0 ? 4 : 3) * MapSize.Width * MapSize.Height + 17;

				writer.Write((uint)TilesOffset);
				writer.Write((uint)heightsOffset);
				writer.Write((uint)resourcesOffset);

				// Tile data
				if (TilesOffset != 0)
				{
					for (var i = 0; i < MapSize.Width; i++)
					{
						for (var j = 0; j < MapSize.Height; j++)
						{
							var tile = Tiles[new MPos(i, j)];
							writer.Write(tile.Type);
							writer.Write(tile.Index);
						}
					}
				}

				// Height data
				if (heightsOffset != 0)
					for (var i = 0; i < MapSize.Width; i++)
						for (var j = 0; j < MapSize.Height; j++)
							writer.Write(Height[new MPos(i, j)]);

				// Resource data
				if (resourcesOffset != 0)
				{
					for (var i = 0; i < MapSize.Width; i++)
					{
						for (var j = 0; j < MapSize.Height; j++)
						{
							var tile = Resources[new MPos(i, j)];
							writer.Write(tile.Type);
							writer.Write(tile.Index);
						}
					}
				}
			}

			return dataStream.ToArray();
		}

		public (Color Left, Color Right) GetTerrainColorPair(MPos uv)
		{
			var terrainInfo = Rules.TerrainInfo;
			var type = terrainInfo.GetTerrainInfo(Tiles[uv]);
			var left = type.GetColor(Game.CosmeticRandom);
			var right = type.GetColor(Game.CosmeticRandom);

			if (terrainInfo.MinHeightColorBrightness != 1.0f || terrainInfo.MaxHeightColorBrightness != 1.0f)
			{
				var scale = float2.Lerp(terrainInfo.MinHeightColorBrightness, terrainInfo.MaxHeightColorBrightness, Height[uv] * 1f / Grid.MaximumTerrainHeight);
				left = Color.FromArgb((int)(scale * left.R).Clamp(0, 255), (int)(scale * left.G).Clamp(0, 255), (int)(scale * left.B).Clamp(0, 255));
				right = Color.FromArgb((int)(scale * right.R).Clamp(0, 255), (int)(scale * right.G).Clamp(0, 255), (int)(scale * right.B).Clamp(0, 255));
			}

			return (left, right);
		}

		public byte[] SavePreview()
		{
			var actorTypes = Rules.Actors.Values.Where(a => a.HasTraitInfo<IMapPreviewSignatureInfo>());
			var actors = ActorDefinitions.Where(a => actorTypes.Any(ai => ai.Name == a.Value.Value));
			var positions = new List<(MPos Uv, Color Color)>();
			foreach (var actor in actors)
			{
				var s = new ActorReference(actor.Value.Value, actor.Value);

				var ai = Rules.Actors[actor.Value.Value];
				var impsis = ai.TraitInfos<IMapPreviewSignatureInfo>();
				foreach (var impsi in impsis)
					impsi.PopulateMapPreviewSignatureCells(this, ai, s, positions);
			}

			// ResourceLayer is on world actor, which isn't caught above, so an extra check for it.
			var worldActorInfo = Rules.Actors[SystemActors.World];
			var worldimpsis = worldActorInfo.TraitInfos<IMapPreviewSignatureInfo>();
			foreach (var worldimpsi in worldimpsis)
				worldimpsi.PopulateMapPreviewSignatureCells(this, worldActorInfo, null, positions);

			var isRectangularIsometric = Grid.Type == MapGridType.RectangularIsometric;

			var top = int.MaxValue;
			var bottom = int.MinValue;

			if (Grid.MaximumTerrainHeight > 0)
			{
				(top, bottom) = GetCellSpaceBounds();

				if (top == int.MaxValue || bottom == int.MinValue)
					throw new InvalidDataException("The map has invalid boundaries");
			}
			else
			{
				// If the mod uses flat maps, MPos == PPos and we can take the bounds rect directly
				top = Bounds.Top;
				bottom = Bounds.Bottom;
			}

			var width = Bounds.Width;
			var height = bottom - top;

			var bitmapWidth = width;
			if (isRectangularIsometric)
				bitmapWidth = 2 * bitmapWidth - 1;

			var stride = bitmapWidth * 4;
			const int PxStride = 4;
			var minimapData = new byte[stride * height];
			(Color Left, Color Right) terrainColor = default;

			var colorsByPosition = positions
				.GroupBy(p => p.Uv)
				.ToDictionary(g => g.Key, g => g.First().Color);
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
				{
					var uv = new MPos(x + Bounds.Left, y + top);

					// TryGetValue will return Color.Transparent if not found
					colorsByPosition.TryGetValue(uv, out var actorColor);
					if (actorColor.A == 0)
						terrainColor = GetTerrainColorPair(uv);

					if (isRectangularIsometric)
					{
						// Odd rows are shifted right by 1px
						var dx = uv.V & 1;
						var xOffset = PxStride * (2 * x + dx);
						if (x + dx > 0)
						{
							var z = y * stride + xOffset - PxStride;
							var c = actorColor.A == 0 ? terrainColor.Left : actorColor;
							minimapData[z++] = c.R;
							minimapData[z++] = c.G;
							minimapData[z++] = c.B;
							minimapData[z] = c.A;
						}

						if (xOffset < stride)
						{
							var z = y * stride + xOffset;
							var c = actorColor.A == 0 ? terrainColor.Right : actorColor;
							minimapData[z++] = c.R;
							minimapData[z++] = c.G;
							minimapData[z++] = c.B;
							minimapData[z] = c.A;
						}
					}
					else
					{
						var z = y * stride + PxStride * x;
						var c = actorColor.A == 0 ? terrainColor.Left : actorColor;
						minimapData[z++] = c.R;
						minimapData[z++] = c.G;
						minimapData[z++] = c.B;
						minimapData[z] = c.A;
					}
				}
			}

			var png = new Png(minimapData, SpriteFrameType.Rgba32, bitmapWidth, height);
			return png.Save();
		}

		public (int Top, int Bottom) GetCellSpaceBounds()
		{
			var top = int.MaxValue;
			var bottom = int.MinValue;

			// The minimap is drawn in cell space, so we need to
			// unproject the PPos bounds to find the MPos boundaries.
			// This matches the calculation in RadarWidget that is used ingame
			for (var x = Bounds.Left; x < Bounds.Right; x++)
			{
				var allTop = Unproject(new PPos(x, Bounds.Top));
				var allBottom = Unproject(new PPos(x, Bounds.Bottom));
				if (allTop.Count > 0)
					top = Math.Min(top, allTop.MinBy(uv => uv.V).V);

				if (allBottom.Count > 0)
					bottom = Math.Max(bottom, allBottom.MaxBy(uv => uv.V).V);
			}

			return (top, bottom);
		}
	}
}
