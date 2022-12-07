using DMCompiler.Compiler.DM;
using OpenDreamShared.Compiler;

namespace ClopenDream {
    public partial class ConvertAST {

        DMASTExpression GetOperator(Node n) {
            switch (n.Tags["operator"]) {
                case "?": {
                        if (n.Leaves.Count == 3) { return new DMASTTernary(n.Location, GetExpression(n.Leaves[0]), GetExpression(n.Leaves[1]), GetExpression(n.Leaves[2])); }
                        throw n.Error("GetOperator.?");
                    }
                case "!": {
                        if (n.Leaves.Count == 1) { return new DMASTNot(n.Location, GetExpression(n.Leaves[0])); }
                        throw n.Error("GetOperator.~");
                    }
                case "~": { 
                        if (n.Leaves.Count == 1) { return new DMASTBinaryNot(n.Location, GetExpression(n.Leaves[0])); }
                        throw n.Error("GetOperator.~");
                    }
                case "&": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTBinaryAnd));
                case "|": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTBinaryOr));
                case "^": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTBinaryXor));
                case "&&": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTAnd));
                case "||": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTOr));
                case "+": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTAdd));
                case "-": {
                        if (n.Leaves.Count == 1) { return new DMASTNegate(n.Location, GetExpression(n.Leaves[0])); }
                        else if (n.Leaves.Count > 1) { return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTSubtract)); }
                        throw n.Error("GetOperator.-");
                    }
                case "*": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTMultiply));
                case "/": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTDivide));
                case "%": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTModulus));
                case "**": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTPower));
                case "<<": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTLeftShift));
                case ">>": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTRightShift));
                case "==": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTEqual));
                case "!=": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTNotEqual));
                case "~=": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTEquivalent));
                case "~!": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTNotEquivalent));
                case ">": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTGreaterThan));
                case "<": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTLessThan));
                case ">=": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTGreaterThanOrEqual));
                case "<=": return GetLeftAssoc(n.Location, n.Leaves, typeof(DMASTLessThanOrEqual));
                case "=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTAssign));
                case "+=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTAppend));
                case "-=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTRemove));
                case "|=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTCombine));
                case "&=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTMask));
                case "||=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTLogicalOrAssign));
                case "&&=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTLogicalAndAssign));
                case "*=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTMultiplyAssign));
                case "/=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTDivideAssign));
                case "<<=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTLeftShiftAssign));
                case ">>=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTRightShiftAssign));
                case "^=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTXorAssign));
                case "%=": return GetRightAssoc(n.Location, n.Leaves, typeof(DMASTModulusAssign));
                case "in": return new DMASTExpressionIn(n.Location, GetExpression(n.Leaves[0]), GetExpression(n.Leaves[1]));
                case "to": return new DMASTConstantNull(n.Location);
                case "step": {
                        var paras = GetCallParameters(n.Leaves);
                        return new DMASTProcCall(n.Location, new DMASTCallableProcIdentifier(n.Location, "step"), paras.ToArray());
                    }
                case "++": return new DMASTPreIncrement(n.Location, GetExpression(n.Leaves[0]));
                case "--": return new DMASTPreDecrement(n.Location, GetExpression(n.Leaves[0]));
                case ".": return GetDerefOperator(n);
                case "?:": return GetDerefOperator(n);
                case "?.": return GetDerefOperator(n);
                case "?.(lhs)": return GetDerefOperator(n);
                case "?:(lhs)": return GetDerefOperator(n);
                default: throw n.Error("GetOperator " + n.Tags["operator"]);
            }
        }

        DMASTExpression GetDerefOperator(Node n) {
            if (n.Leaves.Count != 2) {
                throw n.Error("Count != 2");
            }
            string op = n.Tags["operator"] as string;
            var sub_expr = GetExpression(n.Leaves[0]);
            derefExprStack.Push(sub_expr);
            if (op.Contains("?")) {
                derefExprCond.Push(true);
            }
            else {
                derefExprCond.Push(false);
            }
            var supra_expr = GetExpression(n.Leaves[1]);
            derefExprStack.Pop();
            derefExprCond.Pop();
            return supra_expr;
        }
        DMASTExpression GetRightAssoc(Location loc, List<Node> ns, Type t) {
            if (ns.Count < 2) { throw new Exception(); }
            List<object> os = new() { loc, GetExpression(ns[ns.Count-2]), GetExpression(ns[ns.Count-1]) };
            DMASTExpression expr = (DMASTExpression)Activator.CreateInstance(t, args: os.ToArray());
            for (int i = ns.Count-3; i > 0; i--) {
                os = new() { ns[i].Location, GetExpression(ns[i]), expr };
                expr = (DMASTExpression)Activator.CreateInstance(t, os.ToArray());
            }
            return expr;
        }
        DMASTExpression GetLeftAssoc(Location loc, List<Node> ns, Type t) {
            if (ns.Count < 2) { throw new Exception(); }
            List<object> os = new() { loc, GetExpression(ns[0]), GetExpression(ns[1]) };
            DMASTExpression expr = (DMASTExpression)Activator.CreateInstance(t, args: os.ToArray());
            for (int i = 2; i < ns.Count; i++) {
                os = new() { ns[i].Location, expr, GetExpression(ns[i]) };
                expr = (DMASTExpression)Activator.CreateInstance(t, os.ToArray());
            }
            return expr;

        }
    }
}