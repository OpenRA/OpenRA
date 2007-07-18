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
				File.WriteAllText( "error.log", e.ToString() );
				Log.Write( "{0}", e.ToString() );
				throw;
			}
		}

		private static void Run( string[] args )
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );

			Settings settings = new Settings( args );

			new MainWindow( settings ).Run();
		}
	}
}