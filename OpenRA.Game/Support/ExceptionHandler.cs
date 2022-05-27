#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace OpenRA
{
	public static class ExceptionHandler
	{
		public static void HandleFatalError(Exception ex)
		{
			var exceptionName = "exception-" + DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssZ", CultureInfo.InvariantCulture) + ".log";
			Log.AddChannel("exception", exceptionName);

			if (Game.EngineVersion != null)
				Log.Write("exception", "OpenRA engine version {0}", Game.EngineVersion);

			if (Game.ModData != null)
			{
				var mod = Game.ModData.Manifest.Metadata;
				Log.Write("exception", "{0} mod version {1}", mod.Title, mod.Version);
			}

			if (Game.OrderManager != null && Game.OrderManager.World != null && Game.OrderManager.World.Map != null)
			{
				var map = Game.OrderManager.World.Map;
				Log.Write("exception", "on map {0} ({1} by {2}).", map.Uid, map.Title, map.Author);
			}

			Log.Write("exception", "Date: {0:u}", DateTime.UtcNow);
			Log.Write("exception", "Operating System: {0} ({1})", Platform.CurrentPlatform, Environment.OSVersion);
			Log.Write("exception", "Runtime Version: {0}", Platform.RuntimeVersion);
			var rpt = BuildExceptionReport(ex).ToString();
			Log.Write("exception", "{0}", rpt);
			Console.Error.WriteLine(rpt);
		}

		static StringBuilder BuildExceptionReport(Exception ex)
		{
			return BuildExceptionReport(ex, new StringBuilder(), 0);
		}

		static StringBuilder AppendIndentedFormatLine(this StringBuilder sb, int indent, string format, params object[] args)
		{
			return sb.Append(new string(' ', indent * 2)).AppendFormat(format, args).AppendLine();
		}

		static StringBuilder BuildExceptionReport(Exception ex, StringBuilder sb, int indent)
		{
			if (ex == null)
				return sb;

			sb.AppendIndentedFormatLine(indent, "Exception of type `{0}`: {1}", ex.GetType().FullName, ex.Message);

			if (ex is TypeLoadException tle)
			{
				sb.AppendIndentedFormatLine(indent, "TypeName=`{0}`", tle.TypeName);
			}
			else if (ex is OutOfMemoryException)
			{
				var gcMemoryBeforeCollect = GC.GetTotalMemory(false);
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
				sb.AppendIndentedFormatLine(indent, "GC Memory (post-collect)={0:N0}", GC.GetTotalMemory(false));
				sb.AppendIndentedFormatLine(indent, "GC Memory (pre-collect)={0:N0}", gcMemoryBeforeCollect);

				using (var p = Process.GetCurrentProcess())
				{
					sb.AppendIndentedFormatLine(indent, "Working Set={0:N0}", p.WorkingSet64);
					sb.AppendIndentedFormatLine(indent, "Private Memory={0:N0}", p.PrivateMemorySize64);
					sb.AppendIndentedFormatLine(indent, "Virtual Memory={0:N0}", p.VirtualMemorySize64);
				}
			}
			else
			{
				// TODO: more exception types
			}

			if (ex.InnerException != null)
			{
				sb.AppendIndentedFormatLine(indent, "Inner");
				BuildExceptionReport(ex.InnerException, sb, indent + 1);
			}

			sb.AppendIndentedFormatLine(indent, "{0}", ex.StackTrace);

			return sb;
		}
	}
}
