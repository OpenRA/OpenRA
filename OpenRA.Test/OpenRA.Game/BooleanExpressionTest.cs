#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Support;

namespace OpenRA.Test
{
	[TestFixture]
	public class BooleanExpressionTest
	{
		Dictionary<string, bool> testValues = new Dictionary<string, bool>()
		{
			{ "true", true },
			{ "false", false }
		};

		void AssertFalse(string expression)
		{
			Assert.False(new BooleanExpression(expression).Evaluate(testValues), expression);
		}

		void AssertTrue(string expression)
		{
			Assert.True(new BooleanExpression(expression).Evaluate(testValues), expression);
		}

		void AssertParseFailure(string expression)
		{
			Assert.Throws(typeof(InvalidDataException), () => new BooleanExpression(expression).Evaluate(testValues), expression);
		}

		[TestCase(TestName = "AND operation")]
		public void TestAnd()
		{
			AssertTrue("true && true");
			AssertFalse("false && false");
			AssertFalse("true && false");
			AssertFalse("false && true");
		}

		[TestCase(TestName = "OR operation")]
		public void TestOR()
		{
			AssertTrue("true || true");
			AssertFalse("false || false");
			AssertTrue("true || false");
			AssertTrue("false || true");
		}

		[TestCase(TestName = "Equals operation")]
		public void TestEquals()
		{
			AssertTrue("true == true");
			AssertTrue("false == false");
			AssertFalse("true == false");
			AssertFalse("false == true");
		}

		[TestCase(TestName = "Not-equals (XOR) operation")]
		public void TestNotEquals()
		{
			AssertFalse("true != true");
			AssertFalse("false != false");
			AssertTrue("true != false");
			AssertTrue("false != true");
		}

		[TestCase(TestName = "NOT operation")]
		public void TestNOT()
		{
			AssertFalse("!true");
			AssertTrue("!false");
			AssertTrue("!!true");
			AssertFalse("!!false");
		}

		[TestCase(TestName = "Precedence")]
		public void TestPrecedence()
		{
			AssertTrue("true && false || true");
			AssertFalse("false || false && true");
			AssertTrue("true && !true || !false");
			AssertFalse("false || !true && !false");
		}

		[TestCase(TestName = "Parenthesis")]
		public void TestParens()
		{
			AssertTrue("(true)");
			AssertTrue("((true))");
			AssertFalse("(false)");
			AssertFalse("((false))");
		}

		[TestCase(TestName = "Parenthesis and mixed operations")]
		public void TestMixedParens()
		{
			AssertTrue("(!false)");
			AssertTrue("!(false)");
			AssertFalse("!(!false)");
			AssertTrue("(true) || (false)");
			AssertTrue("true && (false || true)");
			AssertTrue("(true && false) || true");
			AssertTrue("!(true && false) || false");
			AssertTrue("((true != true) == false) && true");
			AssertFalse("(true != false) == false && true");
			AssertTrue("true || ((true != false) != !(false && true))");
			AssertFalse("((true != false) != !(false && true))");
		}

		[TestCase(TestName = "Test parser errors")]
		public void TestParseErrors()
		{
			AssertParseFailure("()");
			AssertParseFailure("! && true");
			AssertParseFailure("(true");
			AssertParseFailure(")true");
			AssertParseFailure("false)");
			AssertParseFailure("false(");
			AssertParseFailure("false!");
			AssertParseFailure("true false");
			AssertParseFailure("true & false");
			AssertParseFailure("true | false");
			AssertParseFailure("true / false");
			AssertParseFailure("true & false && !");
			AssertParseFailure("(true && !)");
			AssertParseFailure("&& false");
			AssertParseFailure("false ||");
		}

		[TestCase(TestName = "Undefined symbols are treated as `false` values")]
		public void TestUndefinedSymbols()
		{
			AssertFalse("undef1 || undef2");
		}
	}
}
