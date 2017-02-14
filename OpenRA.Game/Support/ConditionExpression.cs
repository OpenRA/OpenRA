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
using System.Linq.Expressions;
using Expressions = System.Linq.Expressions;

namespace OpenRA.Support
{
	public class ConditionExpression
	{
		public readonly string Expression;
		readonly HashSet<string> variables = new HashSet<string>();
		public IEnumerable<string> Variables { get { return variables; } }

		readonly Func<IReadOnlyDictionary<string, int>, int> asFunction;

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
			// varying values
			Number,
			Variable,

			// operators
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

			public static TokenType GetNextType(string expression, ref int i)
			{
				var start = i;

				switch (expression[i])
				{
					case '!':
						i++;
						if (i < expression.Length && expression[i] == '=')
						{
							i++;
							return TokenType.NotEquals;
						}

						return TokenType.Not;

					case '=':
						i++;
						if (i < expression.Length && expression[i] == '=')
						{
							i++;
							return TokenType.Equals;
						}

						throw new InvalidDataException("Unexpected character '=' at index {0} - should it be `==`?".F(start));

					case '&':
						i++;
						if (i < expression.Length && expression[i] == '&')
						{
							i++;
							return TokenType.And;
						}

						throw new InvalidDataException("Unexpected character '&' at index {0} - should it be `&&`?".F(start));

					case '|':
						i++;
						if (i < expression.Length && expression[i] == '|')
						{
							i++;
							return TokenType.Or;
						}

						throw new InvalidDataException("Unexpected character '|' at index {0} - should it be `||`?".F(start));

					case '(':
						i++;
						return TokenType.OpenParen;

					case ')':
						i++;
						return TokenType.CloseParen;
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

							return TokenType.Number;
						}
					}

					return TokenType.Number;
				}

				if (cc != CharClass.Id)
					throw new InvalidDataException("Invalid character '{0}' at index {1}".F(expression[i], start));

				// Scan forwards until we find an invalid name character
				for (; i < expression.Length; i++)
				{
					cc = CharClassOf(expression[i]);
					if (cc == CharClass.Whitespace || cc == CharClass.Operator)
						return TokenType.Variable;
				}

				return TokenType.Variable;
			}

			public static Token GetNext(string expression, ref int i)
			{
				if (i == expression.Length)
					return null;

				// Ignore whitespace
				while (CharClassOf(expression[i]) == CharClass.Whitespace)
				{
					if (++i == expression.Length)
						return null;
				}

				var start = i;

				var type = GetNextType(expression, ref i);
				switch (type)
				{
					case TokenType.Number:
						return new NumberToken(start, expression.Substring(start, i - start));

					case TokenType.Variable:
						return new VariableToken(start, expression.Substring(start, i - start));

					default:
						return new Token(type, start);
				}
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
			var tokens = new List<Token>();
			var currentOpeners = new Stack<Token>();
			Token lastToken = null;
			for (var i = 0;;)
			{
				var token = Token.GetNext(expression, ref i);
				if (token == null)
				{
					// Sanity check parsed tree
					if (lastToken == null)
						throw new InvalidDataException("Empty expression");

					// Expressions can't end with a binary or unary prefix operation
					if (lastToken.RightOperand)
						throw new InvalidDataException("Missing value or sub-expression at end for `{0}` operator".F(lastToken.Symbol));

					break;
				}

				if (token.Closes != Grouping.None)
				{
					if (currentOpeners.Count == 0)
						throw new InvalidDataException("Unmatched closing parenthesis at index {0}".F(token.Index));

					currentOpeners.Pop();
				}

				if (token.Opens != Grouping.None)
					currentOpeners.Push(token);

				if (lastToken == null)
				{
					// Expressions can't start with a binary or unary postfix operation or closer
					if (token.LeftOperand)
						throw new InvalidDataException("Missing value or sub-expression at beginning for `{0}` operator".F(token.Symbol));
				}
				else
				{
					// Disallow empty parentheses
					if (lastToken.Opens != Grouping.None && token.Closes != Grouping.None)
						throw new InvalidDataException("Empty parenthesis at index {0}".F(lastToken.Index));

					// Exactly one of two consective tokens must take the other's sub-expression evaluation as an operand
					if (lastToken.RightOperand == token.LeftOperand)
					{
						if (lastToken.RightOperand)
							throw new InvalidDataException(
								"Missing value or sub-expression or there is an extra operator `{0}` at index {1} or `{2}` at index {3}".F(
									lastToken.Symbol, lastToken.Index, token.Symbol, token.Index));
						throw new InvalidDataException("Missing binary operation before `{0}` at index {1}".F(token.Symbol, token.Index));
					}
				}

				if (token.Type == TokenType.Variable)
					variables.Add(token.Symbol);

				tokens.Add(token);
				lastToken = token;
			}

			if (currentOpeners.Count > 0)
				throw new InvalidDataException("Unclosed opening parenthesis at index {0}".F(currentOpeners.Peek().Index));

			asFunction = new Compiler().Compile(ToPostfix(tokens).ToArray());
		}

		static int ParseSymbol(string symbol, IReadOnlyDictionary<string, int> symbols)
		{
			int value;
			symbols.TryGetValue(symbol, out value);
			return value;
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

		enum ExpressionType { Int, Bool }

		static readonly ParameterExpression SymbolsParam =
			Expressions.Expression.Parameter(typeof(IReadOnlyDictionary<string, int>), "symbols");
		static readonly ConstantExpression Zero = Expressions.Expression.Constant(0);
		static readonly ConstantExpression One = Expressions.Expression.Constant(1);
		static readonly ConstantExpression False = Expressions.Expression.Constant(false);
		static readonly ConstantExpression True = Expressions.Expression.Constant(true);

		static Expression AsBool(Expression expression)
		{
			return Expressions.Expression.GreaterThan(expression, Zero);
		}

		static Expression AsNegBool(Expression expression)
		{
			return Expressions.Expression.LessThanOrEqual(expression, Zero);
		}

		static Expression IfThenElse(Expression test, Expression ifTrue, Expression ifFalse)
		{
			return Expressions.Expression.Condition(test, ifTrue, ifFalse);
		}

		class AstStack
		{
			readonly List<Expression> expressions = new List<Expression>();
			readonly List<ExpressionType> types = new List<ExpressionType>();

			public ExpressionType PeekType() { return types[types.Count - 1]; }

			public Expression Peek(ExpressionType toType)
			{
				var fromType = types[types.Count - 1];
				var expression = expressions[expressions.Count - 1];
				if (toType == fromType)
					return expression;

				switch (toType)
				{
					case ExpressionType.Bool:
						return IfThenElse(AsBool(expression), True, False);
					case ExpressionType.Int:
						return IfThenElse(expression, One, Zero);
				}

				throw new InvalidProgramException("Unable to convert ExpressionType.{0} to ExpressionType.{1}".F(
					Enum<ExpressionType>.GetValues()[(int)fromType], Enum<ExpressionType>.GetValues()[(int)toType]));
			}

			public Expression Pop(ExpressionType type)
			{
				var expression = Peek(type);
				expressions.RemoveAt(expressions.Count - 1);
				types.RemoveAt(types.Count - 1);
				return expression;
			}

			public void Push(Expression expression, ExpressionType type)
			{
				expressions.Add(expression);
				if (type == ExpressionType.Int)
					if (expression.Type != typeof(int))
						throw new InvalidOperationException("Expected System.Int type instead of {0} for {1}".F(expression.Type, expression));

				if (type == ExpressionType.Bool)
					if (expression.Type != typeof(bool))
						throw new InvalidOperationException("Expected System.Boolean type instead of {0} for {1}".F(expression.Type, expression));
				types.Add(type);
			}

			public void Push(Expression expression)
			{
				expressions.Add(expression);
				if (expression.Type == typeof(int))
					types.Add(ExpressionType.Int);
				else if (expression.Type == typeof(bool))
					types.Add(ExpressionType.Bool);
				else
					throw new InvalidOperationException("Unhandled result type {0} for {1}".F(expression.Type, expression));
			}
		}

		class Compiler
		{
			readonly AstStack ast = new AstStack();

			public Func<IReadOnlyDictionary<string, int>, int> Compile(Token[] postfix)
			{
				foreach (var t in postfix)
				{
					switch (t.Type)
					{
						case TokenType.And:
						{
							var y = ast.Pop(ExpressionType.Bool);
							var x = ast.Pop(ExpressionType.Bool);
							ast.Push(Expressions.Expression.And(x, y));
							continue;
						}

						case TokenType.Or:
						{
							var y = ast.Pop(ExpressionType.Bool);
							var x = ast.Pop(ExpressionType.Bool);
							ast.Push(Expressions.Expression.Or(x, y));
							continue;
						}

						case TokenType.NotEquals:
						{
							var y = ast.Pop(ExpressionType.Int);
							var x = ast.Pop(ExpressionType.Int);
							ast.Push(Expressions.Expression.NotEqual(x, y));
							continue;
						}

						case TokenType.Equals:
						{
							var y = ast.Pop(ExpressionType.Int);
							var x = ast.Pop(ExpressionType.Int);
							ast.Push(Expressions.Expression.Equal(x, y));
							continue;
						}

						case TokenType.Not:
						{
							if (ast.PeekType() == ExpressionType.Bool)
								ast.Push(Expressions.Expression.Not(ast.Pop(ExpressionType.Bool)));
							else
								ast.Push(AsNegBool(ast.Pop(ExpressionType.Int)));
							continue;
						}

						case TokenType.Number:
						{
							ast.Push(Expressions.Expression.Constant(((NumberToken)t).Value));
							continue;
						}

						case TokenType.Variable:
						{
							var symbol = Expressions.Expression.Constant(((VariableToken)t).Symbol);
							Func<string, IReadOnlyDictionary<string, int>, int> parseSymbol = ParseSymbol;
							ast.Push(Expressions.Expression.Call(parseSymbol.Method, symbol, SymbolsParam));
							continue;
						}

						default:
							throw new InvalidProgramException(
								"ConditionExpression.Compiler.Compile() is missing an expression builder for TokenType.{0}".F(
									Enum<TokenType>.GetValues()[(int)t.Type]));
					}
				}

				return Expressions.Expression.Lambda<Func<IReadOnlyDictionary<string, int>, int>>(
					ast.Pop(ExpressionType.Int), SymbolsParam).Compile();
			}
		}

		public int Evaluate(IReadOnlyDictionary<string, int> symbols)
		{
			return asFunction(symbols);
		}
	}
}
