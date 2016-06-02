#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("CPos")]
	public class CPosGlobal : ScriptGlobal
	{
		public CPosGlobal(ScriptContext context) : base(context) { }

		[Desc("Create a new CPos with the specified coordinates.")]
		public CPos New(int x, int y) { return new CPos(x, y); }

		[Desc("The cell coordinate origin.")]
		public CPos Zero { get { return CPos.Zero; } }
	}

	[ScriptGlobal("CVec")]
	public class CVecGlobal : ScriptGlobal
	{
		public CVecGlobal(ScriptContext context) : base(context) { }

		[Desc("Create a new CVec with the specified coordinates.")]
		public CVec New(int x, int y) { return new CVec(x, y); }

		[Desc("The cell zero-vector.")]
		public CVec Zero { get { return CVec.Zero; } }
	}

	[ScriptGlobal("WPos")]
	public class WPosGlobal : ScriptGlobal
	{
		public WPosGlobal(ScriptContext context) : base(context) { }

		[Desc("Create a new WPos with the specified coordinates.")]
		public WPos New(int x, int y, int z) { return new WPos(x, y, z); }

		[Desc("The world coordinate origin.")]
		public WPos Zero { get { return WPos.Zero; } }
	}

	[ScriptGlobal("WVec")]
	public class WVecGlobal : ScriptGlobal
	{
		public WVecGlobal(ScriptContext context) : base(context) { }

		[Desc("Create a new WVec with the specified coordinates.")]
		public WVec New(int x, int y, int z) { return new WVec(x, y, z); }

		[Desc("The world zero-vector.")]
		public WVec Zero { get { return WVec.Zero; } }
	}

	[ScriptGlobal("WDist")]
	public class WDistGlobal : ScriptGlobal
	{
		public WDistGlobal(ScriptContext context) : base(context) { }

		[Desc("Create a new WDist.")]
		public WDist New(int r) { return new WDist(r); }

		[Desc("Create a new WDist by cell distance.")]
		public WDist FromCells(int numCells) { return WDist.FromCells(numCells); }
	}

	[ScriptGlobal("WRange")]
	public class WRangeGlobal : ScriptGlobal
	{
		public WRangeGlobal(ScriptContext context) : base(context) { }

		[Desc("Create a new WRange. DEPRECATED! Will be removed.")]
		public WDist New(int r) { Game.Debug("WRange is deprecated. Use WDist instead."); return new WDist(r); }

		[Desc("Create a new WRange by cell distance. DEPRECATED! Will be removed.")]
		public WDist FromCells(int numCells) { Game.Debug("WRange is deprecated. Use WDist instead."); return WDist.FromCells(numCells); }
	}
}
