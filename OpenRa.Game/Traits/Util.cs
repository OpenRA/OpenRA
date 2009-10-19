using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	static class Util
	{
		public static void TickFacing( ref int facing, int desiredFacing, int rot )
		{
			var leftTurn = ( facing - desiredFacing ) & 0xFF;
			var rightTurn = ( desiredFacing - facing ) & 0xFF;
			if( Math.Min( leftTurn, rightTurn ) < rot )
				facing = desiredFacing;
			else if( rightTurn < leftTurn )
				facing = ( facing + rot ) & 0xFF;
			else
				facing = ( facing - rot ) & 0xFF;
		}

		static float2[] fvecs = Graphics.Util.MakeArray<float2>( 32,
			i => -float2.FromAngle( i / 16.0f * (float)Math.PI ) * new float2( 1f, 1.3f ) );

		public static int GetFacing( float2 d, int currentFacing )
		{
			if( float2.WithinEpsilon( d, float2.Zero, 0.001f ) )
				return currentFacing;

			int highest = -1;
			float highestDot = -1.0f;

			for( int i = 0 ; i < fvecs.Length ; i++ )
			{
				float dot = float2.Dot( fvecs[ i ], d );
				if( dot > highestDot )
				{
					highestDot = dot;
					highest = i;
				}
			}

			return highest * 8;
		}

	}
}
