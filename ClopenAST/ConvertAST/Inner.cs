using System;
using System.Collections.Generic;
using System.Linq;
using OpenDreamShared.Dream;
using OpenDreamShared.Compiler.DM;

namespace DMTreeParse {
    public partial class ConvertAST {

        DMASTExpression ConvertDeref(Node node) {
            string[] path = null;
            if (node.Tags.ContainsKey("deref")) {
                path = node.Tags["deref"] as string[];
            }
            else if (node.Tags.ContainsKey("ident")) {
                path = node.Tags["ident"] as string[];
            }
            else {
                throw node.Error("cannot convert deref");
            }
            if (path.Length == 1) {
                if (path[0] == "null") {
                    return new DMASTConstantNull();
                }
                return new DMASTIdentifier(path[0]);
            }
            if (path.Length > 1) {
                var ident = path[0];
                var conditional = false;
                DMASTExpression expr = null;
                if (ident == "<expression>") {
                    expr = derefExprStack.Peek();
                    conditional = derefExprCond.Peek();
                }
                else {
                    expr = new DMASTIdentifier(ident);
                }

                int pos = 1;
                while (pos < path.Length) {
                    if (path[pos] == ".") {
                        expr = new DMASTDereference(expr, path[pos + 1], DMASTDereference.DereferenceType.Direct, conditional);
                        conditional = false;
                    }
                    else if (path[pos] == ":") {
                        expr = new DMASTDereference(expr, path[pos + 1], DMASTDereference.DereferenceType.Search, conditional);
                        conditional = false;
                    }
                    else {
                        throw new Exception();
                    }
                    pos += 2;
                }
                return expr;
            }
            return null;
        }

        DMASTExpression ExprInnerToNode(Node node) {
            if (node.Tags.ContainsKey("expr")) {
                var v = node.Tags["expr"] as string;
                if (v == ".") {
                    return new DMASTCallableSelf();
                }
                else if (v == "..") {
                    return new DMASTCallableSuper();
                }
                else { throw node.Error("unknown expr tag"); }
            }
            if (node.Tags.ContainsKey("deref") || node.Tags.ContainsKey("ident")) {
                return ConvertDeref(node);
            }
            if (node.Tags.ContainsKey("num")) {
                return ConvertNumericLiteral((string)node.Tags["num"]);
                    
            }
            if (node.Tags.ContainsKey("resource")) {
                return new DMASTConstantResource(node.Tags["resource"] as string);
            }
            if (node.Tags.ContainsKey("path")) {
                // TODO double check paths with .proc in them
                var path = string.Join("", node.Tags["path"] as string[]);
                return new DMASTConstantPath(new DMASTPath( new DreamPath(path)));

            }
            if (node.Tags.ContainsKey("string")) {
                return new DMASTConstantString(EscapeString(node.Tags["string"] as string));

            }
            if (node.Leaves.Count == 0) {
                return null;
            }
            Console.WriteLine( node.PrintLeaves(1) );
            throw node.Error("unknown inner");
        }

        DMASTExpression IndexInnerToNode(Node node) {
            if (node.Tags.ContainsKey("expr")) {
                var v = node.Tags["expr"] as string;
                if (v == ".") {
                    return new DMASTCallableSelf();
                }
                else if (v == "..") {
                    return new DMASTCallableSuper();
                }
                else { throw node.Error("unknown expr tag"); }
            }
            if (node.Tags.ContainsKey("deref") || node.Tags.ContainsKey("ident")) {
                return ConvertDeref(node);
            }
            return null;
        }

        DMASTCallable CallInnerToNode(Node node) {
            if (node.Tags.ContainsKey("expr")) {
                var v = node.Tags["expr"] as string;
                if (v == ".") {
                    return new DMASTCallableSelf();
                }
                else if (v == "..") {
                    return new DMASTCallableSuper();
                }
                else { throw node.Error("unknown expr tag"); }
            }
            if (node.Tags.ContainsKey("deref") || node.Tags.ContainsKey("ident")) {
                var expr = ConvertDeref(node);

                if (expr is DMASTIdentifier idexpr) {
                    return new DMASTCallableProcIdentifier(idexpr.Identifier);
                }
                else if (expr is DMASTDereference deref_expr) {
                    return new DMASTDereferenceProc(deref_expr.Expression, deref_expr.Property, deref_expr.Type, deref_expr.Conditional);
                }
                else {
                    throw node.Error("no callable created");;
                }
            }
            return null;
        }
    }
}
