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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace OpenRA.Support
{
	public sealed class PerfTimer : IDisposable
	{
		readonly string name;
		readonly object item;
		readonly float thresholdMs;
		readonly byte depth;
		readonly PerfTimer parent;
		List<PerfTimer> children;
		long ticks;

		static ThreadLocal<PerfTimer> Parent = new ThreadLocal<PerfTimer>();

		// Tree settings
		const int MaxWidth = 60, Digits = 6;
		const int MaxIndentedLabel = MaxWidth - Digits;
		const string IndentationString = "|   ";
		static readonly string FormatString = "{0," + MaxIndentedLabel + "} {1," + Digits + ":0} ms";

		public PerfTimer(string name, float thresholdMs = 0)
		{
			this.name = name;
			this.thresholdMs = thresholdMs;

			parent = Parent.Value;
			depth = parent == null ? (byte)0 : (byte)(parent.depth + 1);
			Parent.Value = this;

			ticks = Stopwatch.GetTimestamp();
		}

		private PerfTimer(string name, object item, float thresholdMs)
			: this(name, thresholdMs)
		{
			this.item = item;
		}

		public static PerfTimer TimeUsingLongTickThreshold(string name, object item)
		{
			return new PerfTimer(name, item, (float)Game.Settings.Debug.LongTickThreshold.TotalMilliseconds);
		}

		public void Dispose()
		{
			ticks = Stopwatch.GetTimestamp() - ticks;

			Parent.Value = parent;

			if (parent == null)
				Write();
			else if (elapsedMs > thresholdMs)
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
				Log.Write("perf", GetHeader(Indentation, output));
				foreach (var child in children)
					child.Write();
				Log.Write("perf", FormatString, GetFooter(Indentation), elapsedMs);
			}
			else if (elapsedMs >= thresholdMs)
				Log.Write("perf", FormatString, GetOneLiner(Indentation, output), elapsedMs);
		}

		float elapsedMs { get { return (float)ticks / Stopwatch.Frequency; } }
		string output { get { return item != null ? "[{0}] {1}: {2}".F(Game.LocalTick, name, item) : name; } }

		#region Formatting helpers
		static string GetHeader(string indentation, string label)
		{
			return string.Concat(indentation, LimitLength(label, MaxIndentedLabel - indentation.Length));
		}

		static string GetOneLiner(string indentation, string label)
		{
			return string.Concat(indentation, SetLength(label, MaxIndentedLabel - indentation.Length));
		}

		static string GetFooter(string indentation)
		{
			return string.Concat(indentation, new string('-', MaxIndentedLabel - indentation.Length));
		}

		static string LimitLength(string s, int length, int minLength = 8)
		{
			length = Math.Max(length, minLength);

			if (s == null || s.Length <= length)
				return s;

			return s.Substring(0, length);
		}

		static string SetLength(string s, int length, int minLength = 8)
		{
			length = Math.Max(length, minLength);

			if (s == null || s.Length == length)
				return s;

			if (s.Length < length)
				return s.PadRight(length);

			return s.Substring(0, length);
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
