#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Effects;
using System;

namespace OpenRA.GameRules
{
	public class ProjectileArgs
	{
		public NewWeaponInfo weapon;
		public Actor firedBy;
		public int2 offset;
		public int srcAltitude;
		public int facing;
		public Actor target;
		public int2 dest;
		public int destAltitude;
	}

	public interface IProjectileInfo
	{
		IEffect Create(ProjectileArgs args);
	}

	public class NewWeaponInfo
	{
		public readonly float Range = 0;
		public readonly string Report = null;
		public readonly int ROF = 1;
		public readonly int Burst = 1;
		public readonly bool Charges = false;

		public IProjectileInfo Projectile;
		public List<WarheadInfo> Warheads = new List<WarheadInfo>();

		public NewWeaponInfo(string name, MiniYaml content)
		{
			foreach (var kv in content.Nodes)
			{
				var key = kv.Key.Split('@')[0];
				switch (key)
				{
					case "Range": FieldLoader.LoadField(this, "Range", content.Nodes["Range"].Value); break;
					case "ROF": FieldLoader.LoadField(this, "ROF", content.Nodes["ROF"].Value); break;
					case "Report": FieldLoader.LoadField(this, "Report", content.Nodes["Report"].Value); break;
					case "Burst": FieldLoader.LoadField(this, "Burst", content.Nodes["Burst"].Value); break;
					case "Charges": FieldLoader.LoadField(this, "Charges", content.Nodes["Charges"].Value); break;

					case "Warhead":
						{
							var warhead = new WarheadInfo();
							FieldLoader.Load(warhead, kv.Value);
							Warheads.Add(warhead);
						} break;

					// in this case, it's an implementation of IProjectileInfo
					default:
						{
							var fullTypeName = typeof(IEffect).Namespace + "." + key + "Info";
							Projectile = (IProjectileInfo)typeof(IEffect).Assembly.CreateInstance(fullTypeName);
							if (Projectile == null)
								throw new InvalidOperationException("Cannot locate projectile type: {0}".F(key));
							FieldLoader.Load(Projectile, kv.Value);
						} break;
				}
			}	
		}
	}
}
