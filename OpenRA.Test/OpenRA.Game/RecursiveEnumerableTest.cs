#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Support;

namespace OpenRA.Test
{
	[TestFixture]
	public class RecursiveEnumerableTest
	{
		[TestCase(TestName = "single string")]
		public void GetStringsFromSingle()
		{
			object value = "hello";
			var result = value.AsRecursiveEnumerable<string>().ToArray();
			Assert.AreEqual(1, result.Length, "Wrong length");
			Assert.True(result.Length == 1 && result[0] == "hello", "Wrong string");
		}

		[TestCase(TestName = "string array")]
		public void GetStringsFromStringArray()
		{
			object value = new string[] { "hello", "world" };
			var result = value.AsRecursiveEnumerable<string>().ToArray();
			Assert.AreEqual(2, result.Length, "Wrong length");
			Assert.True(result.Length >= 1 && result[0] == "hello", "Missing 'hello'");
			Assert.True(result.Length > 1 && result[1] == "world", "Missing 'World'");
		}

		[TestCase(TestName = "string array array")]
		public void GetStringsFromStringArrayArray()
		{
			object value = new string[][] { new string[] { "hello", "world" } };
			var result = value.AsRecursiveEnumerable<string>().ToArray();
			Assert.AreEqual(2, result.Length, "Wrong length");
			Assert.True(result.Length >= 1 && result[0] == "hello", "Missing 'hello'");
			Assert.True(result.Length > 1 && result[1] == "world", "Missing 'World'");
		}

		[TestCase(TestName = "Dictionary<string, string[]>")]
		public void GetStringsFromStringStringArrayDictionary()
		{
			object value = new Dictionary<string, string[]> { { "message", new string[] { "hello", "world" } } };
			var result = value.AsRecursiveEnumerable<string>().ToArray();
			Assert.AreEqual(3, result.Length, "Wrong length");
			Assert.True(result.Length >= 1 && result[0] == "message", "Missing 'message'");
			Assert.True(result.Length > 1 && result[1] == "hello", "Missing 'hello'");
			Assert.True(result.Length > 2 && result[2] == "world", "Missing 'World'");
		}

		[TestCase(TestName = "Dictionary<string, string[][]>")]
		public void GetStringsFromStringStringArrayArrayDictionary()
		{
			object value = new Dictionary<string, string[][]> { { "message", new string[][] { new string[] { "hello" }, new string[] { "world" } } } };
			var result = value.AsRecursiveEnumerable<string>().ToArray();
			Assert.AreEqual(3, result.Length, "Wrong length");
			Assert.True(result.Length >= 1 && result[0] == "message", "Missing 'message'");
			Assert.True(result.Length > 1 && result[1] == "hello", "Missing 'hello'");
			Assert.True(result.Length > 2 && result[2] == "world", "Missing 'World'");
		}

		[TestCase(TestName = "Dictionary<string, List<string[]>>")]
		public void GetStringsFromStringStringArrayListDictionary()
		{
			object value = new Dictionary<string, List<string[]>> { { "message", new List<string[]> { new string[] { "hello" }, new string[] { "world" } } } };
			var result = value.AsRecursiveEnumerable<string>().ToArray();
			Assert.AreEqual(3, result.Length, "Wrong length");
			Assert.True(result.Length >= 1 && result[0] == "message", "Missing 'message'");
			Assert.True(result.Length > 1 && result[1] == "hello", "Missing 'hello'");
			Assert.True(result.Length > 2 && result[2] == "world", "Missing 'World'");
		}

		// Note: object type neither is a string type nor acts a collection of strings.
		[TestCase(TestName = "object array string array")]
		public void GetStringsFromObjectArrayStringArray()
		{
			object value = new object[] { new string[] { "hello", "world" } };
			var result = value.AsRecursiveEnumerable<string>().ToArray();
			Assert.AreEqual(0, result.Length, "Wrong length");
		}

		[TestCase(TestName = "single GrantedVariableReference<bool>")]
		public void GetGrantedConditionReferenceFromSingle()
		{
			object value = new GrantedVariableReference<bool>("hello");
			var result = value.AsRecursiveEnumerable<IWithGrantedVariables>().ToArray();
			Assert.AreEqual(1, result.Length, "Wrong source count");
			Assert.AreEqual(1, result.SelectMany(r => r.GetGrantedVariables()).Count(), "Wrong source count");
			Assert.True(result.SelectMany(r => r.GetGrantedVariables()).Any() && result.SelectMany(r => r.GetGrantedVariables()).First().Key == "hello", "Wrong string");
		}

		[TestCase(TestName = "GrantedVariableReference<bool> array")]
		public void GetGrantedConditionReferencesFromGrantedConditionReferenceArray()
		{
			object value = new GrantedVariableReference<bool>[]
			{
				new GrantedVariableReference<bool>("hello"),
				new GrantedVariableReference<bool>("world")
			};
			var result = value.AsRecursiveEnumerable<IWithGrantedVariables>().ToArray();
			Assert.AreEqual(2, result.Length, "Wrong source count");
			Assert.AreEqual(2, result.SelectMany(r => r.GetGrantedVariables()).Count(), "Wrong source count");
			Assert.True(result.SelectMany(r => r.GetGrantedVariables()).Any() && result.SelectMany(r => r.GetGrantedVariables()).First().Key == "hello", "Missing 'hello'");
			Assert.True(result.SelectMany(r => r.GetGrantedVariables()).Any() && result.SelectMany(r => r.GetGrantedVariables()).Last().Key == "world", "Missing 'World'");
		}

		[TestCase(TestName = "GrantedVariableReference<bool> array array")]
		public void GetIVariablesReferencesFromGrantedConditionReferenceArrayArray()
		{
			object value = new GrantedVariableReference<bool>[][]
			{
				new GrantedVariableReference<bool>[]
				{
					new GrantedVariableReference<bool>("hello"),
					new GrantedVariableReference<bool>("world")
				}
			};
			var result = value.AsRecursiveEnumerable<IWithGrantedVariables>().ToArray();
			Assert.AreEqual(2, result.Length, "Wrong source count");
			Assert.AreEqual(2, result.SelectMany(r => r.GetGrantedVariables()).Count(), "Wrong variable count");
			Assert.True(result.SelectMany(r => r.GetGrantedVariables()).Any() && result.SelectMany(r => r.GetGrantedVariables()).First().Key == "hello", "Missing 'hello'");
			Assert.True(result.SelectMany(r => r.GetGrantedVariables()).Any() && result.SelectMany(r => r.GetGrantedVariables()).Last().Key == "world", "Missing 'World'");
		}

		[TestCase(TestName = "Dictionary<GrantedVariableReference<bool>, BooleanExpression>")]
		public void GetStringsFromGrantedConditionReferenceBooleanExpressionyDictionary()
		{
			object value = new Dictionary<GrantedVariableReference<bool>, BooleanExpression>
			{
				{ new GrantedVariableReference<bool>("message"), new BooleanExpression("hello && world") }
			};
			var granted = value.AsRecursiveEnumerable<IWithGrantedVariables>().ToArray();
			var used = value.AsRecursiveEnumerable<IWithUsedVariables>().ToArray();
			Assert.AreEqual(1, used.Length, "Wrong used source count");
			Assert.AreEqual(1, used.Length, "Wrong granted source count");
			Assert.AreEqual(2, used.GetUsedVariables().Count(), "Wrong used variables count");
			Assert.AreEqual(1, granted.GetGrantedVariables().Count(), "Wrong granted variables count");
			Assert.True(granted.Length > 0 && granted[0].GetGrantedVariables().Any() && granted[0].GetGrantedVariables().First().Key == "message", "Missing 'message'");
			Assert.True(used.Length > 0 && used[0].GetUsedVariables().FirstOrDefault() == "hello", "Missing 'hello'");
			Assert.True(used.Length > 0 && used[0].GetUsedVariables().LastOrDefault() == "world", "Missing 'World'");
		}
	}
}
