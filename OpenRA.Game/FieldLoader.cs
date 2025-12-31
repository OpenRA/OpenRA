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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA
{
	public static class FieldLoader
	{
		const char Comma = ',';

		public class MissingFieldsException : YamlException
		{
			public readonly string[] Missing;
			public readonly string Header;
			public override string Message
			{
				get
				{
					return (string.IsNullOrEmpty(Header) ? "" : Header + ": ") + Missing[0]
						+ string.Concat(Missing.Skip(1).Select(m => ", " + m));
				}
			}

			public MissingFieldsException(string[] missing, string header = null, string headerSingle = null)
				: base(null)
			{
				Header = missing.Length > 1 ? header : headerSingle ?? header;
				Missing = missing;
			}
		}

		public delegate object InvalidValueActionDelegate(ReadOnlySpan<char> value, Type fieldType, string fieldName);
		public delegate void UnknownFieldActionDelegate(string fieldName);

		public static InvalidValueActionDelegate InvalidValueAction = (value, fieldType, fieldName) =>
			throw new YamlException($"FieldLoader: Cannot parse `{value}` into field `{fieldName}` of type `{fieldType}`");

		public static UnknownFieldActionDelegate UnknownFieldAction = (fieldName) =>
			throw new NotImplementedException($"FieldLoader: Missing field `{fieldName}`");

		static readonly ConcurrentCache<Type, FieldLoadInfo[]> TypeLoadInfo =
			new(BuildTypeLoadInfo);
		static readonly ConcurrentCache<string, BooleanExpression> BooleanExpressionCache =
			new(expression => new BooleanExpression(expression));
		static readonly ConcurrentCache<string, IntegerExpression> IntegerExpressionCache =
			new(expression => new IntegerExpression(expression));

		delegate object ParseValueDelegate(string fieldName, Type fieldType, YamlValue value);
		delegate object ParseNodesDelegate(string fieldName, Type fieldType, ImmutableArray<MiniYamlNode> nodes);

		static readonly FrozenDictionary<Type, ParseValueDelegate> TypeParsers =
			new Dictionary<Type, ParseValueDelegate>
			{
				{ typeof(byte), ParseByte },
				{ typeof(ushort), ParseUShort },
				{ typeof(short), ParseShort },
				{ typeof(int), ParseInt },
				{ typeof(float), ParseFloat },
				{ typeof(decimal), ParseDecimal },
				{ typeof(string), ParseString },
				{ typeof(Color), ParseColor },
				{ typeof(Hotkey), ParseHotkey },
				{ typeof(HotkeyReference), ParseHotkeyReference },
				{ typeof(WDist), ParseWDist },
				{ typeof(WVec), ParseWVec },
				{ typeof(WVec[]), ParseWVecArray },
				{ typeof(WPos), ParseWPos },
				{ typeof(WAngle), ParseWAngle },
				{ typeof(WRot), ParseWRot },
				{ typeof(CPos), ParseCPos },
				{ typeof(CPos[]), ParseCPosArray },
				{ typeof(CVec), ParseCVec },
				{ typeof(CVec[]), ParseCVecArray },
				{ typeof(BooleanExpression), ParseBooleanExpression },
				{ typeof(IntegerExpression), ParseIntegerExpression },
				{ typeof(bool), ParseBool },
				{ typeof(int2[]), ParseInt2Array },
				{ typeof(Size), ParseSize },
				{ typeof(int2), ParseInt2 },
				{ typeof(Vector2), ParseVector2 },
				{ typeof(Vector3), ParseVector3 },
				{ typeof(Rectangle), ParseRectangle },
				{ typeof(DateTime), ParseDateTime }
			}.ToFrozenDictionary();

		static readonly FrozenDictionary<Type, ParseValueDelegate> GenericTypeValueParsers =
			new Dictionary<Type, ParseValueDelegate>
			{
				{ typeof(HashSet<>), ParseHashSetOrList },
				{ typeof(List<>), ParseHashSetOrList },
				{ typeof(ImmutableArray<>), ParseImmutableArray },
				{ typeof(FrozenSet<>), ParseFrozenSet },
				{ typeof(BitSet<>), ParseBitSet },
				{ typeof(Nullable<>), ParseNullable },
			}.ToFrozenDictionary();

		static readonly FrozenDictionary<Type, ParseNodesDelegate> GenericTypeNodesParsers =
			new Dictionary<Type, ParseNodesDelegate>
			{
				{ typeof(Dictionary<,>), ParseDictionary },
				{ typeof(FrozenDictionary<,>), ParseFrozenDictionary },
			}.ToFrozenDictionary();

		static readonly object BoxedTrue = true;
		static readonly object BoxedFalse = false;
		static readonly object[] BoxedInts = Exts.MakeArray(33, i => (object)i);

		static readonly MethodInfo ToImmutableArray =
			typeof(ImmutableArray)
			.GetMethods()
			.Single(m =>
				m.Name == nameof(ImmutableArray.ToImmutableArray) &&
				m.GetParameters()?.First().ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>));

		static readonly MethodInfo ToFrozenSet =
			typeof(FrozenSet)
			.GetMethod(nameof(FrozenSet.ToFrozenSet));

		static readonly MethodInfo ToFrozenDictionary =
			typeof(FrozenDictionary)
			.GetMethods()
			.Single(m =>
				m.Name == nameof(FrozenDictionary.ToFrozenDictionary) &&
				m.GetParameters().Length == 2);

		static object ParseByte(string fieldName, Type fieldType, YamlValue value)
		{
			if (Exts.TryParseByteInvariant(value.Span, out var res))
				return res;
			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseUShort(string fieldName, Type fieldType, YamlValue value)
		{
			if (Exts.TryParseUInt16Invariant(value.Span, out var res))
				return res;
			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseShort(string fieldName, Type fieldType, YamlValue value)
		{
			if (Exts.TryParseInt16Invariant(value.Span, out var res))
				return res;
			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseInt(string fieldName, Type fieldType, YamlValue value)
		{
			if (Exts.TryParseInt32Invariant(value.Span, out var res))
			{
				if (res >= 0 && res < BoxedInts.Length)
					return BoxedInts[res];
				return res;
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseFloat(string fieldName, Type fieldType, YamlValue value)
		{
			if (Exts.TryParseFloatOrPercentInvariant(value.Span, out var res))
				return res;
			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseDecimal(string fieldName, Type fieldType, YamlValue value)
		{
			var raw = value.Span;
			var mult = 1m;
			if (value.Span.Contains('%'))
			{
				raw = value.ToString().Replace("%", "");
				mult = 0.01m;
			}

			if (decimal.TryParse(raw, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var res))
				return res * mult;
			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseString(string fieldName, Type fieldType, YamlValue value)
		{
			return value.ToString();
		}

		static object ParseColor(string fieldName, Type fieldType, YamlValue value)
		{
			if (Color.TryParse(value.Span, out var color))
				return color;

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseHotkey(string fieldName, Type fieldType, YamlValue value)
		{
			if (Hotkey.TryParse(value.Span, out var res))
				return res;

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseHotkeyReference(string fieldName, Type fieldType, YamlValue value)
		{
			return Game.ModData.Hotkeys[value.ToString()];
		}

		static object ParseWDist(string fieldName, Type fieldType, YamlValue value)
		{
			if (WDist.TryParse(value.Span, out var res))
				return res;

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseWVec(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				Span<Range> ranges = stackalloc Range[4];
				var parts = value.Span.Split(ranges, Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts == 3
					&& WDist.TryParse(value.Span[ranges[0]], out var rx)
					&& WDist.TryParse(value.Span[ranges[1]], out var ry)
					&& WDist.TryParse(value.Span[ranges[2]], out var rz))
					return new WVec(rx, ry, rz);
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseWVecArray(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				var res = new List<WVec>();
				var parts = value.Span.Split(Comma);
				Span<WDist> elements = stackalloc WDist[3];
				var index = 0;
				foreach (var part in parts)
				{
					var p = part.Trim(); // StringSplitOptions.TrimEntries
					if (p.IsEmpty) continue; // StringSplitOptions.RemoveEmptyEntries

					if (!WDist.TryParse(p, out var element))
						return InvalidValueAction(value.Span, fieldType, fieldName);

					elements[index++] = element;
					if (index == elements.Length)
					{
						index = 0;
						res.Add(new WVec(elements[0], elements[1], elements[2]));
					}
				}

				if (index != 0)
					return InvalidValueAction(value.Span, fieldType, fieldName);

				return res.ToArray();
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseWPos(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				Span<Range> ranges = stackalloc Range[4];
				var parts = value.Span.Split(ranges, Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts == 3
					&& WDist.TryParse(value.Span[ranges[0]], out var rx)
					&& WDist.TryParse(value.Span[ranges[1]], out var ry)
					&& WDist.TryParse(value.Span[ranges[2]], out var rz))
					return new WPos(rx, ry, rz);
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseWAngle(string fieldName, Type fieldType, YamlValue value)
		{
			if (Exts.TryParseInt32Invariant(value.Span, out var res))
				return new WAngle(res);
			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseWRot(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				Span<Range> ranges = stackalloc Range[4];
				var parts = value.Span.Split(ranges, Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts == 3
					&& Exts.TryParseInt32Invariant(value.Span[ranges[0]], out var rr)
					&& Exts.TryParseInt32Invariant(value.Span[ranges[1]], out var rp)
					&& Exts.TryParseInt32Invariant(value.Span[ranges[2]], out var ry))
					return new WRot(new WAngle(rr), new WAngle(rp), new WAngle(ry));
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseCPos(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				Span<Range> ranges = stackalloc Range[4];
				var parts = value.Span.Split(ranges, Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts == 3
					&& Exts.TryParseInt32Invariant(value.Span[ranges[0]], out var x)
					&& Exts.TryParseInt32Invariant(value.Span[ranges[1]], out var y)
					&& Exts.TryParseByteInvariant(value.Span[ranges[2]], out var layer))
					return new CPos(x, y, layer);

				if (parts == 2
					&& Exts.TryParseInt32Invariant(value.Span[ranges[0]], out x)
					&& Exts.TryParseInt32Invariant(value.Span[ranges[1]], out y))
					return new CPos(x, y);
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseCPosArray(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				var res = new List<CPos>();
				var parts = value.Span.Split(Comma);
				Span<int> elements = stackalloc int[2];
				var index = 0;
				foreach (var part in parts)
				{
					var p = part.Trim(); // StringSplitOptions.TrimEntries
					if (p.IsEmpty) continue; // StringSplitOptions.RemoveEmptyEntries

					if (!Exts.TryParseInt32Invariant(p, out var element))
						return InvalidValueAction(value.Span, fieldType, fieldName);

					elements[index++] = element;
					if (index == elements.Length)
					{
						index = 0;
						res.Add(new CPos(elements[0], elements[1]));
					}
				}

				if (index != 0)
					return InvalidValueAction(value.Span, fieldType, fieldName);

				return res.ToArray();
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseCVec(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				Span<Range> ranges = stackalloc Range[3];
				var parts = value.Span.Split(ranges, Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts == 2
					&& Exts.TryParseInt32Invariant(value.Span[ranges[0]], out var x)
					&& Exts.TryParseInt32Invariant(value.Span[ranges[1]], out var y))
					return new CVec(x, y);
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseCVecArray(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				var res = new List<CVec>();
				var parts = value.Span.Split(Comma);
				Span<int> elements = stackalloc int[2];
				var index = 0;
				foreach (var part in parts)
				{
					var p = part.Trim(); // StringSplitOptions.TrimEntries
					if (p.IsEmpty) continue; // StringSplitOptions.RemoveEmptyEntries

					if (!Exts.TryParseInt32Invariant(p, out var element))
						return InvalidValueAction(value.Span, fieldType, fieldName);

					elements[index++] = element;
					if (index == elements.Length)
					{
						index = 0;
						res.Add(new CVec(elements[0], elements[1]));
					}
				}

				if (index != 0)
					return InvalidValueAction(value.Span, fieldType, fieldName);

				return res.ToArray();
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseBooleanExpression(string fieldName, Type fieldType, YamlValue value)
		{
			try
			{
				return BooleanExpressionCache[value.ToString()];
			}
			catch (InvalidDataException e)
			{
				throw new YamlException($"FieldLoader: Cannot parse `{value.Span}` into field `{fieldName}` of type `{fieldType}`: {e.Message}");
			}
		}

		static object ParseIntegerExpression(string fieldName, Type fieldType, YamlValue value)
		{
			try
			{
				return IntegerExpressionCache[value.ToString()];
			}
			catch (InvalidDataException e)
			{
				throw new YamlException($"FieldLoader: Cannot parse `{value.Span}` into field `{fieldName}` of type `{fieldType}`: {e.Message}");
			}
		}

		static object ParseEnum(string fieldName, Type fieldType, YamlValue value)
		{
			// Will allow numeric values that fit the underlying type of the enum, even if they aren't defined enumeration members.
			if (Enum.TryParse(fieldType, value.Span, true, out var enumValue))
			{
				return enumValue;
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseBool(string fieldName, Type fieldType, YamlValue value)
		{
			if (bool.TryParse(value.Span, out var result))
				return result ? BoxedTrue : BoxedFalse;

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseInt2Array(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				var res = new List<int2>();
				var parts = value.Span.Split(Comma);
				Span<int> elements = stackalloc int[2];
				var index = 0;
				foreach (var part in parts)
				{
					var p = part.Trim(); // StringSplitOptions.TrimEntries
					if (p.IsEmpty) continue; // StringSplitOptions.RemoveEmptyEntries

					if (!Exts.TryParseInt32Invariant(p, out var element))
						return InvalidValueAction(value.Span, fieldType, fieldName);

					elements[index++] = element;
					if (index == elements.Length)
					{
						index = 0;
						res.Add(new int2(elements[0], elements[1]));
					}
				}

				if (index != 0)
					return InvalidValueAction(value.Span, fieldType, fieldName);

				return res.ToArray();
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseSize(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				Span<Range> ranges = stackalloc Range[3];
				var parts = value.Span.Split(ranges, Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts == 2
					&& Exts.TryParseInt32Invariant(value.Span[ranges[0]], out var width)
					&& Exts.TryParseInt32Invariant(value.Span[ranges[1]], out var height))
					return new Size(width, height);
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseInt2(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				Span<Range> ranges = stackalloc Range[3];
				var parts = value.Span.Split(ranges, Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts == 2
					&& Exts.TryParseInt32Invariant(value.Span[ranges[0]], out var x)
					&& Exts.TryParseInt32Invariant(value.Span[ranges[1]], out var y))
					return new int2(x, y);
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseVector2(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				Span<Range> ranges = stackalloc Range[3];
				var parts = value.Span.Split(ranges, Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts == 2
					&& Exts.TryParseFloatOrPercentInvariant(value.Span[ranges[0]], out var x)
					&& Exts.TryParseFloatOrPercentInvariant(value.Span[ranges[1]], out var y))
					return new Vector2(x, y);
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseVector3(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				Span<Range> ranges = stackalloc Range[4];
				var parts = value.Span.Split(ranges, Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts == 3
					&& Exts.TryParseFloatOrPercentInvariant(value.Span[ranges[0]], out var x)
					&& Exts.TryParseFloatOrPercentInvariant(value.Span[ranges[1]], out var y)
					&& Exts.TryParseFloatOrPercentInvariant(value.Span[ranges[2]], out var z))
					return new Vector3(x, y, z);

				// z component is optional for compatibility with older Vector2 definitions
				if (parts == 2
					&& Exts.TryParseFloatOrPercentInvariant(value.Span[ranges[0]], out x)
					&& Exts.TryParseFloatOrPercentInvariant(value.Span[ranges[1]], out y))
					return new Vector3(x, y, 0);
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseRectangle(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				Span<Range> ranges = stackalloc Range[5];
				var parts = value.Span.Split(ranges, Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts == 4
					&& Exts.TryParseInt32Invariant(value.Span[ranges[0]], out var x)
					&& Exts.TryParseInt32Invariant(value.Span[ranges[1]], out var y)
					&& Exts.TryParseInt32Invariant(value.Span[ranges[2]], out var width)
					&& Exts.TryParseInt32Invariant(value.Span[ranges[3]], out var height))
					return new Rectangle(x, y, width, height);
			}

			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseDateTime(string fieldName, Type fieldType, YamlValue value)
		{
			if (DateTime.TryParseExact(value.Span, "yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture,
					DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
				return dt;
			return InvalidValueAction(value.Span, fieldType, fieldName);
		}

		static object ParseArray(string fieldName, Type fieldType, YamlValue value)
		{
			var elementType = fieldType.GetElementType();

			if (value.Span.IsEmpty)
				return typeof(Array)
					.GetMethod(nameof(Array.Empty))
					.MakeGenericMethod(elementType)
					.Invoke(null, null);

			var objs = new List<object>();
			var parts = value.Span.Split(Comma);
			foreach (var part in parts)
			{
				var p = part.Trim(); // StringSplitOptions.TrimEntries
				if (p.IsEmpty) continue; // StringSplitOptions.RemoveEmptyEntries

				objs.Add(GetValue(fieldName, elementType, new YamlValue(p)));
			}

			var ret = Array.CreateInstance(elementType, objs.Count);
			for (var i = 0; i < objs.Count; i++)
				ret.SetValue(objs[i], i);
			return ret;
		}

		static object ParseHashSetOrList(string fieldName, Type fieldType, YamlValue value)
		{
			if (value.Span.IsEmpty)
				return Activator.CreateInstance(fieldType);

			var set = Activator.CreateInstance(fieldType);
			var arguments = fieldType.GetGenericArguments();
			var addMethod = fieldType.GetMethod(nameof(List<object>.Add), arguments);
			var addArgs = new object[1];
			var parts = value.Span.Split(Comma);
			foreach (var part in parts)
			{
				var p = part.Trim(); // StringSplitOptions.TrimEntries
				if (p.IsEmpty) continue; // StringSplitOptions.RemoveEmptyEntries

				addArgs[0] = GetValue(fieldName, arguments[0], new YamlValue(p));
				addMethod.Invoke(set, addArgs);
			}

			return set;
		}

		static object ParseDictionary(string fieldName, Type fieldType, ImmutableArray<MiniYamlNode> nodes)
		{
			if (nodes.Length == 0)
				return Activator.CreateInstance(fieldType);

			var dict = Activator.CreateInstance(fieldType, nodes.Length);
			var arguments = fieldType.GetGenericArguments();
			var addMethod = fieldType.GetMethod(nameof(Dictionary<object, object>.Add), arguments);
			var addArgs = new object[2];
			foreach (var node in nodes)
			{
				addArgs[0] = GetValue(fieldName, arguments[0], new YamlValue(node.Key));
				addArgs[1] = GetValue(fieldName, arguments[1], node.Value);
				addMethod.Invoke(dict, addArgs);
			}

			return dict;
		}

		static object ParseImmutableArray(string fieldName, Type fieldType, YamlValue value)
		{
			var typeArgs = fieldType.GenericTypeArguments;

			if (value.Span.IsEmpty)
				return typeof(ImmutableArray<>).MakeGenericType(typeArgs)
					.GetField(nameof(ImmutableArray<object>.Empty))
					.GetValue(null);

			object array;
			if (typeArgs[0] == typeof(WVec))
				array = ParseWVecArray(fieldName, typeArgs[0].MakeArrayType(), value);
			else if (typeArgs[0] == typeof(CPos))
				array = ParseCPosArray(fieldName, typeArgs[0].MakeArrayType(), value);
			else if (typeArgs[0] == typeof(CVec))
				array = ParseCVecArray(fieldName, typeArgs[0].MakeArrayType(), value);
			else if (typeArgs[0] == typeof(int2))
				array = ParseInt2Array(fieldName, typeArgs[0].MakeArrayType(), value);
			else
				array = ParseArray(fieldName, typeArgs[0].MakeArrayType(), value);

			var toImmutableArray = ToImmutableArray.MakeGenericMethod(typeArgs);

			return toImmutableArray.Invoke(null, [array]);
		}

		static object ParseFrozenSet(string fieldName, Type fieldType, YamlValue value)
		{
			var typeArgs = fieldType.GenericTypeArguments;

			if (value.Span.IsEmpty)
				return typeof(FrozenSet<>).MakeGenericType(typeArgs)
					.GetProperty(nameof(FrozenSet<object>.Empty))
					.GetValue(null);

			var set =
				ParseHashSetOrList(fieldName, typeof(HashSet<>).MakeGenericType(typeArgs), value);

			var toFrozenSet = ToFrozenSet.MakeGenericMethod(typeArgs);

			return toFrozenSet.Invoke(null, [set, null]);
		}

		static object ParseFrozenDictionary(string fieldName, Type fieldType, ImmutableArray<MiniYamlNode> nodes)
		{
			var typeArgs = fieldType.GenericTypeArguments;

			if (nodes.Length == 0)
				return typeof(FrozenDictionary<,>).MakeGenericType(typeArgs)
					.GetProperty(nameof(FrozenDictionary<object, object>.Empty))
					.GetValue(null);

			var dict =
				ParseDictionary(fieldName, typeof(Dictionary<,>).MakeGenericType(typeArgs), nodes);

			var toFrozenDict = ToFrozenDictionary.MakeGenericMethod(typeArgs);

			return toFrozenDict.Invoke(null, [dict, null]);
		}

		static object ParseBitSet(string fieldName, Type fieldType, YamlValue value)
		{
			if (!value.Span.IsEmpty)
			{
				var values = new List<string>();
				var parts = value.Span.Split(Comma);
				foreach (var part in parts)
				{
					var p = part.Trim(); // StringSplitOptions.TrimEntries
					if (p.IsEmpty) continue; // StringSplitOptions.RemoveEmptyEntries

					values.Add(p.ToString());
				}

				var ctor = fieldType.GetConstructor([typeof(string[])]);
				return ctor.Invoke([values.ToArray()]);
			}
			else
			{
				var ctor = fieldType.GetConstructor([typeof(string[])]);
				return ctor.Invoke([Array.Empty<string>()]);
			}
		}

		static object ParseNullable(string fieldName, Type fieldType, YamlValue value)
		{
			if (value.Span.IsEmpty)
				return null;

			var innerType = fieldType.GetGenericArguments()[0];
			var innerValue = GetValue("Nullable<T>", innerType, value);
			return fieldType.GetConstructor([innerType]).Invoke([innerValue]);
		}

		public static void Load(object self, MiniYaml my)
		{
			var loadInfo = TypeLoadInfo[self.GetType()];
			List<string> missing = null;

			Dictionary<string, MiniYaml> md = null;

			foreach (var fli in loadInfo)
			{
				object val;

				md ??= my.ToDictionary();
				if (fli.Loader != null)
				{
					if (!fli.Attribute.Required || md.ContainsKey(fli.YamlName))
						val = fli.Loader(my);
					else
					{
						missing ??= [];
						missing.Add(fli.YamlName);
						continue;
					}
				}
				else
				{
					if (!TryGetValueFromYaml(fli.YamlName, fli.Field, md, out val))
					{
						if (fli.Attribute.Required)
						{
							missing ??= [];
							missing.Add(fli.YamlName);
						}

						continue;
					}
				}

				fli.Field.SetValue(self, val);
			}

			if (missing != null)
				throw new MissingFieldsException(missing.ToArray());
		}

		static bool TryGetValueFromYaml(string yamlName, FieldInfo field, Dictionary<string, MiniYaml> md, out object ret)
		{
			ret = null;

			if (!md.TryGetValue(yamlName, out var yaml))
				return false;

			ret = GetValue(field.Name, field.FieldType, yaml);
			return true;
		}

		public static T Load<T>(MiniYaml y) where T : new()
		{
			var t = new T();
			Load(t, y);
			return t;
		}

		public static void LoadFieldOrProperty(object target, string key, string value)
		{
			const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

			key = key.Trim();

			var field = target.GetType().GetField(key, Flags);
			if (field != null)
			{
				field.SetValue(target, GetValue(field.Name, field.FieldType, new YamlValue(value)));
				return;
			}

			var prop = target.GetType().GetProperty(key, Flags);
			if (prop != null)
			{
				prop.SetValue(target, GetValue(prop.Name, prop.PropertyType, new YamlValue(value)), null);
				return;
			}

			UnknownFieldAction(key);
		}

		public static T GetValue<T>(string field, string value)
		{
			return (T)GetValue(field, typeof(T), new YamlValue(value));
		}

		static object GetValue(string fieldName, Type fieldType, YamlValue value)
		{
			return GetValue(fieldName, fieldType, value, []);
		}

		static object GetValue(string fieldName, Type fieldType, MiniYaml yaml)
		{
			return GetValue(fieldName, fieldType, new YamlValue(yaml.Value), yaml.Nodes);
		}

		static object GetValue(string fieldName, Type fieldType, YamlValue value, ImmutableArray<MiniYamlNode> nodes)
		{
			value = value.Trim();
			if (fieldType.IsGenericType)
			{
				if (GenericTypeValueParsers.TryGetValue(fieldType.GetGenericTypeDefinition(), out var parseValueFuncGeneric))
					return parseValueFuncGeneric(fieldName, fieldType, value);

				if (GenericTypeNodesParsers.TryGetValue(fieldType.GetGenericTypeDefinition(), out var parseNodesFuncGeneric))
					return parseNodesFuncGeneric(fieldName, fieldType, nodes);
			}
			else
			{
				if (TypeParsers.TryGetValue(fieldType, out var parseFunc))
					return parseFunc(fieldName, fieldType, value);

				if (fieldType.IsArray && fieldType.GetArrayRank() == 1)
					return ParseArray(fieldName, fieldType, value);

				if (fieldType.IsEnum)
					return ParseEnum(fieldName, fieldType, value);
			}

			var conv = TypeDescriptor.GetConverter(fieldType);
			if (conv.CanConvertFrom(typeof(string)))
			{
				try
				{
					return conv.ConvertFromInvariantString(value.ToString());
				}
				catch
				{
					return InvalidValueAction(value.Span, fieldType, fieldName);
				}
			}

			UnknownFieldAction(fieldName);
			return null;
		}

		readonly ref struct YamlValue
		{
			public YamlValue(string value)
				: this(value.AsSpan())
			{
				this.value = value;
			}

			public YamlValue(ReadOnlySpan<char> valueSpan)
			{
				this.valueSpan = valueSpan;
			}

			readonly string value;
			readonly ReadOnlySpan<char> valueSpan;

			public readonly ReadOnlySpan<char> Span => valueSpan;

			public readonly YamlValue Trim()
			{
				var trimmed = valueSpan.Trim();
				if (trimmed != valueSpan)
					return new YamlValue(trimmed);
				return this;
			}

			/// <summary>
			/// If the input came from a string, returns the original string instance. Otherwise, allocates a string from the span.
			/// </summary>
			public override readonly string ToString()
			{
				// When the source string is null, valueSpan.ToString() will return an empty string.
				// We don't want to return an empty string, we want to return null, so special case this.
				if (value == null && valueSpan == ReadOnlySpan<char>.Empty)
					return null;

				// If the input value came from a string, we can return it directly and avoid allocating a new one from the span.
				// This can be quite important as MiniYaml may have de-duplicated repeated strings from config files.
				// When we take a span over the original string, having to ToString it again recreates the duplicates, so we are keen to avoid that.
				return value ?? valueSpan.ToString();
			}
		}

		public sealed class FieldLoadInfo
		{
			public readonly FieldInfo Field;
			public readonly SerializeAttribute Attribute;
			public readonly Func<MiniYaml, object> Loader;
			public string YamlName => Field.Name;

			public FieldLoadInfo(FieldInfo field, SerializeAttribute attr, Func<MiniYaml, object> loader = null)
			{
				Field = field;
				Attribute = attr;
				Loader = loader;
			}
		}

		public static IEnumerable<FieldLoadInfo> GetTypeLoadInfo(Type type)
		{
			return TypeLoadInfo[type].Where(fli => fli.Field.IsPublic || (fli.Attribute.Serialize && !fli.Attribute.IsDefault));
		}

		static FieldLoadInfo[] BuildTypeLoadInfo(Type type)
		{
			var ret = new List<FieldLoadInfo>();

			foreach (var ff in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				var field = ff;

				var sa = field.GetCustomAttributes<SerializeAttribute>(false).DefaultIfEmpty(SerializeAttribute.Default).First();
				if (!sa.Serialize)
					continue;

				var loader = sa.GetLoader(type);

				var fli = new FieldLoadInfo(field, sa, loader);
				ret.Add(fli);
			}

			return ret.ToArray();
		}

		[AttributeUsage(AttributeTargets.Field)]
		public sealed class IgnoreAttribute : SerializeAttribute
		{
			public IgnoreAttribute()
				: base(serialize: false) { }
		}

		[AttributeUsage(AttributeTargets.Field)]
		public sealed class RequireAttribute : SerializeAttribute
		{
			public RequireAttribute()
				: base(serialize: true, required: true) { }
		}

		[AttributeUsage(AttributeTargets.Field)]
		public sealed class LoadUsingAttribute : SerializeAttribute
		{
			public LoadUsingAttribute(string loader, bool required = false)
				: base(serialize: true, required, loader) { }
		}

		[AttributeUsage(AttributeTargets.Field)]
		public class SerializeAttribute : Attribute
		{
			public static readonly SerializeAttribute Default = new(true);

			public bool IsDefault => this == Default;

			public readonly bool Serialize;
			public readonly bool Required;
			public readonly string Loader;

			protected SerializeAttribute(bool serialize = true, bool required = false, string loader = null)
			{
				Serialize = serialize;
				Required = required;
				Loader = loader;
			}

			internal Func<MiniYaml, object> GetLoader(Type type)
			{
				const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

				if (!string.IsNullOrEmpty(Loader))
				{
					var method = type.GetMethod(Loader, Flags);
					if (method == null)
						throw new InvalidOperationException($"{type.Name} does not specify a loader function '{Loader}'");

					return (Func<MiniYaml, object>)Delegate.CreateDelegate(typeof(Func<MiniYaml, object>), method);
				}

				return null;
			}
		}
	}
}
