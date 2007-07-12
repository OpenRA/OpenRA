using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace OpenRa.Game
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault( false );

				new MainWindow().Run();
			}
			catch( Exception e )
			{
				File.WriteAllText( "error.log", e.ToString() );
				throw;
			}
		}
	}
}