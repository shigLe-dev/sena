namespace sena.Lexing;

public class Lexer
{
    private readonly string code;
    private char currentChar
    {
        get
        {
            if (position < code.Length) return code[position];
            return (char)0;
        }
    }
    private char nextChar
    {
        get
        {
            if (position + 1 < code.Length) return code[position + 1];
            return (char)0;
        }
    }
    private int position;

    public Lexer(string code)
    {
        this.code = code;
    }

    public Token NextToken()
    {
        SkipWhiteSpace();
        Token retToken = new Token(currentChar.ToString(), TokenType.ILLEGAL);

        switch (currentChar)
        {
            case ';':
                retToken = new Token(";", TokenType.SEMICOLON);
                break;
            case '=':
                retToken = new Token("=", TokenType.ASSIGN);
                break;
            case '+':
                retToken = new Token("+", TokenType.PLUS);
                break;
            case '-':
                retToken = new Token("-", TokenType.MINUS);
                break;
            case '*':
                retToken = new Token("*", TokenType.ASTERISK);
                break;
            case '/':
                retToken = new Token("/", TokenType.SLASH);
                break;
            case '(':
                retToken = new Token("(", TokenType.LPAREN);
                break;
            case ')':
                retToken = new Token(")", TokenType.RPAREN);
                break;
            default:
                if (IsDigit(currentChar))
                {
                    retToken = new Token(ReadIntegerLiteral(), TokenType.INTEGER_LITERAL);
                }
                else if (IsLetter(currentChar))
                {
                    string literal = ReadIdentifier();
                    retToken = new Token(literal, LookUpIdentifier(literal));
                }
                else if (currentChar == (char)0)
                {
                    retToken = new Token(currentChar.ToString(), TokenType.EOF);
                }
                break;
        }

        ReadChar();
        return retToken;
    }

    private void SkipWhiteSpace()
    {
        while (currentChar == ' '
            || currentChar == '\t'
            || currentChar == '\r'
            || currentChar == '\n')
        {
            ReadChar();
        }
    }

    private string ReadIntegerLiteral()
    {
        string literal = currentChar.ToString();
        while (IsDigit(nextChar))
        {
            literal += nextChar;

            ReadChar();
        }

        return literal;
    }

    private string ReadIdentifier()
    {
        string literal = currentChar.ToString();

        while (IsLetter(nextChar))
        {
            literal += nextChar;
            ReadChar();
        }

        return literal;
    }

    private TokenType LookUpIdentifier(string literal)
    {
        if (Token.keywords.TryGetValue(literal, out TokenType value))
        {
            return value;
        }

        return TokenType.IDENTIFIER;
    }

    private void ReadChar()
    {
        position++;
    }

    public static bool IsLetter(char c)
    {
        return ('a' <= c && c <= 'z')
            || ('A' <= c && c <= 'Z')
            || (c == '_');
    }

    public static bool IsDigit(char c)
    {
        return ('0' <= c && c <= '9');
    }
}
