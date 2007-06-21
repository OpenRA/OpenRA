using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace ShpViewer
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );

			try
			{
				OpenFileDialog ofd = new OpenFileDialog();
				ofd.Filter = "SHP Files|*.shp";
				if( ofd.ShowDialog() == DialogResult.OK )
					Application.Run( new ShpViewForm( ofd.FileName ) );
			}
			catch( Exception e )
			{
				MessageBox.Show( e.ToString() );
			}
		}
	}
}