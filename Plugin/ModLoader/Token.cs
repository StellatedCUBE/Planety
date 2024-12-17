using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader
{
    struct Token
    {
        public enum Type
        {
            COLON, COMMA, SEMICOLON, DOT, ASSIGN, QUESTION,
            PLUS, MINUS, TIMES, DIV,

            LPAREN, RPAREN,
            LBRACKET, RBRACKET,
            LBRACE, RBRACE,

            LT, LE, EQ, GE, GT, NE,

            KWIF, KWFOR,

            COLOR,
            IDENTIFIER,
            NUMBER,
            STRING,
            VERSION,

            SOF, EOF
        }

        public readonly Type type;
        public readonly object data;

        public Token(Type type)
        {
            this.type = type;
            this.data = null;
        }

        public Token(Type type, object data)
        {
            this.type = type;
            this.data = data;
        }

        public static implicit operator Token(Type type) => new Token(type);
    }
}
