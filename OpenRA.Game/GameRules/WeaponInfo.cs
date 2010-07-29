#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.GameRules
{
	public class WarheadInfo
	{
		public readonly int Spread = 1;									// distance (in pixels) from the explosion center at which damage is 1/2.
		public readonly float[] Verses = { 1, 1, 1, 1, 1 };				// damage vs each armortype
		public readonly bool Ore = false;								// can this damage ore?
		public readonly string Explosion = null;						// explosion effect to use
		public readonly string WaterExplosion = null;					// explosion effect on hitting water (usually a splash)
		public readonly string SmudgeType = null;						// type of smudge to apply
		public readonly int[] Size = { 0, 0 };							// size of the explosion. provide 2 values for a ring effect (outer/inner)
		public readonly int InfDeath = 0;								// infantry death animation to use
		public readonly string ImpactSound = null;						// sound to play on impact
		public readonly string WaterImpactSound = null;					// sound to play on impact with water
		public readonly int Damage = 0;									// how much (raw) damage to deal
		public readonly int Delay = 0;									// delay in ticks before dealing the damage. 0=instant (old model)
		public readonly DamageModel DamageModel = DamageModel.Normal;	// which damage model to use

		public float EffectivenessAgainst(Actor self)
		{
			var health = self.Info.Traits.GetOrDefault<HealthInfo>();
			if (health == null) return 0f;
			
			return Verses[(int)(health.Armor)];
		}
	}

	public enum ArmorType
	{
		none = 0,
		wood = 1,
		light = 2,
		heavy = 3,
		concrete = 4,
	}

	public enum DamageModel
	{
		Normal,								// classic RA damage model: point actors, distance-based falloff
		PerCell,							// like RA's "nuke damage"
	}

	public class ProjectileArgs
	{
		public WeaponInfo weapon;
		public Actor firedBy;
		public int2 src;
		public int srcAltitude;
		public int facing;
		public Target target;
		public int2 dest;
		public int destAltitude;
	}

	public interface IProjectileInfo { IEffect Create(ProjectileArgs args); }

	public class WeaponInfo
	{
		public readonly float Range = 0;
		public readonly string Report = null;
		public readonly int ROF = 1;
		public readonly int Burst = 1;
		public readonly bool Charges = false;
		public readonly bool Underwater = false;
		public readonly string[] ValidTargets = { "Ground" };
		public readonly int BurstDelay = 5;

		public IProjectileInfo Projectile;
		public List<WarheadInfo> Warheads = new List<WarheadInfo>();

		public WeaponInfo(string name, MiniYaml content)
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
					case "ValidTargets": FieldLoader.LoadField(this, "ValidTargets", content.Nodes["ValidTargets"].Value); break;
					case "Underwater": FieldLoader.LoadField(this, "Underwater", content.Nodes["Underwater"].Value); break;
					case "BurstDelay": FieldLoader.LoadField(this, "BurstDelay", content.Nodes["BurstDelay"].Value); break;

					case "Warhead":
						{
							var warhead = new WarheadInfo();
							FieldLoader.Load(warhead, kv.Value);
							Warheads.Add(warhead);
						} break;

					// in this case, it's an implementation of IProjectileInfo
					default:
						{
							Projectile = Game.CreateObject<IProjectileInfo>(key + "Info");
							FieldLoader.Load(Projectile, kv.Value);
						} break;
				}
			}	
		}
	}
}
