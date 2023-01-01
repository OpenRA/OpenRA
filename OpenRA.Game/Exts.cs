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
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRA.Primitives;
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

		public static Lazy<T> Lazy<T>(Func<T> p) { return new Lazy<T>(p); }

		public static IEnumerable<string> GetNamespaces(this Assembly a)
		{
			return a.GetTypes().Select(t => t.Namespace).Distinct().Where(n => n != null);
		}

		public static bool HasAttribute<T>(this MemberInfo mi)
		{
			return Attribute.IsDefined(mi, typeof(T));
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

		static int WindingDirectionTest(int2 v0, int2 v1, int2 p)
		{
			return Math.Sign((v1.X - v0.X) * (p.Y - v0.Y) - (p.X - v0.X) * (v1.Y - v0.Y));
		}

		public static bool PolygonContains(this int2[] polygon, int2 p)
		{
			var windingNumber = 0;

			for (var i = 0; i < polygon.Length; i++)
			{
				var tv = polygon[i];
				var nv = polygon[(i + 1) % polygon.Length];

				if (tv.Y <= p.Y && nv.Y > p.Y && WindingDirectionTest(tv, nv, p) > 0)
					windingNumber++;
				else if (tv.Y > p.Y && nv.Y <= p.Y && WindingDirectionTest(tv, nv, p) < 0)
					windingNumber--;
			}

			return windingNumber != 0;
		}

		public static bool LinesIntersect(int2 a, int2 b, int2 c, int2 d)
		{
			// If line segments AB and CD intersect:
			//  - the triangles ACD and BCD must have opposite sense (clockwise or anticlockwise)
			//  - the triangles CAB and DAB must have opposite sense
			// Segments intersect if the orientation (clockwise or anticlockwise) of the two points in each line segment are opposite with respect to the other
			// Assumes that lines are not collinear
			return WindingDirectionTest(c, d, a) != WindingDirectionTest(c, d, b) && WindingDirectionTest(a, b, c) != WindingDirectionTest(a, b, d);
		}

		public static bool HasModifier(this Modifiers k, Modifiers mod)
		{
			// PERF: Enum.HasFlag is slower and requires allocations.
			return (k & mod) == mod;
		}

		public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k)
			where V : new()
		{
			return d.GetOrAdd(k, new V());
		}

		public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k, V v)
		{
			if (!d.TryGetValue(k, out var ret))
				d.Add(k, ret = v);
			return ret;
		}

		public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k, Func<K, V> createFn)
		{
			if (!d.TryGetValue(k, out var ret))
				d.Add(k, ret = createFn(k));
			return ret;
		}

		public static int IndexOf<T>(this T[] array, T value)
		{
			return Array.IndexOf(array, value);
		}

		public static T Random<T>(this IEnumerable<T> ts, MersenneTwister r)
		{
			return Random(ts, r, true);
		}

		public static T RandomOrDefault<T>(this IEnumerable<T> ts, MersenneTwister r)
		{
			return Random(ts, r, false);
		}

		static T Random<T>(IEnumerable<T> ts, MersenneTwister r, bool throws)
		{
			var xs = ts as ICollection<T>;
			xs = xs ?? ts.ToList();
			if (xs.Count == 0)
			{
				if (throws)
					throw new ArgumentException("Collection must not be empty.", nameof(ts));
				else
					return default;
			}
			else
				return xs.ElementAt(r.Next(xs.Count));
		}

		public static Rectangle Union(this IEnumerable<Rectangle> rects)
		{
			// PERF: Avoid LINQ.
			var first = true;
			var result = Rectangle.Empty;
			foreach (var rect in rects)
			{
				if (first)
				{
					first = false;
					result = rect;
					continue;
				}

				result = Rectangle.Union(rect, result);
			}

			return result;
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
			while (true)
			{
				yield return t;
				t = f(t);
			}
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
						throw new ArgumentException("Collection must not be empty.", nameof(ts));
					else
						return default;
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
				throw new InvalidOperationException($"Attempted to calculate the square root of a negative integer: {number}");

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
				throw new InvalidOperationException($"Attempted to calculate the square root of a negative integer: {number}");

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

		public static int MultiplyBySqrtTwo(short number)
		{
			return number * 46341 / 32768;
		}

		public static int IntegerDivisionRoundingAwayFromZero(int dividend, int divisor)
		{
			var quotient = Math.DivRem(dividend, divisor, out var remainder);
			if (remainder == 0)
				return quotient;
			return quotient + (Math.Sign(dividend) == Math.Sign(divisor) ? 1 : -1);
		}

		public static string JoinWith<T>(this IEnumerable<T> ts, string j)
		{
			return string.Join(j, ts);
		}

		public static IEnumerable<T> Append<T>(this IEnumerable<T> ts, params T[] moreTs)
		{
			return ts.Concat(moreTs);
		}

		public static IEnumerable<T> Exclude<T>(this IEnumerable<T> ts, params T[] exclusions)
		{
			return ts.Except(exclusions);
		}

		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
		{
			return new HashSet<T>(source);
		}

		public static Dictionary<TKey, TSource> ToDictionaryWithConflictLog<TSource, TKey>(
			this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
			string debugName, Func<TKey, string> logKey, Func<TSource, string> logValue)
		{
			return ToDictionaryWithConflictLog(source, keySelector, x => x, debugName, logKey, logValue);
		}

		public static Dictionary<TKey, TElement> ToDictionaryWithConflictLog<TSource, TKey, TElement>(
			this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector,
			string debugName, Func<TKey, string> logKey = null, Func<TElement, string> logValue = null)
		{
			// Fall back on ToString() if null functions are provided:
			logKey = logKey ?? (s => s.ToString());
			logValue = logValue ?? (s => s.ToString());

			// Try to build a dictionary and log all duplicates found (if any):
			var dupKeys = new Dictionary<TKey, List<string>>();
			var capacity = source is ICollection<TSource> collection ? collection.Count : 0;
			var d = new Dictionary<TKey, TElement>(capacity);
			foreach (var item in source)
			{
				var key = keySelector(item);
				var element = elementSelector(item);

				// Discard elements with null keys
				if (!typeof(TKey).IsValueType && key == null)
					continue;

				// Check for a key conflict:
				if (!d.TryAdd(key, element))
				{
					if (!dupKeys.TryGetValue(key, out var dupKeyMessages))
					{
						// Log the initial conflicting value already inserted:
						dupKeyMessages = new List<string>
						{
							logValue(d[key])
						};
						dupKeys.Add(key, dupKeyMessages);
					}

					// Log this conflicting value:
					dupKeyMessages.Add(logValue(element));
				}
			}

			// If any duplicates were found, throw a descriptive error
			if (dupKeys.Count > 0)
			{
				var badKeysFormatted = string.Join(", ", dupKeys.Select(p => $"{logKey(p.Key)}: [{string.Join(",", p.Value)}]"));
				var msg = $"{debugName}, duplicate values found for the following keys: {badKeysFormatted}";
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
			{
				for (var j = 0; j < height; j++)
				{
					// Workaround for broken ternary operators in certain versions of mono
					// (3.10 and certain versions of the 3.8 series): https://bugzilla.xamarin.com/show_bug.cgi?id=23319
					if (i <= ts.GetUpperBound(0) && j <= ts.GetUpperBound(1))
						result[i, j] = ts[i, j];
					else
						result[i, j] = t;
				}
			}

			return result;
		}

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

		public static byte ParseByte(string s)
		{
			return byte.Parse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
		}

		public static bool TryParseIntegerInvariant(string s, out int i)
		{
			return int.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i);
		}

		public static bool TryParseInt64Invariant(string s, out long i)
		{
			return long.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i);
		}

		public static bool IsTraitEnabled<T>(this T trait)
		{
			return !(trait is IDisabledTrait disabledTrait) || !disabledTrait.IsTraitDisabled;
		}

		public static T FirstEnabledTraitOrDefault<T>(this IEnumerable<T> ts)
		{
			// PERF: Avoid LINQ.
			foreach (var t in ts)
				if (t.IsTraitEnabled())
					return t;

			return default;
		}

		public static T FirstEnabledTraitOrDefault<T>(this T[] ts)
		{
			// PERF: Avoid LINQ.
			foreach (var t in ts)
				if (t.IsTraitEnabled())
					return t;

			return default;
		}

		public static T FirstEnabledConditionalTraitOrDefault<T>(this IEnumerable<T> ts) where T : IDisabledTrait
		{
			// PERF: Avoid LINQ.
			foreach (var t in ts)
				if (!t.IsTraitDisabled)
					return t;

			return default;
		}

		public static T FirstEnabledConditionalTraitOrDefault<T>(this T[] ts) where T : IDisabledTrait
		{
			// PERF: Avoid LINQ.
			foreach (var t in ts)
				if (!t.IsTraitDisabled)
					return t;

			return default;
		}

		public static LineSplitEnumerator SplitLines(this string str, char separator)
		{
			return new LineSplitEnumerator(str.AsSpan(), separator);
		}
	}

	public ref struct LineSplitEnumerator
	{
		ReadOnlySpan<char> str;
		readonly char separator;

		public LineSplitEnumerator(ReadOnlySpan<char> str, char separator)
		{
			this.str = str;
			this.separator = separator;
			Current = default;
		}

		public LineSplitEnumerator GetEnumerator() => this;

		public bool MoveNext()
		{
			var span = str;

			// Reach the end of the string
			if (span.Length == 0)
				return false;

			var index = span.IndexOf(separator);
			if (index == -1)
			{
				// The remaining string is an empty string
				str = ReadOnlySpan<char>.Empty;
				Current = span;
				return true;
			}

			Current = span.Slice(0, index);
			str = span.Slice(index + 1);
			return true;
		}

		public ReadOnlySpan<char> Current { get; private set; }
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
				value = default;
				return false;
			}

			value = (T)Enum.Parse(typeof(T), s, ignoreCase);

			return true;
		}
	}
}
