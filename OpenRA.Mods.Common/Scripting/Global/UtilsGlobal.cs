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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Utils")]
	public class UtilsGlobal : ScriptGlobal
	{
		public UtilsGlobal(ScriptContext context) : base(context) { }

		[Desc("Calls a function on every element in a collection.")]
		public void Do(LuaValue[] collection, LuaFunction func)
		{
			foreach (var c in collection)
				func.Call(c).Dispose();
		}

		[Desc("Returns true if func returns true for any element in a collection.")]
		public bool Any(LuaValue[] collection, LuaFunction func)
		{
			foreach (var c in collection)
			{
				using (var ret = func.Call(c))
				{
					var result = ret.FirstOrDefault();
					if (result != null && result.ToBoolean())
						return true;
				}
			}

			return false;
		}

		[Desc("Returns true if func returns true for all elements in a collection.")]
		public bool All(LuaValue[] collection, LuaFunction func)
		{
			foreach (var c in collection)
			{
				using (var ret = func.Call(c))
				{
					var result = ret.FirstOrDefault();
					if (result == null || !result.ToBoolean())
						return false;
				}
			}

			return true;
		}

		[Desc("Skips over the first numElements members of a table and return the rest.")]
		public LuaTable Skip(LuaTable table, int numElements)
		{
			var t = context.CreateTable();

			for (var i = numElements; i <= table.Count; i++)
				t.Add(t.Count + 1, table[i]);

			return t;
		}

		[Desc("Returns a random value from a collection.")]
		public LuaValue Random(LuaValue[] collection)
		{
			return collection.Random(context.World.SharedRandom);
		}

		[Desc("Expands the given footprint one step along the coordinate axes, and (if requested) diagonals.")]
		public CPos[] ExpandFootprint(CPos[] footprint, bool allowDiagonal)
		{
			return Util.ExpandFootprint(footprint, allowDiagonal).ToArray();
		}

		[Desc("Returns a random integer x in the range low &lt;= x &lt; high.")]
		public int RandomInteger(int low, int high)
		{
			if (high <= low)
				return low;

			return context.World.SharedRandom.Next(low, high);
		}
	}
}
