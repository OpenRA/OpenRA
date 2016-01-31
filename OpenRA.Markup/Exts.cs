using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Markup
{
	internal static class Exts
	{
		public static string JoinWith<T>(this IEnumerable<T> ts, string j)
		{
			return string.Join(j, ts);
		}

		public static string F(this string str, params object[] fmt)
		{
			return string.Format(str, fmt);
		}

		public static Dictionary<TKey, TSource> ToDictionaryWithConflictLog<TSource, TKey>(
			this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
			string debugName, Func<TKey, string> logKey, Func<TSource, string> logValue)
		{
			return ToDictionaryWithConflictLog(source, keySelector, x => x, debugName, logKey, logValue);
		}

		public static Dictionary<TKey, TElement> ToDictionaryWithConflictLog<TSource, TKey, TElement>(
			this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector,
			string debugName, Func<TKey, string> logKey, Func<TElement, string> logValue)
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
				var badKeysFormatted = string.Join(", ", dupKeys.Select(p => "{0}: [{1}]".F(logKey(p.Key), string.Join(",", p.Value))));
				var msg = "{0}, duplicate values found for the following keys: {1}".F(debugName, badKeysFormatted);
				throw new ArgumentException(msg);
			}

			// Return the dictionary we built:
			return d;
		}
	}
}