using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace OpenRa.Game
{
	static class Program
	{
		[STAThread]
		static void Main( string[] args )
		{
			if( System.Diagnostics.Debugger.IsAttached )
			{
				Run( args );
				return;
			}

			try
			{
				Run( args );
			}
			catch( Exception e )
			{
				Log.Write( "{0}", e.ToString() );
				throw;
			}
		}

		static void Run( string[] args )
		{
			new MainWindow( new Settings( args ) ).Run();
		}
	}
}