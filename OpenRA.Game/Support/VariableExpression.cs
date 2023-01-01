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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Expressions = System.Linq.Expressions;

namespace OpenRA.Support
{
	public abstract class VariableExpression
	{
		public static readonly IReadOnlyDictionary<string, int> NoVariables = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>());

		public readonly string Expression;
		readonly HashSet<string> variables = new HashSet<string>();
		public IEnumerable<string> Variables => variables;

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
		enum Sides
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
			// fixed values
			False,
			True,

			// varying values
			Number,
			Variable,

			// operators
			OpenParen,
			CloseParen,
			Not,
			Negate,
			OnesComplement,
			And,
			Or,
			Equals,
			NotEquals,
			LessThan,
			LessThanOrEqual,
			GreaterThan,
			GreaterThanOrEqual,
			Add,
			Subtract,
			Multiply,
			Divide,
			Modulo,

			Invalid
		}

		enum Precedence
		{
			Unary = 16,
			Multiplication = 12,
			Addition = 11,
			Relation = 9,
			Equality = 8,
			And = 4,
			Or = 3,
			Binary = 0,
			Value = 0,
			Parens = -1,
			Invalid = ~0
		}

		readonly struct TokenTypeInfo
		{
			public readonly string Symbol;
			public readonly Precedence Precedence;
			public readonly Sides OperandSides;
			public readonly Sides WhitespaceSides;
			public readonly Associativity Associativity;
			public readonly Grouping Opens;
			public readonly Grouping Closes;

			public TokenTypeInfo(string symbol, Precedence precedence, Sides operandSides = Sides.None,
				Associativity associativity = Associativity.Left,
				Grouping opens = Grouping.None, Grouping closes = Grouping.None)
			{
				Symbol = symbol;
				Precedence = precedence;
				OperandSides = operandSides;
				WhitespaceSides = Sides.None;
				Associativity = associativity;
				Opens = opens;
				Closes = closes;
			}

			public TokenTypeInfo(string symbol, Precedence precedence, Sides operandSides,
				Sides whitespaceSides,
				Associativity associativity = Associativity.Left,
				Grouping opens = Grouping.None, Grouping closes = Grouping.None)
			{
				Symbol = symbol;
				Precedence = precedence;
				OperandSides = operandSides;
				WhitespaceSides = whitespaceSides;
				Associativity = associativity;
				Opens = opens;
				Closes = closes;
			}

			public TokenTypeInfo(string symbol, Precedence precedence, Grouping opens, Grouping closes = Grouping.None,
				Associativity associativity = Associativity.Left)
			{
				Symbol = symbol;
				Precedence = precedence;
				WhitespaceSides = Sides.None;
				OperandSides = opens == Grouping.None ?
					(closes == Grouping.None ? Sides.None : Sides.Left) :
					(closes == Grouping.None ? Sides.Right : Sides.Both);
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
					case TokenType.False:
						yield return new TokenTypeInfo("false", Precedence.Value);
						continue;
					case TokenType.True:
						yield return new TokenTypeInfo("true", Precedence.Value);
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
						yield return new TokenTypeInfo("!", Precedence.Unary, Sides.Right, Associativity.Right);
						continue;
					case TokenType.OnesComplement:
						yield return new TokenTypeInfo("~", Precedence.Unary, Sides.Right, Associativity.Right);
						continue;
					case TokenType.Negate:
						yield return new TokenTypeInfo("-", Precedence.Unary, Sides.Right, Associativity.Right);
						continue;
					case TokenType.And:
						yield return new TokenTypeInfo("&&", Precedence.And, Sides.Both, Sides.Both);
						continue;
					case TokenType.Or:
						yield return new TokenTypeInfo("||", Precedence.Or, Sides.Both, Sides.Both);
						continue;
					case TokenType.Equals:
						yield return new TokenTypeInfo("==", Precedence.Equality, Sides.Both, Sides.Both);
						continue;
					case TokenType.NotEquals:
						yield return new TokenTypeInfo("!=", Precedence.Equality, Sides.Both, Sides.Both);
						continue;
					case TokenType.LessThan:
						yield return new TokenTypeInfo("<", Precedence.Relation, Sides.Both, Sides.Both);
						continue;
					case TokenType.LessThanOrEqual:
						yield return new TokenTypeInfo("<=", Precedence.Relation, Sides.Both, Sides.Both);
						continue;
					case TokenType.GreaterThan:
						yield return new TokenTypeInfo(">", Precedence.Relation, Sides.Both, Sides.Both);
						continue;
					case TokenType.GreaterThanOrEqual:
						yield return new TokenTypeInfo(">=", Precedence.Relation, Sides.Both, Sides.Both);
						continue;
					case TokenType.Add:
						yield return new TokenTypeInfo("+", Precedence.Addition, Sides.Both, Sides.Both);
						continue;
					case TokenType.Subtract:
						yield return new TokenTypeInfo("-", Precedence.Addition, Sides.Both, Sides.Both);
						continue;
					case TokenType.Multiply:
						yield return new TokenTypeInfo("*", Precedence.Multiplication, Sides.Both, Sides.Both);
						continue;
					case TokenType.Divide:
						yield return new TokenTypeInfo("/", Precedence.Multiplication, Sides.Both, Sides.Both);
						continue;
					case TokenType.Modulo:
						yield return new TokenTypeInfo("%", Precedence.Multiplication, Sides.Both, Sides.Both);
						continue;
				}

				throw new InvalidProgramException($"CreateTokenTypeInfoEnumeration is missing a TokenTypeInfo entry for TokenType.{Enum<TokenType>.GetValues()[i]}");
			}
		}

		static readonly TokenTypeInfo[] TokenTypeInfos = CreateTokenTypeInfoEnumeration().ToArray();

		static bool HasRightOperand(TokenType type)
		{
			return ((int)TokenTypeInfos[(int)type].OperandSides & (int)Sides.Right) != 0;
		}

		static bool IsLeftOperandOrNone(TokenType type)
		{
			return type == TokenType.Invalid || HasRightOperand(type);
		}

		static bool RequiresWhitespaceAfter(TokenType type)
		{
			return ((int)TokenTypeInfos[(int)type].WhitespaceSides & (int)Sides.Right) != 0;
		}

		static bool RequiresWhitespaceBefore(TokenType type)
		{
			return ((int)TokenTypeInfos[(int)type].WhitespaceSides & (int)Sides.Left) != 0;
		}

		static string GetTokenSymbol(TokenType type)
		{
			return TokenTypeInfos[(int)type].Symbol;
		}

		class Token
		{
			public readonly TokenType Type;
			public readonly int Index;

			public virtual string Symbol => TokenTypeInfos[(int)Type].Symbol;

			public int Precedence => (int)TokenTypeInfos[(int)Type].Precedence;
			public Sides OperandSides => TokenTypeInfos[(int)Type].OperandSides;
			public Associativity Associativity => TokenTypeInfos[(int)Type].Associativity;
			public bool LeftOperand => ((int)TokenTypeInfos[(int)Type].OperandSides & (int)Sides.Left) != 0;
			public bool RightOperand => ((int)TokenTypeInfos[(int)Type].OperandSides & (int)Sides.Right) != 0;

			public Grouping Opens => TokenTypeInfos[(int)Type].Opens;
			public Grouping Closes => TokenTypeInfos[(int)Type].Closes;

			public Token(TokenType type, int index)
			{
				Type = type;
				Index = index;
			}

			static bool ScanIsNumber(string expression, int start, ref int i)
			{
				var cc = CharClassOf(expression[i]);

				// Scan forwards until we find an non-digit character
				if (cc == CharClass.Digit)
				{
					i++;
					for (; i < expression.Length; i++)
					{
						cc = CharClassOf(expression[i]);
						if (cc != CharClass.Digit)
						{
							if (cc != CharClass.Whitespace && cc != CharClass.Operator && cc != CharClass.Mixed)
								throw new InvalidDataException($"Number {int.Parse(expression.Substring(start, i - start))} and variable merged at index {start}");

							return true;
						}
					}

					return true;
				}

				return false;
			}

			static TokenType VariableOrKeyword(string expression, int start, ref int i)
			{
				if (CharClassOf(expression[i - 1]) == CharClass.Mixed)
					throw new InvalidDataException($"Invalid identifier end character at index {i - 1} for `{expression.Substring(start, i - start)}`");

				return VariableOrKeyword(expression, start, i - start);
			}

			static TokenType VariableOrKeyword(string expression, int start, int length)
			{
				var i = start;
				if (length == 4 && expression[i++] == 't' && expression[i++] == 'r' && expression[i++] == 'u'
						&& expression[i] == 'e')
					return TokenType.True;

				if (length == 5 && expression[i++] == 'f' && expression[i++] == 'a' && expression[i++] == 'l'
						&& expression[i++] == 's' && expression[i] == 'e')
					return TokenType.False;

				return TokenType.Variable;
			}

			public static TokenType GetNextType(string expression, ref int i, TokenType lastType = TokenType.Invalid)
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

					case '<':
						i++;
						if (i < expression.Length && expression[i] == '=')
						{
							i++;
							return TokenType.LessThanOrEqual;
						}

						return TokenType.LessThan;

					case '>':
						i++;
						if (i < expression.Length && expression[i] == '=')
						{
							i++;
							return TokenType.GreaterThanOrEqual;
						}

						return TokenType.GreaterThan;

					case '=':
						i++;
						if (i < expression.Length && expression[i] == '=')
						{
							i++;
							return TokenType.Equals;
						}

						throw new InvalidDataException($"Unexpected character '=' at index {start} - should it be `==`?");

					case '&':
						i++;
						if (i < expression.Length && expression[i] == '&')
						{
							i++;
							return TokenType.And;
						}

						throw new InvalidDataException($"Unexpected character '&' at index {start} - should it be `&&`?");

					case '|':
						i++;
						if (i < expression.Length && expression[i] == '|')
						{
							i++;
							return TokenType.Or;
						}

						throw new InvalidDataException($"Unexpected character '|' at index {start} - should it be `||`?");

					case '(':
						i++;
						return TokenType.OpenParen;

					case ')':
						i++;
						return TokenType.CloseParen;

					case '~':
						i++;
						return TokenType.OnesComplement;
					case '+':
						i++;
						return TokenType.Add;

					case '-':
						if (++i < expression.Length && ScanIsNumber(expression, start, ref i))
							return TokenType.Number;

						i = start + 1;
						if (IsLeftOperandOrNone(lastType))
							return TokenType.Negate;
						return TokenType.Subtract;

					case '*':
						i++;
						return TokenType.Multiply;

					case '/':
						i++;
						return TokenType.Divide;

					case '%':
						i++;
						return TokenType.Modulo;
				}

				if (ScanIsNumber(expression, start, ref i))
					return TokenType.Number;

				var cc = CharClassOf(expression[start]);

				if (cc != CharClass.Id)
					throw new InvalidDataException($"Invalid character '{expression[i]}' at index {start}");

				// Scan forwards until we find an invalid name character
				for (i = start; i < expression.Length; i++)
				{
					cc = CharClassOf(expression[i]);
					if (cc == CharClass.Whitespace || cc == CharClass.Operator)
						return VariableOrKeyword(expression, start, ref i);
				}

				return VariableOrKeyword(expression, start, ref i);
			}

			public static Token GetNext(string expression, ref int i, TokenType lastType = TokenType.Invalid)
			{
				if (i == expression.Length)
					return null;

				// Check and eat whitespace
				var whitespaceBefore = false;
				if (CharClassOf(expression[i]) == CharClass.Whitespace)
				{
					whitespaceBefore = true;
					while (CharClassOf(expression[i]) == CharClass.Whitespace)
					{
						if (++i == expression.Length)
							return null;
					}
				}
				else if (lastType == TokenType.Invalid)
					whitespaceBefore = true;
				else if (RequiresWhitespaceAfter(lastType))
					throw new InvalidDataException($"Missing whitespace at index {i}, after `{GetTokenSymbol(lastType)}` operator.");

				var start = i;

				var type = GetNextType(expression, ref i, lastType);
				if (!whitespaceBefore && RequiresWhitespaceBefore(type))
					throw new InvalidDataException($"Missing whitespace at index {i}, before `{GetTokenSymbol(type)}` operator.");

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

			public override string Symbol => Name;

			public VariableToken(int index, string symbol)
				: base(TokenType.Variable, index) { Name = symbol; }
		}

		class NumberToken : Token
		{
			public readonly int Value;
			readonly string symbol;

			public override string Symbol => symbol;

			public NumberToken(int index, string symbol)
				: base(TokenType.Number, index)
			{
				Value = int.Parse(symbol);
				this.symbol = symbol;
			}
		}

		public VariableExpression(string expression)
		{
			Expression = expression;
		}

		Expression Build(ExpressionType resultType)
		{
			var tokens = new List<Token>();
			var currentOpeners = new Stack<Token>();
			Token lastToken = null;
			for (var i = 0; ;)
			{
				var token = Token.GetNext(Expression, ref i, lastToken?.Type ?? TokenType.Invalid);
				if (token == null)
				{
					// Sanity check parsed tree
					if (lastToken == null)
						throw new InvalidDataException("Empty expression");

					// Expressions can't end with a binary or unary prefix operation
					if (lastToken.RightOperand)
						throw new InvalidDataException($"Missing value or sub-expression at end for `{lastToken.Symbol}` operator");

					break;
				}

				if (token.Closes != Grouping.None)
				{
					if (currentOpeners.Count == 0)
						throw new InvalidDataException($"Unmatched closing parenthesis at index {token.Index}");

					currentOpeners.Pop();
				}

				if (token.Opens != Grouping.None)
					currentOpeners.Push(token);

				if (lastToken == null)
				{
					// Expressions can't start with a binary or unary postfix operation or closer
					if (token.LeftOperand)
						throw new InvalidDataException($"Missing value or sub-expression at beginning for `{token.Symbol}` operator");
				}
				else
				{
					// Disallow empty parentheses
					if (lastToken.Opens != Grouping.None && token.Closes != Grouping.None)
						throw new InvalidDataException($"Empty parenthesis at index {lastToken.Index}");

					// Exactly one of two consecutive tokens must take the other's sub-expression evaluation as an operand
					if (lastToken.RightOperand == token.LeftOperand)
					{
						if (lastToken.RightOperand)
							throw new InvalidDataException($"Missing value or sub-expression or there is an extra operator `{lastToken.Symbol}` at index {lastToken.Index} or `{token.Symbol}` at index {token.Index}");
						throw new InvalidDataException($"Missing binary operation before `{token.Symbol}` at index {token.Index}");
					}
				}

				if (token.Type == TokenType.Variable)
					variables.Add(token.Symbol);

				tokens.Add(token);
				lastToken = token;
			}

			if (currentOpeners.Count > 0)
				throw new InvalidDataException($"Unclosed opening parenthesis at index {currentOpeners.Peek().Index}");

			return new Compiler().Build(ToPostfix(tokens).ToArray(), resultType);
		}

		protected Func<IReadOnlyDictionary<string, int>, T> Compile<T>()
		{
			ExpressionType resultType;
			if (typeof(T) == typeof(int))
				resultType = ExpressionType.Int;
			else if (typeof(T) == typeof(bool))
				resultType = ExpressionType.Bool;
			else
				throw new InvalidCastException("Variable expressions can only be int or bool.");

			return Expressions.Expression.Lambda<Func<IReadOnlyDictionary<string, int>, T>>(Build(resultType), SymbolsParam).Compile();
		}

		static int ParseSymbol(string symbol, IReadOnlyDictionary<string, int> symbols)
		{
			symbols.TryGetValue(symbol, out var value);
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
				else if (t.OperandSides == Sides.None)
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
			return Expressions.Expression.NotEqual(expression, Zero);
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

				throw new InvalidProgramException($"Unable to convert ExpressionType.{Enum<ExpressionType>.GetValues()[(int)fromType]} to ExpressionType.{Enum<ExpressionType>.GetValues()[(int)toType]}");
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
						throw new InvalidOperationException($"Expected System.Int type instead of {expression.Type} for {expression}");

				if (type == ExpressionType.Bool)
					if (expression.Type != typeof(bool))
						throw new InvalidOperationException($"Expected System.Boolean type instead of {expression.Type} for {expression}");
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
					throw new InvalidOperationException($"Unhandled result type {expression.Type} for {expression}");
			}
		}

		class Compiler
		{
			readonly AstStack ast = new AstStack();

			public Expression Build(Token[] postfix, ExpressionType resultType)
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
							ast.Push(Expressions.Expression.Not(ast.Pop(ExpressionType.Bool)));
							continue;
						}

						case TokenType.Negate:
						{
							ast.Push(Expressions.Expression.Negate(ast.Pop(ExpressionType.Int)));
							continue;
						}

						case TokenType.OnesComplement:
						{
							ast.Push(Expressions.Expression.OnesComplement(ast.Pop(ExpressionType.Int)));
							continue;
						}

						case TokenType.LessThan:
						{
							var y = ast.Pop(ExpressionType.Int);
							var x = ast.Pop(ExpressionType.Int);
							ast.Push(Expressions.Expression.LessThan(x, y));
							continue;
						}

						case TokenType.LessThanOrEqual:
						{
							var y = ast.Pop(ExpressionType.Int);
							var x = ast.Pop(ExpressionType.Int);
							ast.Push(Expressions.Expression.LessThanOrEqual(x, y));
							continue;
						}

						case TokenType.GreaterThan:
						{
							var y = ast.Pop(ExpressionType.Int);
							var x = ast.Pop(ExpressionType.Int);
							ast.Push(Expressions.Expression.GreaterThan(x, y));
							continue;
						}

						case TokenType.GreaterThanOrEqual:
						{
							var y = ast.Pop(ExpressionType.Int);
							var x = ast.Pop(ExpressionType.Int);
							ast.Push(Expressions.Expression.GreaterThanOrEqual(x, y));
							continue;
						}

						case TokenType.Add:
						{
							var y = ast.Pop(ExpressionType.Int);
							var x = ast.Pop(ExpressionType.Int);
							ast.Push(Expressions.Expression.Add(x, y));
							continue;
						}

						case TokenType.Subtract:
						{
							var y = ast.Pop(ExpressionType.Int);
							var x = ast.Pop(ExpressionType.Int);
							ast.Push(Expressions.Expression.Subtract(x, y));
							continue;
						}

						case TokenType.Multiply:
						{
							var y = ast.Pop(ExpressionType.Int);
							var x = ast.Pop(ExpressionType.Int);
							ast.Push(Expressions.Expression.Multiply(x, y));
							continue;
						}

						case TokenType.Divide:
						{
							var y = ast.Pop(ExpressionType.Int);
							var x = ast.Pop(ExpressionType.Int);
							var isNotZero = Expressions.Expression.NotEqual(y, Zero);
							var divide = Expressions.Expression.Divide(x, y);
							ast.Push(IfThenElse(isNotZero, divide, Zero));
							continue;
						}

						case TokenType.Modulo:
						{
							var y = ast.Pop(ExpressionType.Int);
							var x = ast.Pop(ExpressionType.Int);
							var isNotZero = Expressions.Expression.NotEqual(y, Zero);
							var modulo = Expressions.Expression.Modulo(x, y);
							ast.Push(IfThenElse(isNotZero, modulo, Zero));
							continue;
						}

						case TokenType.False:
						{
							ast.Push(False);
							continue;
						}

						case TokenType.True:
						{
							ast.Push(True);
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
							throw new InvalidProgramException($"ConditionExpression.Compiler.Compile() is missing an expression builder for TokenType.{Enum<TokenType>.GetValues()[(int)t.Type]}");
					}
				}

				return ast.Pop(resultType);
			}
		}
	}

	public class BooleanExpression : VariableExpression
	{
		readonly Func<IReadOnlyDictionary<string, int>, bool> asFunction;

		public BooleanExpression(string expression)
			: base(expression)
		{
			asFunction = Compile<bool>();
		}

		public bool Evaluate(IReadOnlyDictionary<string, int> symbols)
		{
			return asFunction(symbols);
		}
	}

	public class IntegerExpression : VariableExpression
	{
		readonly Func<IReadOnlyDictionary<string, int>, int> asFunction;

		public IntegerExpression(string expression)
			: base(expression)
		{
			asFunction = Compile<int>();
		}

		public int Evaluate(IReadOnlyDictionary<string, int> symbols)
		{
			return asFunction(symbols);
		}
	}
}
