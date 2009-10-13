using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.GameRules
{
	class WeaponInfoLoader
	{
		readonly Dictionary<string, WeaponInfo> weaponInfos = new Dictionary<string, WeaponInfo>();

		public WeaponInfoLoader( IniFile rules )
		{
			foreach( var s in Util.ReadAllLines( FileSystem.Open( "weapons.txt" ) ) )
			{
				var unitName = s.Split( ',' )[ 0 ];
				weaponInfos.Add( unitName.ToLowerInvariant(),
					new WeaponInfo( rules.GetSection( unitName ) ) );
			}
		}

		public WeaponInfo this[ string unitName ]
		{
			get
			{
				return weaponInfos[ unitName.ToLowerInvariant() ];
			}
		}
	}

	class WeaponInfo
	{
		public readonly string Anim = null;
		public readonly int Burst = 1;
		public readonly bool Camera = false;
		public readonly bool Charges = false;
		public readonly int Damage = 0;
		public readonly string Projectile = "Invisible";
		public readonly int ROF = 1; // in 1/15 second units.
		public readonly float Range = 0;
		public readonly string Report = null;
		public readonly int Speed = -1;
		public readonly bool Supress = false;
		public readonly bool TurboBoost = false;
		public readonly string Warhead = null;

		public WeaponInfo( IniSection ini )
		{
			FieldLoader.Load( this, ini );
		}
	}
}
