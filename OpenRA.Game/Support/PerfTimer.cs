#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace OpenRA.Support
{
	public sealed class PerfTimer : IDisposable
	{
		// Tree settings
		const int Digits = 6;
		const string IndentationString = "|   ";
		const string FormatSeperation = " ms ";
		static readonly string FormatString = "{0," + Digits + ":0}" + FormatSeperation + "{1}";
		readonly string name;
		readonly float thresholdMs;
		readonly byte depth;
		readonly PerfTimer parent;
		List<PerfTimer> children;
		long ticks;

		static readonly ThreadLocal<PerfTimer> ParentThreadLocal = new ThreadLocal<PerfTimer>();

		public PerfTimer(string name, float thresholdMs = 0)
		{
			this.name = name;
			this.thresholdMs = thresholdMs;

			parent = ParentThreadLocal.Value;
			depth = parent == null ? (byte)0 : (byte)(parent.depth + 1);
			ParentThreadLocal.Value = this;

			ticks = Stopwatch.GetTimestamp();
		}

		public void Dispose()
		{
			ticks = Stopwatch.GetTimestamp() - ticks;

			ParentThreadLocal.Value = parent;

			if (parent == null)
				Write();
			else if (ElapsedMs > thresholdMs)
			{
				if (parent.children == null)
					parent.children = new List<PerfTimer>();
				parent.children.Add(this);
			}
		}

		void Write()
		{
			if (children != null)
			{
				Log.Write("perf", GetHeader(Indentation, name));
				foreach (var child in children)
					child.Write();
				Log.Write("perf", FormatString, ElapsedMs, GetFooter(Indentation));
			}
			else if (ElapsedMs >= thresholdMs)
				Log.Write("perf", FormatString, ElapsedMs, Indentation + name);
		}

		float ElapsedMs => 1000f * ticks / Stopwatch.Frequency;

		public static void LogLongTick(long startStopwatchTicks, long endStopwatchTicks, string name, object item)
		{
			var type = item.GetType();
			var label = type == typeof(string) || type.IsGenericType ? item.ToString() : type.Name;
			Log.Write("perf", FormatString,
				1000f * (endStopwatchTicks - startStopwatchTicks) / Stopwatch.Frequency,
				"[" + Game.LocalTick + "] " + name + ": " + label);
		}

		public static long LongTickThresholdInStopwatchTicks => (long)(Stopwatch.Frequency * Game.Settings.Debug.LongTickThresholdMs / 1000f);

		#region Formatting helpers
		static string GetHeader(string indentation, string label)
		{
			return string.Concat(new string(' ', Digits + FormatSeperation.Length), indentation, label);
		}

		static string GetFooter(string indentation)
		{
			return string.Concat(indentation, new string('-', Math.Max(15, 50 - indentation.Length)));
		}

		string Indentation
		{
			get
			{
				if (depth <= 0)
					return string.Empty;
				else if (depth == 1)
					return IndentationString;
				else if (depth == 2)
					return string.Concat(IndentationString, IndentationString);
				else
					return string.Concat(Enumerable.Repeat(IndentationString, depth));
			}
		}
		#endregion
	}
}
