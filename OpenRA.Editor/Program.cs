using System;
using System.Globalization;
using System.Windows.Forms;

namespace OpenRA.Editor
{
	static class Program
	{
		[STAThread]
		static void Main( string[] args )
		{
			Application.CurrentCulture = CultureInfo.InvariantCulture;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1(args));
		}
	}
}
