using OpenDreamShared.Dream;
using DMCompiler.Compiler.DM;

namespace ClopenDream {
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
                    return new DMASTConstantNull(node.Location);
                }
                if (path[0] == "<expression>") {
                    return derefExprStack.Peek();
                }
                return new DMASTIdentifier(node.Location, path[0]);
            }
            if (path.Length > 1) {
                var ident = path[0];
                var conditional = false;
                DMASTExpression expr = null;
                int pos = 0;
                if (ident == "<expression>") {
                    expr = derefExprStack.Peek();
                    conditional = derefExprCond.Peek();
                    pos = 1;
                }
                else if (ident == "global") {
                    expr = new DMASTGlobalIdentifier(node.Location, path[2]);
                    pos = 3;
                }
                else {
                    expr = new DMASTIdentifier(node.Location, ident);
                    pos = 1;
                }

                while (pos < path.Length) {
                    if (path[pos] == ".") {
                        expr = new DMASTDereference(node.Location, expr, path[pos + 1], DMASTDereference.DereferenceType.Direct, conditional);
                        conditional = false;
                    }
                    else if (path[pos] == ":") {
                        expr = new DMASTDereference(node.Location, expr, path[pos + 1], DMASTDereference.DereferenceType.Search, conditional);
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
                    return new DMASTCallableSelf(node.Location);
                }
                else if (v == "..") {
                    return new DMASTCallableSuper(node.Location);
                }
                else { throw node.Error("unknown expr tag"); }
            }
            if (node.Tags.ContainsKey("deref") || node.Tags.ContainsKey("ident")) {
                return ConvertDeref(node);
            }
            if (node.Tags.ContainsKey("num")) {
                return ConvertNumericLiteral(node, (string)node.Tags["num"]);
                    
            }
            if (node.Tags.ContainsKey("resource")) {
                return new DMASTConstantResource(node.Location, node.Tags["resource"] as string);
            }
            if (node.Tags.ContainsKey("path")) {
                return ConvertPath(node);
            }
            if (node.Tags.ContainsKey("string")) {
                return new DMASTConstantString(node.Location, EscapeString(node.Tags["string"] as string));

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
                    return new DMASTCallableSelf(node.Location);
                }
                else if (v == "..") {
                    return new DMASTCallableSuper(node.Location);
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
                    return new DMASTCallableSelf(node.Location);
                }
                else if (v == "..") {
                    return new DMASTCallableSuper(node.Location);
                }
                else { throw node.Error("unknown expr tag"); }
            }
            if (node.Tags.ContainsKey("deref") || node.Tags.ContainsKey("ident")) {
                var expr = ConvertDeref(node);

                if (expr is DMASTIdentifier idexpr) {
                    return new DMASTCallableProcIdentifier(node.Location, idexpr.Identifier);
                }
                else if (expr is DMASTGlobalIdentifier gidexpr) {
                    return new DMASTCallableProcIdentifier(node.Location, gidexpr.Identifier);
                } else if (expr is DMASTDereference deref_expr) {
                    return new DMASTDereferenceProc(node.Location, deref_expr.Expression, deref_expr.Property, deref_expr.Type, deref_expr.Conditional);
                }
                else {
                    throw node.Error("no callable created");;
                }
            }
            return null;
        }
    }
}
