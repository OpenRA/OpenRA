#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OpenRA
{
	enum RunStatus
	{
		Error = -1,
		Success = 0,
		Restart = 1,
		Running = int.MaxValue
	}

	static class Program
	{
		[STAThread]
		static int Main(string[] args)
		{
			if (Debugger.IsAttached || args.Contains("--just-die"))
				return (int)Run(args);

			AppDomain.CurrentDomain.UnhandledException += (_, e) => FatalError((Exception)e.ExceptionObject);

			try
			{
				return (int)Run(args);
			}
			catch (Exception e)
			{
				FatalError(e);
				return (int)RunStatus.Error;
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

			var tle = e as TypeLoadException;
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

		static RunStatus Run(string[] args)
		{
			if (AppDomain.CurrentDomain.IsDefaultAppDomain())
			{
				var name = Assembly.GetEntryAssembly().GetName();
				int retCode;
				do
				{
					var domain = AppDomain.CreateDomain("Game");
					retCode = domain.ExecuteAssemblyByName(name, args);
					AppDomain.Unload(domain);
				}
				while (retCode == (int)RunStatus.Restart);
				return RunStatus.Success;
			}

			Game.Initialize(new Arguments(args));
			GC.Collect();
			return Game.Run();
		}
	}
}