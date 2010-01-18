using OpenRa.Graphics;

namespace OpenRa
{
	public enum UnitMovementType : byte
	{
		Foot = 0,
		Track = 1,
		Wheel = 2,
		Float = 3,
		Fly = 4,
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
		Special = 10,
	}

	static class TerrainCosts
	{
		static double[][] costs = Util.MakeArray<double[]>( 4,
			a => Util.MakeArray<double>( 11, b => double.PositiveInfinity ));

		static TerrainCosts()
		{
			for( int i = 0 ; i < 11 ; i++ )
			{
				if( i == 4 ) continue;
				var section = Rules.AllRules.GetSection( ( (TerrainMovementType)i ).ToString() );
				for( int j = 0 ; j < 4 ; j++ )
				{
					string val = section.GetValue( ( (UnitMovementType)j ).ToString(), "0%" );
					costs[ j ][ i ] = 100.0 / double.Parse( val.Substring( 0, val.Length - 1 ) );
				}
			}
		}

		public static double Cost( UnitMovementType unitMovementType, int r )
		{
			if (r >= 10) return 1.0;
			return costs[ (byte)unitMovementType ][ r ];
		}
	}
}
