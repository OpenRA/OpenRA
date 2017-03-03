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
using System.Reflection;
using Expressions = System.Linq.Expressions;

namespace OpenRA.Support
{
	public interface ICondition
	{
		bool AsBool();
		int AsInt();
		ICondition Get(string name);
	}

	public class EmptyCondition : ICondition
	{
		public static readonly EmptyCondition Instance = new EmptyCondition();
		EmptyCondition() { }

		bool ICondition.AsBool() { return false; }
		int ICondition.AsInt() { return 0; }
		ICondition ICondition.Get(string name) { return Instance; }
	}

	public class ConditionContext : Dictionary<string, ICondition>, ICondition
	{
		bool ICondition.AsBool() { return Count > 0; }
		int ICondition.AsInt() { return Count; }
		public ICondition Get(string name)
		{
			ICondition variable;
			if (TryGetValue(name, out variable))
				return variable;
			return EmptyCondition.Instance;
		}
	}

	public struct NumberCondition : ICondition
	{
		public int Value;
		public NumberCondition(int value = 0) { Value = value; }

		int ICondition.AsInt() { return Value; }
		bool ICondition.AsBool() { return Value != 0; }
		ICondition ICondition.Get(string name) { return EmptyCondition.Instance; }
	}

	public struct BoolCondition : ICondition
	{
		public bool Value;
		public BoolCondition(bool value = false) { Value = value; }
		public static readonly BoolCondition False = new BoolCondition(false);
		public static readonly BoolCondition True = new BoolCondition(true);

		int ICondition.AsInt() { return Value ? 1 : 0; }
		bool ICondition.AsBool() { return Value; }
		ICondition ICondition.Get(string name) { return EmptyCondition.Instance; }
	}

	public class ConditionExpression
	{
		public readonly string Expression;
		readonly HashSet<string> variables = new HashSet<string>();
		public IEnumerable<string> Variables { get { return variables; } }

		readonly Func<ICondition, int> asFunction;

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
				case '.':
					return CharClass.Operator;

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
			// fixed values
			False,
			True,

			// varying values
			Number,
			Variable,
			Property,

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
			Dot,

			Invalid
		}

		enum Precedence
		{
			Dot = 24,
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
					case TokenType.Property:
						yield return new TokenTypeInfo("(<property>)", Precedence.Value);
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
					case TokenType.OnesComplement:
						yield return new TokenTypeInfo("~", Precedence.Unary, OperandSides.Right, Associativity.Right);
						continue;
					case TokenType.Negate:
						yield return new TokenTypeInfo("-", Precedence.Unary, OperandSides.Right, Associativity.Right);
						continue;
					case TokenType.And:
						yield return new TokenTypeInfo("&&", Precedence.And, OperandSides.Both);
						continue;
					case TokenType.Or:
						yield return new TokenTypeInfo("||", Precedence.Or, OperandSides.Both);
						continue;
					case TokenType.Equals:
						yield return new TokenTypeInfo("==", Precedence.Equality, OperandSides.Both);
						continue;
					case TokenType.NotEquals:
						yield return new TokenTypeInfo("!=", Precedence.Equality, OperandSides.Both);
						continue;
					case TokenType.LessThan:
						yield return new TokenTypeInfo("<", Precedence.Relation, OperandSides.Both);
						continue;
					case TokenType.LessThanOrEqual:
						yield return new TokenTypeInfo("<=", Precedence.Relation, OperandSides.Both);
						continue;
					case TokenType.GreaterThan:
						yield return new TokenTypeInfo(">", Precedence.Relation, OperandSides.Both);
						continue;
					case TokenType.GreaterThanOrEqual:
						yield return new TokenTypeInfo(">=", Precedence.Relation, OperandSides.Both);
						continue;
					case TokenType.Add:
						yield return new TokenTypeInfo("+", Precedence.Addition, OperandSides.Both);
						continue;
					case TokenType.Subtract:
						yield return new TokenTypeInfo("-", Precedence.Addition, OperandSides.Both);
						continue;
					case TokenType.Multiply:
						yield return new TokenTypeInfo("*", Precedence.Multiplication, OperandSides.Both);
						continue;
					case TokenType.Divide:
						yield return new TokenTypeInfo("/", Precedence.Multiplication, OperandSides.Both);
						continue;
					case TokenType.Modulo:
						yield return new TokenTypeInfo("%", Precedence.Multiplication, OperandSides.Both);
						continue;
					case TokenType.Dot:
						yield return new TokenTypeInfo(".", Precedence.Dot, OperandSides.Both);
						continue;
				}

				throw new InvalidProgramException("CreateTokenTypeInfoEnumeration is missing a TokenTypeInfo entry for TokenType.{0}".F(
					Enum<TokenType>.GetValues()[i]));
			}
		}

		static readonly TokenTypeInfo[] TokenTypeInfos = CreateTokenTypeInfoEnumeration().ToArray();

		static bool HasRightOperand(TokenType type)
		{
			return ((int)TokenTypeInfos[(int)type].OperandSides & (int)OperandSides.Right) != 0;
		}

		static bool IsLeftOperandOrNone(TokenType type)
		{
			return type == TokenType.Invalid || HasRightOperand(type);
		}

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
							if (cc != CharClass.Whitespace && cc != CharClass.Operator)
								throw new InvalidDataException("Number {0} and variable merged at index {1}".F(
									int.Parse(expression.Substring(start, i - start)), start));

							return true;
						}
					}

					return true;
				}

				return false;
			}

			static TokenType VariableOrKeyword(string expression, int start, int length, TokenType lastType = TokenType.Invalid)
			{
				var i = start;
				if (length == 4 && expression[i++] == 't' && expression[i++] == 'r' && expression[i++] == 'u'
						&& expression[i] == 'e')
					return TokenType.True;

				if (length == 5 && expression[i++] == 'f' && expression[i++] == 'a' && expression[i++] == 'l'
						&& expression[i++] == 's' && expression[i] == 'e')
					return TokenType.False;

				return lastType == TokenType.Dot ? TokenType.Property : TokenType.Variable;
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

					case '.':
						i++;
						return TokenType.Dot;
				}

				if (ScanIsNumber(expression, start, ref i))
					return TokenType.Number;

				var cc = CharClassOf(expression[start]);

				if (cc != CharClass.Id)
					throw new InvalidDataException("Invalid character '{0}' at index {1}".F(expression[i], start));

				// Scan forwards until we find an invalid name character
				for (i = start; i < expression.Length; i++)
				{
					cc = CharClassOf(expression[i]);
					if (cc == CharClass.Whitespace || cc == CharClass.Operator)
						return VariableOrKeyword(expression, start, i - start, lastType);
				}

				return VariableOrKeyword(expression, start, i - start, lastType);
			}

			public static Token GetNext(string expression, ref int i, TokenType lastType = TokenType.Invalid)
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

				var type = GetNextType(expression, ref i, lastType);
				switch (type)
				{
					case TokenType.Number:
						return new NumberToken(start, expression.Substring(start, i - start));

					case TokenType.Variable:
						return new VariableToken(start, expression.Substring(start, i - start));

					case TokenType.Property:
						return new PropertyToken(start, expression.Substring(start, i - start));

					default:
						return new Token(type, start);
				}
			}
		}

		public static bool IsValidVariableName(string name)
		{
			int i = 0;
			return Token.GetNextType(name, ref i) == TokenType.Variable && i == name.Length;
		}

		class VariableToken : Token
		{
			public readonly string Name;

			public override string Symbol { get { return Name; } }

			public VariableToken(int index, string symbol) : base(TokenType.Variable, index) { Name = symbol; }
		}

		class PropertyToken : Token
		{
			public readonly string Name;

			public override string Symbol { get { return Name; } }

			public PropertyToken(int index, string symbol) : base(TokenType.Property, index) { Name = symbol; }
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
				var token = Token.GetNext(expression, ref i, lastToken != null ? lastToken.Type : TokenType.Invalid);
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

		enum ExpressionType { Int, Bool, Variable, Property }

		static readonly MethodInfo VariableInVariable = typeof(ICondition).GetMethod("Get");
		static readonly MethodInfo VariableAsInt = typeof(ICondition).GetMethod("AsInt");
		static readonly MethodInfo VariableAsBool = typeof(ICondition).GetMethod("AsBool");
		static readonly ParameterExpression ContextParam = Expressions.Expression.Parameter(typeof(ICondition), "context");
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

				if (fromType == ExpressionType.Variable)
				{
					switch (toType)
					{
						case ExpressionType.Bool:
							return Expressions.Expression.Call(expression, VariableAsBool);
						case ExpressionType.Int:
							return Expressions.Expression.Call(expression, VariableAsInt);
					}
				}
				else
				{
					switch (toType)
					{
						case ExpressionType.Bool:
							return AsBool(expression);
						case ExpressionType.Int:
							return IfThenElse(expression, One, Zero);
					}
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

			public Func<ICondition, int> Compile(Token[] postfix)
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

						case TokenType.Dot:
						{
							if (ast.PeekType() != ExpressionType.Property)
								throw new InvalidDataException("`.` operator at index {0} requires a property name as its right operand.".F(t.Index));

							var property = ast.Pop(ExpressionType.Property);

							if (ast.PeekType() != ExpressionType.Variable)
								throw new InvalidDataException("`.` operator at index {0} requires a variable as its left operand.".F(t.Index));

							var variable = ast.Pop(ExpressionType.Variable);
							ast.Push(Expressions.Expression.Call(variable, VariableInVariable, property), ExpressionType.Variable);
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
							var name = Expressions.Expression.Constant(((VariableToken)t).Symbol);
							ast.Push(Expressions.Expression.Call(ContextParam, VariableInVariable, name), ExpressionType.Variable);
							continue;
						}

						case TokenType.Property:
						{
							ast.Push(Expressions.Expression.Constant(((PropertyToken)t).Symbol), ExpressionType.Property);
							continue;
						}

						default:
							throw new InvalidProgramException(
								"ConditionExpression.Compiler.Compile() is missing an expression builder for TokenType.{0}".F(
									Enum<TokenType>.GetValues()[(int)t.Type]));
					}
				}

				return Expressions.Expression.Lambda<Func<ICondition, int>>(ast.Pop(ExpressionType.Int), ContextParam).Compile();
			}
		}

		public int Evaluate(ICondition symbols)
		{
			return asFunction(symbols);
		}
	}
}
