using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using OpenDreamShared.Dream;
using OpenDreamShared.Compiler.DM;

namespace ClopenDream {
    public partial class ConvertAST {

        DMASTExpression GetOperator(Node n) {
            switch (n.Tags["operator"]) {
                case "?": {
                        if (n.Leaves.Count == 3) { return new DMASTTernary(GetExpression(n.Leaves[0]), GetExpression(n.Leaves[1]), GetExpression(n.Leaves[2])); }
                        throw n.Error("GetOperator.?");
                    }
                case "!": {
                        if (n.Leaves.Count == 1) { return new DMASTNot(GetExpression(n.Leaves[0])); }
                        throw n.Error("GetOperator.~");
                    }
                case "~": { 
                        if (n.Leaves.Count == 1) { return new DMASTBinaryNot(GetExpression(n.Leaves[0])); }
                        throw n.Error("GetOperator.~");
                    }
                case "&": return GetLeftAssoc(n.Leaves, typeof(DMASTBinaryAnd));
                case "|": return GetLeftAssoc(n.Leaves, typeof(DMASTBinaryOr));
                case "^": return GetLeftAssoc(n.Leaves, typeof(DMASTBinaryXor));
                case "&&": return GetLeftAssoc(n.Leaves, typeof(DMASTAnd));
                case "||": return GetLeftAssoc(n.Leaves, typeof(DMASTOr));
                case "+": return GetLeftAssoc(n.Leaves, typeof(DMASTAdd));
                case "-": {
                        if (n.Leaves.Count == 1) { return new DMASTNegate(GetExpression(n.Leaves[0])); }
                        else if (n.Leaves.Count > 1) { return GetLeftAssoc(n.Leaves, typeof(DMASTSubtract)); }
                        throw n.Error("GetOperator.-");
                    }
                case "*": return GetLeftAssoc(n.Leaves, typeof(DMASTMultiply));
                case "/": return GetLeftAssoc(n.Leaves, typeof(DMASTDivide));
                case "%": return GetLeftAssoc(n.Leaves, typeof(DMASTModulus));
                case "**": return GetLeftAssoc(n.Leaves, typeof(DMASTPower));
                case "<<": return GetLeftAssoc(n.Leaves, typeof(DMASTLeftShift));
                case ">>": return GetLeftAssoc(n.Leaves, typeof(DMASTRightShift));
                case "==": return GetLeftAssoc(n.Leaves, typeof(DMASTEqual));
                case "!=": return GetLeftAssoc(n.Leaves, typeof(DMASTNotEqual));
                case "~=": return GetLeftAssoc(n.Leaves, typeof(DMASTEquivalent));
                case "~!": return GetLeftAssoc(n.Leaves, typeof(DMASTNotEquivalent));
                case ">": return GetLeftAssoc(n.Leaves, typeof(DMASTGreaterThan));
                case "<": return GetLeftAssoc(n.Leaves, typeof(DMASTLessThan));
                case ">=": return GetLeftAssoc(n.Leaves, typeof(DMASTGreaterThanOrEqual));
                case "<=": return GetLeftAssoc(n.Leaves, typeof(DMASTLessThanOrEqual));
                case "=": return GetRightAssoc(n.Leaves, typeof(DMASTAssign));
                case "+=": return GetRightAssoc(n.Leaves, typeof(DMASTAppend));
                case "-=": return GetRightAssoc(n.Leaves, typeof(DMASTRemove));
                case "|=": return GetRightAssoc(n.Leaves, typeof(DMASTCombine));
                case "&=": return GetRightAssoc(n.Leaves, typeof(DMASTMask));
                case "||=": return GetRightAssoc(n.Leaves, typeof(DMASTLogicalOrAssign));
                case "&&=": return GetRightAssoc(n.Leaves, typeof(DMASTLogicalAndAssign));
                case "*=": return GetRightAssoc(n.Leaves, typeof(DMASTMultiplyAssign));
                case "/=": return GetRightAssoc(n.Leaves, typeof(DMASTDivideAssign));
                case "<<=": return GetRightAssoc(n.Leaves, typeof(DMASTLeftShiftAssign));
                case ">>=": return GetRightAssoc(n.Leaves, typeof(DMASTRightShiftAssign));
                case "^=": return GetRightAssoc(n.Leaves, typeof(DMASTXorAssign));
                case "%=": return GetRightAssoc(n.Leaves, typeof(DMASTModulusAssign));
                case "in": return new DMASTExpressionIn(GetExpression(n.Leaves[0]), GetExpression(n.Leaves[1]));
                case "to": return null;
                case "step": {
                        var paras = GetCallParameters(n.Leaves);
                        return new DMASTProcCall(new DMASTCallableProcIdentifier("step"), paras.ToArray());
                    }
                case "++": return new DMASTPreIncrement(GetExpression(n.Leaves[0]));
                case "--": return new DMASTPreDecrement(GetExpression(n.Leaves[0]));
                case ".": return GetDerefOperator(n);
                case "?:": return GetDerefOperator(n);
                case "?.": return GetDerefOperator(n);
                case "?.(lhs)": return GetDerefOperator(n);
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
        DMASTExpression GetRightAssoc(List<Node> ns, Type t) {
            if (ns.Count < 2) { throw new Exception(); }
            List<object> os = new() { GetExpression(ns[ns.Count-2]), GetExpression(ns[ns.Count-1]) };
            DMASTExpression expr = (DMASTExpression)Activator.CreateInstance(t, args: os.ToArray());
            for (int i = ns.Count-3; i > 0; i--) {
                os = new() { GetExpression(ns[i]), expr };
                expr = (DMASTExpression)Activator.CreateInstance(t, os.ToArray());
            }
            return expr;
        }
        DMASTExpression GetLeftAssoc(List<Node> ns, Type t) {
            if (ns.Count < 2) { throw new Exception(); }
            List<object> os = new() { GetExpression(ns[0]), GetExpression(ns[1]) };
            DMASTExpression expr = (DMASTExpression)Activator.CreateInstance(t, args: os.ToArray());
            for (int i = 2; i < ns.Count; i++) {
                os = new() { expr, GetExpression(ns[i]) };
                expr = (DMASTExpression)Activator.CreateInstance(t, os.ToArray());
            }
            return expr;

        }
    }
}