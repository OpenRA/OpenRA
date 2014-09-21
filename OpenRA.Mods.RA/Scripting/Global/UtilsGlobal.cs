#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using Eluant;
using OpenRA.Scripting;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptGlobal("Utils")]
	public class UtilsGlobal : ScriptGlobal
	{
		public UtilsGlobal(ScriptContext context) : base(context) { }

		[Desc("Calls a function on every value in table.")]
		public void Do(LuaTable table, LuaFunction func)
		{
			foreach (var kv in table)
				func.Call(kv.Value).Dispose();
		}

		[Desc("Returns true if func returns true for any value in table.")]
		public bool Any(LuaTable table, LuaFunction func)
		{
			foreach (var kv in table)
			{
				using (var ret = func.Call(kv.Value))
				{
					var result = ret.FirstOrDefault();
					if (result != null && result.ToBoolean())
						return true;
				}
			}

			return false;
		}

		[Desc("Returns true if func returns true for all values in table.")]
		public bool All(LuaTable table, LuaFunction func)
		{
			foreach (var kv in table)
			{
				using (var ret = func.Call(kv.Value))
				{
					var result = ret.FirstOrDefault();
					if (result == null || !result.ToBoolean())
						return false;
				}
			}

			return true;
		}

		[Desc("Skips over the first numElements members of the array and returns the rest.")]
		public LuaTable Skip(LuaTable table, int numElements)
		{
			var t = context.CreateTable();

			for (var i = numElements; i <= table.Count; i++)
				t.Add(t.Count + 1, table[i]);

			return t;
		}

		[Desc("Returns a random value from table.")]
		public LuaValue Random(LuaTable table)
		{
			return table.Values.Random<LuaValue>(context.World.SharedRandom);
		}

		[Desc("Expands the given footprint one step along the coordinate axes, and (if requested) diagonals.")]
		public LuaTable ExpandFootprint(LuaTable cells, bool allowDiagonal)
		{
			var footprint = cells.Values.Select(c =>
			{
				CPos cell;
				if (!c.TryGetClrValue<CPos>(out cell))
					throw new LuaException("ExpandFootprint only accepts a table of CPos");

				return cell;
			});

			var expanded = Traits.Util.ExpandFootprint(footprint, allowDiagonal);
			return expanded.ToLuaTable(context);
		}

		[Desc("Returns a random integer x in the range low &lt;= x &lt; high.")]
		public int RandomInteger(int low, int high)
		{
			if (high <= low)
				return low;

			return context.World.SharedRandom.Next(low, high);
		}

		[Desc("Returns the center of a cell in world coordinates.")]
		public WPos CenterOfCell(CPos cell)
		{
			return context.World.Map.CenterOfCell(cell);
		}

		[Desc("Converts the number of seconds into game time (ticks).")]
		public int Seconds(int seconds)
		{
			return seconds * 25;
		}

		[Desc("Converts the number of minutes into game time (ticks).")]
		public int Minutes(int minutes)
		{
			return Seconds(minutes * 60);
		}
	}
}
