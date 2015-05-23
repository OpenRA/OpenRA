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
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	public static class Exts
	{
		public static bool IsUppercase(this string str)
		{
			return string.Compare(str.ToUpperInvariant(), str, false) == 0;
		}

		public static string F(this string fmt, params object[] args)
		{
			return string.Format(fmt, args);
		}

		public static T WithDefault<T>(T def, Func<T> f)
		{
			try { return f(); }
			catch { return def; }
		}

		public static void Do<T>(this IEnumerable<T> e, Action<T> fn)
		{
			foreach (var ee in e)
				fn(ee);
		}

		public static Lazy<T> Lazy<T>(Func<T> p) { return new Lazy<T>(p); }

		public static IEnumerable<string> GetNamespaces(this Assembly a)
		{
			return a.GetTypes().Select(t => t.Namespace).Distinct().Where(n => n != null);
		}

		public static bool HasAttribute<T>(this MemberInfo mi)
		{
			return mi.GetCustomAttributes(typeof(T), true).Length != 0;
		}

		public static T[] GetCustomAttributes<T>(this MemberInfo mi, bool inherit)
			where T : class
		{
			return (T[])mi.GetCustomAttributes(typeof(T), inherit);
		}

		public static T[] GetCustomAttributes<T>(this ParameterInfo mi)
			where T : class
		{
			return (T[])mi.GetCustomAttributes(typeof(T), true);
		}

		public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
		{
			if (val.CompareTo(min) < 0)
				return min;
			else if (val.CompareTo(max) > 0)
				return max;
			else
				return val;
		}

		public static bool Contains(this Rectangle r, int2 p)
		{
			return r.Contains(p.ToPoint());
		}

		public static bool Contains(this RectangleF r, int2 p)
		{
			return r.Contains(p.ToPointF());
		}

		public static bool HasModifier(this Modifiers k, Modifiers mod)
		{
			return (k & mod) == mod;
		}


		public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k)
			where V : new()
		{
			return d.GetOrAdd(k, _ => new V());
		}

		public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k, Func<K, V> createFn)
		{
			V ret;
			if (!d.TryGetValue(k, out ret))
				d.Add(k, ret = createFn(k));
			return ret;
		}

		public static T Random<T>(this IEnumerable<T> ts, MersenneTwister r)
		{
			var xs = ts as ICollection<T>;
			if (xs != null)
				return xs.ElementAt(r.Next(xs.Count));
			var ys = ts.ToList();
			return ys[r.Next(ys.Count)];
		}

		public static T RandomOrDefault<T>(this IEnumerable<T> ts, MersenneTwister r)
		{
			if (!ts.Any())
				return default(T);

			return ts.Random(r);
		}

		public static float Product(this IEnumerable<float> xs)
		{
			return xs.Aggregate(1f, (a, x) => a * x);
		}

		public static IEnumerable<T> SymmetricDifference<T>(this IEnumerable<T> xs, IEnumerable<T> ys)
		{
			// this is probably a shockingly-slow way to do this, but it's concise.
			return xs.Except(ys).Concat(ys.Except(xs));
		}

		public static IEnumerable<T> Iterate<T>(this T t, Func<T, T> f)
		{
			for (;;) { yield return t; t = f(t); }
		}

		public static T MinBy<T, U>(this IEnumerable<T> ts, Func<T, U> selector)
		{
			return ts.CompareBy(selector, 1, true);
		}

		public static T MaxBy<T, U>(this IEnumerable<T> ts, Func<T, U> selector)
		{
			return ts.CompareBy(selector, -1, true);
		}

		public static T MinByOrDefault<T, U>(this IEnumerable<T> ts, Func<T, U> selector)
		{
			return ts.CompareBy(selector, 1, false);
		}

		public static T MaxByOrDefault<T, U>(this IEnumerable<T> ts, Func<T, U> selector)
		{
			return ts.CompareBy(selector, -1, false);
		}

		static T CompareBy<T, U>(this IEnumerable<T> ts, Func<T, U> selector, int modifier, bool throws)
		{
			var comparer = Comparer<U>.Default;
			T t;
			U u;
			using (var e = ts.GetEnumerator())
			{
				if (!e.MoveNext())
					if (throws)
						throw new ArgumentException("Collection must not be empty.", "ts");
					else
						return default(T);
				t = e.Current;
				u = selector(t);
				while (e.MoveNext())
				{
					var nextT = e.Current;
					var nextU = selector(nextT);
					if (comparer.Compare(nextU, u) * modifier < 0)
					{
						t = nextT;
						u = nextU;
					}
				}
				return t;
			}
		}

		public static int NextPowerOf2(int v)
		{
			--v;
			v |= v >> 1;
			v |= v >> 2;
			v |= v >> 4;
			v |= v >> 8;
			++v;
			return v;
		}

		public static bool IsPowerOf2(int v)
		{
			return (v & (v - 1)) == 0;
		}

		public static Size NextPowerOf2(this Size s) { return new Size(NextPowerOf2(s.Width), NextPowerOf2(s.Height)); }

		public enum ISqrtRoundMode { Floor, Nearest, Ceiling }
		public static int ISqrt(int number, ISqrtRoundMode round = ISqrtRoundMode.Floor)
		{
			if (number < 0)
				throw new InvalidOperationException("Attempted to calculate the square root of a negative integer: {0}".F(number));

			return (int)ISqrt((uint)number, round);
		}

		public static uint ISqrt(uint number, ISqrtRoundMode round = ISqrtRoundMode.Floor)
		{
			var divisor = 1U << 30;

			var root = 0U;
			var remainder = number;

			// Find the highest term in the divisor
			while (divisor > number)
				divisor >>= 2;

			// Evaluate the root, two bits at a time
			while (divisor != 0)
			{
				if (root + divisor <= remainder)
				{
					remainder -= root + divisor;
					root += 2 * divisor;
				}

				root >>= 1;
				divisor >>= 2;
			}

			// Adjust for other rounding modes
			if (round == ISqrtRoundMode.Nearest && remainder > root)
				root += 1;

			else if (round == ISqrtRoundMode.Ceiling && root * root < number)
				root += 1;

			return root;
		}

		public static long ISqrt(long number, ISqrtRoundMode round = ISqrtRoundMode.Floor)
		{
			if (number < 0)
				throw new InvalidOperationException("Attempted to calculate the square root of a negative integer: {0}".F(number));

			return (long)ISqrt((ulong)number, round);
		}

		public static ulong ISqrt(ulong number, ISqrtRoundMode round = ISqrtRoundMode.Floor)
		{
			var divisor = 1UL << 62;

			var root = 0UL;
			var remainder = number;

			// Find the highest term in the divisor
			while (divisor > number)
				divisor >>= 2;

			// Evaluate the root, two bits at a time
			while (divisor != 0)
			{
				if (root + divisor <= remainder)
				{
					remainder -= root + divisor;
					root += 2 * divisor;
				}

				root >>= 1;
				divisor >>= 2;
			}

			// Adjust for other rounding modes
			if (round == ISqrtRoundMode.Nearest && remainder > root)
				root += 1;

			else if (round == ISqrtRoundMode.Ceiling && root * root < number)
				root += 1;

			return root;
		}

		public static string JoinWith<T>(this IEnumerable<T> ts, string j)
		{
			return string.Join(j, ts);
		}

		public static IEnumerable<T> Append<T>(this IEnumerable<T> ts, params T[] moreTs)
		{
			return ts.Concat(moreTs);
		}

		public static Dictionary<TKey, TSource> ToDictionaryWithConflictLog<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, string debugName, Func<TKey, string> logKey, Func<TSource, string> logValue)
		{
			return ToDictionaryWithConflictLog(source, keySelector, x => x, debugName, logKey, logValue);
		}

		public static Dictionary<TKey, TElement> ToDictionaryWithConflictLog<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, string debugName, Func<TKey, string> logKey, Func<TElement, string> logValue)
		{
			// Fall back on ToString() if null functions are provided:
			logKey = logKey ?? (s => s.ToString());
			logValue = logValue ?? (s => s.ToString());

			// Try to build a dictionary and log all duplicates found (if any):
			var dupKeys = new Dictionary<TKey, List<string>>();
			var d = new Dictionary<TKey, TElement>();
			foreach (var item in source)
			{
				var key = keySelector(item);
				var element = elementSelector(item);

				// Check for a key conflict:
				if (d.ContainsKey(key))
				{
					List<string> dupKeyMessages;
					if (!dupKeys.TryGetValue(key, out dupKeyMessages))
					{
						// Log the initial conflicting value already inserted:
						dupKeyMessages = new List<string>();
						dupKeyMessages.Add(logValue(d[key]));
						dupKeys.Add(key, dupKeyMessages);
					}

					// Log this conflicting value:
					dupKeyMessages.Add(logValue(element));
					continue;
				}

				d.Add(key, element);
			}

			// If any duplicates were found, throw a descriptive error
			if (dupKeys.Count > 0)
			{
				var badKeysFormatted = string.Join(", ", dupKeys.Select(p => "{0}: [{1}]".F(logKey(p.Key), string.Join(",", p.Value.ToArray()))).ToArray());
				var msg = "{0}, duplicate values found for the following keys: {1}".F(debugName, badKeysFormatted);
				throw new ArgumentException(msg);
			}

			// Return the dictionary we built:
			return d;
		}

		public static Color ColorLerp(float t, Color c1, Color c2)
		{
			return Color.FromArgb(
				(int)(t * c2.A + (1 - t) * c1.A),
				(int)(t * c2.R + (1 - t) * c1.R),
				(int)(t * c2.G + (1 - t) * c1.G),
				(int)(t * c2.B + (1 - t) * c1.B));
		}

		public static T[] MakeArray<T>(int count, Func<int, T> f)
		{
			var result = new T[count];
			for (var i = 0; i < count; i++)
				result[i] = f(i);

			return result;
		}

		public static T[,] ResizeArray<T>(T[,] ts, T t, int width, int height)
		{
			var result = new T[width, height];
			for (var i = 0; i < width; i++)
				for (var j = 0; j < height; j++)
					// Workaround for broken ternary operators in certain versions of mono (3.10 and  
					// certain versions of the 3.8 series): https://bugzilla.xamarin.com/show_bug.cgi?id=23319
					if (i <= ts.GetUpperBound(0) && j <= ts.GetUpperBound(1))
						result[i, j] = ts[i, j];
					else
						result[i, j] = t;
			return result;
		}

		public static Rectangle Bounds(this Bitmap b) { return new Rectangle(0, 0, b.Width, b.Height); }

		public static int ToBits(this IEnumerable<bool> bits)
		{
			var i = 0;
			var result = 0;
			foreach (var b in bits)
				if (b)
					result |= 1 << i++;
				else
					i++;
			if (i > 33)
				throw new InvalidOperationException("ToBits only accepts up to 32 values.");
			return result;
		}

		public static int ParseIntegerInvariant(string s)
		{
			return int.Parse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
		}

		public static bool TryParseIntegerInvariant(string s, out int i)
		{
			return int.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i);
		}

		public static bool IsTraitEnabled(this object trait)
		{
			return trait as IDisabledTrait == null || !(trait as IDisabledTrait).IsTraitDisabled;
		}

		public static bool IsTraitEnabled<T>(T t)
		{
			return IsTraitEnabled(t as object);
		}
	}

	public static class Enum<T>
	{
		public static T Parse(string s) { return (T)Enum.Parse(typeof(T), s); }
		public static T[] GetValues() { return (T[])Enum.GetValues(typeof(T)); }

		public static bool TryParse(string s, bool ignoreCase, out T value)
		{
			// The string may be a comma delimited list of values
			var names = ignoreCase ? Enum.GetNames(typeof(T)).Select(x => x.ToLowerInvariant()) : Enum.GetNames(typeof(T));
			var values = ignoreCase ? s.Split(',').Select(x => x.Trim().ToLowerInvariant()) : s.Split(',').Select(x => x.Trim());

			if (values.Any(x => !names.Contains(x)))
			{
				value = default(T);
				return false;
			}

			value = (T)Enum.Parse(typeof(T), s, ignoreCase);

			return true;
		}
	}
}
