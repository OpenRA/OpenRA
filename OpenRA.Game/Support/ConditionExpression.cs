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

		class BinaryOperationToken : Token
		{
			public BinaryOperationToken(TokenType type, int index) : base(type, index) { }
		}

		class UnaryOperationToken : Token
		{
			public UnaryOperationToken(TokenType type, int index) : base(type, index) { }
		}

		class OpenParenToken : Token { public OpenParenToken(int index) : base(TokenType.OpenParen, index) { } }
		class CloseParenToken : Token { public CloseParenToken(int index) : base(TokenType.CloseParen, index) { } }
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

		class AndToken : BinaryOperationToken { public AndToken(int index) : base(TokenType.And, index) { } }
		class OrToken : BinaryOperationToken { public OrToken(int index) : base(TokenType.Or, index) { } }
		class EqualsToken : BinaryOperationToken { public EqualsToken(int index) : base(TokenType.Equals, index) { } }
		class NotEqualsToken : BinaryOperationToken { public NotEqualsToken(int index) : base(TokenType.NotEquals, index) { } }
		class NotToken : UnaryOperationToken { public NotToken(int index) : base(TokenType.Not, index) { } }

		public ConditionExpression(string expression)
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
						if (CharClassOf(expression[i]) == CharClass.Whitespace)
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
				// Unary tokens must be followed by a variable, number, another unary token, or an opening parenthesis
				if (tokens[i] is UnaryOperationToken && !(tokens[i + 1] is VariableToken || tokens[i + 1] is NumberToken
				        || tokens[i + 1] is UnaryOperationToken || tokens[i + 1] is OpenParenToken))
					throw new InvalidDataException("Unexpected token `{0}` at index {1}".F(tokens[i].Symbol, tokens[i].Index));

				// Disallow empty parentheses
				if (tokens[i] is OpenParenToken && tokens[i + 1] is CloseParenToken)
					throw new InvalidDataException("Empty parenthesis at index {0}".F(tokens[i].Index));

				// A variable or number must be followed by a binary operation or by a closing parenthesis
				if ((tokens[i] is VariableToken || tokens[i] is NumberToken) && !(tokens[i + 1] is BinaryOperationToken || tokens[i + 1] is CloseParenToken))
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
					!(tokens[i - 1] is CloseParenToken || tokens[i - 1] is VariableToken || tokens[i - 1] is NumberToken) ||
					!(tokens[i + 1] is OpenParenToken || tokens[i + 1] is VariableToken || tokens[i + 1] is NumberToken || tokens[i + 1] is UnaryOperationToken)))
					throw new InvalidDataException("Unexpected token `{0}` at index `{1}`".F(tokens[i].Symbol, tokens[i].Index));
			}

			// Convert to postfix (discarding parentheses) ready for evaluation
			postfix = ToPostfix(tokens).ToArray();
		}

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

		static Token ParseSymbol(string expression, ref int i)
		{
			var start = i;

			// Parse operators
			switch (expression[start])
			{
				case '!':
				{
					if (i < expression.Length - 1 && expression[start + 1] == '=')
					{
						i++;
						return new NotEqualsToken(start);
					}

					return new NotToken(start);
				}

				case '=':
				{
					if (i < expression.Length - 1 && expression[start + 1] == '=')
					{
						i++;
						return new EqualsToken(start);
					}

					throw new InvalidDataException("Unexpected character '=' at index {0}".F(start));
				}

				case '&':
				{
					if (i < expression.Length - 1 && expression[start + 1] == '&')
					{
						i++;
						return new AndToken(start);
					}

					throw new InvalidDataException("Unexpected character '&' at index {0}".F(start));
				}

				case '|':
				{
					if (i < expression.Length - 1 && expression[start + 1] == '|')
					{
						i++;
						return new OrToken(start);
					}

					throw new InvalidDataException("Unexpected character '|' at index {0}".F(start));
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
							throw new InvalidDataException("Number and variable merged at index {0}".F(start));

						// Put the bad character back for the next parse attempt
						i--;
						return new NumberToken(start, expression.Substring(start, i - start + 1));
					}
				}

				return new NumberToken(start, expression.Substring(start));
			}

			if (cc != CharClass.Id)
				throw new InvalidDataException("Invalid character at index {0}".F(start));

			// Scan forwards until we find an invalid name character
			for (; i < expression.Length; i++)
			{
				cc = CharClassOf(expression[i]);
				if (cc == CharClass.Whitespace || cc == CharClass.Operator)
				{
					// Put the bad character back for the next parse attempt
					i--;
					return new VariableToken(start, expression.Substring(start, i - start + 1));
				}
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
				if (t is OpenParenToken)
					s.Push(t);
				else if (t is CloseParenToken)
				{
					Token temp;
					while (!((temp = s.Pop()) is OpenParenToken))
						yield return temp;
				}
				else if (t is VariableToken || t is NumberToken)
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
				if (t is AndToken)
					ApplyBinaryOperation(s, (x, y) => y > 0 ? x : y);
				else if (t is NotEqualsToken)
					ApplyBinaryOperation(s, (x, y) => (y != x) ? 1 : 0);
				else if (t is OrToken)
					ApplyBinaryOperation(s, (x, y) => y > 0 ? y : x);
				else if (t is EqualsToken)
					ApplyBinaryOperation(s, (x, y) => (y == x) ? 1 : 0);
				else if (t is NotToken)
					ApplyUnaryOperation(s, x => (x > 0) ? 0 : 1);
				else if (t is NumberToken)
					s.Push(((NumberToken)t).Value);
				else if (t is VariableToken)
					s.Push(ParseSymbol((VariableToken)t, symbols));
			}

			return s.Pop();
		}
	}
}
