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

namespace OpenRA.Support
{
	public class BooleanExpression
	{
		public readonly string Expression;
		readonly HashSet<string> variables = new HashSet<string>();
		public IEnumerable<string> Variables { get { return variables; } }

		readonly Token[] postfix;

		enum Associativity { Left, Right }
		class Token
		{
			public readonly string Symbol;
			public readonly int Index;
			public readonly int Precedence;
			public readonly Associativity Associativity;

			public Token(string symbol, int index, Associativity associativity, int precedence)
			{
				Symbol = symbol;
				Index = index;
				Associativity = associativity;
				Precedence = precedence;
			}
		}

		class BinaryOperationToken : Token
		{
			public BinaryOperationToken(string symbol, int index, Associativity associativity = Associativity.Left, int precedence = 0)
				: base(symbol, index, associativity, precedence) { }
		}

		class UnaryOperationToken : Token
		{
			public UnaryOperationToken(string symbol, int index, Associativity associativity = Associativity.Right, int precedence = 1)
				: base(symbol, index, associativity, precedence) { }
		}

		class OpenParenToken : Token { public OpenParenToken(int index) : base("(", index, Associativity.Left, -1) { } }
		class CloseParenToken : Token { public CloseParenToken(int index) : base(")", index, Associativity.Left, -1) { } }
		class VariableToken : Token
		{
			public VariableToken(int index, string symbol)
				: base(symbol, index, Associativity.Left, 0) { }
		}

		class AndToken : BinaryOperationToken { public AndToken(int index) : base("&&", index) { } }
		class OrToken : BinaryOperationToken { public OrToken(int index) : base("||", index) { } }
		class EqualsToken : BinaryOperationToken { public EqualsToken(int index) : base("==", index) { } }
		class NotEqualsToken : BinaryOperationToken { public NotEqualsToken(int index) : base("!=", index) { } }
		class NotToken : UnaryOperationToken { public NotToken(int index) : base("!", index) { } }

		public BooleanExpression(string expression)
		{
			Expression = expression;
			var openParens = 0;
			var closeParens = 0;
			var tokens = new List<Token>();
			for (var i = 0; i < expression.Length; i++)
			{
				switch (expression[i])
				{
					case '(':
					{
						tokens.Add(new OpenParenToken(i));
						openParens++;
						break;
					}

					case ')':
					{
						tokens.Add(new CloseParenToken(i));
						if (++closeParens > openParens)
							throw new InvalidDataException("Unmatched closing parenthesis at index {0}".F(i));

						break;
					}

					default:
					{
						// Ignore whitespace
						if (char.IsWhiteSpace(expression[i]))
							break;

						var token = ParseSymbol(expression, ref i);
						tokens.Add(token);

						var variable = token as VariableToken;
						if (variable != null)
							variables.Add(variable.Symbol);

						break;
					}
				}
			}

			// Sanity check parsed tree
			if (!tokens.Any())
				throw new InvalidDataException("Empty expression");

			if (closeParens != openParens)
				throw new InvalidDataException("Mismatched opening and closing parentheses");

			for (var i = 0; i < tokens.Count - 1; i++)
			{
				// Unary tokens must be followed by a variable, another unary token, or an opening parenthesis
				if (tokens[i] is UnaryOperationToken && !(tokens[i + 1] is VariableToken || tokens[i + 1] is UnaryOperationToken
						|| tokens[i + 1] is OpenParenToken))
					throw new InvalidDataException("Unexpected token `{0}` at index {1}".F(tokens[i].Symbol, tokens[i].Index));

				// Disallow empty parentheses
				if (tokens[i] is OpenParenToken && tokens[i + 1] is CloseParenToken)
					throw new InvalidDataException("Empty parenthesis at index {0}".F(tokens[i].Index));

				// A variable must be followed by a binary operation or by a closing parenthesis
				if (tokens[i] is VariableToken && !(tokens[i + 1] is BinaryOperationToken || tokens[i + 1] is CloseParenToken))
					throw new InvalidDataException("Missing binary operation at index {0}".F(tokens[i + 1].Index));
			}

			// Expressions can't start with an operation
			if (tokens[0] is BinaryOperationToken)
				throw new InvalidDataException("Unexpected token `{0}` at index `{1}`".F(tokens[0].Symbol, tokens[0].Index));

			// Expressions can't end with a binary or unary operation
			if (tokens[tokens.Count - 1] is BinaryOperationToken || tokens[tokens.Count - 1] is UnaryOperationToken)
				throw new InvalidDataException("Unexpected token `{0}` at index `{1}`".F(tokens[tokens.Count - 1].Symbol, tokens[tokens.Count - 1].Index));

			// Binary operations must be preceeded by a closing paren or a variable
			// Binary operations must be followed by an opening paren, a variable, or a unary operation
			for (var i = 1; i < tokens.Count - 1; i++)
			{
				if (tokens[i] is BinaryOperationToken && (
					!(tokens[i - 1] is CloseParenToken || tokens[i - 1] is VariableToken) ||
					!(tokens[i + 1] is OpenParenToken || tokens[i + 1] is VariableToken || tokens[i + 1] is UnaryOperationToken)))
					throw new InvalidDataException("Unexpected token `{0}` at index `{1}`".F(tokens[i].Symbol, tokens[i].Index));
			}

			// Convert to postfix (discarding parentheses) ready for evaluation
			postfix = ToPostfix(tokens).ToArray();
		}

		static Token ParseSymbol(string expression, ref int i)
		{
			var start = i;
			var c = expression[start];

			// Parse operators
			if (c == '!')
			{
				if (i < expression.Length - 1 && expression[start + 1] == '=')
				{
					i++;
					return new NotEqualsToken(start);
				}

				return new NotToken(start);
			}

			if (c == '=')
			{
				if (i < expression.Length - 1 && expression[start + 1] == '=')
				{
					i++;
					return new EqualsToken(start);
				}

				throw new InvalidDataException("Unexpected character '=' at index {0}".F(start));
			}

			if (c == '&')
			{
				if (i < expression.Length - 1 && expression[start + 1] == '&')
				{
					i++;
					return new AndToken(start);
				}

				throw new InvalidDataException("Unexpected character '&' at index {0}".F(start));
			}

			if (c == '|')
			{
				if (i < expression.Length - 1 && expression[start + 1] == '|')
				{
					i++;
					return new OrToken(start);
				}

				throw new InvalidDataException("Unexpected character '|' at index {0}".F(start));
			}

			// Scan forwards until we find an invalid name character
			for (; i < expression.Length; i++)
			{
				c = expression[i];
				if (char.IsWhiteSpace(c) || c == '(' || c == ')' || c == '!' || c == '&' || c == '|' || c == '=')
				{
					// Put the bad character back for the next parse attempt
					i--;
					return new VariableToken(start, expression.Substring(start, i - start + 1));
				}
			}

			// Take the rest of the string
			return new VariableToken(start, expression.Substring(start));
		}

		static bool ParseSymbol(VariableToken t, IReadOnlyDictionary<string, bool> symbols)
		{
			bool value;
			symbols.TryGetValue(t.Symbol, out value);
			return value;
		}

		static void ApplyBinaryOperation(Stack<bool> s, Func<bool, bool, bool> f)
		{
			var x = s.Pop();
			var y = s.Pop();
			s.Push(f(x, y));
		}

		static void ApplyUnaryOperation(Stack<bool> s, Func<bool, bool> f)
		{
			var x = s.Pop();
			s.Push(f(x));
		}

		static IEnumerable<Token> ToPostfix(IEnumerable<Token> tokens)
		{
			var s = new Stack<Token>();
			foreach (var t in tokens)
			{
				if (t is OpenParenToken)
					s.Push(t);
				else if (t is CloseParenToken)
				{
					Token temp;
					while (!((temp = s.Pop()) is OpenParenToken))
						yield return temp;
				}
				else if (t is VariableToken)
					yield return t;
				else
				{
					while (s.Count > 0 && ((t.Associativity == Associativity.Right && t.Precedence < s.Peek().Precedence)
						|| (t.Associativity == Associativity.Left && t.Precedence <= s.Peek().Precedence)))
						yield return s.Pop();

					s.Push(t);
				}
			}

			while (s.Count > 0)
				yield return s.Pop();
		}

		public bool Evaluate(IReadOnlyDictionary<string, bool> symbols)
		{
			var s = new Stack<bool>();
			foreach (var t in postfix)
			{
				if (t is AndToken)
					ApplyBinaryOperation(s, (x, y) => y & x);
				else if (t is NotEqualsToken)
					ApplyBinaryOperation(s, (x, y) => y ^ x);
				else if (t is OrToken)
					ApplyBinaryOperation(s, (x, y) => y | x);
				else if (t is EqualsToken)
					ApplyBinaryOperation(s, (x, y) => y == x);
				else if (t is NotToken)
					ApplyUnaryOperation(s, x => !x);
				else if (t is VariableToken)
					s.Push(ParseSymbol((VariableToken)t, symbols));
			}

			return s.Pop();
		}
	}
}
