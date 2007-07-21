using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.IO;

namespace OpenRa.Game
{
	enum UnitMovementType : byte
	{
		Foot = 0,
		Track = 1,
		Wheel = 2,
		Float = 3,
	}

	enum TerrainMovementType : byte
	{
		Clear = 0,
		Water = 1,
		Road = 2,
		Rock = 3,
		//Tree = 4,
		River = 5,
		Rough = 6,
		Wall = 7,
		Beach = 8,
		Ore = 9,
	}

	static class TerrainCosts
	{
		static double[][] costs = Util.MakeArray<double[]>( 4,
			delegate { return Util.MakeArray<double>( 10,
				delegate { return double.PositiveInfinity; } ); } );

		static TerrainCosts()
		{
			IniFile file = new IniFile(FileSystem.Open("rules.ini"));

			for( int i = 0 ; i < 10 ; i++ )
			{
				if( i == 4 )
					continue;
				IniSection section = file.GetSection( ( (TerrainMovementType)i ).ToString() );
				for( int j = 0 ; j < 4 ; j++ )
				{
					string val = section.GetValue( ( (UnitMovementType)j ).ToString(), "0%" );
					costs[ j ][ i ] = 100.0 / double.Parse( val.Substring( 0, val.Length - 1 ) );
				}
			}
		}

		public static double Cost( UnitMovementType unitMovementType, int r )
		{
			return costs[ (byte)unitMovementType ][ r ];
		}
	}
}
