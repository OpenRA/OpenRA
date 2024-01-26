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
using Newtonsoft.Json;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;
using Color = OpenRA.Primitives.Color;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	public class MarkerLayerOverlayInfo : TraitInfo
	{
		[Desc("A list of colors to be used for drawing.")]
		public readonly Color[] Colors = new[]
		{
			Color.FromArgb(255, 0, 0),
			Color.FromArgb(255, 127, 0),
			Color.FromArgb(255, 238, 70),
			Color.FromArgb(0, 255, 33),
			Color.FromArgb(0, 255, 255),
			Color.FromArgb(0, 42, 255),
			Color.FromArgb(165, 0, 255),
			Color.FromArgb(255, 0, 220),
		};

		[Desc("Default alpha blend.")]
		public readonly int Alpha = 85;

		[Desc("Color of the axis angle display.")]
		public readonly Color AxisAngleColor = Color.Crimson;

		public override object Create(ActorInitializer init)
		{
			return new MarkerLayerOverlay(init.Self, this);
		}
	}

	public class MarkerLayerOverlay : IRenderAnnotations, INotifyActorDisposing, IWorldLoaded
	{
		public class MarkerLayerFile
		{
			public Dictionary<int, List<int>> Tiles { get; set; }
			public MarkerTileMirrorMode MirrorMode { get; set; }
			public int NumSides { get; set; }
			public int AxisAngle { get; set; }
			public int TileAlpha { get; set; }
		}

		const double DegreesToRadians = Math.PI / 180;

		readonly int[] validFlipModeSides = { 2, 4 };

		public enum MarkerTileMirrorMode
		{
			None,
			Flip,
			Rotate
		}

		readonly World world;
		readonly WPos mapCenter;
		readonly Color[] alphaBlendColors;

		public readonly CellLayer<int?> CellLayer;
		public readonly Dictionary<int, HashSet<CPos>> Tiles = new();

		public bool Enabled = true;
		public MarkerTileMirrorMode MirrorMode { get; private set; } = MarkerTileMirrorMode.None;
		public MarkerLayerOverlayInfo Info { get; }
		public int NumSides = 2;
		public int AxisAngle;
		public int TileAlpha
		{
			get => tileAlpha;
			set
			{
				tileAlpha = value;
				UpdateTileAlpha();
			}
		}

		int tileAlpha;
		bool disposed;

		public MarkerLayerOverlay(Actor self, MarkerLayerOverlayInfo info)
		{
			Info = info;
			world = self.World;
			var map = self.World.Map;

			tileAlpha = info.Alpha;
			alphaBlendColors = new Color[info.Colors.Length];
			UpdateTileAlpha();

			CellLayer = new CellLayer<int?>(map);

			mapCenter = GetMapCenterWPos();
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			try
			{
				var modData = Game.ModData;
				var mod = modData.Manifest.Metadata;
				var directory = Path.Combine(Platform.SupportDir, "Editor", modData.Manifest.Id, mod.Version, "MarkerTiles");
				if (!Directory.Exists(directory))
					return;

				if (string.IsNullOrWhiteSpace(world.Map.Package.Name))
					return;

				var markerTileFilename = $"{Path.GetFileNameWithoutExtension(world.Map.Package.Name)}.json";
				var markerTilePath = Path.Combine(directory, markerTileFilename);
				if (!File.Exists(markerTilePath))
					return;

				using (var streamReader = new StreamReader(markerTilePath))
				{
					var content = streamReader.ReadToEnd();
					var file = JsonConvert.DeserializeObject<MarkerLayerFile>(content);

					TileAlpha = file.TileAlpha;
					MirrorMode = file.MirrorMode;
					NumSides = file.NumSides;
					AxisAngle = file.AxisAngle;

					var savedTilesHashSetDictionary = file.Tiles.ToDictionary(x => x.Key, x => x.Value.Select(bits => new CPos(bits)).ToHashSet());
					SetAll(savedTilesHashSetDictionary);
				}
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to load map editor marker tiles.");
				Log.Write("debug", e);
			}
		}

		public MarkerLayerFile ToFile()
		{
			var tilesBitsDictionary = Tiles.ToDictionary(x => x.Key, x => x.Value.Select(cpos => cpos.Bits).ToList());
			return new MarkerLayerFile
			{
				Tiles = tilesBitsDictionary,
				TileAlpha = TileAlpha,
				MirrorMode = MirrorMode,
				NumSides = NumSides,
				AxisAngle = AxisAngle,
			};
		}

		void UpdateTileAlpha()
		{
			for (var i = 0; i < Info.Colors.Length; i++)
				alphaBlendColors[i] = Color.FromArgb(tileAlpha, Info.Colors[i]);
		}

		public void ClearSelected(int tileType)
		{
			if (Tiles.TryGetValue(tileType, out var set))
				foreach (var pos in set)
					SetTile(pos, null);
		}

		public void ClearAll()
		{
			foreach (var position in Tiles.SelectMany(x => x.Value))
				CellLayer[position] = null;

			Tiles.Clear();
		}

		public void SetAll(Dictionary<int, HashSet<CPos>> newTiles)
		{
			ClearAll();

			foreach (var type in newTiles)
			{
				var set = new HashSet<CPos>();
				Tiles.Add(type.Key, set);

				foreach (var position in type.Value)
				{
					if (!world.Map.Contains(position))
						continue;

					set.Add(position);
					CellLayer[position] = type.Key;
				}
			}
		}

		public void SetSelected(int tile, HashSet<CPos> newTiles)
		{
			var type = Tiles[tile];
			foreach (var pos in type)
				SetTile(pos, null);

			type.Clear();

			foreach (var pos in newTiles)
			{
				type.Add(pos);
				CellLayer[pos] = tile;
			}
		}

		public void SetMirrorMode(MarkerTileMirrorMode mirrorMode)
		{
			MirrorMode = mirrorMode;

			if (mirrorMode == MarkerTileMirrorMode.Flip && !validFlipModeSides.Contains(NumSides))
				NumSides = validFlipModeSides[0];
		}

		WPos GetMapCenterWPos()
		{
			var map = world.Map;

			var boundsWidth = map.AllCells.BottomRight.X - map.AllCells.TopLeft.X;
			var boundsHeight = map.AllCells.BottomRight.Y - map.AllCells.TopLeft.Y;

			var xIsOdd = boundsWidth % 2 != 0;
			var yIsOdd = boundsHeight % 2 != 0;

			var xCenter = boundsWidth / 2;
			var yCenter = boundsHeight / 2;

			var centerWpos = map.CenterOfCell(new CPos(xCenter, yCenter));
			if (xIsOdd)
				centerWpos += new WVec(512, 0, 0);

			if (yIsOdd)
				centerWpos += new WVec(0, 512, 0);

			return centerWpos;
		}

		public CPos[] CalculateMirrorPositions(CPos cell)
		{
			const int DegreesInCircle = 360;

			var map = world.Map;

			var wpos = map.CenterOfCell(cell);
			var wposVec = wpos - mapCenter;
			var angle = DegreesInCircle / NumSides;

			var targets = new List<CPos>();

			if (map.Contains(cell))
				targets.Add(cell);

			if (MirrorMode == MarkerTileMirrorMode.Flip)
			{
				var startAxis = new WVec(1024, 0, 0);
				var axes = new List<WVec>();
				for (var i = 0; i < NumSides / 2; i++)
				{
					var targetAngle = (i * angle + AxisAngle) * DegreesToRadians;
					var point = new WVec((int)(startAxis.X * Math.Cos(targetAngle) - startAxis.Y * Math.Sin(targetAngle)),
						(int)(startAxis.X * Math.Sin(targetAngle) + startAxis.Y * Math.Cos(targetAngle)),
						wpos.Z);

					axes.Add(point);
				}

				foreach (var axis in axes)
				{
					var point = GetAxisMirrorPoint(mapCenter, axis, wpos);
					var cellPoint = map.CellContaining(point);

					if (map.Contains(cellPoint))
						targets.Add(cellPoint);
				}

				// Mirror twice for both
				if (axes.Count == 2)
				{
					var point = GetAxisMirrorPoint(mapCenter, axes[0], wpos);
					point = GetAxisMirrorPoint(mapCenter, axes[1], point);
					var cellPoint = map.CellContaining(point);

					if (map.Contains(cellPoint))
						targets.Add(cellPoint);
				}

				///////////////

				static WPos GetAxisMirrorPoint(WPos center, WVec axis, WPos point)
				{
					var testPoint = center - new WVec(point.X, point.Y, 0);
					var a = axis.Y;
					var b = -axis.X;
					var c = -a * 0 - b * 0;

					var m = Math.Sqrt(a * a + b * b);
					var aDash = a / m;
					var bDash = b / m;
					var cDash = c / m;

					var d = aDash * testPoint.X + bDash * testPoint.Y + cDash;
					var pxDash = testPoint.X - 2 * aDash * d;
					var pyDash = testPoint.Y - 2 * bDash * d;

					return new WPos((int)pxDash + center.X, (int)pyDash + center.Y, 0);
				}
			}
			else if (MirrorMode == MarkerTileMirrorMode.Rotate)
			{
				// Rotate
				var flipAngleRadians = DegreesToRadians * angle;

				var sidesAreEven = NumSides % 2 == 0;
				var oddSideStartIndex = (int)Math.Floor((double)NumSides / 2);
				var startIndex = sidesAreEven ? 0 : -oddSideStartIndex;
				var count = sidesAreEven ? NumSides : oddSideStartIndex + 1;

				for (var i = startIndex; i < count; i++)
				{
					var targetAngle = i * flipAngleRadians;
					var point = new WPos((int)(wposVec.X * Math.Cos(targetAngle) - wposVec.Y * Math.Sin(targetAngle)),
						(int)(wposVec.X * Math.Sin(targetAngle) + wposVec.Y * Math.Cos(targetAngle)),
						wpos.Z);

					var cellPoint = map.CellContaining(point + new WVec(mapCenter.X, mapCenter.Y, 0));

					if (map.Contains(cellPoint))
						targets.Add(cellPoint);
				}
			}

			return targets.ToArray();
		}

		public void SetTile(CPos target, int? tileType)
		{
			if (!world.Map.Contains(target))
				return;

			// Maintain map of tile types for selective clearing
			var prevTile = CellLayer[target];
			if (prevTile.HasValue && Tiles.TryGetValue(prevTile.Value, out var set))
				set.Remove(target);

			if (tileType.HasValue)
			{
				if (Tiles.TryGetValue(tileType.Value, out set))
					set.Add(target);
				else
					Tiles.Add(tileType.Value, new HashSet<CPos> { target });

				CellLayer[target] = tileType;
			}
			else
				CellLayer[target] = null;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			disposed = true;
		}

		readonly struct MapLine
		{
			public readonly float2 Start;
			public readonly float2 End;

			public MapLine(float2 start, float2 end)
			{
				Start = start;
				End = end;
			}
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				yield break;

			foreach (var cellPair in Tiles)
				foreach (var cellPos in cellPair.Value)
					yield return new MarkerTileRenderable(cellPos, alphaBlendColors[cellPair.Key]);

			if (MirrorMode != MarkerTileMirrorMode.Flip)
				yield break;

			const int LineWidth = 1;

			var color = Info.AxisAngleColor;
			var targetAngle = AxisAngle * DegreesToRadians;

			var mapCenterFloat = new float2(mapCenter.X, mapCenter.Y);
			var mapBoundsWorldSize = mapCenterFloat * 2;

			// Create our axis lines
			var horizontalVec = new float2(1, 0);
			var verticalVec = new float2(0, 1);
			var edges = new[]
			{
				new MapLine(mapCenterFloat, mapCenterFloat + horizontalVec),
				new MapLine(mapCenterFloat, mapCenterFloat + verticalVec),
			};

			var sourceAxes = new[] { verticalVec, -verticalVec, horizontalVec, -horizontalVec };
			for (var i = 0; i < NumSides; i++)
			{
				var isOpposite = i % 2 != 0;
				var sourceAxis = sourceAxes[i];
				var rotatedAxis = new float2(
					(float)(sourceAxis.X * Math.Cos(targetAngle) - sourceAxis.Y * Math.Sin(targetAngle)),
					(float)(sourceAxis.X * Math.Sin(targetAngle) + sourceAxis.Y * Math.Cos(targetAngle)));

				var axisLine = new MapLine(float2.Zero, rotatedAxis);
				var collisionPoints = FindEdgeCollisionPoints(edges, axisLine);

				var closestCollisionPoint = collisionPoints.OrderBy(x => x.LengthSquared).First();
				if (isOpposite)
					closestCollisionPoint *= -1;

				var resultPos = new WVec((int)closestCollisionPoint.X, (int)closestCollisionPoint.Y, 0);
				yield return new LineAnnotationRenderable(mapCenter, mapCenter + resultPos, LineWidth, color, color);
			}
		}

		static float2[] FindEdgeCollisionPoints(MapLine[] mapEdges, MapLine axis)
		{
			var collisionResults = new List<float2>();
			foreach (var mapEdge in mapEdges)
				if (FindIntersection(axis.Start, axis.End, mapEdge.Start, mapEdge.End, out var collisionVec))
					collisionResults.Add(collisionVec);

			return collisionResults.ToArray();
		}

		static bool FindIntersection(float2 a1, float2 a2, float2 b1, float2 b2, out float2 result)
		{
			result = float2.Zero;
			var d = (a1.X - a2.X) * (b1.Y - b2.Y) - (a1.Y - a2.Y) * (b1.X - b2.X);

			// check if lines are parallel
			if (d == 0)
				return false;

			var px = (a1.X * a2.Y - a1.Y * a2.X) * (b1.X - b2.X) - (a1.X - a2.X) * (b1.X * b2.Y - b1.Y * b2.X);
			var py = (a1.X * a2.Y - a1.Y * a2.X) * (b1.Y - b2.Y) - (a1.Y - a2.Y) * (b1.X * b2.Y - b1.Y * b2.X);

			result = new float2(px, py) / d;
			return true;
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
