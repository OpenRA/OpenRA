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
			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault( false );

				Settings settings = new Settings(args);

				new MainWindow( settings ).Run();
			}
			catch( Exception e )
			{
			    File.WriteAllText( "error.log", e.ToString() );
			    throw;
			}
		}
	}
}