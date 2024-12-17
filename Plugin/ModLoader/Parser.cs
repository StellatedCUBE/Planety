using Planety.ModLoader.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader
{
    class Parser
    {
        Lexer lexer;

        public Parser(Lexer lexer) => this.lexer = lexer;

        public Mod ParseModDefinition()
        {
            (int, int) pos;

            if (lexer.current.type == Token.Type.SOF)
                lexer.Next();

            bool extend = false;

            while (true)
            {
                if (lexer.current.type == Token.Type.EOF)
                    return null;

                var id = (string)lexer.Expect(Token.Type.IDENTIFIER).data;

                if (id == "mod")
                    break;

                if (id == "extend")
                {
                    extend = true;
                    break;
                }

                Version ver;

                if (id == "planety")
                    ver = new Version(Plugin.VERSION);

                else
                    throw new ScriptException($"Dependency {id} is not installed");

                while (lexer.current.type != Token.Type.SEMICOLON)
                {
                    var cmp = lexer.current.type;
                    pos = lexer.GetCurrentPosition();
                    lexer.Next();
                    var cver = (Version)lexer.Expect(Token.Type.VERSION).data;
                    bool bad;

                    switch (cmp)
                    {
                        case Token.Type.LT:
                            bad = ver >= cver;
                            break;

                        case Token.Type.LE:
                            bad = ver > cver;
                            break;

                        case Token.Type.EQ:
                            bad = ver != cver;
                            break;

                        case Token.Type.GE:
                            bad = ver < cver;
                            break;

                        case Token.Type.GT:
                            bad = ver <= cver;
                            break;

                        case Token.Type.NE:
                            bad = ver == cver;
                            break;

                        default:
                            throw new ScriptException($"Expected version comparison, got {cmp}") { sourceFile = Content.currentFileBeingParsed, pos = pos };
                    }

                    if (bad)
                        throw new ScriptException($"Dependency {id} is not a supported version") { sourceFile = Content.currentFileBeingParsed };
                }

                lexer.Next();
            }

            pos = lexer.GetCurrentPosition();
            var modId = lexer.ExpectIdentifierOrString();

            if (!IsValidASCIIID(modId) || modId == "Stock")
                throw new ScriptException($"Invalid mod ID \"{modId}\"") { sourceFile = Content.currentFileBeingParsed, pos = pos };

            if (extend)
            {
                bool failSilently = lexer.AcceptAndPop(Token.Type.QUESTION);
                return new Mod.Extension { failSilently = failSilently, id = modId, code = ParseBlock(), pos = pos };
            }

            var mod = new Mod { id = modId, name = modId };

            while (lexer.current.type == Token.Type.IDENTIFIER)
            {
                pos = lexer.GetCurrentPosition();
                var @case = lexer.ExpectIdentifierOrString();
                switch (@case)
                {
                    case "named":
                        mod.name = (string)lexer.Expect(Token.Type.STRING).data;
                        break;

                    case "runs":
                        var rt = lexer.current.type == Token.Type.NUMBER ? (RunTime)Convert.ToInt32(lexer.Pop().data) : (RunTime)Enum.Parse(typeof(RunTime), lexer.ExpectIdentifierOrString(), true);
                        if (rt == RunTime.Always)
                            mod.always = true;
                        else
                            mod.runTime = rt;
                        break;

                    case "description":
                        lexer.Expect(Token.Type.STRING);
                        break;

                    default:
                        throw new ScriptException($"Unknown mod parameter {@case}") { sourceFile = Content.currentFileBeingParsed, pos = pos };
                }
            }

            mod.code = ParseBlock();

            return mod;
        }

        Block ParseBlock()
        {
            var pos = lexer.GetCurrentPosition();
            lexer.Expect(Token.Type.LBRACE);
            var stmts = new List<Node>();
            while (lexer.current.type != Token.Type.RBRACE)
            {
                stmts.Add(ParseExpr());
                while (lexer.current.type == Token.Type.SEMICOLON)
                    lexer.Pop();
            }
            lexer.Pop();
            return new Block(stmts, pos);
        }

        Node ParseExpr() => ParseExprAssign();

        Node ParseExprAssign()
        {
            var lhs = ParseExprLogic();

            var pos = lexer.GetCurrentPosition();
            if (lexer.AcceptAndPop(Token.Type.ASSIGN))
                return new Assign(lexer.current.type == Token.Type.SEMICOLON ? new Constant { position = pos } : ParseExprAssign(), lhs, pos);

            return lhs;
        }

        Node ParseExprLogic()
        {
            var lhs = ParseExprCmp();

            return lhs;
        }

        Node ParseExprCmp()
        {
            var lhs = ParseExprAddSub();

            return lhs;
        }

        Node ParseExprAddSub()
        {
            var node = ParseExprMulDiv();

            while (true)
            {
                var pos = lexer.GetCurrentPosition();
                if (lexer.AcceptAndPop(Token.Type.PLUS))
                    node = new Add { lhs = node, rhs = ParseExprMulDiv(), position = pos };
                else if (lexer.AcceptAndPop(Token.Type.MINUS))
                    node = new MathBinOp { lhs = node, rhs = ParseExprMulDiv(), op = (l, r) => l - r, position = pos };
                else
                    return node;
            }
        }

        Node ParseExprMulDiv()
        {
            var node = ParseExprPrefix();

            while (true)
            {
                var pos = lexer.GetCurrentPosition();
                if (lexer.AcceptAndPop(Token.Type.TIMES))
                    node = new MathBinOp { lhs = node, rhs = ParseExprPrefix(), op = (l, r) => l * r, position = pos };
                else if (lexer.AcceptAndPop(Token.Type.DIV))
                    node = new MathBinOp { lhs = node, rhs = ParseExprPrefix(), op = (l, r) => l / r, position = pos };
                else
                    return node;
            }
        }

        Node ParseExprPrefix()
        {
            var pos = lexer.GetCurrentPosition();
            return lexer.AcceptAndPop(Token.Type.MINUS) ? new Negate { value = ParseExprPrefix(), position = pos } : ParseExprSuffix();
        }

        Node ParseExprSuffix()
        {
            var node = ParseExprInner();

            while (true)
            {
                var pos = lexer.GetCurrentPosition();
                if (lexer.AcceptAndPop(Token.Type.DOT))
                    node = new Property { of = node, property = (string)lexer.Expect(Token.Type.IDENTIFIER).data, position = pos };
                else if (lexer.AcceptAndPop(Token.Type.LPAREN))
                {
                    var args = new List<Node>();
                    while (!lexer.AcceptAndPop(Token.Type.RPAREN))
                    {
                        args.Add(ParseExpr());
                        if (lexer.current.type != Token.Type.RPAREN)
                            lexer.Expect(Token.Type.COMMA);
                    }
                    var call = new Call { method = node, arguments = args, position = pos };
                    call.DoWarnings();
                    node = call;
                }
                else
                    return node;
            }
        }

        Node ParseExprInner()
        {
            if (lexer.AcceptAndPop(Token.Type.LPAREN))
            {
                var ret = ParseExpr();
                lexer.Expect(Token.Type.RPAREN);
                return ret;
            }

            if (lexer.current.type == Token.Type.LBRACE)
                return ParseBlock();

            if (lexer.current.type == Token.Type.LBRACKET)
                return ParseListMapLit();

            var pos = lexer.GetCurrentPosition();

            if (lexer.current.type == Token.Type.STRING || lexer.current.type == Token.Type.NUMBER || lexer.current.type == Token.Type.COLOR)
                return new Constant { value = lexer.Pop().data, position = pos };

            if (lexer.current.type == Token.Type.IDENTIFIER)
                return new Property { of = new Scope(), property = (string)lexer.Pop().data, position = pos };

            throw new ScriptException("Expected expression") { sourceFile = Content.currentFileBeingParsed, pos = pos};
        }

        Node ParseListMapLit()
        {
            var pos = lexer.GetCurrentPosition();

            lexer.Expect(Token.Type.LBRACKET);

            if (lexer.AcceptAndPop(Token.Type.RBRACKET))
                return new ListLit { list = new() };

            var cl = lexer.Clone();
            cl.Pop();
            if (cl.AcceptAndPop(Token.Type.COLON))
            {
                var map = new Dictionary<string, Node>();

                while (!lexer.AcceptAndPop(Token.Type.RBRACKET))
                {
                    var key = lexer.ExpectIdentifierOrString();
                    lexer.Expect(Token.Type.COLON);
                    map.Add(key, ParseExpr());
                    if (lexer.current.type != Token.Type.RBRACKET)
                        lexer.Expect(Token.Type.COMMA);
                }

                return new MapLit { map = map, position = pos };
            }

            var list = new List<Node>();
            List<Node> counts = null;

            while (true)
            {
                list.Add(ParseExpr());
                if (lexer.AcceptAndPop(Token.Type.SEMICOLON))
                {
                    if (counts == null)
                    {
                        counts = new(list.Count);
                        for (int i = 1; i < list.Count; i++)
                            counts.Add(new Constant { value = 1 });
                    }
                    counts.Add(ParseExpr());
                }
                else if (counts != null)
                    counts.Add(new Constant { value = 1 });
                if (lexer.AcceptAndPop(Token.Type.RBRACKET))
                    return counts == null ? new ListLit { list = list, position = pos } : new ListLit2 { list = list, position = pos, counts = counts };
                lexer.Expect(Token.Type.COMMA);
            }
        }

        internal static bool IsValidASCIIID(string id) => id.Length > 0 && !char.IsDigit(id[0]) && id.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_');
    }
}
