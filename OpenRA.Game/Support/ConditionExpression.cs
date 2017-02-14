#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
	public class ConditionExpression
	{
		public readonly string Expression;
		readonly HashSet<string> variables = new HashSet<string>();
		public IEnumerable<string> Variables { get { return variables; } }

		readonly Token[] postfix;

		enum CharClass { Whitespace, Operator, Mixed, Id, Digit }

		static CharClass CharClassOf(char c)
		{
			switch (c)
			{
				case '~':
				case '!':
				case '%':
				case '^':
				case '&':
				case '*':
				case '(':
				case ')':
				case '+':
				case '=':
				case '[':
				case ']':
				case '{':
				case '}':
				case '|':
				case ':':
				case ';':
				case '\'':
				case '"':
				case '<':
				case '>':
				case '?':
				case ',':
				case '/':
					return CharClass.Operator;

				case '.':
				case '$':
				case '-':
				case '@':
					return CharClass.Mixed;

				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					return CharClass.Digit;

					// Fast-track normal whitespace
				case ' ':
				case '\t':
				case '\n':
				case '\r':
					return CharClass.Whitespace;

					// Should other whitespace be tested?
				default:
					return char.IsWhiteSpace(c) ? CharClass.Whitespace : CharClass.Id;
			}
		}

		enum Associativity { Left, Right }

		[Flags]
		enum OperandSides
		{
			// Value type
			None = 0,

			// Postfix unary operator and/or group closer
			Left = 1,

			// Prefix unary operator and/or group opener
			Right = 2,

			// Binary+ operator
			Both = Left | Right
		}

		enum Grouping { None, Parens }

		enum TokenType
		{
			Number,
			Variable,
			OpenParen,
			CloseParen,
			Not,
			And,
			Or,
			Equals,
			NotEquals,
			Invalid
		}

		enum Precedence
		{
			Invalid = ~0,
			Parens = -1,
			Value = 0,
			Unary = 1,
			Binary = 0
		}

		struct TokenTypeInfo
		{
			public readonly string Symbol;
			public readonly Precedence Precedence;
			public readonly OperandSides OperandSides;
			public readonly Associativity Associativity;
			public readonly Grouping Opens;
			public readonly Grouping Closes;

			public TokenTypeInfo(string symbol, Precedence precedence, OperandSides operandSides = OperandSides.None,
			                     Associativity associativity = Associativity.Left,
			                     Grouping opens = Grouping.None, Grouping closes = Grouping.None)
			{
				Symbol = symbol;
				Precedence = precedence;
				OperandSides = operandSides;
				Associativity = associativity;
				Opens = opens;
				Closes = closes;
			}

			public TokenTypeInfo(string symbol, Precedence precedence, Grouping opens, Grouping closes = Grouping.None,
			                     Associativity associativity = Associativity.Left)
			{
				Symbol = symbol;
				Precedence = precedence;
				OperandSides = opens == Grouping.None ?
				                                (closes == Grouping.None ? OperandSides.None : OperandSides.Left)
				                                :
				                                (closes == Grouping.None ? OperandSides.Right : OperandSides.Both);
				Associativity = associativity;
				Opens = opens;
				Closes = closes;
			}
		}

		static IEnumerable<TokenTypeInfo> CreateTokenTypeInfoEnumeration()
		{
			for (var i = 0; i <= (int)TokenType.Invalid; i++)
			{
				switch ((TokenType)i)
				{
					case TokenType.Invalid:
						yield return new TokenTypeInfo("(<INVALID>)", Precedence.Invalid);
						continue;
					case TokenType.Number:
						yield return new TokenTypeInfo("(<number>)", Precedence.Value);
						continue;
					case TokenType.Variable:
						yield return new TokenTypeInfo("(<variable>)", Precedence.Value);
						continue;
					case TokenType.OpenParen:
						yield return new TokenTypeInfo("(", Precedence.Parens, Grouping.Parens);
						continue;
					case TokenType.CloseParen:
						yield return new TokenTypeInfo(")", Precedence.Parens, Grouping.None, Grouping.Parens);
						continue;
					case TokenType.Not:
						yield return new TokenTypeInfo("!", Precedence.Unary, OperandSides.Right, Associativity.Right);
						continue;
					case TokenType.And:
						yield return new TokenTypeInfo("&&", Precedence.Binary, OperandSides.Both);
						continue;
					case TokenType.Or:
						yield return new TokenTypeInfo("||", Precedence.Binary, OperandSides.Both);
						continue;
					case TokenType.Equals:
						yield return new TokenTypeInfo("==", Precedence.Binary, OperandSides.Both);
						continue;
					case TokenType.NotEquals:
						yield return new TokenTypeInfo("!=", Precedence.Binary, OperandSides.Both);
						continue;
				}

				throw new InvalidProgramException("CreateTokenTypeInfoEnumeration is missing a TokenTypeInfo entry for TokenType.{0}".F(
					Enum<TokenType>.GetValues()[i]));
			}
		}

		static readonly TokenTypeInfo[] TokenTypeInfos = CreateTokenTypeInfoEnumeration().ToArray();

		class Token
		{
			public readonly TokenType Type;
			public readonly int Index;

			public virtual string Symbol { get { return TokenTypeInfos[(int)Type].Symbol; } }

			public int Precedence { get { return (int)TokenTypeInfos[(int)Type].Precedence; } }
			public OperandSides OperandSides { get { return TokenTypeInfos[(int)Type].OperandSides; } }
			public Associativity Associativity { get { return TokenTypeInfos[(int)Type].Associativity; } }
			public bool LeftOperand { get { return ((int)TokenTypeInfos[(int)Type].OperandSides & (int)OperandSides.Left) != 0; } }
			public bool RightOperand { get { return ((int)TokenTypeInfos[(int)Type].OperandSides & (int)OperandSides.Right) != 0; } }

			public Grouping Opens { get { return TokenTypeInfos[(int)Type].Opens; } }
			public Grouping Closes { get { return TokenTypeInfos[(int)Type].Closes; } }

			public Token(TokenType type, int index)
			{
				Type = type;
				Index = index;
			}
		}

		class VariableToken : Token
		{
			public readonly string Name;

			public override string Symbol { get { return Name; } }

			public VariableToken(int index, string symbol) : base(TokenType.Variable, index) { Name = symbol; }
		}

		class NumberToken : Token
		{
			public readonly int Value;
			readonly string symbol;

			public override string Symbol { get { return symbol; } }

			public NumberToken(int index, string symbol)
				: base(TokenType.Number, index)
			{
				Value = int.Parse(symbol);
				this.symbol = symbol;
			}
		}

		public ConditionExpression(string expression)
		{
			Expression = expression;
			var openParens = 0;
			var closeParens = 0;
			var tokens = new List<Token>();
			for (var i = 0; i < expression.Length;)
			{
				// Ignore whitespace
				if (CharClassOf(expression[i]) == CharClass.Whitespace)
				{
					i++;
					continue;
				}

				var token = ParseSymbol(expression, ref i);
				switch (token.Type)
				{
					case TokenType.OpenParen:
						openParens++;
						break;

					case TokenType.CloseParen:
						if (++closeParens > openParens)
							throw new InvalidDataException("Unmatched closing parenthesis at index {0}".F(i - 1));

						break;

					case TokenType.Variable:
						variables.Add(token.Symbol);
						break;
				}

				tokens.Add(token);
			}

			// Sanity check parsed tree
			if (!tokens.Any())
				throw new InvalidDataException("Empty expression");

			if (closeParens != openParens)
				throw new InvalidDataException("Mismatched opening and closing parentheses");

			// Expressions can't start with a binary or unary postfix operation or closer
			if (tokens[0].LeftOperand)
				throw new InvalidDataException("Missing value or sub-expression at beginning for `{0}` operator".F(tokens[0].Symbol));

			for (var i = 0; i < tokens.Count - 1; i++)
			{
				// Disallow empty parentheses
				if (tokens[i].Opens != Grouping.None && tokens[i + 1].Closes != Grouping.None)
					throw new InvalidDataException("Empty parenthesis at index {0}".F(tokens[i].Index));

				// Exactly one of two consective tokens must take the other's sub-expression evaluation as an operand
				if (tokens[i].RightOperand == tokens[i + 1].LeftOperand)
				{
					if (tokens[i].RightOperand)
						throw new InvalidDataException(
							"Missing value or sub-expression or there is an extra operator `{0}` at index {1} or `{2}` at index {3}".F(
								tokens[i].Symbol, tokens[i].Index, tokens[i + 1].Symbol, tokens[i + 1].Index));
					throw new InvalidDataException("Missing binary operation before `{0}` at index {1}".F(
						tokens[i + 1].Symbol, tokens[i + 1].Index));
				}
			}

			// Expressions can't end with a binary or unary prefix operation
			if (tokens[tokens.Count - 1].RightOperand)
				throw new InvalidDataException("Missing value or sub-expression at end for `{0}` operator".F(tokens[tokens.Count - 1].Symbol));

			// Convert to postfix (discarding parentheses) ready for evaluation
			postfix = ToPostfix(tokens).ToArray();
		}

		static Token ParseSymbol(string expression, ref int i)
		{
			var start = i;

			// Parse operators
			switch (expression[start])
			{
				case '!':
				{
					i++;
					if (i < expression.Length && expression[start + 1] == '=')
					{
						i++;
						return new Token(TokenType.NotEquals, start);
					}

					return new Token(TokenType.Not, start);
				}

				case '=':
				{
					i++;
					if (i < expression.Length && expression[start + 1] == '=')
					{
						i++;
						return new Token(TokenType.Equals, start);
					}

					throw new InvalidDataException("Unexpected character '=' at index {0} - should it be `==`?".F(start));
				}

				case '&':
				{
					i++;
					if (i < expression.Length && expression[start + 1] == '&')
					{
						i++;
						return new Token(TokenType.And, start);
					}

					throw new InvalidDataException("Unexpected character '&' at index {0} - should it be `&&`?".F(start));
				}

				case '|':
				{
					i++;
					if (i < expression.Length && expression[start + 1] == '|')
					{
						i++;
						return new Token(TokenType.Or, start);
					}

					throw new InvalidDataException("Unexpected character '|' at index {0} - should it be `||`?".F(start));
				}

				case '(':
				{
					i++;
					return new Token(TokenType.OpenParen, start);
				}

				case ')':
				{
					i++;
					return new Token(TokenType.CloseParen, start);
				}
			}

			var cc = CharClassOf(expression[start]);

			// Scan forwards until we find an non-digit character
			if (expression[start] == '-' || cc == CharClass.Digit)
			{
				i++;
				for (; i < expression.Length; i++)
				{
					cc = CharClassOf(expression[i]);
					if (cc != CharClass.Digit)
					{
						if (cc != CharClass.Whitespace && cc != CharClass.Operator)
							throw new InvalidDataException("Number {0} and variable merged at index {1}".F(
								int.Parse(expression.Substring(start, i - start)), start));

						return new NumberToken(start, expression.Substring(start, i - start));
					}
				}

				return new NumberToken(start, expression.Substring(start));
			}

			if (cc != CharClass.Id)
				throw new InvalidDataException("Invalid character '{0}' at index {1}".F(expression[i], start));

			// Scan forwards until we find an invalid name character
			for (; i < expression.Length; i++)
			{
				cc = CharClassOf(expression[i]);
				if (cc == CharClass.Whitespace || cc == CharClass.Operator)
					return new VariableToken(start, expression.Substring(start, i - start));
			}

			// Take the rest of the string
			return new VariableToken(start, expression.Substring(start));
		}

		static int ParseSymbol(VariableToken t, IReadOnlyDictionary<string, int> symbols)
		{
			int value;
			symbols.TryGetValue(t.Symbol, out value);
			return value;
		}

		static void ApplyBinaryOperation(Stack<int> s, Func<int, int, int> f)
		{
			var x = s.Pop();
			var y = s.Pop();
			s.Push(f(x, y));
		}

		static void ApplyUnaryOperation(Stack<int> s, Func<int, int> f)
		{
			var x = s.Pop();
			s.Push(f(x));
		}

		static IEnumerable<Token> ToPostfix(IEnumerable<Token> tokens)
		{
			var s = new Stack<Token>();
			foreach (var t in tokens)
			{
				if (t.Opens != Grouping.None)
					s.Push(t);
				else if (t.Closes != Grouping.None)
				{
					Token temp;
					while (!((temp = s.Pop()).Opens != Grouping.None))
						yield return temp;
				}
				else if (t.OperandSides == OperandSides.None)
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

		public int Evaluate(IReadOnlyDictionary<string, int> symbols)
		{
			var s = new Stack<int>();
			foreach (var t in postfix)
			{
				switch (t.Type)
				{
					case TokenType.And:
						ApplyBinaryOperation(s, (x, y) => y > 0 ? x : y);
						continue;
					case TokenType.NotEquals:
						ApplyBinaryOperation(s, (x, y) => (y != x) ? 1 : 0);
						continue;
					case TokenType.Or:
						ApplyBinaryOperation(s, (x, y) => y > 0 ? y : x);
						continue;
					case TokenType.Equals:
						ApplyBinaryOperation(s, (x, y) => (y == x) ? 1 : 0);
						continue;
					case TokenType.Not:
						ApplyUnaryOperation(s, x => (x > 0) ? 0 : 1);
						continue;
					case TokenType.Number:
						s.Push(((NumberToken)t).Value);
						continue;
					case TokenType.Variable:
						s.Push(ParseSymbol((VariableToken)t, symbols));
						continue;
					default:
						throw new InvalidProgramException("Evaluate is missing an evaluator for TokenType.{0}".F(
							Enum<TokenType>.GetValues()[(int)t.Type]));
				}
			}

			return s.Pop();
		}
	}
}
