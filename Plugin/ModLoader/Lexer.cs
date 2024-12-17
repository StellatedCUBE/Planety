using SpaceWarp.API.Versions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety.ModLoader
{
    class Lexer
    {
        string code;
        int i = 0, lastStart = 0, lastPositionLookup = 0, lastPositionLine = 1, lastPositionColumn = 1;
        public Token current = Token.Type.SOF;
        bool mod, planety;

        public Lexer(string code) => this.code = code;

        public Lexer Clone() => new(code) { i = i, lastStart = lastStart, current = current, mod = mod, planety = planety };

        char C => code[i];
        char C2 => i < code.Length - 1 ? code[i + 1] : ' ';

        Token GetNext()
        {
            start:

            while (i < code.Length && char.IsWhiteSpace(C))
                i++;

            lastStart = i;

            if (i == code.Length)
                return Token.Type.EOF;

            if (C == '/' && C2 == '/')
            {
                while (i < code.Length && C != '\n')
                    i++;

                goto start;
            }

            if (C == '/' && C2 == '*')
            {
                while (i < code.Length && (C != '*' || C2 != '/'))
                    i++;

                i += 2;
                goto start;
            }

            if (C == '"')
            {
                StringBuilder s = new StringBuilder();

                while (true)
                {
                    i++;

                    if (i == code.Length)
                        throw new ScriptException("Unterminated string literal") { sourceFile = Content.currentFileBeingParsed };

                    if (C == '"')
                        break;

                    s.Append(C);
                }

                i++;

                return new Token(Token.Type.STRING, s.ToString());
            }

            if (IsIdentifierPart(C) || (C == '.' && char.IsDigit(C2)))
            {
                var pos = GetCurrentPosition();
                StringBuilder s = new StringBuilder(C == '.' ? "0" : "");

                while ((IsIdentifierPart(C) || (C == '.' && char.IsDigit(C2))) && i < code.Length)
                {
                    s.Append(C);
                    i++;
                }

                string t = s.ToString();

                if (t == "planety")
                    planety = true;

                else if (t == "mod")
                {
                    if (!planety)
                        throw new ScriptException("Missing header") { sourceFile = Content.currentFileBeingParsed };
                    mod = true;
                }

                if (t[0] == '#')
                {
                    t = t.Substring(1).ToLowerInvariant();
                    if (t == "nrm" || t == "normal")
                        return new Token(Token.Type.COLOR, new Color(0.5f, 0.5f, 1f, 1f));
                    if (t.Length == 3 || t.Length == 4)
                        t = string.Concat(t.Select(c => $"{c}{c}"));
                    if (t.Length == 6)
                        t += "ff";
                    if (t.Length == 8)
                    {
                        uint x = uint.Parse(t, NumberStyles.AllowHexSpecifier);
                        return new Token(Token.Type.COLOR, (Color)new Color32(
                            (byte)(x >> 24),
                            (byte)(x >> 16),
                            (byte)(x >> 8),
                            (byte)x
                        ));
                    }
                    throw new ScriptException("Invalid color " + t) { sourceFile = Content.currentFileBeingParsed, pos = pos };
                }

                if (char.IsDigit(t[0]))
                    return mod ? new Token(Token.Type.NUMBER, double.Parse(t, CultureInfo.InvariantCulture)) : new Token(Token.Type.VERSION, new Version(t.Contains('.') ? t : t + ".0"));

                return (t == t.ToLowerInvariant() && Enum.TryParse("KW" + t.ToUpperInvariant(), out Token.Type tt)) ? tt : new Token(Token.Type.IDENTIFIER, t);
            }

            if (C2 == '=')
            {
                var t = Token.Type.SOF;
                switch (C)
                {
                    case '<':
                        t = Token.Type.LE;
                        break;

                    case '>':
                        t = Token.Type.GE;
                        break;

                    case '=':
                        t = Token.Type.EQ;
                        break;
                }

                if (t != Token.Type.SOF)
                {
                    i += 2;
                    return t;
                }
            }

            {
                var c = C;
                i++;
                switch (c)
                {
                    case ':': return Token.Type.COLON;
                    case ',': return Token.Type.COMMA;
                    case ';': return Token.Type.SEMICOLON;
                    case '.': return Token.Type.DOT;
                    case '=': return Token.Type.ASSIGN;
                    case '?': return Token.Type.QUESTION;
                    case '+': return Token.Type.PLUS;
                    case '-': return Token.Type.MINUS;
                    case '*': return Token.Type.TIMES;
                    case '/': return Token.Type.DIV;
                    case '(': return Token.Type.LPAREN;
                    case ')': return Token.Type.RPAREN;
                    case '[': return Token.Type.LBRACKET;
                    case ']': return Token.Type.RBRACKET;
                    case '{': return Token.Type.LBRACE;
                    case '}': return Token.Type.RBRACE;
                    case '<': return Token.Type.LT;
                    case '>': return Token.Type.GT;
                }

                throw new ScriptException($"Unexpected Character '{c}' (0x{(int)c:X4})") { sourceFile = Content.currentFileBeingParsed, pos = GetCurrentPosition() };
            }
        }

        public Token Next()
        {
            current = GetNext();
            //Plugin.Log(BepInEx.Logging.LogLevel.Info, $"Lex {current.type}");
            return current;
        }

        public Token Pop()
        {
            var ret = current;
            Next();
            return ret;
        }

        public bool AcceptAndPop(Token.Type type)
        {
            if (current.type == type)
            {
                Next();
                return true;
            }

            return false;
        }

        public Token Expect(Token.Type type)
        {
            if (current.type != type)
                throw new ScriptException($"Expected {type} (Got {current.type})") { sourceFile = Content.currentFileBeingParsed, pos = GetCurrentPosition()};

            var ret = current;
            Next();
            return ret;
        }

        public string ExpectIdentifierOrString()
        {
            if (current.type != Token.Type.IDENTIFIER && current.type != Token.Type.STRING)
            {
                var ks = current.type.ToString();
                if (ks.StartsWith("KW"))
                {
                    Next();
                    return ks.Substring(2).ToLowerInvariant();
                }
                throw new ScriptException($"Expected IDENTIFIER or STRING (Got {ks})") { sourceFile = Content.currentFileBeingParsed, pos = GetCurrentPosition() };
            }

            var ret = (string)current.data;
            Next();
            return ret;
        }

        public (int, int) GetCurrentPosition()
        {
            for (; lastPositionLookup < lastStart; lastPositionLookup++)
            {
                lastPositionColumn++;
                if (lastPositionLookup < code.Length && code[lastPositionLookup] == '\n')
                {
                    lastPositionColumn = 1;
                    lastPositionLine++;
                }
            }

            return (lastPositionLine, lastPositionColumn);
        }

        bool IsIdentifierPart(char c) => c == '_' || c == '#' || char.IsLetterOrDigit(c) || (c > 127 && !char.IsWhiteSpace(c));
    }
}
