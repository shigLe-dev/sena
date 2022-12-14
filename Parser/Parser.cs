using sena.AST;
using sena.AST.Expressions;
using sena.AST.Statements;
using sena.Lexing;
using System.Collections.ObjectModel;

namespace sena.Parsing;

using PrefixParseFunction = Func<IExpression?>;
using InfixParseFunction = Func<IExpression, IExpression?>;

public class Parser
{
    public Token currentToken { get; private set; }
    public Token nextToken { get; private set; }
    readonly Lexer lexer;
    readonly Errors errors;
    event Action<string> Log;
    readonly ReadOnlyDictionary<TokenType, PrefixParseFunction> PrefixParseFunctions;
    readonly ReadOnlyDictionary<TokenType, InfixParseFunction> InfixParseFunctions;
    readonly ReadOnlyDictionary<TokenType, Precedence> Precedences;
    Precedence currentPrecedence
    {
        get
        {
            if (Precedences.TryGetValue(currentToken.tokenType, out Precedence value))
            {
                return value;
            }

            return Precedence.LOWEST;
        }
    }
    Precedence nextPrecedence
    {
        get
        {
            if (Precedences.TryGetValue(nextToken.tokenType, out Precedence value))
            {
                return value;
            }

            return Precedence.LOWEST;
        }
    }

    public Parser(Lexer lexer, Errors errors, Action<string>? Log = null)
    {
        this.lexer = lexer;
        this.errors = errors;
        this.Log = Console.WriteLine;
        if (Log != null) this.Log = Log;
        PrefixParseFunctions = RegisterPrefixParseFunctions().AsReadOnly();
        InfixParseFunctions = RegisterInfixParseFunctions().AsReadOnly();
        Precedences = RegisterPrecedences().AsReadOnly();

        currentToken = lexer.NextToken();
        nextToken = lexer.NextToken();
    }
    public Root Parse()
    {
        List<IStatement> statements = new List<IStatement>();

        while (currentToken.tokenType != TokenType.EOF)
        {
            IStatement? statement = ParseStatement();
            if (statement != null)
            {
                statements.Add(statement);
            }
            else
            {
                ReadToken();
            }
        }

        return new Root(statements);
    }

    private Dictionary<TokenType, Precedence> RegisterPrecedences()
    {
        return new Dictionary<TokenType, Precedence>()
        {
            [TokenType.PLUS] = Precedence.SUM,
            [TokenType.MINUS] = Precedence.SUM,
            [TokenType.ASTERISK] = Precedence.PRODUCT,
            [TokenType.SLASH] = Precedence.PRODUCT,
        };
    }

    private Dictionary<TokenType, PrefixParseFunction> RegisterPrefixParseFunctions()
    {
        return new Dictionary<TokenType, PrefixParseFunction>()
        {
            [TokenType.IDENTIFIER] = ParseIdentifier,
            [TokenType.INTEGER_LITERAL] = ParseIntLiteral,
            [TokenType.MINUS] = ParsePrefixExpression,
            [TokenType.LPAREN] = ParseGroupedExpression,
            [TokenType.TRUE] = ParseBoolLiteral,
            [TokenType.FALSE] = ParseBoolLiteral,
        };
    }

    private Dictionary<TokenType, InfixParseFunction> RegisterInfixParseFunctions()
    {
        return new Dictionary<TokenType, InfixParseFunction>()
        {
            [TokenType.PLUS] = ParseInfixExpression,
            [TokenType.MINUS] = ParseInfixExpression,
            [TokenType.ASTERISK] = ParseInfixExpression,
            [TokenType.SLASH] = ParseInfixExpression,
        };
    }

    private void ReadToken()
    {
        currentToken = nextToken;
        nextToken = lexer.NextToken();
    }

    private bool ExpectCurrent(TokenType tokenType)
    {
        if (currentToken.tokenType == tokenType)
        {
            ReadToken();
            return true;
        }

        errors.AddError($"{currentToken.tokenType} ではなく {tokenType} である必要があります。");
        return false;
    }

    private IStatement? ParseStatement()
    {
        switch (currentToken.tokenType)
        {
            case TokenType.LET_KEYWORD:
                return ParseLetStatement();
            default:
                ExpressionStatement? expressionStatement = ParseExpressionStatement();
                if (expressionStatement != null) return expressionStatement;  

                errors.AddError(currentToken.tokenType + " から始まる文は存在しません。");
                return null;
        }
    }

    private IExpression? ParseExpression(Precedence precedence)
    {
        // 前置
        PrefixParseFunctions.TryGetValue(currentToken.tokenType, out var prefix);
        if (prefix == null)
        {
            errors.AddError($"{currentToken.tokenType} から始まる PrefixParseFunction はありません。");
            return null;
        }

        IExpression? leftExpression = prefix();

        // 中置
        while (precedence < currentPrecedence)
        {
            InfixParseFunctions.TryGetValue(currentToken.tokenType, out var infix);
            if (infix == null)
            {
                return leftExpression;
            }

            if (leftExpression == null) return null;
            leftExpression = infix(leftExpression);
        }

        return leftExpression;
    }

    #region ParseStatements
    private LetStatement? ParseLetStatement()
    {
        // let
        if (!ExpectCurrent(TokenType.LET_KEYWORD)) return null;

        // identifier
        Identifier? name = ParseIdentifier();

        // =
        if (!ExpectCurrent(TokenType.ASSIGN)) return null;

        // value
        IExpression? value = ParseExpression(Precedence.LOWEST);

        // ;
        if (!ExpectCurrent(TokenType.SEMICOLON)) return null;

        if (name == null) return null;
        if (value == null) return null;

        return new LetStatement(name, value);
    }

    private ExpressionStatement? ParseExpressionStatement()
    {
        // 式
        IExpression? expression = ParseExpression(Precedence.LOWEST);
        if (expression == null) return null;

        // ;
        if(!ExpectCurrent(TokenType.SEMICOLON)) return null;

        return new ExpressionStatement(expression);
    }
    #endregion

    #region ParseExpressions
    private Identifier? ParseIdentifier()
    {
        if (currentToken.tokenType != TokenType.IDENTIFIER) return null;
        Identifier identifier = new Identifier(currentToken.literal);
        ReadToken();
        return identifier;
    }

    private IntLiteral? ParseIntLiteral()
    {
        if (currentToken.tokenType != TokenType.INTEGER_LITERAL) return null;
        IntLiteral intLiteral = new IntLiteral(currentToken.literal);
        ReadToken();
        return intLiteral;
    }

    private BoolLiteral? ParseBoolLiteral()
    {
        BoolLiteral boolLiteral = new BoolLiteral(currentToken.tokenType == TokenType.TRUE);
        ReadToken();

        return boolLiteral;
    }

    private PrefixExpression? ParsePrefixExpression()
    {
        string op = currentToken.literal;

        ReadToken();

        IExpression? expression = ParseExpression(Precedence.PREFIX);
        if (expression == null) return null;

        return new PrefixExpression(op, expression);
    }

    private InfixExpression? ParseInfixExpression(IExpression leftExpression)
    {
        string op = currentToken.literal;
        Precedence precedence = currentPrecedence;
        ReadToken();
        IExpression? rightExpression = ParseExpression(precedence);
        if (rightExpression == null) return null;

        return new InfixExpression(op, rightExpression, leftExpression);
    }

    private IExpression? ParseGroupedExpression()
    {
        if (!ExpectCurrent(TokenType.LPAREN)) return null;

        IExpression? expression = ParseExpression(Precedence.LOWEST);

        if (!ExpectCurrent(TokenType.RPAREN)) return null;

        return expression;
    }
    #endregion
}
