using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	public interface IMap : IDisposable
	{
		Dictionary<CPos, TerrainTile> ReplacedInvalidTerrainTiles { get; }
		MapGrid Grid { get; }
		int2 MapSize { get; }
		string Tileset { get; }
		Ruleset Rules { get; }
		SequenceSet Sequences { get; }
		CellRegion AllCells { get; }
		List<CPos> AllEdgeCells { get; }
		WDist CellHeightStep { get; }
		CellLayer<byte> CustomTerrain { get; }
		WPos ProjectedBottomRight { get; }
		PPos[] ProjectedCells { get; }
		WPos ProjectedTopLeft { get; }
		Rectangle Bounds { get; }
		int MapFormat { get; }
		byte GetTerrainIndex(CPos cell);
		TerrainTypeInfo GetTerrainInfo(CPos cell);
		void NewSize(Size size, ITerrainInfo terrainInfo);
		WVec Offset(CVec delta, int dz);
		WAngle FacingBetween(CPos cell, CPos towards, WAngle fallbackfacing);
		IEnumerable<CPos> FindTilesInAnnulus(CPos center, int minRange, int maxRange, bool allowOutsideBounds = false);
		IEnumerable<CPos> FindTilesInCircle(CPos center, int maxRange, bool allowOutsideBounds = false);
		CPos CellContaining(WPos pos);
		PPos ProjectedCellCovering(WPos pos);
		WDist DistanceAboveTerrain(WPos pos);
		WPos CenterOfCell(CPos cell);
		WPos CenterOfSubCell(CPos cell, SubCell subCell);
		CPos ChooseClosestEdgeCell(CPos cell);
		MPos ChooseClosestEdgeCell(MPos uv);
		CPos ChooseClosestMatchingEdgeCell(CPos cell, Func<CPos, bool> match);
		CPos ChooseRandomCell(MersenneTwister rand);
		CPos ChooseRandomEdgeCell(MersenneTwister rand);
		CPos Clamp(CPos cell);
		MPos Clamp(MPos uv);
		PPos Clamp(PPos puv);
		bool Contains(CPos cell);
		bool Contains(MPos uv);
		bool Contains(PPos puv);
		WDist DistanceToEdge(WPos pos, in WVec dir);
		PPos[] ProjectedCellsCovering(MPos uv);

		event Action<CPos> CellProjectionChanged;
		byte ProjectedHeight(PPos puv);
		void Resize(int width, int height);
		void SetBounds(PPos tl, PPos br);
		WRot TerrainOrientation(CPos cell);
		List<MPos> Unproject(PPos puv);
		(Color Left, Color Right) GetTerrainColorPair(MPos uv);
		byte[] SavePreview();
	}

	public interface IMapElevation : IMap
	{
		CellLayer<byte> Ramp { get; }
		CellLayer<byte> Height { get; }
	}

	public interface IMapTiles : IMap
	{
		CellLayer<TerrainTile> Tiles { get; }
	}

	public interface IMapResource : IMap
	{
		CellLayer<ResourceTile> Resources { get; }
	}
}
