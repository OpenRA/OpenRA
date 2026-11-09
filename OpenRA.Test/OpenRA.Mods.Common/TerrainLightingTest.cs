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
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Test
{
	[TestFixture(MapGridType.Rectangular)]
	[TestFixture(MapGridType.RectangularIsometric)]
	sealed class TerrainLightingTest(MapGridType gridType)
	{
		Map map;
		TerrainLighting lighting;
		TerrainLightingInfo info;
		WPos position;

		[SetUp]
		public void SetUp()
		{
			// Lighting needs only map geometry, not a renderer, ruleset or game assets.
			map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
			var size = new Size(4, 4);
			var grid = new MapGrid(new MiniYaml(null, [new MiniYamlNode("Type", gridType.ToString())]));
			typeof(Map).GetField(nameof(Map.Grid)).SetValue(map, grid);
			typeof(Map).GetProperty(nameof(Map.MapSize)).SetValue(map, size);
			typeof(Map).GetProperty(nameof(Map.Height)).SetValue(map, new CellLayer<byte>(gridType, size));
			typeof(Map).GetProperty(nameof(Map.Ramp)).SetValue(map, new CellLayer<byte>(gridType, size));
			typeof(Map).GetProperty(nameof(Map.AllCells)).SetValue(map,
				new CellRegion(gridType, new MPos(0, 0), new MPos(3, 3)));
			var world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
			typeof(World).GetField(nameof(World.Map)).SetValue(world, map);

			info = new TerrainLightingInfo();
			FieldLoader.LoadFieldOrProperty(info, nameof(TerrainLightingInfo.Intensity), "0.8");
			FieldLoader.LoadFieldOrProperty(info, nameof(TerrainLightingInfo.HeightStep), "0.1");
			FieldLoader.LoadFieldOrProperty(info, nameof(TerrainLightingInfo.RedTint), "0.9");
			FieldLoader.LoadFieldOrProperty(info, nameof(TerrainLightingInfo.GreenTint), "0.8");
			FieldLoader.LoadFieldOrProperty(info, nameof(TerrainLightingInfo.BlueTint), "0.7");
			lighting = new TerrainLighting(world, info);
			var uv = new MPos(1, 1);
			map.Height[uv] = 2;
			position = map.CenterOfCell(uv.ToCPos(map));
		}

		Vector3 TintAt(WPos pos) => ((ITerrainLighting)lighting).TintAt(pos);

		static void AssertTint(Vector3 actual, Vector3 expected)
		{
			Assert.That(actual.X, Is.EqualTo(expected.X).Within(0.00001f));
			Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(0.00001f));
			Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(0.00001f));
		}

		[Test]
		public void InitialLightingUsesRulesAndHeight()
		{
			Assert.That(lighting.GlobalIntensity, Is.EqualTo(info.Intensity));
			AssertTint(lighting.GlobalTint, new Vector3(0.9f, 0.8f, 0.7f));
			AssertTint(TintAt(position), new Vector3(0.9f, 0.8f, 0.7f));
		}

		[Test]
		public void GlobalChangePreservesHeightAndLocalLights()
		{
			var localTint = new Vector3(0.2f, 0.3f, 0.4f);
			var token = lighting.AddLightSource(position, WDist.FromCells(3), 0.25f, localTint);
			var tint = new Vector3(0.4f, 0.6f, 0.8f);
			lighting.SetGlobalLighting(0.5f, tint);
			AssertTint(TintAt(position), 0.95f * (tint + localTint));
			lighting.RemoveLightSource(token);
			AssertTint(TintAt(position), 0.7f * tint);
		}

		[Test]
		public void ChangeNotifiesEveryStoredCellAfterUpdatingState()
		{
			var changed = new List<MPos>();
			var tint = new Vector3(0.4f, 0.6f, 0.8f);
			lighting.CellChanged += uv =>
			{
				changed.Add(uv);
				AssertTint(TintAt(position), 0.7f * tint);
			};
			lighting.SetGlobalLighting(0.5f, tint);
			Assert.That(changed, Is.EquivalentTo(map.AllCells.MapCoords.ToArray()));
			Assert.That(changed, Has.Count.EqualTo(16));
		}

		[Test]
		public void UnchangedValuesDoNotInvalidateCells()
		{
			var changed = 0;
			lighting.CellChanged += _ => changed++;
			lighting.SetGlobalLighting(lighting.GlobalIntensity, lighting.GlobalTint);
			Assert.That(changed, Is.Zero);
		}

		[Test]
		public void IntensityOnlyChangeInvalidatesCells()
		{
			var changed = 0;
			lighting.CellChanged += _ => changed++;
			lighting.SetGlobalLighting(0.5f, lighting.GlobalTint);
			Assert.That(changed, Is.EqualTo(16));
			AssertTint(TintAt(position), 0.7f * lighting.GlobalTint);
		}

		[Test]
		public void TintOnlyChangeInvalidatesCells()
		{
			var changed = 0;
			lighting.CellChanged += _ => changed++;
			lighting.SetGlobalLighting(lighting.GlobalIntensity, Vector3.One);
			Assert.That(changed, Is.EqualTo(16));
			AssertTint(TintAt(position), Vector3.One);
		}

		[Test]
		public void OutOfMapQueryKeepsTintOnlyFallback()
		{
			lighting.SetGlobalLighting(0.5f, Vector3.One);
			AssertTint(TintAt(new WPos(100000, 100000, 0)), Vector3.One);
		}
	}
}
