﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using IronJS.Compiler.Ast;
using IronJS.Extensions;
using EcmaLexer = IronJS.Compiler.Parser.ES3Lexer;
using EcmaParser = IronJS.Compiler.Parser.ES3Parser;

namespace IronJS.Compiler
{
    public class AstGenerator
    {
        public List<Node> Build(string fileName, Encoding encoding)
        {
            return Build(
                System.IO.File.ReadAllText(
                    fileName, 
                    encoding
                )
            );
        }

        public List<Node> Build(string source)
        {
            var lexer = new EcmaLexer(new ANTLRStringStream(source));
            var parser = new EcmaParser(new CommonTokenStream(lexer));

            var program = parser.program();
            var root = (ITree)program.Tree;
            var nodes = new List<Node>();

            if (root == null)
            {
                nodes.Add(new NullNode(null));
            }
            else if (root.IsNil)
            {
                root.EachChild(node => {
                    nodes.Add(Build(node));
                });
            }
            else
            {
                nodes.Add(Build(root));
            }

            return nodes;
        }

        private Node Build(ITree node)
        {
            if (node == null)
                return null;

            switch (node.Type)
            {

                case EcmaParser.WITH:
                    return BuildWith(node);

                case EcmaParser.BLOCK:
                    return BuildBlock(node);


                case EcmaParser.RegularExpressionLiteral:
                    return BuildRegex(node);

                /*
                 * Expresion statements
                 */

                case EcmaParser.PAREXPR:
                    return Build(node.GetChildSafe(0));

                case EcmaParser.EXPR:
                    return Build(node.GetChildSafe(0));

                case EcmaParser.CEXPR:
                    return BuildCommaExpression(node);

                /*
                 * Objects
                 */
                case EcmaParser.OBJECT:
                    return BuildObject(node);

                case EcmaParser.NEW:
                    return BuildNew(node);

                case EcmaParser.INSTANCEOF:
                    return BuildInstanceOf(node);

                case EcmaParser.ARRAY:
                    return BuildArray(node);

                /*
                 * Functions
                 */

                case EcmaParser.FUNCTION:
                    return BuildFunction(node);

                case EcmaParser.RETURN:
                    return BuildReturn(node);

                case EcmaParser.CALL:
                    return BuildCall(node);

                /*
                 * If statement
                 */

                case EcmaParser.IF:
                case EcmaParser.QUE: // <expr> ? <expr> : <expr>
                    return BuildIf(node);


                case EcmaParser.SWITCH:
                    return BuildSwitch(node);

                /*
                 * Variables
                 */
                case EcmaParser.THIS: // 'this' is just an identifier
                case EcmaParser.Identifier:
                    return BuildIdentifier(node);

                /*
                 * Exceptions
                 */

                case EcmaParser.TRY:
                    return BuildTry(node);

                case EcmaParser.CATCH:
                    return BuildCatch(node);

                case EcmaParser.FINALLY:
                    return BuildFinally(node);

                case EcmaParser.THROW:
                    return BuildThrow(node);

                /*
                 * Property
                 */

                case EcmaParser.BYFIELD:
                    return BuildMemberAccess(node);

                case EcmaParser.BYINDEX:
                    return BuildIndexAccess(node);

                case EcmaLexer.IN:
                    return BuildIn(node);

                /*
                 * Loops
                 */
                case EcmaParser.WHILE:
                    return BuildWhile(node);

                case EcmaParser.FOR:
                    return BuildFor(node);

                case EcmaParser.DO:
                    return BuildDoWhile(node);

                case EcmaParser.BREAK:
                    return BuildBreak(node);

                case EcmaParser.CONTINUE:
                    return BuildContinue(node);

                case EcmaParser.LABELLED:
                    return BuildLabelled(node);

                /*
                 * Literals
                 */
                case EcmaParser.DecimalLiteral:
                    return BuildNumber(node);

                case EcmaParser.StringLiteral:
                    return BuildString(node);

                case EcmaParser.NULL:
                    return BuildNull(node);

                case EcmaParser.TRUE:
                case EcmaParser.FALSE:
                    return BuildBoolean(node);

                /*
                 * Assignments
                 */
                case EcmaParser.VAR:
                    return BuildVarAssign(node);

                case EcmaParser.ASSIGN:
                    return BuildAssign(node, false);

                /*
                 * Binary Operators
                 */
                // 1 + 2
                case EcmaParser.ADD:
                    return BuildBinaryOp(node, ExpressionType.Add);

                // 1 - 2
                case EcmaParser.SUB:
                    return BuildBinaryOp(node, ExpressionType.Subtract);

                // 1 * 2
                case EcmaParser.MUL:
                    return BuildBinaryOp(node, ExpressionType.Multiply);

                // 1 / 2
                case EcmaParser.DIV:
                    return BuildBinaryOp(node, ExpressionType.Divide);

                // 1 % 2
                case EcmaParser.MOD:
                    return BuildBinaryOp(node, ExpressionType.Modulo);

                // foo += 1
                case EcmaParser.ADDASS:
                    return BuildBinaryOpAssign(node, ExpressionType.Add);

                // foo -= 1
                case EcmaParser.SUBASS:
                    return BuildBinaryOpAssign(node, ExpressionType.Subtract);

                // foo *= 1
                case EcmaParser.MULASS:
                    return BuildBinaryOpAssign(node, ExpressionType.Multiply);

                // foo /= 1
                case EcmaParser.DIVASS:
                    return BuildBinaryOpAssign(node, ExpressionType.Divide);

                // foo %= 1
                case EcmaParser.MODASS:
                    return BuildBinaryOpAssign(node, ExpressionType.Modulo);

                // 1 == 2
                case EcmaParser.EQ:
                    return BuildBinaryOp(node, ExpressionType.Equal);

                // 1 != 2
                case EcmaParser.NEQ:
                    return BuildBinaryOp(node, ExpressionType.NotEqual);

                // 1 === 2
                case EcmaParser.SAME:
                    return BuildStrictCompare(node, ExpressionType.Equal);

                // 1 !== 2
                case EcmaParser.NSAME:
                    return BuildStrictCompare(node, ExpressionType.NotEqual);

                // 1 < 2
                case EcmaParser.LT:
                    return BuildBinaryOp(node, ExpressionType.LessThan);

                // 1 > 2
                case EcmaParser.GT:
                    return BuildBinaryOp(node, ExpressionType.GreaterThan);

                // 1 >= 2
                case EcmaParser.GTE:
                    return BuildBinaryOp(node, ExpressionType.GreaterThanOrEqual);

                // 1 <= 2
                case EcmaParser.LTE:
                    return BuildBinaryOp(node, ExpressionType.LessThanOrEqual);

                // 1 >> 2
                case EcmaParser.SHR:
                    return BuildBinaryOp(node, ExpressionType.RightShift);

                // 1 << 2
                case EcmaParser.SHL:
                    return BuildBinaryOp(node, ExpressionType.LeftShift);

                // 1 >>> 2
                case EcmaParser.SHU:
                    return BuildUnsignedRightShift(node);

                // foo >>= 1
                case EcmaParser.SHRASS:
                    return BuildBinaryOpAssign(node, ExpressionType.RightShift);

                // foo <<= 1
                case EcmaParser.SHLASS:
                    return BuildBinaryOpAssign(node, ExpressionType.LeftShift);

                // foo >>>= 1
                case EcmaParser.SHUASS:
                    return BuildUnsignedRightShiftAssign(node);

                // 1 & 2
                case EcmaParser.AND:
                    return BuildBinaryOp(node, ExpressionType.And);

                // 1 | 2
                case EcmaParser.OR:
                    return BuildBinaryOp(node, ExpressionType.Or);

                // 1 ^ 2
                case EcmaParser.XOR:
                    return BuildBinaryOp(node, ExpressionType.ExclusiveOr);

                // foo &= 1
                case EcmaParser.ANDASS:
                    return BuildBinaryOpAssign(node, ExpressionType.And);

                // foo |= 1
                case EcmaParser.ORASS:
                    return BuildBinaryOpAssign(node, ExpressionType.Or);

                // foo ^= 1
                case EcmaParser.XORASS:
                    return BuildBinaryOpAssign(node, ExpressionType.ExclusiveOr);

                // true && false
                case EcmaParser.LAND:
                    return BuildLogicalNode(node, ExpressionType.AndAlso);

                // true || false
                case EcmaParser.LOR:
                    return BuildLogicalNode(node, ExpressionType.OrElse);

                /*
                 * Increment/Decrement operators
                 */
                // foo++
                case EcmaParser.PINC:
                    return BuildIncDecNode(node, ExpressionType.PostIncrementAssign);

                // foo--
                case EcmaParser.PDEC:
                    return BuildIncDecNode(node, ExpressionType.PostDecrementAssign);

                // ++foo
                case EcmaParser.INC:
                    return BuildIncDecNode(node, ExpressionType.PreIncrementAssign);

                // --foo
                case EcmaParser.DEC:
                    return BuildIncDecNode(node, ExpressionType.PreDecrementAssign);

                /*
                 * Unary operators
                 */
                //TODO: make sure ~, ! and - have the right ExpressionType
                // ~foo
                case EcmaParser.INV:
                    return BuildUnuaryOp(node, ExpressionType.OnesComplement);

                // !foo
                case EcmaParser.NOT:
                    return BuildUnuaryOp(node, ExpressionType.Not);

                // -foo
                case EcmaParser.NEG:
                    return BuildUnuaryOp(node, ExpressionType.Negate);

                // +foo
                case EcmaParser.POS:
                    return BuildUnuaryOp(node, ExpressionType.UnaryPlus);

                // typeof foo
                case EcmaParser.TYPEOF:
                    return BuildTypeOfOp(node);

                // void foo
                case EcmaParser.VOID:
                    return BuildVoidOp(node);

                // delete foo
                case EcmaParser.DELETE:
                    return BuildDelete(node);

                /*
                 * Error handling
                 */
                default:
                    throw new Compiler.CompilerError(
                        String.Format("Unrecognized token '{0}'", Name(node))
                    );
            }
        }

        private Node BuildRegex(ITree node)
        {
            return new RegexNode(node.Text, node);
        }

        private Node BuildArray(ITree node)
        {
            return new ArrayNode(
                node.Map(
                    x => Build(x.GetChildSafe(0))
                ),
                node
            );
        }

        private Node BuildInstanceOf(ITree node)
        {
            return new InstanceOfNode(
                Build(node.GetChildSafe(0)),
                Build(node.GetChildSafe(1)),
                node
            );
        }

        private Node BuildSwitch(ITree node)
        {
            var def = (Node)(new NullNode(node));
            var cases = new List<Tuple<Node, Node>>();

            for (int i = 1; i < node.ChildCount; ++i)
            {
                var child = node.GetChildSafe(i);

                if (child.Type == EcmaParser.DEFAULT)
                {
                    def = Build(child.GetChildSafe(0));
                }
                else
                {
                    var caseBlock = new List<Node>();

                    for(int j = 1; j < child.ChildCount; ++j)
                        caseBlock.Add(
                            Build(child.GetChildSafe(j))
                        );

                    cases.Add(
                        Tuple.Create(
                            Build(child.GetChildSafe(0)),
                            (Node)new BlockNode(caseBlock, node)
                        )
                    );
                }
            }

            return new SwitchNode(
                Build(node.GetChildSafe(0)),
                def,
                cases,
                node
            );

            throw new NotImplementedException();
        }

        private Node BuildCommaExpression(ITree node)
        {
            return new AssignmentBlockNode(
                node.Map(x => Build(x)),
                false,
                node
            );
        }

        private Node BuildIn(ITree node)
        {
            return new InNode(
                Build(node.GetChildSafe(1)), 
                Build(node.GetChildSafe(0)),
                node
            );
        }

        private Node BuildDelete(ITree node)
        {
            return new DeleteNode(
                Build(node.GetChildSafe(0)),
                node
            );
        }

        private Node BuildLabelled(ITree node)
        {
            var label = node.GetChildSafe(0);
            var target = Build(node.GetChildSafe(1));

            if (!(target is IStatementNode))
                throw new CompilerError("Can only label nodes that implement ILabelableNode");

           (target as IStatementNode).SetLabel(label.Text);
           return target;
        }

        private Node BuildIndexAccess(ITree node)
        {
            if (RewriteIfContainsNew(node.GetChildSafe(0)))
            {
                return new NewNode(Build(node), node);
            }
            else
            {
                return new IndexAccessNode(
                    Build(node.GetChildSafe(0)),
                    Build(node.GetChildSafe(1)),
                    node
                );
            }
        }

        private Node BuildThrow(ITree node)
        {
            return new ThrowNode(
                Build(node.GetChildSafe(0)),
                node
            );
        }

        private Node BuildFinally(ITree node)
        {
            return BuildBlock(node.GetChildSafe(0));
        }

        private Node BuildCatch(ITree node)
        {
            return new CatchNode(
                Build(node.GetChildSafe(0)),
                Build(node.GetChildSafe(1)),
                node
            );
        }

        private Node BuildTry(ITree node)
        {
            if (node.ChildCount > 2)
            {
                return new TryNode(
                    Build(node.GetChildSafe(0)),
                    (CatchNode)Build(node.GetChildSafe(1)),
                    Build(node.GetChildSafe(2)),
                    node
                );
            }
            else
            {
                var secondChild = node.GetChildSafe(1);

                if (secondChild.Type == EcmaParser.FINALLY)
                {
                    return new TryNode(
                        Build(node.GetChildSafe(0)),
                        null,
                        Build(node.GetChildSafe(1)),
                        node
                    );
                }
                else
                {
                    return new TryNode(
                        Build(node.GetChildSafe(0)),
                        (CatchNode)Build(node.GetChildSafe(1)),
                        null,
                        node
                    );
                }
            }
        }

        private Node BuildWith(ITree node)
        {
            return new WithNode(
                Build(node.GetChildSafe(0)),
                Build(node.GetChildSafe(1)),
                node
            );
        }

        private Node BuildDoWhile(ITree node)
        {
            var body = Build(node.GetChildSafe(0));
            var test = Build(node.GetChildSafe(1));

            return new WhileNode(
                test,
                body,
                WhileType.DoWhile,
                node
            );
        }

        private Node BuildContinue(ITree node)
        {
            if (node.ChildCount == 0)
                return new ContinueNode(null, node);

            return new ContinueNode(
                node.GetChildSafe(0).Text, 
                node
            );
        }

        // 12.8
        private Node BuildBreak(ITree node)
        {
            if (node.ChildCount == 0)
                return new BreakNode(null, node);

            return new BreakNode(
                node.GetChildSafe(0).Text, 
                node
            );
        }

        private Node BuildFor(ITree node)
        {
            var body = Build(node.GetChildSafe(1));
            var type = node.GetChildSafe(0);

            // 12.6.3
            if (type.Type == EcmaParser.FORSTEP)
            {
                var init = type.GetChildSafe(0);
                var test = type.GetChildSafe(1);
                var incr = type.GetChildSafe(2);

                var initNode = init.ChildCount > 0
                             ? Build(init)
                             : new NullNode(node);

                var testNode = test.ChildCount > 0
                             ? Build(test)
                             : new BooleanNode(true, node);

                var incrNode = incr.ChildCount > 0
                             ? Build(incr)
                             : new NullNode(node);

                return new ForStepNode(
                    initNode,
                    testNode,
                    incrNode,
                    body,
                    node
                );
            }
            // 12.6.4
            else if(type.Type == EcmaParser.FORITER)
            {
                return new ForInNode(
                    Build(type.GetChildSafe(0)),
                    Build(type.GetChildSafe(1)),
                    body,
                    node
                );
            }

            throw new NotImplementedException();
        }

        private Node BuildUnsignedRightShiftAssign(ITree node)
        {
            return new AssignNode(
                Build(node.GetChildSafe(0)),
                new UnsignedRightShiftNode(
                    Build(node.GetChildSafe(0)),
                    Build(node.GetChildSafe(1)),
                    node.GetChildSafe(0)
                ),
                node
            );
        }

        private Node BuildUnsignedRightShift(ITree node)
        {
            return new UnsignedRightShiftNode(
                Build(node.GetChildSafe(0)),
                Build(node.GetChildSafe(1)),
                node
            );
        }

        private Node BuildStrictCompare(ITree node, ExpressionType type)
        {
            return new StrictCompareNode(
                Build(node.GetChildSafe(0)), 
                Build(node.GetChildSafe(1)),
                type,
                node
            );
        }

        private Node BuildVoidOp(ITree node)
        {
            return new VoidNode(
                Build(node.GetChildSafe(0)), 
                node
            );
        }

        private Node BuildBoolean(ITree node)
        {
            return new BooleanNode(
                node.Type == 5, 
                node
            );
        }

        private Node BuildTypeOfOp(ITree node)
        {
            return new TypeOfNode(
                Build(node.GetChildSafe(0)), 
                node
            );
        }

        private Node BuildIncDecNode(ITree node, ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                    return new AssignNode(
                        Build(node.GetChildSafe(0)),
                        new BinaryOpNode(
                            Build(node.GetChildSafe(0)),
                            new NumberNode<double>(1.0, NodeType.Double, node),
                            type == ExpressionType.PreIncrementAssign
                                  ? ExpressionType.Add
                                  : ExpressionType.Subtract,
                            node
                        ),
                        node
                    );

                case ExpressionType.PostIncrementAssign:
                    return new PostfixOperatorNode(
                        Build(node.GetChildSafe(0)),
                        ExpressionType.PostIncrementAssign,
                        node
                    );

                case ExpressionType.PostDecrementAssign:
                    return new PostfixOperatorNode(
                        Build(node.GetChildSafe(0)),
                        ExpressionType.PostDecrementAssign,
                        node
                    );
            }

            throw new NotImplementedException();
        }

        private Node BuildLogicalNode(ITree node, ExpressionType op)
        {
            return new LogicalNode(
                Build(node.GetChildSafe(0)),
                Build(node.GetChildSafe(1)),
                op,
                node
            );
        }

        private Node BuildUnuaryOp(ITree node, ExpressionType op)
        {
            return new UnaryOpNode(
                Build(node.GetChildSafe(0)),
                op,
                node
            );
        }

        private Node BuildBinaryOpAssign(ITree node, ExpressionType op)
        {
            return new AssignNode(
                Build(node.GetChildSafe(0)),
                new BinaryOpNode(
                    Build(node.GetChildSafe(0)),
                    Build(node.GetChildSafe(1)),
                    op,
                    node
                ),
                node
            );
        }

        private Node BuildBinaryOp(ITree node, ExpressionType op)
        {
            return new BinaryOpNode(
                Build(node.GetChildSafe(0)),
                Build(node.GetChildSafe(1)),
                op,
                node
            );
        }

        private Node BuildReturn(ITree node)
        {
            if (node.ChildCount == 0)
                return new ReturnNode(new NullNode(node), node);

            return new ReturnNode(Build(node.GetChildSafe(0)), node);
        }

        private Node BuildVarAssign(ITree node)
        {
            var nodes = new List<Node>();

            for (int i = 0; i < node.ChildCount; ++i)
            {
                var assignNode = Build(node.GetChildSafe(i));

                if (assignNode is AssignNode)
                {
                    var target = ((AssignNode)assignNode).Target;

                    if (target is IdentifierNode)
                        ((IdentifierNode)target).IsDefinition = true;
                }

                var identifierNode = assignNode as IdentifierNode;

                if (identifierNode != null)
                    identifierNode.IsDefinition = true;

                nodes.Add(assignNode);
            }

            if (nodes.Count == 1)
                return nodes[0];

            return new AssignmentBlockNode(nodes, true, node);
        }

        private Node BuildObject(ITree node)
        {
            var propertyDict = new Dictionary<string, Node>();

            node.EachChild(x => 
                propertyDict.Add(
                    x.GetChildSafe(0).Text.Trim('\'', '"'),
                    Build(x.GetChildSafe(1))
                )
            );

            return new ObjectNode(propertyDict, node);
        }

        private Node BuildWhile(ITree node)
        {
            var testNode = Build(node.GetChildSafe(0));
            var bodyNode = Build(node.GetChildSafe(1));

            return new WhileNode(
                testNode, 
                bodyNode,
                WhileType.While,
                node
            );
        }

        private Node BuildFunction(ITree node)
        {
            if (node.ChildCount > 2)
            {
                return BuildLambda(
                    node.GetChildSafe(1),
                    node.GetChildSafe(2),
                    node.GetChildSafe(0).Text
                );
            }
            else
            {
                return BuildLambda(
                    node.GetChildSafe(0), 
                    node.GetChildSafe(1), 
                    null
                );
            }
        }

        private Node BuildLambda(ITree argsNode, ITree block, string name)
        {
            var args = argsNode.Map(x => x.Text);
            var body = BuildBlock(block);

            return new LambdaNode(args, body, name, argsNode);
        }

        private Node BuildNew(ITree node)
        {
            return new NewNode(
                Build(node.GetChildSafe(0)),
                node
            );
        }

        private Node BuildString(ITree node)
        {
            return new StringNode(
                node.Text.Substring(1, node.Text.Length - 2),
                node.Text[0],
                node
            );
        }

        private Node BuildBlock(ITree node)
        {
            var nodes = new List<Node>();

            node.EachChild( x => nodes.Add(Build(x)) );

            return new BlockNode(nodes, node);
        }

        private Node BuildIf(ITree node)
        {
            return new IfNode(
                Build(node.GetChildSafe(0)), 
                Build(node.GetChildSafe(1)), 
                Build(node.GetChild(2)), // can be null
                node.Type == EcmaParser.QUE,
                node
            );
        }

        private Node BuildMemberAccess(ITree node)
        {
            if (RewriteIfContainsNew(node.GetChildSafe(0)))
            {
                return new NewNode(Build(node), node);
            }
            else
            {
                return new MemberAccessNode(
                    Build(node.GetChildSafe(0)),
                    node.GetChildSafe(1).Text,
                    node
                );
            }
        }

        private Node BuildCall(ITree node)
        {
            if (RewriteIfContainsNew(node.GetChildSafe(0)))
            {
                return new NewNode(
                    Build(node.GetChildSafe(0)),
                    node.GetChildSafe(1).Map(x => Build(x)),
                    node
                );
            }
            else
            {
                return new CallNode(
                    Build(node.GetChildSafe(0)),
                    node.GetChildSafe(1).Map(x => Build(x)),
                    node
                );
            }
        }

        private Node BuildNull(ITree node)
        {
            return new NullNode(node);
        }

        private Node BuildNumber(ITree node)
        {
            if (node.Text.Contains("."))
                return new NumberNode<double>(Double.Parse(node.Text, CultureInfo.InvariantCulture), NodeType.Double, node);
            else
                return new NumberNode<int>(Int32.Parse(node.Text, CultureInfo.InvariantCulture), NodeType.Integer, node);
        }

        private Node BuildIdentifier(ITree node)
        {
            return new IdentifierNode(node.Text, node);
        }

        private Node BuildAssign(ITree node, bool isLocal)
        {
            var lhs = node.GetChildSafe(0);
            var rhs = node.GetChildSafe(1);

            return new AssignNode(
                Build(lhs), 
                Build(rhs),
                node
            );
        }

        private bool RewriteIfContainsNew(ITree node)
        {
            while (node != null)
            {
                if (node.Type == EcmaParser.NEW)
                {
                    var child = node.GetChildSafe(0);

                    var idNode = new CommonTree(
                        new CommonToken(
                            child.Type,
                            child.Text
                        )
                    );

                    node.Parent.ReplaceChildren(0, 0, idNode);
                    return true;
                }

                if (node.Type == EcmaParser.CALL)
                    return false;

                if (node.Type == EcmaParser.PAREXPR)
                    return false;

                node = node.GetChild(0);
            }

            return false;
        }

        static internal string Name(int type)
        {
            return EcmaParser.tokenNames[type];
        }

        static internal string Name(ITree node)
        {
            return Name(node.Type);
        }
    }
}