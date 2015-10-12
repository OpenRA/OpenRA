#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA
{
	public class GameSpeed
	{
		[Translate]
		public readonly string Name = "Default";
		public readonly int Timestep = 40;
		public readonly int OrderLatency = 3;
	}

	public class GameSpeeds : IGlobalModData
	{
		[FieldLoader.LoadUsing("LoadSpeeds")]
		public readonly Dictionary<string, GameSpeed> Speeds;

		static object LoadSpeeds(MiniYaml y)
		{
			var ret = new Dictionary<string, GameSpeed>();
			foreach (var node in y.Nodes)
				ret.Add(node.Key, FieldLoader.Load<GameSpeed>(node.Value));

			return ret;
		}
	}
}
