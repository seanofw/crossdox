using System;
using System.Collections.Generic;
using System.Linq;
using Crossdox.DocTypes;

namespace Crossdox.Xml
{
	internal class NameParser
	{
		// Names found in the generated doc XML have surprisingly complex grammars.  We parse
		// the name according to the following grammar in LL(1):
		//
		//  name ::= full-unqualified-type method-arguments-opt
		//
		//  method-arguments-opt ::= '(' populated-arg-list-opt ')' |
		//  populated-arg-list-opt ::= populated-arg-list |
		//  populated-arg-list ::= populated-arg out-opt | populated-arg out-opt ',' populated-arg-list
		//  populated-arg ::= populated-arg-type arrays-opt
		//  populated-arg-type ::= populated-type | '`' NUMBER | "``" NUMBER
		//
		//  populated-type ::= populated-name | populated-name '.' populatedtype
		//  populated-name ::= NAME | NAME '{' populated-generic-args '}'
		//  populated-generic-args ::= populated-arg | populated-arg ',' populated-generic-args
		//
		//  arrays-opt ::= arrays |
		//  arrays ::= array | array arrays
		//  array ::= '[' dimensions_opt ']'
		//  dimensions_opt ::= dimensions |
		//  dimensions ::= NUMBER ':' | NUMBER ':' COMMA dimensions
		//
		//  out-opt ::= '@' |
		//
		//  full-unqualified-type ::= unqualified-name | unqualified-name '.' full-unqualified-type
		//  unqualified-name ::= name-part generic-part-opt
		//  name-part ::= '#' NAME | "##" NAME | hashed-name
		//  hashed-name ::= NAME | NAME '#' hashed-name
		//  generic-part-opt ::= '`' NUMBER | "``" NUMBER |
		//
		//  This grammar seems to cover everything we've found in the wild, but there
		//  could always be more we don't know about :-/

		#region Private parsing state

		private NameLexer _lexer;
		private List<string> _classTypeParameterNames;
		private List<string> _methodTypeParameterNames;
		private int _numTypeParameterNames;

		#endregion

		#region Public interface

		// name ::= full-unqualified-type method-arguments-opt
		public NameInfo Parse(string text, bool isMethod)
		{
			_lexer = new NameLexer(text);

			_classTypeParameterNames = new List<string>();
			_methodTypeParameterNames = new List<string>();
			_numTypeParameterNames = 0;

			NameInfo nameInfo = ParseFullUnqualifiedType(isMethod);

			nameInfo = nameInfo.WithParameters(ParseMethodArgumentsOpt());

			Expect(NameTokenKind.EOI, "Unknown garbage at end of name");

			return nameInfo;
		}

		#endregion

		#region Full unqualified type parsing

		//  full-unqualified-type ::= unqualified-name | unqualified-name '.' full-unqualified-type
		private NameInfo ParseFullUnqualifiedType(bool isMethod)
		{
			List<ClassInfo> classes;

			// First, collect up all the ClassInfos.  This is the actual parsing part.
			{
				classes = new List<ClassInfo>();

				ClassInfo classInfo = ParseUnqualifiedName();
				classes.Add(classInfo);

				while (_lexer.Next() == NameTokenKind.Period)
				{
					classInfo = ParseUnqualifiedName();
					classes.Add(classInfo);
				}
				_lexer.Unget();
			}

			// Decompose the last one, since it's the actual name, and make a NameInfo object.
			{
				ClassInfo classInfo = classes.Last();
				classes.RemoveAt(classes.Count - 1);

				return new NameInfo(classes, classInfo.Name, classInfo.TypeParameters, null,
					isMethod ? classInfo.Flags | NameFlags.Method : classInfo.Flags);
			}
		}

		//  unqualified-name ::= name-part generic-part-opt
		private ClassInfo ParseUnqualifiedName()
		{
			string name = ParseNamePart(out NameFlags flags);

			int count = ParseGenericPartOpt(out NameTokenKind kind);

			if (count > 0)
			{
				List<string> typeParams = new List<string>();
				for (int j = 0; j < count; j++)
				{
					string typeParam = "T" + (++_numTypeParameterNames);
					if (kind == NameTokenKind.Backtick)
					{
						_classTypeParameterNames.Add(typeParam);
						typeParams.Add(typeParam);
					}
					else if (kind == NameTokenKind.DoubleBacktick)
					{
						_methodTypeParameterNames.Add(typeParam);
						typeParams.Add(typeParam);
					}
					else
						throw new InvalidOperationException("Invalid generic parameter.");
				}
				return new ClassInfo(name, typeParams, flags);
			}
			else return new ClassInfo(name, null, flags);
		}

		//  name-part ::= '#' NAME | "##" NAME | hashed-name
		//  hashed-name ::= NAME | NAME '#' hashed-name
		private string ParseNamePart(out NameFlags flags)
		{
			switch (_lexer.Next())
			{
				case NameTokenKind.Sharp:
					Expect(NameTokenKind.Name, "Missing ctor or other special name after '#'");
					flags = NameFlags.SpecialName;
					if (_lexer.Token.Text == "ctor")
						flags |= NameFlags.Constructor;
					return "#" + _lexer.Token.Text;

				case NameTokenKind.DoubleSharp:
					Expect(NameTokenKind.Name, "Missing ctor or other special name after '##'");
					flags = NameFlags.SpecialName;
					if (_lexer.Token.Text == "ctor")
						flags |= NameFlags.ClassConstructor | NameFlags.Static;
					return "##" + _lexer.Token.Text;

				case NameTokenKind.Name:
					string name = _lexer.Token.Text;
					flags = 0;
					while (_lexer.Next() == NameTokenKind.Sharp)
					{
						Expect(NameTokenKind.Name, "Missing namespace/interface name after '#'");
						name = name + "#" + _lexer.Token.Text;
						flags = NameFlags.SpecialName | NameFlags.ExplicitInterfaceImplementation;
					}
					_lexer.Unget();
					return name;

				default:
					_lexer.Unget();
					Expect(NameTokenKind.Name, "Missing class or method name");
					flags = default;
					return null;
			}
		}

		//  generic-part-opt ::= '`' NUMBER | "``" NUMBER |
		private int ParseGenericPartOpt(out NameTokenKind kind)
		{
			switch (_lexer.Next())
			{
				case NameTokenKind.Backtick:
					Expect(NameTokenKind.Number, "Count of generic arguments after `");
					kind = NameTokenKind.Backtick;
					return int.Parse(_lexer.Token.Text);

				case NameTokenKind.DoubleBacktick:
					Expect(NameTokenKind.Number, "Count of generic arguments after ``");
					kind = NameTokenKind.DoubleBacktick;
					return int.Parse(_lexer.Token.Text);

				default:
					_lexer.Unget();
					kind = NameTokenKind.Error;
					return 0;
			}
		}

		#endregion

		#region Method argument parsing

		//  method-arguments-opt ::= '(' populated-arg-list-opt ')' |
		//  populated-arg-list-opt ::= populated-arg-list |
		private IEnumerable<ParamInfo> ParseMethodArgumentsOpt()
		{
			if (_lexer.Next() != NameTokenKind.LeftParenthesis)
			{
				_lexer.Unget();
				return null;
			}

			if (_lexer.Next() == NameTokenKind.RightParenthesis)
				return null;
			_lexer.Unget();

			IEnumerable<ParamInfo> paramInfos = ParsePopulatedArgList();

			Expect(NameTokenKind.RightParenthesis, "Missing right parenthesis for method parameters");
			return paramInfos;
		}

		//  populated-arg-list ::= populated-arg out-opt | populated-arg out-opt ',' populated-arg-list
		//  out-opt ::= '@' |
		private IEnumerable<ParamInfo> ParsePopulatedArgList()
		{
			List<ParamInfo> args = new List<ParamInfo>();

			do
			{
				PopulatedType type = ParsePopulatedArg();
				ParamInfo paramInfo = new ParamInfo(type);
				if (_lexer.Next() == NameTokenKind.At)
					paramInfo = paramInfo.WithKind(ParameterKind.Out);
				else
					_lexer.Unget();
				args.Add(paramInfo);
			}
			while (_lexer.Next() == NameTokenKind.Comma);

			_lexer.Unget();

			return args;
		}

		//  populated-arg ::= populated-arg-type arrays-opt out-opt
		//  out-opt ::= '@' |
		private PopulatedType ParsePopulatedArg()
		{
			PopulatedType type = ParsePopulatedArgType();

			IEnumerable<ArrayInfo> arrays = ParseArraysOpt();
			if (arrays != null && arrays.Any())
				type = type.WithArrays(arrays);

			return type;
		}

		//  arrays-opt ::= arrays |
		//  arrays ::= array | array arrays
		private IEnumerable<ArrayInfo> ParseArraysOpt()
		{
			List<ArrayInfo> arrays = new List<ArrayInfo>();

			while (_lexer.Next() == NameTokenKind.LeftBracket)
			{
				_lexer.Unget();
				ArrayInfo array = ParseArray();
				arrays.Add(array);
			}
			_lexer.Unget();

			return arrays;
		}

		//  array ::= '[' dimensions_opt ']'
		//  dimensions_opt ::= dimensions |
		//  dimensions ::= NUMBER ':' | NUMBER ':' COMMA dimensions
		private ArrayInfo ParseArray()
		{
			Expect(NameTokenKind.LeftBracket, "Missing left bracket '[' before array");

			if (_lexer.Next() == NameTokenKind.RightBracket)
				return new ArrayInfo(0);
			_lexer.Unget();

			int numDimensions = 0;
			do
			{
				Expect(NameTokenKind.Number, "Missing number in array brackets");
				Expect(NameTokenKind.Colon, "Missing ':' in array brackets");
				numDimensions++;
			} while (_lexer.Next() == NameTokenKind.Comma);
			_lexer.Unget();

			Expect(NameTokenKind.RightBracket, "Missing right bracket ']' after array");

			return new ArrayInfo(numDimensions);
		}

		//  populated-arg-type ::= populated-type | '`' NUMBER | "``" NUMBER
		private PopulatedType ParsePopulatedArgType()
		{
			switch (_lexer.Next())
			{
				case NameTokenKind.Backtick:
					Expect(NameTokenKind.Number, "Missing generic argument number after `");
					int index = int.Parse(_lexer.Token.Text);
					if (index < 0 || index >= _classTypeParameterNames.Count)
						throw new NameParseException($"Invalid generic argument index at {_lexer.Token.Start}");
					return new PopulatedType(new PopulatedName(_classTypeParameterNames[index]));

				case NameTokenKind.DoubleBacktick:
					Expect(NameTokenKind.Number, "Missing generic argument number after ``");
					index = int.Parse(_lexer.Token.Text);
					if (index < 0 || index >= _methodTypeParameterNames.Count)
						throw new NameParseException($"Invalid generic argument index at {_lexer.Token.Start}");
					return new PopulatedType(new PopulatedName(_methodTypeParameterNames[index]));

				default:
					_lexer.Unget();
					return ParsePopulatedType();
			}
		}

		//  populated-type ::= populated-name | populated-name '.' populatedtype
		private PopulatedType ParsePopulatedType()
		{
			List<PopulatedName> names = new List<PopulatedName>();

			PopulatedName name = ParsePopulatedName();
			names.Add(name);

			while (_lexer.Next() == NameTokenKind.Period)
			{
				name = ParsePopulatedName();
				names.Add(name);
			}
			_lexer.Unget();

			return new PopulatedType(names, null);
		}

		//  populated-name ::= NAME | NAME '{' populated-generic-args '}'
		private PopulatedName ParsePopulatedName()
		{
			Expect(NameTokenKind.Name, "Missing type name");
			string name = _lexer.Token.Text;

			if (_lexer.Next() != NameTokenKind.LeftCurlyBrace)
			{
				_lexer.Unget();
				return new PopulatedName(name);
			}
			else
			{
				IEnumerable<PopulatedType> typeParameters = ParsePopulatedGenericArgs();
				Expect(NameTokenKind.RightCurlyBrace, "Missing '}' after generic arguments");
				return new PopulatedName(name, typeParameters);
			}
		}

		//  populated-generic-args ::= populated-arg | populated-arg ',' populated-generic-args
		private IEnumerable<PopulatedType> ParsePopulatedGenericArgs()
		{
			List<PopulatedType> args = new List<PopulatedType>();

			PopulatedType type = ParsePopulatedArg();
			args.Add(type);

			while (_lexer.Next() == NameTokenKind.Comma)
			{
				type = ParsePopulatedArg();
				args.Add(type);
			}
			_lexer.Unget();

			return args;
		}

		#endregion

		#region Helper methods

		/// <summary>
		/// Require the given token to come next, or throw an exception.
		/// </summary>
		/// <param name="kind">What kind of token to expect.</param>
		/// <param name="errorMessage">The error message, which will have a token position added to it.</param>
		private void Expect(NameTokenKind kind, string errorMessage)
		{
			if (_lexer.Next() == kind) return;
			throw new NameParseException($"{errorMessage} at character {_lexer.Token.Start}");
		}

		#endregion
	}
}
