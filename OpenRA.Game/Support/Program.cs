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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OpenRA
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			if (Debugger.IsAttached || args.Contains("--just-die"))
			{
				Run(args);
				return;
			}

			AppDomain.CurrentDomain.UnhandledException += (_, e) => FatalError((Exception)e.ExceptionObject);

			try
			{
				Run(args);
			}
			catch (Exception e)
			{
				FatalError(e);
			}
		}

		static void FatalError(Exception e)
		{
			Log.AddChannel("exception", "exception.log");

			if (Game.modData != null)
			{
				var mod = Game.modData.Manifest.Mod;
				Log.Write("exception", "{0} Mod at Version {1}", mod.Title, mod.Version);
			}

			Log.Write("exception", "Operating System: {0} ({1})", Platform.CurrentPlatform, Environment.OSVersion);
			Log.Write("exception", "Runtime Version: {0}", Platform.RuntimeVersion);
			var rpt = BuildExceptionReport(e).ToString();
			Log.Write("exception", "{0}", rpt);
			Console.Error.WriteLine(rpt);

			if (Game.Settings.Debug.ShowFatalErrorDialog && !Game.Settings.Server.Dedicated)
			{
				Game.Renderer.Device.Dispose();
				Platform.ShowFatalErrorDialog();
			}
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
			if (e == null)
				return sb;

			sb.AppendFormat("Exception of type `{0}`: {1}", e.GetType().FullName, e.Message);

            TypeLoadException tle = e as TypeLoadException;
			if (tle != null)
			{
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

			return sb;
		}

		static void Run(string[] args)
		{
			Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
			Game.Initialize(new Arguments(args));
			GC.Collect();
			Game.Run();
		}
	}
}