﻿using sena.AST;
using sena.AST.Expressions;
using sena.AST.Statements;
using sena.Parsing;
using Xunit.Abstractions;

namespace sena.Test;

public class ParserTest
{
    private readonly ITestOutputHelper Console;
    public ParserTest(ITestOutputHelper testOutputHelper)
    {
        Console = testOutputHelper;
    }

    [Fact]
    public void LetStatement1()
    {
        var code = @"
let a = 3;
let b = 12314314;
let cfawfaw = 444444444;
";
        Errors errors = new Errors();
        Lexer lexer = new Lexer(code);
        Parser parser = new Parser(lexer, errors);
        Root root = parser.Parse();

        //        Console.WriteLine(root.ToCode());
        foreach (var error in errors.errors)
        {
            Console.WriteLine(error);
        }

        Assert.Equal(3, root.statements.Count);

        var names = new List<string>()
        {
            "a",
            "b",
            "cfawfaw"
        };

        for (int i = 0; i < 3; i++)
        {
            LetStatement statement = root.statements[i] as LetStatement;
            string name = statement.name.value;
        }
    }

    [Fact]
    public void ExpressionStatement1()
    {
        var code = @"
hoge;
foo;
piyo;
";
        Errors errors = new Errors();
        Lexer lexer = new Lexer(code);
        Parser parser = new Parser(lexer, errors);
        Root root = parser.Parse();

        Assert.Equal(3, root.statements.Count);

        ExpressionStatement expressionStatement1 = root.statements[0] as ExpressionStatement;
        ExpressionStatement expressionStatement2 = root.statements[1] as ExpressionStatement;
        ExpressionStatement expressionStatement3 = root.statements[2] as ExpressionStatement;

        Identifier identifier1 = expressionStatement1.expression as Identifier;
        Identifier identifier2 = expressionStatement2.expression as Identifier;
        Identifier identifier3 = expressionStatement3.expression as Identifier;

        Assert.Equal("hoge", identifier1.value);
        Assert.Equal("foo", identifier2.value);
        Assert.Equal("piyo", identifier3.value);
    }
}
