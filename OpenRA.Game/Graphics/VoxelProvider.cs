#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Graphics
{
	public static class VoxelProvider
	{
		static Dictionary<string, Dictionary<string, Voxel>> units;

		public static void Initialize(string[] voxelFiles, List<MiniYamlNode> voxelNodes)
		{
			units = new Dictionary<string, Dictionary<string, Voxel>>();

			var sequences = voxelFiles
				.Select(s => MiniYaml.FromFile(s))
				.Aggregate(voxelNodes, MiniYaml.MergeLiberal);

			foreach (var s in sequences)
				LoadVoxelsForUnit(s.Key, s.Value);

			Game.modData.VoxelLoader.RefreshBuffer();
		}

		static Voxel LoadVoxel(string unit, MiniYaml info)
		{
			var vxl = unit;
			var hva = unit;
			if (info.Value != null)
			{
				var fields = info.Value.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
				if (fields.Length >= 1)
					vxl = hva = fields[0].Trim();

				if (fields.Length >= 2)
					hva = fields[1].Trim();
			}

			return Game.modData.VoxelLoader.Load(vxl, hva);
		}

		static void LoadVoxelsForUnit(string unit, MiniYaml sequences)
		{
			Game.modData.LoadScreen.Display();
			try
			{
				var seq = sequences.ToDictionary(my => LoadVoxel(unit, my));
				units.Add(unit, seq);
			}
			catch (FileNotFoundException) {} // Do nothing; we can crash later if we actually wanted art
		}

		public static Voxel GetVoxel(string unitName, string voxelName)
		{
			try { return units[unitName][voxelName]; }
			catch (KeyNotFoundException)
			{
				if (units.ContainsKey(unitName))
					throw new InvalidOperationException(
						"Unit `{0}` does not have a voxel `{1}`".F(unitName, voxelName));
				else
					throw new InvalidOperationException(
						"Unit `{0}` does not have any voxels defined.".F(unitName));
			}
		}

		public static bool HasVoxel(string unit, string seq)
		{
			if (!units.ContainsKey(unit))
				throw new InvalidOperationException(
					"Unit `{0}` does not have any voxels defined.".F(unit));

			return units[unit].ContainsKey(seq);
		}
	}
}
