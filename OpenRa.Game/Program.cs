using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OpenRa.Game
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			new MainWindow().Run();
		}
	}
}