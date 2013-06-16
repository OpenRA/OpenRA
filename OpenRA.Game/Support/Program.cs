#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OpenRA
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			// HACK: brutal hack
			Application.CurrentCulture = CultureInfo.InvariantCulture;

			if (Debugger.IsAttached || args.Contains("--just-die"))
			{
				Run(args);
				return;
			}

			AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException((Exception)e.ExceptionObject);

			try
			{
				Run(args);
			}
			catch (Exception e)
			{
				LogException(e);
			}
		}

		static void LogException(Exception e)
		{
			Log.AddChannel("exception", "exception.log");
			var rpt = BuildExceptionReport(e).ToString();
			Log.Write("exception", "{0}", rpt);
			Console.Error.WriteLine(rpt);

			var userChoice = MessageBox.Show("OpenRA has encountered a fatal error and must close. For more information, refer to the crash logs.", "Fatal Error.",
			                                 MessageBoxButtons.YesNo, MessageBoxIcon.Error);
			if (userChoice == DialogResult.Yes)
				Process.Start(Log.LogPath);
		}

		static StringBuilder BuildExceptionReport(Exception e)
		{
			return BuildExceptionReport(e, new StringBuilder(), 0);
		}

		static void Indent(StringBuilder sb, int d)
		{
			sb.Append(new string(' ', d * 2));
		}

		static StringBuilder BuildExceptionReport(Exception e, StringBuilder sb, int d)
		{
			if (e == null) return sb;

			sb.AppendFormat("Exception of type `{0}`: {1}", e.GetType().FullName, e.Message);

			if (e is TypeLoadException)
			{
				var tle = (TypeLoadException)e;
				sb.AppendLine();
				Indent(sb, d);
				sb.AppendFormat("TypeName=`{0}`", tle.TypeName);
			}
			else // TODO: more exception types
			{
			}

			if (e.InnerException != null)
			{
				sb.AppendLine();
				Indent(sb, d); sb.Append("Inner ");
				BuildExceptionReport(e.InnerException, sb, d + 1);
			}

			sb.AppendLine();
			Indent(sb, d); sb.Append(e.StackTrace);

			sb.AppendLine();
			sb.AppendLine();
			sb.AppendLine("The game just crashed. Have a look at https://github.com/OpenRA/OpenRA/wiki/FAQ#my-game-just-crashed for understanding what this means and how to report problems. Sorry for the inconvenience.");

			return sb;
		}

		static void Run(string[] args)
		{
			Game.Initialize(new Arguments(args));
			GC.Collect();
			Game.Run();
		}
	}
}