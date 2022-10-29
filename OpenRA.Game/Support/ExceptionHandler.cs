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
				Log.Write("exception", $"OpenRA engine version {Game.EngineVersion}");

			if (Game.Settings != null && Game.Settings.Player != null && Game.Settings.Player.Language != null)
				Log.Write("exception", $"OpenRA Language: {Game.Settings.Player.Language}");

			if (Game.ModData != null)
			{
				var mod = Game.ModData.Manifest.Metadata;
				Log.Write("exception", $"{mod.Title} mod version ${mod.Version}");
			}

			if (Game.OrderManager != null && Game.OrderManager.World != null && Game.OrderManager.World.Map != null)
			{
				var map = Game.OrderManager.World.Map;
				Log.Write("exception", $"on map {map.Uid} ({map.Title} by {map.Author}).");
			}

			Log.Write("exception", $"Date: {DateTime.UtcNow:u}");
			Log.Write("exception", $"Operating System: {Platform.CurrentPlatform} ({Environment.OSVersion})");
			Log.Write("exception", $"Runtime Version: {Platform.RuntimeVersion}", Platform.RuntimeVersion);
			Log.Write("exception", $"Installed Language: {CultureInfo.InstalledUICulture.TwoLetterISOLanguageName} (Installed) {CultureInfo.CurrentCulture.TwoLetterISOLanguageName} (Current) {CultureInfo.CurrentUICulture.TwoLetterISOLanguageName} (Current UI)");

			var rpt = BuildExceptionReport(ex).ToString();
			Log.Write("exception", rpt);
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

			sb.AppendIndentedFormatLine(indent, $"Exception of type `{ex.GetType().FullName}`: {ex.Message}");

			if (ex is TypeLoadException tle)
			{
				sb.AppendIndentedFormatLine(indent, $"TypeName=`{tle.TypeName}`");
			}
			else if (ex is OutOfMemoryException)
			{
				var gcMemoryBeforeCollect = GC.GetTotalMemory(false);
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
				sb.AppendIndentedFormatLine(indent, $"GC Memory (post-collect)={GC.GetTotalMemory(false):N0}");
				sb.AppendIndentedFormatLine(indent, $"GC Memory (pre-collect)={gcMemoryBeforeCollect:N0}");

				using (var p = Process.GetCurrentProcess())
				{
					sb.AppendIndentedFormatLine(indent, $"Working Set={p.WorkingSet64:N0}");
					sb.AppendIndentedFormatLine(indent, $"Private Memory={p.PrivateMemorySize64:N0}");
					sb.AppendIndentedFormatLine(indent, $"Virtual Memory={p.VirtualMemorySize64:N0}");
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

			sb.AppendIndentedFormatLine(indent, ex.StackTrace);

			return sb;
		}
	}
}
