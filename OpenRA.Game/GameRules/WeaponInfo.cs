#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Traits;
using System.Linq;

namespace OpenRA.GameRules
{
	public class WarheadInfo
	{
		public readonly int Spread = 1;									// distance (in pixels) from the explosion center at which damage is 1/2.
		[FieldLoader.LoadUsing( "LoadVersus" )]
		public readonly Dictionary<string, float> Versus; 				// damage vs each armortype
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
		public readonly bool PreventProne = false;						// whether we should prevent prone response in infantry.

		public float EffectivenessAgainst(Actor self)
		{
			var health = self.Info.Traits.GetOrDefault<HealthInfo>();
			if (health == null) return 0f;
			var armor = self.Info.Traits.GetOrDefault<ArmorInfo>();
			if (armor == null || armor.Type == null) return 1;

			float versus;
			return Versus.TryGetValue(armor.Type, out versus) ? versus : 1;
		}

		public WarheadInfo( MiniYaml yaml )
		{
			FieldLoader.Load( this, yaml );
		}

		static object LoadVersus( MiniYaml y )
		{
			return y.NodesDict.ContainsKey( "Versus" )
				? y.NodesDict[ "Versus" ].NodesDict.ToDictionary(
					a => a.Key,
					a => FieldLoader.GetValue<float>( "(value)", a.Value.Value ) )
				: new Dictionary<string, float>();
		}
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
		public float firepowerModifier = 1.0f;
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
		public readonly string[] ValidTargets = { "Ground", "Water" };
		public readonly int BurstDelay = 5;
		public readonly float MinRange = 0;

		[FieldLoader.LoadUsing( "LoadProjectile" )] public IProjectileInfo Projectile;
		[FieldLoader.LoadUsing( "LoadWarheads" )] public List<WarheadInfo> Warheads;

		public WeaponInfo(string name, MiniYaml content)
		{
			FieldLoader.Load( this, content );
		}

		static object LoadProjectile( MiniYaml yaml )
		{
			MiniYaml proj;
			if( !yaml.NodesDict.TryGetValue( "Projectile", out proj ) )
				return null;
			var ret = Game.CreateObject<IProjectileInfo>( proj.Value + "Info" );
			FieldLoader.Load( ret, proj );
			return ret;
		}

		static object LoadWarheads( MiniYaml yaml )
		{
			var ret = new List<WarheadInfo>();
			foreach( var w in yaml.Nodes )
				if( w.Key.Split( '@' )[ 0 ] == "Warhead" )
					ret.Add( new WarheadInfo( w.Value ) );

			return ret;
		}
	}
}
