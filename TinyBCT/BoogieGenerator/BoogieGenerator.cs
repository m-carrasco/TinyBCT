using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyBCT.Translators;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Test")]
namespace TinyBCT
{
    public class Expression
    {
        protected Expression(Helpers.BoogieType type, string expr)
        {
            Type = type;
            Expr = expr;
        }
        public Helpers.BoogieType Type { get; }
        public string Expr { get; }
        // We should not use this externally, we want to add the assumption that, for
        // any primitive type expression p where we use Int2Union(p) (or Bool2Union),
        // then Union2Int(Int2Union(p)) == p holds.
        private static Expression PrimitiveType2Union(Expression expr)
        {
            return new Expression(Helpers.BoogieType.Union, $"{expr.Type.FirstUppercase()}2Union({expr.Expr})");
        }
        public static Expression PrimitiveType2Union(Expression expr, StatementList stmts)
        {
            Contract.Assert(!Helpers.IsBoogieRefType(expr.Type));
            stmts.Add(Expression.AssumeInverseRelationUnionAndPrimitiveType(expr));
            return PrimitiveType2Union(expr);
        }
        public static Expression PrimitiveType2Union(Expression expr, InstructionTranslator instTranslator)
        {
            Contract.Assert(!Helpers.IsBoogieRefType(expr.Type));
            instTranslator.AddBoogie(Expression.AssumeInverseRelationUnionAndPrimitiveType(expr));
            return PrimitiveType2Union(expr);
        }
        public static Expression Union2PrimitiveType(Helpers.BoogieType boogieType, Expression expr)
        {
            Contract.Assume(!Helpers.IsBoogieRefType(boogieType));
            return new Expression(boogieType, $"Union2{boogieType.FirstUppercase()}({expr.Expr})");
        }
        public static StatementList AssumeInverseRelationUnionAndPrimitiveType(Expression expr)
        {
            var p2u = Expression.PrimitiveType2Union(expr);
            var p2u2p = Expression.Union2PrimitiveType(expr.Type, p2u);
            var eq = Expression.ExprEquals(p2u2p, expr);
            return BoogieStatement.Assume(eq);
        }
        public static Expression As(Expression expr, ITypeReference arg2)
        {
            var type = Helpers.GetNormalizedTypeFunction(arg2, InstructionTranslator.MentionedClasses);
            // TODO(rcastano): Fix for generics
            return new Expression(Helpers.BoogieType.Ref, $"$As({expr.Expr}, {type})");
        }
        public static Expression DynamicType(Expression expr)
        {
            Contract.Assume(Helpers.IsBoogieRefType(expr.Type));
            return new Expression(Helpers.BoogieType.Ref, $"$DynamicType({expr.Expr})");
        }
        public static Expression Subtype(Expression expr, ITypeReference type)
        {
            Contract.Assume(Helpers.IsBoogieRefType(expr.Type));
            return new Expression(Helpers.BoogieType.Bool, $"$Subtype({expr.Expr}, {Helpers.GetNormalizedTypeFunction(type, InstructionTranslator.MentionedClasses)})");
        }
        public static Expression Negation(Expression b)
        {
            Contract.Assume(b.Type.Equals(Helpers.BoogieType.Bool));
            return new Expression(Helpers.BoogieType.Bool, $"!({b.Expr})");
        }
        public static Expression NullOrZero(ITypeReference type)
        {
            if (TypeHelper.IsPrimitiveInteger(type))
            {
                return new Expression(Helpers.BoogieType.Int, "0");
            }
            else if (type.TypeCode.Equals(TypeCode.Single) || type.TypeCode.Equals(TypeCode.Double))
            {
                return new Expression(Helpers.BoogieType.Real, "0.0");
            }
            else
            {
                return new Expression(Helpers.GetBoogieType(type), "null");
            }
        }
        public static Expression BranchOperationExpression(Expression op1, Expression op2, BranchOperation branchOperation)
        {
            var operation = string.Empty;

            switch (branchOperation)
            {
                case BranchOperation.Eq: operation = "=="; break;
                case BranchOperation.Neq: operation = "!="; break;
                case BranchOperation.Gt: operation = ">"; break;
                case BranchOperation.Ge: operation = ">="; break;
                case BranchOperation.Lt: operation = "<"; break;
                case BranchOperation.Le: operation = "<="; break;
            }
            
            return new Expression(Helpers.BoogieType.Bool, $"({op1.Expr} {operation} {op2.Expr})");
        }
        public static Expression ArrayLength(Expression arr)
        {
            // Arrays are of type Ref
            Contract.Assume(Helpers.IsBoogieRefType(arr.Type));
            return new Expression(Helpers.BoogieType.Int, $"$ArrayLength({arr.Expr})");
        }
        public static Expression ArrayContents(Expression arr, Expression index)
        {
            // Arrays are of type Ref
            Contract.Assume(Helpers.IsBoogieRefType(arr.Type));
            Contract.Assume(index.Type.Equals(Helpers.BoogieType.Int));
            // TODO(rcastano): add assumption wrt $ArrayLength
            return new Expression(Helpers.BoogieType.Union, $"$ArrayContents[{arr.Expr}][{index.Expr}]");
        }
        public static Expression ForAll(QuantifiedVariable var, Expression expr)
        {
            Contract.Assume(expr.Type.Equals(Helpers.BoogieType.Bool));
            return new Expression(Helpers.BoogieType.Bool, $"(forall {var.Expr} : {var.Type} :: {expr.Expr})");
        }
        public static Expression Product(IEnumerable<Expression> int_expressions)
        {
            Contract.Assume(int_expressions.All(e => e.Type.Equals(Helpers.BoogieType.Int)));
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            string joinStr = "";
            foreach (var e in int_expressions) {
                sb.Append(joinStr);
                sb.Append(e.Expr);
                joinStr = " * ";
            }
            sb.Append(")");
            return new Expression(Helpers.BoogieType.Int, sb.ToString());
        }
        public static Expression Or(Expression expr1, Expression expr2)
        {
            Contract.Assume(expr1.Type.Equals(Helpers.BoogieType.Bool));
            Contract.Assume(expr2.Type.Equals(Helpers.BoogieType.Bool));
            return new Expression(Helpers.BoogieType.Bool, $"({expr1.Expr} || {expr2.Expr})");
        }
        public static Expression Implies(Expression antecedent, Expression consequent)
        {
            Contract.Assume(antecedent.Type.Equals(Helpers.BoogieType.Bool));
            Contract.Assume(consequent.Type.Equals(Helpers.BoogieType.Bool));
            return new Expression(Helpers.BoogieType.Bool, $"({antecedent.Expr} ==> {consequent.Expr})");
        }
        public static Expression GetNormalizedTypeFunction(
            ITypeReference originalType, ISet<ITypeReference> mentionedClasses,
            IEnumerable<ITypeReference> typeArguments = null,
            Func<ITypeReference, Boolean> forceRecursion = null)
        {
            return new Expression(Helpers.BoogieType.Ref, Helpers.GetNormalizedTypeFunction(originalType, mentionedClasses, typeArguments, forceRecursion));
        }

        public static bool IsSupportedBinaryOperation(BinaryOperation binaryOperation, Helpers.BoogieType type1, Helpers.BoogieType type2)
        {
            switch (binaryOperation)
            {
                case BinaryOperation.And:
                case BinaryOperation.Or:
                    return type1.Equals(Helpers.BoogieType.Bool) && type2.Equals(Helpers.BoogieType.Bool);
                case BinaryOperation.Add:
                case BinaryOperation.Sub:
                case BinaryOperation.Mul:
                case BinaryOperation.Div:
                case BinaryOperation.Eq:
                case BinaryOperation.Neq:
                case BinaryOperation.Gt:
                case BinaryOperation.Ge:
                case BinaryOperation.Lt:
                case BinaryOperation.Le:
                case BinaryOperation.Rem:
                    return true;
                default:
                    return false;
            }
        }
        public static Expression BinaryOperationExpression(Expression op1, Expression op2, BinaryOperation binaryOperation)
        {
            Contract.Assume(IsSupportedBinaryOperation(binaryOperation, op1.Type, op2.Type));
            Contract.Assume(op1.Type.Equals(op2.Type));
            string operation = String.Empty;
            Helpers.BoogieType boogieType = null;
            switch (binaryOperation)
            {
                case BinaryOperation.Add: operation = "+"; break;
                case BinaryOperation.Sub: operation = "-"; break;
                case BinaryOperation.Mul: operation = "*"; break;
                case BinaryOperation.Div:
                    {

                        if (op1.Type.Equals(op2.Type) && op1.Type.Equals(Helpers.BoogieType.Int))
                        {
                            operation = "div";
                        }
                        else
                        {
                            operation = "/";
                        }

                        break;
                    }
                // not implemented yet
                /*case BinaryOperation.Rem: operation = "%"; break;
                case BinaryOperation.And: operation = "&"; break;
                case BinaryOperation.Or: operation = "|"; break;
                case BinaryOperation.Xor: operation = "^"; break;
                case BinaryOperation.Shl: operation = "<<"; break;
                case BinaryOperation.Shr: operation = ">>"; break;*/
                case BinaryOperation.Eq: operation = "=="; break;
                case BinaryOperation.Neq: operation = "!="; break;
                case BinaryOperation.Gt: operation = ">"; break;
                case BinaryOperation.Ge: operation = ">="; break;
                case BinaryOperation.Lt: operation = "<"; break;
                case BinaryOperation.Le: operation = "<="; break;
                case BinaryOperation.Rem: operation = "mod"; break;
                case BinaryOperation.And:
                    {
                        Contract.Assert(op1.Type.Equals(Helpers.BoogieType.Bool));
                        Contract.Assert(op2.Type.Equals(Helpers.BoogieType.Bool));
                        operation = "&&";
                        break;
                    }
                case BinaryOperation.Or:
                    {
                        Contract.Assert(op1.Type.Equals(Helpers.BoogieType.Bool));
                        Contract.Assert(op2.Type.Equals(Helpers.BoogieType.Bool));
                        operation = "||";
                        break;
                    }
                default:
                    Contract.Assert(false);
                    break;
            }
            switch (binaryOperation)
            {
                case BinaryOperation.Add:
                case BinaryOperation.Sub:
                case BinaryOperation.Mul:
                case BinaryOperation.Div:
                    {

                        if (op1.Type.Equals(op2.Type) && op1.Type.Equals(Helpers.BoogieType.Int))
                        {
                            boogieType = Helpers.BoogieType.Int;
                        }
                        else
                        {
                            boogieType = Helpers.BoogieType.Real;
                        }
                        break;
                    }
                // not implemented yet
                /*case BinaryOperation.Rem: operation = "%"; break;
                case BinaryOperation.And: operation = "&"; break;
                case BinaryOperation.Or: operation = "|"; break;
                case BinaryOperation.Xor: operation = "^"; break;
                case BinaryOperation.Shl: operation = "<<"; break;
                case BinaryOperation.Shr: operation = ">>"; break;*/
                case BinaryOperation.Eq:
                case BinaryOperation.Neq:
                case BinaryOperation.Gt:
                case BinaryOperation.Ge:
                case BinaryOperation.Lt:
                case BinaryOperation.Le:
                    boogieType = Helpers.BoogieType.Bool;
                    break;
                case BinaryOperation.Rem:
                    boogieType = Helpers.BoogieType.Int;
                    break;
                case BinaryOperation.And:
                case BinaryOperation.Or:
                        boogieType = Helpers.BoogieType.Bool;
                        break;
                default:
                    Contract.Assert(false);
                    break;
            }
            Contract.Assume(boogieType != null);
            return BinaryOperationExpression(boogieType, op1, op2, operation);
        }
        public static Expression BinaryOperationExpression(Helpers.BoogieType type, Expression op1, Expression op2, string operation)
        {
            return new Expression(type, $"({op1.Expr} {operation} {op2.Expr})");
        }
        public static bool IsSupportedUnaryOperation(UnaryOperation op)
        {
            return UnaryOperation.Neg.Equals(op);
        }
        public static Expression UnaryOperationExpression(IVariable var, UnaryOperation unaryOperation)
        {
            Contract.Assume(IsSupportedUnaryOperation(unaryOperation));
            // currently just neg is supported, equivalent to * -1
            return new Expression(Helpers.GetBoogieType(var), $"-{var.Name}");
        }
        public static Expression ExprEquals(Expression op1, Expression op2)
        {
            Contract.Assume(op1.Type.Equals(op2.Type));
            return new Expression(Helpers.BoogieType.Bool, $"({op1.Expr} == {op2.Expr})");
        }
        public static Expression ExprEquals(Expression op1, int index)
        {
            Contract.Assume(op1.Type.Equals(Helpers.BoogieType.Int));
            return new Expression(Helpers.BoogieType.Bool, $"({op1.Expr} == {index})");
        }
        public static readonly Expression ExceptionVarNotNull = new Expression(Helpers.BoogieType.Bool, $"({BoogieVariable.ExceptionVar().Expr} != null)");
        public static Expression NotEquals(Expression op1, Expression op2)
        {
            Contract.Assume(op1.Type.Equals(op2.Type) || (Helpers.IsBoogieRefType(op1.Type) && Helpers.IsBoogieRefType(op2.Type)));
            // Adding this check because we haven't cleanly separated the new memory model from the old one yet.
            // The problem is that the Boogie variable $Exception is set in the prelude as null, but
            // could be checked programmatically within BoogieGenerator against null_object (which would be wrong).
            // As such, I've added a specific field that should be used: ExceptionVarNotNull.
            var exprs = new List<Expression> { op1, op2 };
            Contract.Assume(!(
                exprs.Contains(BoogieVariable.ExceptionVar()) &&
                exprs.Contains(BoogieGenerator.Instance().NullObject())));
            return new Expression(Helpers.BoogieType.Bool, $"({op1.Expr} != {op2.Expr})");
        }
        public static Expression LessThan(Expression op1, int index)
        {
            Contract.Assume(op1.Type.Equals(Helpers.BoogieType.Int));
            return new Expression(Helpers.BoogieType.Bool, $"({op1.Expr} < {index})");
        }
        public static Expression LoadInstanceFieldAddr(IFieldReference field, Expression instance)
        {
            String map = FieldTranslator.GetFieldName(field);
            return new Expression(Helpers.BoogieType.Addr, $"LoadInstanceFieldAddr({map}, {instance.Expr})");
        }
        public static Expression IsTask(BoogieVariable boogieVar)
        {
            return new Expression(Helpers.BoogieType.Bool, $"({DynamicType(boogieVar).Expr} == T$System.Threading.Tasks.Task`1(T$T()))");
        }
        public static Expression IsAsyncTaskMethodBuilder(BoogieVariable boogieVar)
        {
            return new Expression(Helpers.BoogieType.Bool, $"({DynamicType(boogieVar).Expr} == T$System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1(T$T()))");
        }

        public override string ToString()
        {
            throw new Exception("This method should not be called, use property Expr.");
        }
        
        // TODO(rcastano): I'm not sure why this is necessary. It is related to the dispatching of delegates.
        internal static readonly Expression Type0 = new Expression(Helpers.BoogieType.Ref, "Type0()");
    }

    public class DelegateExpression : Expression
    {
        protected DelegateExpression(Helpers.BoogieType type, string expr) : base(type, expr) { }

        // This method has a clone? GetMethodName
        public static DelegateExpression GetMethodIdentifier(IMethodReference methodRef, IDictionary<IMethodReference, DelegateExpression> methodIdentifiers)
        {
            var methodId = Helpers.CreateUniqueMethodName(methodRef);

            if (methodIdentifiers.ContainsKey(methodRef))
                return methodIdentifiers[methodRef];

            //var methodName = Helpers.GetMethodName(methodRef);
            //var methodArity = Helpers.GetArityWithNonBoogieTypes(methodRef);

            //// example:  cMain2.objectParameter$System.Object;
            //var methodId = methodName + methodArity;

            var methodIdExpr = new DelegateExpression(Helpers.BoogieType.Int, methodId);
            methodIdentifiers.Add(methodRef, methodIdExpr);

            return methodIdExpr;
        }
        public static DelegateExpression RefToDelegateMethod(DelegateExpression methodId)
        {
            Contract.Assume(methodId.Type.Equals(Helpers.BoogieType.Int));
            return new DelegateExpression(Helpers.BoogieType.Bool, $"$RefToDelegateMethod({methodId.Expr}, $this)");
        }

        public static DelegateExpression RefToDelegateReceiver(DelegateExpression methodId)
        {
            Contract.Assume(methodId.Type.Equals(Helpers.BoogieType.Int));
            return new DelegateExpression(Helpers.BoogieType.Ref, $"$RefToDelegateReceiver({methodId.Expr}, $this)");
        }
        
    }

    public class BoogieLiteral : Expression {
        private BoogieLiteral(Helpers.BoogieType type, string expr) : base(type, expr)
        {
            Contract.Requires(!type.Equals(Helpers.BoogieType.Void));
        }
        public static bool IsFloat(Constant constant)
        {
            return constant.Value is Single || constant.Value is Double || constant.Value is Decimal;
        }
        public static bool IsNumeric(Constant constant)
        {
            return IsFloat(constant) || TypeHelper.IsPrimitiveInteger(constant.Type);
        }
        public static BoogieLiteral Numeric(Constant constant)
        {
            Contract.Requires(IsNumeric(constant));
            var consStr = IsFloat(constant) ? FormatFloatValue(constant) : constant.Value.ToString();
            return new BoogieLiteral(Helpers.GetBoogieType(constant.Type), consStr);
        }
        public static BoogieLiteral FromUInt(uint constant)
        {
            return new BoogieLiteral(Helpers.BoogieType.Int, constant.ToString());
        }
        internal static BoogieLiteral FromString(Constant constant)
        {
            return new BoogieLiteral(Helpers.BoogieType.Ref, Strings.fixStringLiteral(constant));
        }
        private static bool IsNullConst(Constant constant)
        {
            return
                constant != null &&
                Helpers.IsBoogieRefType(constant) &&
                (constant.ToString().Equals("null") ||
                 // TODO: This should be removed once the underlying framework
                 // correctly translates null constants.
                 // This hack was added in commit 618c5aef82328a487552a191821afea77bf2cc1e,
                 // as a fix to "inference of null as 0 (remove when framework solve this)" (quoting
                 // from commit message).
                 constant.ToString().Equals("0"));
        }
        private static BoogieLiteral FromBool(Constant constant)
        {
            Contract.Assume(
                constant != null &&
                Helpers.GetBoogieType(constant.Type).Equals(Helpers.BoogieType.Bool));
            return new BoogieLiteral(Helpers.BoogieType.Bool, constant.ToString());
        }
        private static BoogieLiteral FromNull(Constant constant)
        {
            Contract.Assume(IsNullConst(constant));
            // The boogie type varies depending on the address encoding.
            var boogieType = Helpers.GetBoogieType(constant.Type);
            return new BoogieLiteral(boogieType, constant.ToString());
        }
        public static BoogieLiteral FromDotNetConstant(Constant cons)
        {
            if (cons.Value is Single || cons.Value is Double || cons.Value is Decimal || TypeHelper.IsPrimitiveInteger(cons.Type))
            {
                return BoogieLiteral.Numeric(cons);
            }
            else if (Helpers.GetBoogieType(cons.Type).Equals(Helpers.BoogieType.Bool))
            {
                return BoogieLiteral.FromBool(cons);
            }
            else if (BoogieLiteral.IsNullConst(cons))
            {
                return BoogieLiteral.FromNull(cons);
            }
            throw new NotImplementedException();
        }
        public static readonly BoogieLiteral False = new BoogieLiteral(Helpers.BoogieType.Bool, "false");


        public static class Strings
        {
            internal static ISet<string> stringLiterals = new HashSet<string>();
            private const string constNameForNullString = "$string_literal_NullValue";
            private static string ConstNameForStringLiteral(string literal)
            {
                // String literal will start and end with '"'.
                System.Diagnostics.Contracts.Contract.Assume(literal[0] == '"' && literal[literal.Length - 1] == '"');
                stringLiterals.Add(literal);
                var fixedString = Helpers.Strings.ReplaceSpaces(Helpers.Strings.NormalizeStringForCorral(literal.Substring(1, literal.Length - 2)));
                if (Helpers.Strings.ContainsIllegalCharacters(fixedString))
                {
                    fixedString = Helpers.Strings.ReplaceIllegalChars(fixedString);
                }
                return $"$string_literal_{fixedString}";
            }
            internal static string fixStringLiteral(Constant cons)
            {
                string vStr = null;
                if (cons.Value != null)
                {
                    vStr = ConstNameForStringLiteral(cons.ToString());
                    stringLiterals.Add(cons.ToString());
                }
                else
                {
                    vStr = constNameForNullString;
                }
                return vStr;
            }

            public static void WriteStringConsts(System.IO.StreamWriter sw)
            {
                var addedConsts = new HashSet<string>();
                sw.WriteLine($"\tconst unique {constNameForNullString} : Ref;");
                foreach (var lit in stringLiterals)
                {
                    var boogieConst = ConstNameForStringLiteral(lit);
                    sw.WriteLine($"\tconst unique {boogieConst} : Ref;");
                }
            }
        }

        public static readonly BoogieLiteral NullObject = new BoogieLiteral(Helpers.BoogieType.Object, "null_object");

        public static readonly BoogieLiteral NullRef = new BoogieLiteral(Helpers.BoogieType.Ref, "null");

        private static string FormatFloatValue(Constant cons)
        {
            Contract.Requires(cons.Value is Single || cons.Value is Double || cons.Value is Decimal);
            // default string representation of floating point types is not suitable for boogie
            // boogie wants dot instead of ,
            // "F" forces to add decimal part
            // The format specifiers F9 and F17 make the translation lossless for
            // single and double, respectively.
            string str = "";
            if (cons.Value is Single)
            {
                Single v = (Single)cons.Value;
                str = v.ToString("F9").Replace(",", ".");
            }
            else if (cons.Value is Double)
            {
                Double v = (Double)cons.Value;
                str = v.ToString("F17").Replace(",", ".");
            }
            else if (cons.Value is Decimal)
            {
                Decimal v = (Decimal)cons.Value;
                str = v.ToString("F").Replace(",", ".");
            }
            else
            {
                Contract.Assert(false);
            }

            return str;
        }

        public override string ToString()
        {
            throw new Exception("Should not be called.");
        }
    }
    public class BoogieVariable : Expression
    {
        protected BoogieVariable(Helpers.BoogieType type, string name) : base(type, name)
        {
            Contract.Requires(IsValidVariableName(name));
        }

        // Call InstructionTranslator.GetFreshVariable instead of this.
        public static BoogieVariable GetTempVar(Helpers.BoogieType type, Dictionary<string, BoogieVariable> usedVarNames, string prefix = "$temp_")
        {
            int i = usedVarNames != null ? usedVarNames.Count - 1 : -1;
            var name = String.Empty;
            do {
                ++i;
                name = $"{prefix}{type.FirstUppercase()}_{i}";
            } while (usedVarNames != null && usedVarNames.ContainsKey(name));
            var newBoogieVar = new BoogieVariable(type, name);
            if (usedVarNames != null)
            {
                usedVarNames.Add(name, newBoogieVar);
            }
            return newBoogieVar;
        }

        static internal string AdaptNameToBoogie(string name)
        {
            if (name != "type")
            {
                return name;
            }
            return $"$${name}$$";
        }

        static protected bool IsValidVariableName(string name) {
            return !name.Contains(" ");
        }

        public static BoogieVariable ResultVar(Helpers.BoogieType type)
        {
            return new BoogieVariable(type, "$result");
        }

        public static BoogieVariable From(StaticField staticField)
        {
            var fieldName = FieldTranslator.GetFieldName(staticField.Field);
            return new BoogieVariable(Helpers.GetBoogieType(staticField.Field), fieldName);
        }
        // This method should be used with extreme care. In general an incorrect usage should lead to a
        // name resolution error at the Boogie level.
        // The names of the Boogie variable generator for a .NET variable differs when using the
        // new address modeling (sound address modeling) as opposed to the old one, which is unsound.
        // The sound address modeling will create a Boogie variable _{original_name_of_the_dotNET_var},
        // whereas the old one will copy the original name of the .Net variable.
        // TODO(rcastano): restrict the cases in which this conversion from IVariable
        // to BoogieVariable can be performed.
        static public BoogieVariable FromDotNetVariable(IVariable variable)
        {
            Contract.Requires(!Settings.NewAddrModelling);
            return new BoogieVariable(Helpers.GetBoogieType(variable), BoogieVariable.AdaptNameToBoogie(variable.Name));
        }

        private static readonly BoogieVariable _ExceptionVar = ExceptionVar();
        private static readonly BoogieVariable _ExceptionTypeVar = new BoogieVariable(Helpers.BoogieType.Ref, "$ExceptionType");
        private static readonly BoogieVariable _ExceptionInCatchHandlerVar = new BoogieVariable(Helpers.BoogieType.Ref, "$ExceptionInCatchHandler");
        private static readonly BoogieVariable _ExceptionInCatchHandlerTypeVar = new BoogieVariable(Helpers.BoogieType.Ref, "$ExceptionInCatchHandlerType");
        public static BoogieVariable ExceptionVar() { return _ExceptionVar ?? new BoogieVariable(Helpers.BoogieType.Ref, "$Exception"); }
        public static BoogieVariable ExceptionTypeVar() { return _ExceptionTypeVar ?? new BoogieVariable(Helpers.BoogieType.Ref, "$ExceptionType"); }
        public static BoogieVariable ExceptionInCatchHandlerVar() { return _ExceptionInCatchHandlerVar ?? new BoogieVariable(Helpers.BoogieType.Ref, "$ExceptionInCatchHandler"); }
        public static BoogieVariable ExceptionInCatchHandlerTypeVar() { return _ExceptionInCatchHandlerTypeVar ?? new BoogieVariable(Helpers.BoogieType.Ref, "$ExceptionInCatchHandlerType"); }

        public static BoogieVariable AddressVar(IVariable var)
        {
            return new BoogieVariable(Helpers.BoogieType.Addr, $"_{var.Name}");
        }
    }

    public class DelegateHandlingVariable : BoogieVariable
    {
        protected DelegateHandlingVariable(Helpers.BoogieType type, string name) : base(type, name) { }
        public static DelegateHandlingVariable From(IParameterTypeInformation paramInfo)
        {
            return new DelegateHandlingVariable(Helpers.GetBoogieType(paramInfo), $"local{paramInfo.Index}");
        }
        public static DelegateHandlingVariable From(IParameterDefinition paramInfo)
        {
            return new DelegateHandlingVariable(Helpers.GetBoogieType(paramInfo), $"local{paramInfo.Index}");
        }
        public static DelegateHandlingVariable ResultVar(IMethodDefinition methodDefinition)
        {
            return new DelegateHandlingVariable(Helpers.GetBoogieType(methodDefinition.Type), "resultRealType");
        }
    }

    public class DelegateHandlingParameter : DelegateHandlingVariable
    {
        protected DelegateHandlingParameter(Helpers.BoogieType type, string name) : base(type, name) { }
        public new static DelegateHandlingParameter From(IParameterTypeInformation paramInfo)
        {
            return new DelegateHandlingParameter(Helpers.BoogieType.Ref, $"arg{paramInfo.Index}$in");
        }
    }

        public class BoogieParameter : BoogieVariable
    {
        private BoogieParameter(Helpers.BoogieType type, string name) : base(type, name)
        {
            Contract.Requires(IsValidVariableName(name));
        }
        static new public BoogieVariable FromDotNetVariable(IVariable variable)
        {
            Contract.Requires(variable.IsParameter);
            return new BoogieParameter(Helpers.GetBoogieType(variable), AdaptNameToBoogie(variable.Name));
        }
        static public BoogieVariable OutVariable(IParameterDefinition param)
        {
            Contract.Requires(param.IsOut && param.IsByReference);
            return new BoogieParameter(Helpers.GetBoogieType(param), $"{AdaptNameToBoogie(param.Name.Value)}$out");
        }
    }
    public class QuantifiedVariable : BoogieVariable
    {
        public QuantifiedVariable(Helpers.BoogieType type, string name) : base(type, name) { }
    }

    public abstract class SoundAddressModelingExpression : Expression
    {
        protected SoundAddressModelingExpression(Helpers.BoogieType type, string expr) : base(type, expr) { }
    }
    public class ReadTypedMemory : SoundAddressModelingExpression
    {
        private class TemporaryClassToBuildExpression
        {
            public TemporaryClassToBuildExpression(AddressExpression key)
            {
                Key = key;
            }
            public string Expr { get { return $"{ReadFunction}({MemoryMap}, {Key.Expr.Expr})"; } }
            public Helpers.BoogieType Type 
            { get {
                // we want to access the map of the target type not an address -> address map
                if (Key.Type is IManagedPointerType){
                    IManagedPointerType ptrType = Key.Type as IManagedPointerType;
                    return Helpers.GetBoogieType(ptrType.TargetType);
                }

                    return Helpers.GetBoogieType(Key.Type);
              }
            }
            private AddressExpression Key { get; }
            private string MemoryMap { get { return $"$memory{Type.FirstUppercase()}"; } }
            private string ReadFunction { get { return $"Read{Type.FirstUppercase()}"; } }
        }

        private ReadTypedMemory(TemporaryClassToBuildExpression temp) : base(temp.Type, temp.Expr) { }
        public static ReadTypedMemory From(AddressExpression key)
        {
            Contract.Assume(
                Helpers.GetBoogieType(key.Type).Equals(Helpers.BoogieType.Int)     ||
                Helpers.GetBoogieType(key.Type).Equals(Helpers.BoogieType.Bool)    ||
                Helpers.GetBoogieType(key.Type).Equals(Helpers.BoogieType.Object)  ||
                Helpers.GetBoogieType(key.Type).Equals(Helpers.BoogieType.Real)    ||
                Helpers.GetBoogieType(key.Type).Equals(Helpers.BoogieType.Addr));
            return new ReadTypedMemory(new TemporaryClassToBuildExpression(key));
        }
    }

    public abstract class OnlyObjectModelingExpression : Expression
    {
        protected OnlyObjectModelingExpression(Helpers.BoogieType type, string expr) : base(type, expr) { }
    }
    public abstract class SplitFieldsModelingExpression : OnlyObjectModelingExpression
    {
        protected SplitFieldsModelingExpression(Helpers.BoogieType type, string expr) : base(type, expr)
        {
            Contract.Assume(Settings.SplitFields);
        }
    }

    public class ReadFieldExpression : SplitFieldsModelingExpression
    {
        private abstract class TemporaryClassToBuildExpression
        {
            public TemporaryClassToBuildExpression()
            {
            }
            public abstract string Expr();

            public abstract Helpers.BoogieType Type();
        }
        private class TemporaryFromInstanceField : TemporaryClassToBuildExpression
        {

            public TemporaryFromInstanceField(InstanceField instanceField)
            {
                Key = instanceField;
            }
            public override string Expr()
            {
                return $"{MemoryMap}[{Key.Instance.Name}]";
            }
            public override Helpers.BoogieType Type() {
                return Helpers.GetBoogieType(Key.Field);
            }
            private InstanceField Key { get; }
            private string MemoryMap { get { return $"{FieldTranslator.GetFieldName(Key.Field)}"; } }
        }
        private class TemporaryFromStaticField : TemporaryClassToBuildExpression
        {

            public TemporaryFromStaticField(StaticField staticField)
            {
                Field = staticField;
            }
            public override string Expr()
            {
                return FieldTranslator.GetFieldName(Field.Field);
            }
            private StaticField Field;
            public override Helpers.BoogieType Type()
            {
                return Helpers.GetBoogieType(Field.Type);
            }
        }

        private ReadFieldExpression(TemporaryClassToBuildExpression temp) : base(temp.Type(), temp.Expr()) { }
        public static ReadFieldExpression From(InstanceField key)
        {
            return new ReadFieldExpression(new TemporaryFromInstanceField(key));
        }
        public static ReadFieldExpression From(StaticField key)
        {
            return new ReadFieldExpression(new TemporaryFromStaticField(key));
        }
    }

    public abstract class HeapModelingExpression : OnlyObjectModelingExpression
    {
        protected HeapModelingExpression(Helpers.BoogieType type, string expr) : base(type, expr)
        {
            Contract.Assume(!Settings.SplitFields);
        }
    }
    public class ReadHeapExpression : HeapModelingExpression
    {
        private ReadHeapExpression(TemporaryClassToBuildExpression expr) : base(expr.Type, expr.Expr) { }
        private class TemporaryClassToBuildExpression
        {
            public TemporaryClassToBuildExpression(InstanceField instanceField)
            {
                Key = instanceField;
            }
            public string Expr { get { return $"Read($Heap, {Key.Instance.Name}, {Key.Field.Name})"; } }
            public Helpers.BoogieType Type { get { return Helpers.GetBoogieType(Key.Field); } }
            private InstanceField Key { get; }
        }
        public static ReadHeapExpression From(InstanceField expr)
        {
            return new ReadHeapExpression(new TemporaryClassToBuildExpression(expr));
        }
    }
    public class StatementList
    {
        public StatementList()
        {
            Stmts = new List<BoogieStatement>();
        }
        public List<BoogieStatement> Stmts;
        public static readonly StatementList Empty = new StatementList();
        public void Add(BoogieStatement stmt)
        {
            Stmts.Add(stmt);
        }
        public void Add(StatementList stmts)
        {
            Stmts.AddRange(stmts.Stmts);
        }
        public IEnumerator<BoogieStatement> GetEnumerator()
        {
            return Stmts.GetEnumerator();
        }
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var s in Stmts)
            {
                sb.AppendLine(s.Stmt).Replace("<>", "__");
            }
            return sb.ToString();
        }
    }
    public class BoogieStatement {
        protected BoogieStatement(string stmt)
        {
            Stmt = stmt;
        }
        
        internal static BoogieStatement FromString(string str)
        {
            return new BoogieStatement(str);
        }
        public static BoogieStatement VariableDeclaration(BoogieVariable var)
        {
            return new BoogieStatement($"\tvar {var.Expr} : {var.Type};");
        }
        public static BoogieStatement VariableDeclaration(IVariable var)
        {
            return new BoogieStatement($"\tvar {var.Name} : {Helpers.GetBoogieType(var.Type)};");
        }
        public static StatementList VariableAssignment(BoogieVariable variableA, Expression expr)
        {
            // Adding this check because we haven't cleanly separated the new memory model from the old one yet.
            // The problem is that the Boogie variable $Exception is set in the prelude as null, but
            // could be assigned programmatically within BoogieGenerator using null_object (which would be wrong).
            // As such, I've added a specific field that should be used: BoogieStatement.ClearExceptionVar.
            Contract.Assume(!(
                variableA.Equals(BoogieVariable.ExceptionVar()) &&
                expr.Equals(BoogieGenerator.Instance().NullObject())));
            return new BoogieStatement($"{variableA.Expr} := {expr.Expr};");
        }
        public static StatementList ProcedureCall(BoogieMethod procedure, List<Expression> argumentList, List<BoogieVariable> resultArguments, BoogieVariable resultVariable = null)
        {
            StatementList stmts = new StatementList();
            var boogieProcedureName = procedure.Name;
            var arguments = String.Join(",", argumentList.Select(v => v.Expr));
            if (resultArguments.Count > 0)
            {
                var resultArgumentsStr = String.Join(",", resultArguments.Select(v => v.Expr));
                stmts.Add(new BoogieStatement($"call {resultArgumentsStr} := {boogieProcedureName}({arguments});"));
                if (Settings.NewAddrModelling)
                {
                    Contract.Assert(resultArguments.Count == 1);
                    Contract.Assert(resultArguments.Contains(resultVariable));
                }
                return stmts;
            }
            else
                return new BoogieStatement($"call {boogieProcedureName}({arguments});");
        }
        private static BoogieStatement FromList(StatementList stmts)
        {
            Contract.Assume(stmts.Stmts.Count != 0);
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("{");
            foreach (var s in stmts.Stmts)
            {
                sb.AppendLine(s.Stmt);
            }
            sb.AppendLine("}");
            return new BoogieStatement(sb.ToString());
        }

        public static StatementList If(Expression condition, StatementList body)
        {
            var stmts = new StatementList();
            stmts.Add(new BoogieStatement($"if ({condition.Expr})"));
            stmts.Add(FromList(body));
            return stmts;
        }
        public static StatementList ElseIf(Expression condition, StatementList body)
        {
            StatementList stmts = new StatementList();

            stmts.Add(new BoogieStatement($"else if ({condition.Expr})"));
            stmts.Add(FromList(body));

            return stmts;
        }
        public static StatementList Else(StatementList body)
        {
            StatementList stmts = new StatementList();

            stmts.Add(new BoogieStatement("else"));
            stmts.Add(FromList(body));

            return stmts;
        }
        public static BoogieStatement LocationAttributes(string sourceFile, int sourceLine, string additionalAttribute)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"assert {{:sourceFile \"{sourceFile}\"}} {{:sourceLine \"{sourceLine}\"}} ");
            if (additionalAttribute != null)
            {
                Contract.Assume(additionalAttribute.StartsWith("{:"));
                Contract.Assume(additionalAttribute.EndsWith("}"));
                sb.Append($"{additionalAttribute} ");
            }
            sb.Append("true;");
            return new BoogieStatement(sb.ToString());
        }
        public static BoogieStatement AllocObjectAxiom(IVariable paramVariable)
        {
            Contract.Assume(Helpers.GetBoogieType(paramVariable).Equals(Helpers.BoogieType.Object));
            return new BoogieStatement(String.Format("assume $AllocObject[{0}] == true || {0} != null_object;", paramVariable));
        }

        public static BoogieStatement AllocAddrAxiom(IVariable paramVariable)
        {
            Contract.Assume(Helpers.GetBoogieType(paramVariable).Equals(Helpers.BoogieType.Addr));
            return new BoogieStatement(String.Format("assume $AllocAddr[{0}] == true || {0} != null_addr;", paramVariable));
        }
        

        public static readonly BoogieStatement Nop = new BoogieStatement(String.Empty);
        internal static readonly BoogieStatement ReturnStatement = new BoogieStatement("return ;");
        public static implicit operator StatementList(BoogieStatement s)
        {
            var res = new StatementList();
            res.Add(s);
            return res;
        }
        public readonly string Stmt;

        // The hardcoded value of null here is correct!
        // The value is hardcoded because we haven't cleanly separated the new memory model from the old one yet.
        // The problem is that the Boogie variable $Exception is set in the prelude as null, even when we use 
        // the new memory model.
        // As such, I've added a specific method that should be used: ExceptionVarNotNull.
        internal static BoogieStatement ClearExceptionVar = new BoogieStatement($"{BoogieVariable.ExceptionVar().Expr} := null;");
        internal static BoogieStatement ClearExceptionTypeVar = new BoogieStatement($"{BoogieVariable.ExceptionTypeVar().Expr} := null;");

        // TODO(rcastano): This is technically not a statement.
        public static BoogieStatement AddLabel(Instruction instr)
        {
            string label = instr.Label;
            if (!String.IsNullOrEmpty(label))
                return FromString(String.Format("\t{0}:", label));
            else
                return Nop;
        }

        private static string FixAnnotation(string annotation)
        {
            if (annotation != null)
            {
                Contract.Assume(!annotation.Contains(" "));
                Contract.Assume(!annotation.Contains(":"));
                annotation = $"{{ :{annotation} }}";
            }
            else
            {
                annotation = String.Empty;
            }
            return annotation;
        }
        public static BoogieStatement Assume(Expression cond, string annotation = null)
        {
            annotation = FixAnnotation(annotation);
            Contract.Assume(cond.Type.Equals(Helpers.BoogieType.Bool));
            return new BoogieStatement($"assume {annotation} {cond.Expr};");
        }

        public static StatementList Assert(Expression cond, string annotation = null)
        {
            annotation = FixAnnotation(annotation);
            Contract.Assume(cond.Type.Equals(Helpers.BoogieType.Bool));
            return new BoogieStatement($"assert {annotation} {cond.Expr};");
        }

        public static BoogieStatement Goto(string label)
        {
            return new BoogieStatement(String.Format("\t\tgoto {0};", label));
        }
        public static BoogieStatement HavocResult(DefinitionInstruction instruction)
        {
            return new BoogieStatement(String.Format("havoc {0};", instruction.Result));
        }
        public static StatementList AssumeTypeConstructor(Expression arg, string type)
        {
            return BoogieStatement.FromString(String.Format("assume $TypeConstructor($DynamicType({0})) == T${1};", arg.Expr, type));
        }
    }
    

    // TODO(rcastano): The current encoding of blocks is very preliminary.
    // We're opening and closing brackets as boogie statements, hence not
    // keeping any of the structure. This should be fixed.
    public class BoogieBlock
    {
        public BoogieBlock()
        {
            Stmts = new StatementList();
        }
        public StatementList Stmts;
        public void Add(BoogieStatement stmt)
        {
            Stmts.Add(stmt);
        }
        public void Add(StatementList stmt)
        {
            Stmts.Add(stmt);
        }

        public static implicit operator StatementList(BoogieBlock s)
        {
            return s.Stmts;
        }
    }
    public class MemoryMapUpdate : BoogieStatement
    {
        protected MemoryMapUpdate(string stmt) : base(stmt) { }
    }
    public class HeapUpdate : MemoryMapUpdate
    {
        private HeapUpdate(string stmt) : base(stmt) { }
        
        public static HeapUpdate ForKeyValue(InstanceField key, Expression value)
        {
            Contract.Assume(!Settings.SplitFields);
            var supportedTypes = new HashSet<Helpers.BoogieType> { Helpers.BoogieType.Int, Helpers.BoogieType.Bool, Helpers.BoogieType.Ref, Helpers.BoogieType.Real };
            Contract.Assume(supportedTypes.Contains(Helpers.GetBoogieType(key.Field)));
            Contract.Assume(Helpers.GetBoogieType(key.Field).Equals(value.Type));
            var Type = Helpers.GetBoogieType(key.Field);
            var Stmt = $"$Heap := Write($Heap, {key.Instance}, {key.Field}, {value.Expr});";
            return new HeapUpdate(Stmt);
        }
    }
    public class SplitFieldUpdate : MemoryMapUpdate
    {
        private SplitFieldUpdate(string stmt) : base(stmt) { }

        public static SplitFieldUpdate ForKeyValue(InstanceField key, Expression value)
        {
            Contract.Assume(Settings.SplitFields);
            var supportedTypes = new HashSet<Helpers.BoogieType> { Helpers.BoogieType.Int, Helpers.BoogieType.Bool, Helpers.BoogieType.Ref, Helpers.BoogieType.Real };
            Contract.Assume(supportedTypes.Contains(Helpers.GetBoogieType(key.Field)));
            Contract.Assume(Helpers.GetBoogieType(key.Field).Equals(value.Type) || value.Type.Equals(Helpers.BoogieType.Union));
            var Type = Helpers.GetBoogieType(key.Field);
            var fieldName = FieldTranslator.GetFieldName(key.Field);
            var Stmt = $"{fieldName}[{key.Instance}] := {value.Expr};";
            return new SplitFieldUpdate(Stmt);
        }
    }
    public class TypedMemoryMapUpdate : MemoryMapUpdate
    {
        private TypedMemoryMapUpdate(TemporaryClassToBuildExpression temp) : base(temp.Stmt) { }
        private class TemporaryClassToBuildExpression
        {
            public TemporaryClassToBuildExpression(AddressExpression key, string value)
            {
                Key = key;
                Value = value;
            }
            public string Stmt { get { return $"{MemoryMap} := {WriteFunction}({MemoryMap}, {Key.Expr.Expr}, {Value});"; } }
            public Helpers.BoogieType Type
            {
                get
                {
                    // we want to access the map of the target type not an address -> address map
                    if (Key.Type is IManagedPointerType)
                    {
                        IManagedPointerType ptrType = Key.Type as IManagedPointerType;
                        return Helpers.GetBoogieType(ptrType.TargetType);
                    }

                    return Helpers.GetBoogieType(Key.Type);
                }
            }
            private AddressExpression Key { get; }
            // TODO(rcastano): This should be "Expression Value"
            private string Value { get; }
            private string MemoryMap { get { return $"$memory{Type.FirstUppercase()}"; } }
            private string WriteFunction { get { return $"Write{Type.FirstUppercase()}"; } }
        }
        public static TypedMemoryMapUpdate ForKeyValue(AddressExpression key, Expression value)
        {
            return ForKeyValue(key, value.Expr);
        }
        public static TypedMemoryMapUpdate ForKeyValue(AddressExpression key, string value)
        {
            var supportedTypes = new HashSet<Helpers.BoogieType> { Helpers.BoogieType.Int, Helpers.BoogieType.Bool, Helpers.BoogieType.Object, Helpers.BoogieType.Real, Helpers.BoogieType.Addr };
            Contract.Assume(supportedTypes.Contains(Helpers.GetBoogieType(key.Type)));
            // TODO(rcastano): re-add later when value is of the right type (Expression)
            // Contract.Assume(Helpers.GetBoogieType(key.Type).Equals(value.Type));
            return new TypedMemoryMapUpdate(new TemporaryClassToBuildExpression(key, value));
        }
    }
    public class BoogieMethod
    {
        protected BoogieMethod(string methodName)
        {
            Name = methodName;
        }
        public string Name;
        // Keeping the whole method only for the comment.
        private static String GetMethodName(IMethodReference methodDefinition)
        {
            return Helpers.CreateUniqueMethodName(methodDefinition);

            //var signature = MemberHelper.GetMethodSignature(GetUnspecializedVersion(methodDefinition), NameFormattingOptions.UseGenericTypeNameSuffix);
            //signature = signature.Replace("..", ".#"); // for ctor its name is ..ctor it changes to .#ctor            
            //var arity = Helpers.GetArityWithNonBoogieTypes(methodDefinition);
            //arity = arity.Replace("[]", "array");
            //var result = signature + arity;
            //result = result.Replace('<', '$').Replace('>', '$').Replace(", ", "$"); // for example containing type for delegates
            //return result;
        }
        public static BoogieMethod From(IMethodReference methodReference)
        {
            return new BoogieMethod(GetMethodName(methodReference));
        }
        public static BoogieMethod BoxFrom(Helpers.BoogieType type)
        {
            return new BoogieMethod($"$BoxFrom{type.FirstUppercase()}");
        }
        public static BoogieMethod InvokeDelegate(MethodCallInstruction instruction)
        {

            var normalizedType = Helpers.GetNormalizedTypeForDelegates(instruction.Method.ContainingType);
            return new BoogieMethod($"InvokeDelegate_{normalizedType}");
        }
        public static BoogieMethod CreateDelegate(MethodCallInstruction instruction)
        {
            var normalizedType = Helpers.GetNormalizedTypeForDelegates(instruction.Method.ContainingType);
            return new BoogieMethod($"CreateDelegate_{normalizedType}");
        }

        public static readonly BoogieMethod StringEquality = new BoogieMethod("System.String.op_Equality$System.String$System.String");
        public static readonly BoogieMethod StringInequality = new BoogieMethod("System.String.op_Inequality$System.String$System.String");
        public static readonly BoogieMethod StringConcat = new BoogieMethod("System.String.Concat$System.String$System.String");
        public static readonly BoogieMethod AllocObject = new BoogieMethod("AllocObject");
        public static readonly BoogieMethod AllocAddr = new BoogieMethod("AllocAddr");
        public static readonly BoogieMethod Alloc = new BoogieMethod("Alloc");
        public static readonly BoogieMethod ReadArrayElement = new BoogieMethod("$ReadArrayElement");
        public static readonly BoogieMethod WriteArrayElement = new BoogieMethod("$WriteArrayElement");
        public static readonly BoogieMethod HavocArrayElementsNoNull = new BoogieMethod("$HavocArrayElementsNoNull");
        public static readonly BoogieMethod GetTypeMethod = new BoogieMethod("System.Object.GetType");
    }
    public abstract class Addressable
    {

    }
    // TODO(rcastano): Change name. An instance of AddressExpression will not actually be an expression.
    // expr field is an expression that in boogie will be typed as Addr
    public class AddressExpression : Addressable
    {
        public AddressExpression(ITypeReference t, Expression e)
        {
            Type = t;
            Expr = e;
        }

        public override string ToString()
        {
            throw new Exception("This method should not be called, use property Expr within property Expr (that is Expr.Expr).");
        }

        // make them only readable
        public ITypeReference Type;
        public Expression Expr;
    }

    // todo: refactor hierarchy
    // only used when we are not using split fields and the old mem addressing
    public class InstanceField : Addressable
    {
        public IFieldReference Field { get; }
        public IVariable Instance { get; }
        public InstanceField(InstanceFieldAccess fieldAccess)
        {
            Field = fieldAccess.Field;
            Instance = fieldAccess.Instance;
        }
    }

    public class StaticField : Addressable
    {
        public IFieldReference Field { get; }
        public ITypeReference Type { get; }

        public StaticField(StaticFieldAccess fieldAccess)
        {
            Field = fieldAccess.Field;
            Type = fieldAccess.Field.Type;
        }
    }
    public class DotNetVariable : Addressable
    {
        public IVariable Var { get; }

        public DotNetVariable(IVariable var)
        {
            Var = var;
        }

        public override string ToString()
        {
            return Var.Name;
        }
    }

    // TODO: improve inheritance
    // BoogieGenerator class should not have memory specific methods
    public class BoogieGeneratorAddr : BoogieGenerator
    {
        public override Expression NullObject()
        {
            return BoogieLiteral.NullObject;
        }

        // hides implementation in super class
        public override StatementList AllocAddr(IVariable var)
        {
            var resultBoogieVar = BoogieVariable.AddressVar(var);
            return this.ProcedureCall(BoogieMethod.AllocAddr, new List<Expression> { }, resultBoogieVar);
        }
        public override StatementList AllocObject(BoogieVariable boogieVar)
        {
            return this.ProcedureCall(BoogieMethod.AllocObject, new List<Expression> { }, boogieVar);
        }
        public override StatementList AllocObject(IVariable var, InstructionTranslator instTranslator)
        {
            var freshVariable = instTranslator.GetFreshVariable(Helpers.GetBoogieType(var));
            var stmts = new StatementList();
            stmts.Add(this.AllocObject(freshVariable));
            stmts.Add(this.VariableAssignment(var, freshVariable));
            return stmts;
        }

        // hides implementation in super class
        //public new string VariableAssignment(string variableA, string expr)
        //{
        //    return string.Format("{0} := {1};", variableA, expr);
        //}

        public override StatementList DeclareLocalVariables(IList<IVariable> variables, Dictionary<string, BoogieVariable> temporalVariables)
        {
            var stmts = new StatementList();
            foreach (var v in variables)
            {
                stmts.Add(BoogieStatement.VariableDeclaration(BoogieVariable.AddressVar(v)));
            }

            foreach (var kv in temporalVariables)
            {
                stmts.Add(BoogieStatement.VariableDeclaration(kv.Value));
            }

            return stmts;
        }

        public override String GetFieldDefinition(IFieldReference fieldReference, String fieldName)
        {
            if (fieldReference.IsStatic)
                return String.Format("var {0}: {1};", fieldName, "Addr");
            else
                return String.Format("var {0} : InstanceFieldAddr;", fieldName);
        }

        public override BoogieVariable GetProcedureResultVariable(MethodCallInstruction methodCallInstruction, InstructionTranslator instructionTranslator)
        {
            if (methodCallInstruction.HasResult)
            {
                var resultVariable = methodCallInstruction.Result;
                return instructionTranslator.GetFreshVariable(Helpers.GetBoogieType(resultVariable));
            }
            else
                return null;
        }

        public override StatementList SetProcedureResultVariable(BoogieVariable procedureResult, IVariable finalVariable)
        {
            Contract.Assume((procedureResult != null && finalVariable != null) ||
                 (procedureResult == null && finalVariable == null));

            if (procedureResult == null)
                return StatementList.Empty;

            return WriteAddr(AddressOf(finalVariable), procedureResult);
        }

        public override Helpers.BoogieType GetBoogieTypeForProcedureParameter(IParameterTypeInformation parameter)
        {
            //if (parameter.IsByReference)
            //    return Helpers.BoogieType.Addr;
            //else
                return Helpers.GetBoogieType(parameter);
        }

        public override StatementList ProcedureCall(IMethodReference procedure, MethodCallInstruction methodCallInstruction, InstructionTranslator instTranslator, BoogieVariable resultVariable = null)
        {
            var argumentList = ComputeArguments(methodCallInstruction, instTranslator);
            return ProcedureCall(BoogieMethod.From(procedure), argumentList.Select(v => v.Item1).ToList(), resultVariable);
        }

        public override Expression ReadAddr(Addressable addr)
        {
            if (addr is AddressExpression addrExpr)
            {
                var readExpr = ReadTypedMemory.From(addrExpr);
                return readExpr;
            } else
            {
                throw new NotImplementedException();
            }
        }

        public override StatementList WriteAddr(Addressable addr, Expression expr)
        {
            if (addr is AddressExpression)
            {
                var addrExpr = addr as AddressExpression;
                return TypedMemoryMapUpdate.ForKeyValue(addrExpr, expr);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override StatementList VariableAssignment(IVariable variableA, IValue value)
        {
            Constant cons = value as Constant;
            BoogieLiteral boogieConstant = null;
            if (cons != null)
            {
                boogieConstant = BoogieLiteral.FromDotNetConstant(cons);
            }


            var boogieType = Helpers.GetBoogieType(variableA);

            if (value is Constant)
            {
                if (boogieConstant != null)
                {
                    return VariableAssignment(variableA, boogieConstant);
                } else
                {
                    throw new NotImplementedException();
                    // return WriteAddr(variableA, value.ToString());
                }

            } else if (value is IVariable && !(value.Type is IManagedPointerType))
            { // right operand is not a pointer (therefore left operand is not a pointer)

                return WriteAddr(AddressOf(variableA), ReadAddr(value as IVariable));
            } else if (value is Dereference)
            {
                var dereference = value as Dereference;
                var content = ReadAddr(dereference.Reference);
                return WriteAddr(AddressOf(variableA), content);
            } else if (value.Type is IManagedPointerType)
            {

                // if the right operand is a pointer also the left one is a pointer
                // there are two cases for value:
                // 1) value has the form &<something> (in analysis-net this is a Reference object)
                // 2) value is just a variable (static, instance, local, array element) with pointer type
                // for 1) we want to take the allocated address of something and assign it to the boogie variable of the left operand
                // for 2) we just want to make a boogie assignment between the boogie variables of the left and right operands

                // AddressOf will do the work to separate case 1) and 2)
                var addr = AddressOf(value) as AddressExpression;
                Contract.Assume(addr != null);
                return BoogieStatement.VariableAssignment(BoogieVariable.AddressVar(variableA), addr.Expr);
            }

            Contract.Assert(false);
            // This shouldn't be reachable.
            throw new NotImplementedException();
        }

        public override StatementList AllocLocalVariables(IList<IVariable> variables)
        {
            StatementList stmts = new StatementList();

            // we allocate an address for all local variables
            // except they are a pointer, we are assuming that you can't take the address of a pointer
            foreach (var v in variables)
                if (!(v.Type is IManagedPointerType))
                    stmts.Add(AllocAddr(v));

            // load values into stack space
            foreach (var paramVariable in variables.Where(v => v.IsParameter))
            {
                // paramValue are variables in the three address code
                // however in boogie they are treated as values
                // those values are loaded into the stack memory space

                /*
                 void foo(int x){
                 }

                 procedure foo(x : int){
                    var _x : Addr; // stack space (done in previous loop)
                    x_ := AllocAddr();

                    data(_x) := x; // we are doing this conceptually
                 }
                */
                var boogieParamVariable = BoogieParameter.FromDotNetVariable(paramVariable);

                if (paramVariable.Type is IManagedPointerType)
                {
                    stmts.Add(BoogieStatement.VariableAssignment(BoogieVariable.AddressVar(paramVariable), boogieParamVariable));
                    continue;
                }

                Addressable paramAddress = AddressOf(paramVariable);

                // boogie generator knows that must fetch paramVariable's address (_x and not x)
                stmts.Add(WriteAddr(paramAddress, boogieParamVariable));

                if (Helpers.GetBoogieType(paramVariable).Equals(Helpers.BoogieType.Object))
                {
                    stmts.Add(BoogieStatement.AllocObjectAxiom(paramVariable));
                } else if (Helpers.GetBoogieType(paramVariable).Equals(Helpers.BoogieType.Addr))
                {
                    stmts.Add(BoogieStatement.AllocAddrAxiom(paramVariable));
                }
            }

            return stmts;
        }

        public override Addressable AddressOf(InstanceFieldAccess instanceFieldAccess)
        {
            var expr = Expression.LoadInstanceFieldAddr(instanceFieldAccess.Field, ReadAddr(instanceFieldAccess.Instance));
            return new AddressExpression(instanceFieldAccess.Field.Type, expr);
        }

        public override Addressable AddressOf(StaticFieldAccess staticFieldAccess)
        {
            var address = new AddressExpression(staticFieldAccess.Field.Type, BoogieVariable.From(new StaticField(staticFieldAccess)));
            return address;
        }

        public override Addressable AddressOf(IVariable var)
        {
            return new AddressExpression(var.Type, BoogieVariable.AddressVar(var));
        }

        public override Expression ReadInstanceField(InstanceFieldAccess instanceFieldAccess)
        {
            var fieldAddr = AddressOf(instanceFieldAccess);
            var readValue = ReadAddr(fieldAddr);

            return readValue;
        }

        public override StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            var readValue = ReadInstanceField(instanceFieldAccess);

            // dependiendo del type (del result?) indexo en el $memoryInt
            if (Helpers.IsGenericField(instanceFieldAccess.Field))
            {
                var boogieType = Helpers.GetBoogieType(result);
                if (!boogieType.Equals(Helpers.BoogieType.Object))
                {
                    return VariableAssignment(result, Expression.Union2PrimitiveType(boogieType, readValue));
                } else
                {
                    return VariableAssignment(result, readValue);
                }
            }
            else
            {
                return VariableAssignment(result, readValue);
            }
        }
        
        public override StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression expr, InstructionTranslator instTranslator)
        {
            StatementList stmts = new StatementList();

            var boogieType = expr.Type;
            if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals(Helpers.BoogieType.Object))
            {
                stmts.Add(Expression.AssumeInverseRelationUnionAndPrimitiveType(expr));
                stmts.Add(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr, instTranslator)));
            }
            else
                stmts.Add(WriteAddr(AddressOf(instanceFieldAccess), expr));

            return stmts;
        }

        public override StatementList CallReadArrayElement(IVariable resultVariable, Expression array, Expression index, InstructionTranslator instructionTranslator)
        {
            StatementList stmts = new StatementList();

            BoogieVariable boogieResVar = null;
            if (resultVariable != null)
            {
                boogieResVar = instructionTranslator.GetFreshVariable(Helpers.GetBoogieType(resultVariable));
            }
            stmts.Add(CallReadArrayElement(boogieResVar, array, index));
            if (resultVariable != null)
            {
                stmts.Add(WriteAddr(AddressOf(resultVariable), boogieResVar));
            }
            return stmts;

        }
    }

    public class BoogieGeneratorMixed : BoogieGenerator
    {
        private readonly BoogieGeneratorAddr BoogieAddr = new BoogieGeneratorAddr();
        private readonly BoogieGeneratorALaBCT BoogieBCT = new BoogieGeneratorALaBCT();

        private bool RequiresAllocation(IValue value)
        {
            if (value is IReferenceable referenceable)
                RequiresAllocation(referenceable);

            return false;
        }

        private bool RequiresAllocation(IReferenceable referenceable)
        {
            // we are assuming that a IManagedPointerType can not be a pointer of a pointer
            Contract.Assume(referenceable is IManagedPointerType && !ReferenceFinder.IsReferenced(referenceable));

            return ReferenceFinder.IsReferenced(referenceable);
        }

        private bool RequiresAllocation(IFieldReference field)
        {
            // we are assuming that a IManagedPointerType can not be a pointer of a pointer
            Contract.Assume(field.Type is IManagedPointerType && !ReferenceFinder.IsReferenced(field));

            return ReferenceFinder.IsReferenced(field);
        }

        public override Addressable AddressOf(InstanceFieldAccess instanceFieldAccess)
        {
            if (RequiresAllocation(instanceFieldAccess))
                return BoogieAddr.AddressOf(instanceFieldAccess);
            else
                return BoogieBCT.AddressOf(instanceFieldAccess);
        }

        public override Addressable AddressOf(StaticFieldAccess staticFieldAccess)
        {
            if (RequiresAllocation(staticFieldAccess))
                return BoogieAddr.AddressOf(staticFieldAccess);
            else
                return BoogieBCT.AddressOf(staticFieldAccess);
        }

        public override Addressable AddressOf(IVariable var)
        {
            if (RequiresAllocation(var))
                return BoogieAddr.AddressOf(var);
            else
                return BoogieBCT.AddressOf(var);
        }

        public override StatementList AllocAddr(IVariable var)
        {
            if (RequiresAllocation(var))
                return BoogieAddr.AllocAddr(var);
            else
                return BoogieBCT.AllocAddr(var);
        }

        public override StatementList AllocLocalVariables(IList<IVariable> variables)
        {
            StatementList stmts = new StatementList();

            // only allocate an address for variables that are referenced 
            foreach (var v in variables)
                if (RequiresAllocation(v))
                    stmts.Add(AllocAddr(v));

            foreach (var paramVariable in variables.Where(v => v.IsParameter))
            {
                var boogieParamVariable = BoogieParameter.FromDotNetVariable(paramVariable);

                if (!RequiresAllocation(paramVariable))
                {
                    stmts.Add(BoogieStatement.VariableAssignment(BoogieVariable.AddressVar(paramVariable), boogieParamVariable));
                    continue;
                }

                Addressable paramAddress = AddressOf(paramVariable);

                // boogie generator knows that must fetch paramVariable's address (_x and not x)
                stmts.Add(WriteAddr(paramAddress, boogieParamVariable));

                if (Helpers.GetBoogieType(paramVariable).Equals(Helpers.BoogieType.Object))
                {
                    stmts.Add(BoogieStatement.AllocObjectAxiom(paramVariable));
                }
                else if (Helpers.GetBoogieType(paramVariable).Equals(Helpers.BoogieType.Addr))
                {
                    stmts.Add(BoogieStatement.AllocAddrAxiom(paramVariable));
                }
            }

            return stmts;
        }

        public override StatementList AllocObject(BoogieVariable var)
        {
            // actually should be the same in both models
            return BoogieAddr.AllocObject(var);
        }

        public override StatementList AllocObject(IVariable var, InstructionTranslator instTranslator)
        {
            if (RequiresAllocation(var))
                return BoogieAddr.AllocObject(var, instTranslator);
            else
                return BoogieBCT.AllocObject(var, instTranslator);
        }

        public override StatementList DeclareLocalVariables(IList<IVariable> variables, Dictionary<string, BoogieVariable> temporalVariables)
        {
            var stmts = new StatementList();
            foreach (var v in variables)
            {
                if (RequiresAllocation(v))
                    stmts.Add(BoogieStatement.VariableDeclaration(BoogieVariable.AddressVar(v)));
                else
                    stmts.Add(BoogieStatement.VariableDeclaration(v));
            }

            foreach (var kv in temporalVariables)
            {
                stmts.Add(BoogieStatement.VariableDeclaration(kv.Value));
            }

            return stmts;
        }

        public override Helpers.BoogieType GetBoogieTypeForProcedureParameter(IParameterTypeInformation parameter)
        {
            return BoogieAddr.GetBoogieTypeForProcedureParameter(parameter);
        }

        public override string GetFieldDefinition(IFieldReference fieldReference, string fieldName)
        {
            if (RequiresAllocation(fieldReference))
                return BoogieAddr.GetFieldDefinition(fieldReference, fieldName);
            return BoogieBCT.GetFieldDefinition(fieldReference, fieldName);
        }

        public override StatementList SetProcedureResultVariable(BoogieVariable procedureResult, IVariable finalVariable)
        {
            Contract.Assume((procedureResult != null && finalVariable != null) ||
                 (procedureResult == null && finalVariable == null));

            if (procedureResult == null)
                return StatementList.Empty;

            return WriteAddr(AddressOf(finalVariable), procedureResult);
        }

        public override BoogieVariable GetProcedureResultVariable(MethodCallInstruction methodCallInstruction, InstructionTranslator instructionTranslator)
        {
            return BoogieAddr.GetProcedureResultVariable(methodCallInstruction, instructionTranslator);
        }

        public override StatementList ProcedureCall(IMethodReference procedure, MethodCallInstruction methodCallInstruction, InstructionTranslator instTranslator, BoogieVariable resultVariable = null)
        {
            return BoogieAddr.ProcedureCall(procedure, methodCallInstruction, instTranslator, resultVariable);
        }

        public override Expression NullObject()
        {
            return BoogieAddr.NullObject();
        }

        public override Expression ReadAddr(Addressable addr)
        {
            if (addr is AddressExpression)
            {
                return BoogieAddr.ReadAddr(addr);
            }
            else if (addr is InstanceField)
            {
                return BoogieBCT.ReadAddr(addr);
            }
            else if (addr is StaticField)
            {
                return BoogieBCT.ReadAddr(addr);
            }
            else if (addr is DotNetVariable)
            {
                return BoogieBCT.ReadAddr(addr);
            }

            throw new NotImplementedException();
        }

        public override Expression ReadInstanceField(InstanceFieldAccess instanceFieldAccess)
        {
            if (RequiresAllocation(instanceFieldAccess))
                return BoogieAddr.ReadInstanceField(instanceFieldAccess);
            else
                return BoogieBCT.ReadInstanceField(instanceFieldAccess);
        }

        public override StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            // with split fields false, i guess we should cast to union always
            Contract.Assert(Settings.SplitFields);

            Expression readExpr = ReadInstanceField(instanceFieldAccess);

            if (Helpers.IsGenericField(instanceFieldAccess.Field))
            {
                if (!Helpers.IsBoogieRefType(result))
                {
                    var boogieType = Helpers.GetBoogieType(result);
                    return VariableAssignment(result, Expression.Union2PrimitiveType(boogieType, readExpr));
                }
            }

            return VariableAssignment(result, readExpr);  
        }

        public override StatementList VariableAssignment(IVariable variableA, IValue value)
        {
            bool lhsAlloc = RequiresAllocation(variableA);

            Expression expression = GetExpressionFromIValue(value);

            if (lhsAlloc)
                return BoogieAddr.VariableAssignment(variableA, expression);

            if (!lhsAlloc)
                return BoogieBCT.VariableAssignment(variableA, expression);

            return null;
        }

        private Expression GetExpressionFromIValue(IValue value)
        {
            if (RequiresAllocation(value)) // if true, value is in a subset of IReferenceable stuff
            {
                // something requires an allocation because it has been reference (it is referenceable)
                IReferenceable referenceable = value as IReferenceable;
                return BoogieAddr.ReadAddr(BoogieAddr.AddressOf(referenceable));
            }

            if (value.Type is IManagedPointerType)
            {
                AddressExpression addr = BoogieAddr.AddressOf(value) as AddressExpression;
                return addr.Expr;
            }

            if (value is Constant constant)
                return BoogieLiteral.FromDotNetConstant(constant);

            // we use old memory model here because they were not referenced
            if (value is IReferenceable || value is Reference)
            {
                Addressable addressable = BoogieBCT.AddressOf(value);
                return BoogieBCT.ReadAddr(addressable);
            }

            throw new NotImplementedException();
        }

        public override StatementList WriteAddr(Addressable addr, Expression value)
        {
            if (addr is AddressExpression addrExpr)
            {
                return BoogieAddr.WriteAddr(addr, value);
            } else if (addr is InstanceField instanceField)
            {
                return BoogieBCT.WriteAddr(addr, value);
            } else if (addr is StaticField staticField)
            {
                return BoogieBCT.WriteAddr(addr, value);
            } else if (addr is DotNetVariable dotNetVariable)
            {
                return BoogieBCT.WriteAddr(addr, value);
            }

            throw new NotImplementedException();
        }

        public override StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression value, InstructionTranslator instTranslator)
        {
            if (RequiresAllocation(instanceFieldAccess))
                return BoogieAddr.WriteInstanceField(instanceFieldAccess, value, instTranslator);
            else
                return BoogieBCT.WriteInstanceField(instanceFieldAccess, value, instTranslator);
        }

        public override StatementList CallReadArrayElement(IVariable resultVariable, Expression array, Expression index, InstructionTranslator instructionTranslator)
        {
            throw new NotImplementedException();
        }
    }

    public class BoogieGeneratorALaBCT : BoogieGenerator
    {
        public override Expression NullObject()
        {
            return BoogieLiteral.NullRef;
        }

        public override StatementList VariableAssignment(IVariable variableA, IValue value)
        {
            Constant cons = value as Constant;
            if (cons != null)
            {   
                return VariableAssignment(variableA, BoogieLiteral.FromDotNetConstant(cons));
            } else if (value is Dereference)
            {
                var dereference = value as Dereference;
                return VariableAssignment(variableA, dereference.Reference);
            }

            return VariableAssignment(variableA, ReadAddr(AddressOf(value)));
        }

        public override StatementList AllocLocalVariables(IList<IVariable> variables)
        {
            return BoogieStatement.Nop;
        }
        public override StatementList DeclareLocalVariables(IList<IVariable> variables, Dictionary<string, BoogieVariable> temporalVariables)
        {
            StatementList stmts = new StatementList();
            
            foreach (var v in variables.Where(v => !v.IsParameter))
            {
                stmts.Add(BoogieStatement.VariableDeclaration(v));
            }

            foreach (var kv in temporalVariables)
            {
                stmts.Add(BoogieStatement.VariableDeclaration(kv.Value));
            }

            return stmts;
        }

        public override String GetFieldDefinition(IFieldReference fieldReference, String fieldName)
        {
            if (fieldReference.IsStatic)
            {
                return String.Format("var {0}: {1};", fieldName, Helpers.GetBoogieType(fieldReference));
            }
            else
            {
                if (!Settings.SplitFields)
                    return String.Format("const unique {0} : Field;", fieldName);
                else
                {
                    if (Helpers.IsGenericField(fieldReference))
                    {
                        return String.Format("var {0} : [Ref]Union;", fieldName);
                    }
                    else
                    {
                        var boogieType = Helpers.GetBoogieType(fieldReference);
                        return String.Format("var {0} : [Ref]{1};", fieldName, boogieType);
                    }
                }
            }
        }

        public override BoogieVariable GetProcedureResultVariable(MethodCallInstruction methodCallInstruction, InstructionTranslator instructionTranslator)
        {
            if (methodCallInstruction.HasResult)
                return BoogieVariable.FromDotNetVariable(methodCallInstruction.Result);
            else
                return null;
        }

        public override StatementList SetProcedureResultVariable(BoogieVariable procedureResult, IVariable finalVariable)
        {
            // In this mode there is no need to have the result variable in a temp variable
            // BoogieGenerator.ProcedureCall does its work
            return StatementList.Empty;
        }

        public override Helpers.BoogieType GetBoogieTypeForProcedureParameter(IParameterTypeInformation parameter)
        {
            return Helpers.GetBoogieType(parameter);
        }

        public override StatementList ProcedureCall(IMethodReference procedure, MethodCallInstruction methodCallInstruction, InstructionTranslator instTranslator, BoogieVariable resultVariable = null)
        {
            var argumentList = ComputeArguments(methodCallInstruction, instTranslator);
            var boogieProcedure = BoogieMethod.From(procedure);

            int s = procedure.IsStatic ? 0 : 1;

            // check behavior with out arguments
            var referencedIndexes = procedure.Parameters.Where(p => p.IsByReference).Select(p => p.Index + s);

            var resultArguments = new List<BoogieVariable>();
            foreach (var i in referencedIndexes)
            {
                Contract.Assume(argumentList[i].Item2 != null);
                resultArguments.Add(BoogieVariable.FromDotNetVariable(argumentList[i].Item2));
            }

            if (resultVariable != null)
            {
                resultArguments.Add(resultVariable);
            }

            return BoogieStatement.ProcedureCall(boogieProcedure, argumentList.Select(v => v.Item1).ToList(), resultArguments, resultVariable);
        }

        public override StatementList AllocAddr(IVariable var)
        {
            return BoogieStatement.Nop;
        }
        public override StatementList AllocObject(BoogieVariable boogieVar)
        {
            return this.ProcedureCall(BoogieMethod.Alloc, new List<Expression> { }, boogieVar);
        }
        public override StatementList AllocObject(IVariable var, InstructionTranslator instTranslator)
        {
            return this.AllocObject(BoogieVariable.FromDotNetVariable(var));
        }

        public override StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression expr, InstructionTranslator instTranslator)
        {
            StatementList stmts = new StatementList();

            String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

            //var addr = AddressOf(instanceFieldAccess);
            //var writeAddr = WriteAddr(addr, value);

            if (!Settings.SplitFields)
            {
                if (!Helpers.IsBoogieRefType(expr.Type)) // int, bool, real
                {
                    stmts.Add(Expression.AssumeInverseRelationUnionAndPrimitiveType(expr));
                    stmts.Add(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr, instTranslator)));
                    //sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, PrimitiveType2Union(Helpers.GetBoogieType(value.Type), value.Name)));
                }
                else
                {
                    stmts.Add(WriteAddr(AddressOf(instanceFieldAccess), expr));
                    //sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, value.Name));
                }
            }
            else
            {
                var boogieType = expr.Type;
               // var heapAccess = String.Format("{0}[{1}]", fieldName, instanceFieldAccess.Instance);
                //F$ConsoleApplication3.Foo.p[f_Ref] := $ArrayContents[args][0];

                if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals(Helpers.BoogieType.Ref))
                {
                    stmts.Add(Expression.AssumeInverseRelationUnionAndPrimitiveType(expr));
                    //sb.AppendLine(VariableAssignment(heapAccess, PrimitiveType2Union(boogieType, value.Name)));
                    stmts.Add(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr, instTranslator)));
                }
                else
                    stmts.Add(WriteAddr(AddressOf(instanceFieldAccess), expr));
            }

            return stmts;
        }

        public override Expression ReadInstanceField(InstanceFieldAccess instanceFieldAccess)
        {
            if (!Settings.SplitFields)
                return ReadFieldExpression.From(new InstanceField(instanceFieldAccess));
            else
                return this.ReadAddr(AddressOf(instanceFieldAccess));
        }

        public override StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            StatementList stmts = new StatementList();
            var boogieType = Helpers.GetBoogieType(result);

            var readFieldExpr = ReadInstanceField(instanceFieldAccess);

            if (!Settings.SplitFields)
            {
                if (!Helpers.IsBoogieRefType(Helpers.GetBoogieType(result))) // int, bool, real
                {
                    var expr = Expression.Union2PrimitiveType(boogieType, readFieldExpr);
                    stmts.Add(VariableAssignment(result, expr));
                }
                else
                {
                    stmts.Add(VariableAssignment(result, readFieldExpr));
                }
            }
            else
            {
                //p_int:= F$ConsoleApplication3.Holds`1.x[$tmp2];
                if (Helpers.IsGenericField(instanceFieldAccess.Field) &&
                     !boogieType.Equals(Helpers.BoogieType.Ref))
                {
                    stmts.Add(VariableAssignment(result, Expression.Union2PrimitiveType(boogieType, readFieldExpr)));
                }
                else
                    stmts.Add(VariableAssignment(result, readFieldExpr));
            }

            return stmts;
        }

        public override Expression ReadAddr(Addressable addr)
        {
            if (addr is InstanceField instanceField)
            {
                var fieldName = FieldTranslator.GetFieldName(instanceField.Field);
                var instanceName = instanceField.Instance.Name;
                if (Settings.SplitFields)
                {
                    return ReadFieldExpression.From(instanceField);
                } else
                {
                    return ReadHeapExpression.From(instanceField);
                }
            } else if (addr is StaticField staticField)
            {
                return ReadFieldExpression.From(staticField);
            } else if (addr is DotNetVariable v)
            {
                return BoogieVariable.FromDotNetVariable(v.Var);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override Addressable AddressOf(InstanceFieldAccess instanceFieldAccess)
        {
            return new InstanceField(instanceFieldAccess);
        }

        public override Addressable AddressOf(StaticFieldAccess staticFieldAccess)
        {
            return new StaticField(staticFieldAccess);
        }

        public override Addressable AddressOf(IVariable var)
        {
            return new DotNetVariable(var);
        }

        public override StatementList WriteAddr(Addressable addr, Expression expr)
        {
            if (addr is InstanceField instanceField)
            {
                var instanceName = instanceField.Instance;
                var fieldName = FieldTranslator.GetFieldName(instanceField.Field);
                if (Settings.SplitFields)
                {
                    return SplitFieldUpdate.ForKeyValue(instanceField, expr);
                }
                else
                {
                    return HeapUpdate.ForKeyValue(instanceField, expr);
                }

            }
            else if (addr is StaticField staticField)
            {
                var boogieVar = BoogieVariable.From(staticField);
                return VariableAssignment(boogieVar, expr);
            }
            else if (addr is DotNetVariable v)
            {
                return BoogieStatement.VariableAssignment(BoogieVariable.FromDotNetVariable(v.Var), expr);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override StatementList CallReadArrayElement(IVariable resultVariable, Expression array, Expression index, InstructionTranslator instructionTranslator)
        {
            StatementList stmts = new StatementList();
            return CallReadArrayElement(BoogieVariable.FromDotNetVariable(resultVariable), array, index);
        }
    }



    public abstract class BoogieGenerator
    {
        public static BoogieGenerator singleton;

        public static BoogieGenerator Instance()
        {
            if (singleton == null)
            {
                if (!Settings.NewAddrModelling)
                    singleton = new BoogieGeneratorALaBCT();
                else if (!Settings.FastAddrModelling)
                    singleton = new BoogieGeneratorAddr();
                else
                    singleton = new BoogieGeneratorMixed();
            }

            return singleton;
        }

        public Expression ReadAddr(IVariable addr)
        {
            return ReadAddr(AddressOf(addr));
        }

        public abstract Expression ReadAddr(Addressable addr);
        
        public abstract StatementList DeclareLocalVariables(IList<IVariable> variables, Dictionary<string, BoogieVariable> temporalVariables);

        public abstract StatementList AllocLocalVariables(IList<IVariable> variables);

        public abstract String GetFieldDefinition(IFieldReference fieldReference, String fieldName);

        public Addressable AddressOf(IValue value)
        {
            if (value is IReferenceable)
                return AddressOf(value as IReferenceable);
            else if (value is Reference)
                return AddressOf(value as Reference);
            else
                throw new NotImplementedException();

            // we are covering AddressOf for the following types
            // Reference, Dereference, IVariable, InstanceFieldAccess, StaticFieldAccess and ArrayElementAccess
        }

        public Addressable AddressOf(IReferenceable value)
        {
            if (value is InstanceFieldAccess)
            {
                return AddressOf(value as InstanceFieldAccess);
            }
            else if (value is StaticFieldAccess)
            {
                return AddressOf(value as StaticFieldAccess);
            }
            else if (value is IVariable)
            {
                return AddressOf(value as IVariable);
            }
            else if (value is ArrayElementAccess)
            {
                throw new NotImplementedException();
            } else if (value is Dereference dereference)
                return AddressOf(dereference);

            // I should have covered all possible cases
            throw new NotImplementedException();
        }

        public Addressable AddressOf(Reference reference) { return AddressOf(reference.Value); }

        public abstract Addressable AddressOf(InstanceFieldAccess instanceFieldAccess);
        public abstract Addressable AddressOf(StaticFieldAccess staticFieldAccess);
        public abstract Addressable AddressOf(IVariable var);
        
        public abstract StatementList WriteAddr(Addressable addr, Expression value);

        public abstract StatementList AllocAddr(IVariable var);
        public abstract StatementList AllocObject(BoogieVariable var);
        public abstract StatementList AllocObject(IVariable var, InstructionTranslator instTranslator);

        public StatementList WriteStaticField(StaticFieldAccess staticFieldAccess, Expression expr)
        {
            return WriteAddr(AddressOf(staticFieldAccess), expr);
        }
        public StatementList WriteStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            return WriteStaticField(staticFieldAccess, ReadAddr(value));
        }

        public StatementList ReadStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            StatementList stmts = new StatementList();

            var address = AddressOf(staticFieldAccess);

            stmts.Add(VariableAssignment(value, ReadAddr(address)));
            //sb.Append(VariableAssignment(value, fieldName));

            return stmts;
        }
        public StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value, InstructionTranslator instTranslator)
        {
            return WriteInstanceField(instanceFieldAccess, ReadAddr(value), instTranslator);
        }
        public abstract StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression value, InstructionTranslator instTranslator);

        public abstract StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result);
        public abstract Expression ReadInstanceField(InstanceFieldAccess instanceFieldAccess);

        protected List<(Expression, IVariable)> ComputeArguments(MethodCallInstruction instruction, InstructionTranslator instTranslator)
        {
            var copyArgs = new List<(Expression, IVariable)>();

            #region Unespecialize the method
            var unspecializedMethod = Helpers.GetUnspecializedVersion(instruction.Method);
            Contract.Assume(
                unspecializedMethod.Parameters.Count() == instruction.Arguments.Count() ||
                unspecializedMethod.Parameters.Count() + 1 == instruction.Arguments.Count());
            #endregion

            #region Store receiver if there is one
            // Instance methods, passing 'this'
            if (unspecializedMethod.Parameters.Count() != instruction.Arguments.Count())
            {
                var receiver = instruction.Arguments.ElementAt(0);
                if (!Helpers.IsBoogieRefType(receiver)) // not sure when this happens
                {
                    // TODO(rcastano): try to reuse variables.
                    var tempBoogieVar = instTranslator.GetFreshVariable(Helpers.ObjectType(), "$temp_var_");
                    // intended output: String.Format("\t\t{0} := {2}2Union({1});", localVar, receiver, argType)
                    instTranslator.AddBoogie(VariableAssignment(tempBoogieVar, Expression.PrimitiveType2Union(ReadAddr(receiver), instTranslator)));
                    copyArgs.Add((tempBoogieVar, null));
                }
                else
                {
                    copyArgs.Add((ReadAddr(receiver), receiver));
                }
            }
            #endregion

            #region Create the boogie expressions for each argument in the procedure call
            for (int i = 0; i < instruction.Method.Parameters.Count(); ++i)
            {
                // We have to have consistency between the argument expression's types and
                // the procedure parameter's types (all in boogie)
                // we have to be careful when there is a pointer or when we need to cast (typically for generics).

                int arg_i =
                    unspecializedMethod.Parameters.Count() == instruction.Arguments.Count() ?
                    i : i + 1; // +1 depends if there is a this parameter or not

                var paramType = GetBoogieTypeForProcedureParameter(unspecializedMethod.Parameters.ElementAt(i));
                var argType = Helpers.GetBoogieType(instruction.Arguments.ElementAt(arg_i));

                if (!paramType.Equals(argType))
                {
                    #region Cast arguments if they do not match the signature (ie. generics calls, the signature is unespecialized and the arguments are specialized)
                    // TODO(rcastano): try to reuse variables.
                    var tempBoogieVar = instTranslator.GetFreshVariable(Helpers.ObjectType(), "$temp_var_");

                    // intended output: String.Format("\t\t{0} := {2}2Union({1});", localVar, instruction.Arguments.ElementAt(arg_i), argType)
                    instTranslator.AddBoogie(VariableAssignment(tempBoogieVar, Expression.PrimitiveType2Union(ReadAddr(instruction.Arguments.ElementAt(arg_i)), instTranslator)));
                    copyArgs.Add((tempBoogieVar, null));
                    #endregion
                }
                else
                {
                    var iVariable = instruction.Arguments.ElementAt(arg_i);
                    if (iVariable.Type is IManagedPointerType)
                    {
                        #region If the argument is a pointer and the signature expects a pointer, give us the address that the pointer is pointing
                        copyArgs.Add((BoogieVariable.AddressVar(iVariable), iVariable));
                        #endregion
                    }
                    else
                        #region The signature does not expects a pointer, so if there is one we dereference it. 
                        copyArgs.Add((ReadAddr(iVariable), iVariable));
                        #endregion
                }
            }
            #endregion
            return copyArgs;
        }

        // these two abstract methods are used while a MethodCallInstruction is processed
        // depending on the memory model, we may need to hold the result of a boogie procedure call in a temporal variable and then assign it to the intended variable
        public abstract BoogieVariable GetProcedureResultVariable(MethodCallInstruction methodCallInstruction, InstructionTranslator instructionTranslator);
        public abstract StatementList SetProcedureResultVariable(BoogieVariable procedureResult, IVariable finalVariable);

        public abstract Helpers.BoogieType GetBoogieTypeForProcedureParameter(IParameterTypeInformation parameter);

        public StatementList ProcedureCall(BoogieMethod procedure, List<Expression> argumentList, BoogieVariable resultVariable = null)
        {
            List<BoogieVariable> resultArgList = new List<BoogieVariable>();
            if (resultVariable != null)
            {
                resultArgList.Add(resultVariable);
            }
            return BoogieStatement.ProcedureCall(procedure, argumentList, resultArgList, resultVariable);
        }

        public abstract StatementList ProcedureCall(IMethodReference procedure, MethodCallInstruction methodCallInstruction, InstructionTranslator instTranslator, BoogieVariable resultVariable = null);

        public StatementList ProcedureCall(IMethodReference procedure, InstructionTranslator instructionTranslator, MethodCallInstruction methodCallInstruction)
        {
            Contract.Assume(methodCallInstruction != null);
            Contract.Assume(!methodCallInstruction.HasResult || methodCallInstruction.Result != null);
            StatementList stmts = new StatementList();

            IVariable resultVariable = methodCallInstruction.HasResult ? methodCallInstruction.Result : null;

            var result = GetProcedureResultVariable(methodCallInstruction, instructionTranslator); // this variable can be null (valid case)
            stmts.Add(ProcedureCall(procedure, methodCallInstruction, instructionTranslator, result));
            var assignment = SetProcedureResultVariable(result, methodCallInstruction.Result);
            stmts.Add(assignment);

            return stmts;
        }

        public StatementList VariableAssignment(IVariable variableA, Expression expr)
        {
            return WriteAddr(AddressOf(variableA), expr);
        }

        public abstract StatementList VariableAssignment(IVariable variableA, IValue value);

        public StatementList VariableAssignment(BoogieVariable variableA, Expression expr)
        {
            return BoogieStatement.VariableAssignment(variableA, expr);
        }

        public Expression BranchOperationExpression(IVariable op1, IInmediateValue op2, BranchOperation branchOperation)
        {
            BoogieLiteral ifConst = null;
            if (op2 is Constant cons)
            {
                ifConst = BoogieLiteral.FromDotNetConstant(cons);
            }
            var op2Expr = op2 is Constant ? ifConst : ReadAddr(AddressOf(op2));
            return Expression.BranchOperationExpression(ReadAddr(op1), op2Expr, branchOperation);
        }

        public Expression DynamicType(IVariable reference)
        {
            return Expression.DynamicType(ReadAddr(reference));
        }

        public StatementList AssumeDynamicType(IVariable reference, ITypeReference type)
        {
            var typeExpr = Expression.GetNormalizedTypeFunction(type, InstructionTranslator.MentionedClasses);
            var eqExpr = Expression.ExprEquals(Expression.DynamicType(ReadAddr(reference)), typeExpr);
            return BoogieStatement.Assume(eqExpr);
        }

        public StatementList AssumeTypeConstructor(IVariable arg, ITypeReference type)
        {
            return BoogieStatement.AssumeTypeConstructor(ReadAddr(arg), type.ToString());
        }

        public StatementList Assert(IVariable cond)
        {
            Contract.Assume(Helpers.GetBoogieType(cond).Equals(Helpers.BoogieType.Bool));
            return BoogieStatement.Assert(ReadAddr(cond));
        }

        public BoogieStatement Assume(IVariable cond)
        {
            return BoogieStatement.Assume(ReadAddr(cond));
        }

        public abstract Expression NullObject();

        public Expression Subtype(IVariable var, ITypeReference type)
        {
            return Expression.Subtype(ReadAddr(var), type);
        }
        public Expression Subtype(BoogieVariable var, ITypeReference type)
        {
            return Expression.Subtype(var, type);
        }

        public BoogieStatement AssumeArrayLength(Expression array, Expression length)
        {
            Contract.Assume(length.Type.Equals(Helpers.BoogieType.Int));
            var eqLength = Expression.ExprEquals(Expression.ArrayLength(array), length);
            return BoogieStatement.Assume(eqLength);
        }

        public abstract StatementList CallReadArrayElement(IVariable resultVariable, Expression array, Expression index, InstructionTranslator instructionTranslator);

        public StatementList CallReadArrayElement(BoogieVariable result, Expression array, Expression index)
        {
            var l = new List<Expression>();
            l.Add(array);
            l.Add(index);
            return ProcedureCall(BoogieMethod.ReadArrayElement, l, result);
        }

        public StatementList CallWriteArrayElement(Expression array, Expression index, Expression value)
        {
            var l = new List<Expression>();
            l.Add(array);
            l.Add(index);
            l.Add(value);
            return ProcedureCall(BoogieMethod.WriteArrayElement, l);
        }

        public StatementList BoxFrom(IVariable op1, ConvertInstruction convertInstruction, InstructionTranslator instructionTranslator)
        {
            var boogieType = Helpers.GetBoogieType(op1);
            if (Helpers.IsBoogieRefType(boogieType))
                boogieType = Helpers.BoogieType.Union;

            var boxFromProcedure = BoogieMethod.BoxFrom(boogieType);
            var args = new List<IVariable> { op1 };
            var result = convertInstruction.Result;

            if (Settings.NewAddrModelling)
            {
                var tempBoogieVar = instructionTranslator.GetFreshVariable(Helpers.GetBoogieType(result));
                var resultArguments = new List<BoogieVariable> { tempBoogieVar};
                return BoogieStatement.ProcedureCall(boxFromProcedure, args.Select(v => ReadAddr(v)).ToList(), resultArguments, tempBoogieVar);
            }
            else
            {
                var resultBoogieVar = BoogieVariable.FromDotNetVariable(convertInstruction.Result);
                var resultArguments = new List<BoogieVariable> { resultBoogieVar };
                return BoogieStatement.ProcedureCall(boxFromProcedure, args.Select(v => ReadAddr(v)).ToList(), resultArguments, resultBoogieVar);
            }
        }
    }
}
