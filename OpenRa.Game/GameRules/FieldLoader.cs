using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;

namespace OpenRa.Game.GameRules
{
	static class FieldLoader
	{
		public  static void Load( UnitInfo.BaseInfo self, IniSection ini )
		{
			foreach( var x in ini )
			{
				var field = self.GetType().GetField( x.Key );
				if( field.FieldType == typeof( int ) )
					field.SetValue( self, int.Parse( x.Value ) );

				else if( field.FieldType == typeof( float ) )
					field.SetValue( self, float.Parse( x.Value ) );

				else if( field.FieldType == typeof( string ) )
					field.SetValue( self, x.Value.ToLowerInvariant() );

				else if( field.FieldType.IsEnum )
					field.SetValue( self, Enum.Parse( field.FieldType, x.Value ) );

				else if( field.FieldType == typeof( bool ) )
					field.SetValue( self, ParseYesNo( x.Value ) );

				else
					do { } while( false );
			}
		}

		static bool ParseYesNo( string p )
		{
			p = p.ToLowerInvariant();
			if( p == "yes" ) return true;
			if( p == "true" ) return true;
			if( p == "no" ) return false;
			if( p == "false" ) return false;
			throw new InvalidOperationException();
		}
	}
}
