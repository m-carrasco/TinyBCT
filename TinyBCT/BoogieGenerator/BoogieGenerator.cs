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
        public static Expression PrimitiveType2Union(Expression expr)
        {
            Contract.Assert(!Helpers.IsBoogieRefType(expr.Type));
            return new Expression(Helpers.BoogieType.Union, $"{expr.Type.FirstUppercase()}2Union({expr.Expr})");
        }
        public static Expression Union2PrimitiveType(Helpers.BoogieType boogieType, Expression expr)
        {
            Contract.Assume(!Helpers.IsBoogieRefType(boogieType));
            return new Expression(boogieType, $"Union2{boogieType.FirstUppercase()}({expr.Expr})");
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
            return new Expression(Helpers.GetBoogieType(var.Type), $"-{var.Name}");
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
                Helpers.IsBoogieRefType(constant.Type) &&
                constant.ToString().Equals("null");
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
            int i = usedVarNames.Count - 1;
            var name = String.Empty;
            do {
                ++i;
                name = $"{prefix}{type.FirstUppercase()}_{i}";
            } while (usedVarNames.ContainsKey(name));
            var newBoogieVar = new BoogieVariable(type, name);
            usedVarNames.Add(name, newBoogieVar);
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
        public static BoogieVariable GetAssignedVar(MethodCallInstruction methodCallInstruction)
        {
            Contract.Assume(!Settings.NewAddrModelling);
            Contract.Assume(methodCallInstruction != null);
            var resultVar = methodCallInstruction.Result;
            return new BoogieVariable(Helpers.GetBoogieType(resultVar.Type), resultVar.Name);
        }
        public static BoogieVariable GetAssignedVar(ConvertInstruction convertInstruction)
        {
            Contract.Assume(!Settings.NewAddrModelling);
            var resultVar = convertInstruction.Result;
            return new BoogieVariable(Helpers.GetBoogieType(resultVar.Type), resultVar.Name);
        }
        public static BoogieVariable From(StaticField staticField)
        {
            var fieldName = FieldTranslator.GetFieldName(staticField.Field);
            return new BoogieVariable(Helpers.GetBoogieType(staticField.Type), fieldName);
        }
        static public BoogieVariable FromDotNetVariable(IVariable variable)
        {
            Contract.Requires(!Settings.NewAddrModelling);
            return new BoogieVariable(Helpers.GetBoogieType(variable.Type), BoogieVariable.AdaptNameToBoogie(variable.Name));
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
    public class BoogieParameter : BoogieVariable
    {
        private BoogieParameter(Helpers.BoogieType type, string name) : base(type, name)
        {
            Contract.Requires(IsValidVariableName(name));
        }
        static new public BoogieVariable FromDotNetVariable(IVariable variable)
        {
            Contract.Requires(variable.IsParameter);
            return new BoogieParameter(Helpers.GetBoogieType(variable.Type), AdaptNameToBoogie(variable.Name));
        }
        static public BoogieVariable OutVariable(IParameterDefinition param)
        {
            Contract.Requires(param.IsOut && param.IsByReference);
            return new BoogieParameter(Helpers.GetBoogieType(param.Type), $"{AdaptNameToBoogie(param.Name.Value)}$out");
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
            public Helpers.BoogieType Type { get { return Helpers.GetBoogieType(Key.Type); } }
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
                return Helpers.GetBoogieType(Key.Field.Type);
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
            public Helpers.BoogieType Type { get { return Helpers.GetBoogieType(Key.Field.Type); } }
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
            return BoogieStatement.FromString($"{variableA.Expr} := {expr.Expr};");
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
            return BoogieStatement.FromString($"assume {annotation} {cond.Expr};");
        }

        public static StatementList Assert(Expression cond, string annotation = null)
        {
            annotation = FixAnnotation(annotation);
            Contract.Assume(cond.Type.Equals(Helpers.BoogieType.Bool));
            return BoogieStatement.FromString($"assert {annotation} {cond.Expr};");
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
            Contract.Assume(supportedTypes.Contains(Helpers.GetBoogieType(key.Field.Type)));
            Contract.Assume(Helpers.GetBoogieType(key.Field.Type).Equals(value.Type));
            var Type = Helpers.GetBoogieType(key.Field.Type);
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
            Contract.Assume(supportedTypes.Contains(Helpers.GetBoogieType(key.Field.Type)));
            Contract.Assume(Helpers.GetBoogieType(key.Field.Type).Equals(value.Type) || value.Type.Equals(Helpers.BoogieType.Union));
            var Type = Helpers.GetBoogieType(key.Field.Type);
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
            public Helpers.BoogieType Type { get { return Helpers.GetBoogieType(Key.Type); } }
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

        public override StatementList AllocObject(IVariable var, InstructionTranslator instTranslator)
        {
            var freshVariable = instTranslator.GetFreshVariable(Helpers.GetBoogieType(var.Type));
            var stmts = new StatementList();
            stmts.Add(this.ProcedureCall(BoogieMethod.AllocObject, new List<Expression> { }, freshVariable));
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
        public override StatementList ProcedureCall(IMethodReference procedure, MethodCallInstruction methodCallInstruction, InstructionTranslator instTranslator, BoogieVariable resultVariable = null)
        {
            var argumentList = ComputeArguments(methodCallInstruction, instTranslator);

            int s = procedure.IsStatic ? 0 : 1;
            var resultArguments = new List<BoogieVariable> { resultVariable };

            return ProcedureCall(BoogieMethod.From(procedure), argumentList.Select(v => v.Item1).ToList(), resultVariable);
        }
        public override Expression ReadAddr(IVariable addr)
        {
            return ReadAddr(AddressOf(addr));
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
                var boogieType = Helpers.GetBoogieType(addrExpr.Type);
                return TypedMemoryMapUpdate.ForKeyValue(addrExpr, expr);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override StatementList VariableAssignment(IVariable variableA, Expression expr)
        {
            return WriteAddr(AddressOf(variableA), expr);
        }
        public override StatementList VariableAssignment(IVariable variableA, IValue value)
        {
            Constant cons = value as Constant;
            BoogieLiteral boogieConstant = null;
            if (cons != null)
            {
                boogieConstant = BoogieLiteral.FromDotNetConstant(cons);
            }


            var boogieType = Helpers.GetBoogieType(variableA.Type);

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
                
            } else if (value is IVariable)
            {
                return WriteAddr(AddressOf(variableA), ValueOfVariable(value as IVariable));
            } else if (value is Dereference)
            {
                var dereference = value as Dereference;
                // read addr of the reference
                // index that addr into the corresponding 'heap'
                var addr = new AddressExpression(variableA.Type, ReadAddr(dereference.Reference));
                return WriteAddr(AddressOf(variableA), ReadAddr(addr));
            } else if (value is Reference)
            {
                var reference = value as Reference;
                var addr = AddressOf(reference.Value) as AddressExpression;
                Contract.Assume(addr != null);
                return WriteAddr(AddressOf(variableA), addr.Expr);
            }

            Contract.Assert(false);
            // This shouldn't be reachable.
            throw new NotImplementedException();
        }

        public override StatementList AllocLocalVariables(IList<IVariable> variables)
        {
            StatementList stmts = new StatementList();

            foreach (var v in variables)
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
                Addressable paramAddress = AddressOf(paramVariable);

                // boogie generator knows that must fetch paramVariable's address (_x and not x)
                stmts.Add(WriteAddr(paramAddress, boogieParamVariable));

                if (Helpers.GetBoogieType(paramVariable.Type).Equals(Helpers.BoogieType.Object))
                {
                    stmts.Add(BoogieStatement.FromString(String.Format("assume $AllocObject[{0}] == true || {0} != null_object;", paramVariable)));
                } else if (Helpers.GetBoogieType(paramVariable.Type).Equals(Helpers.BoogieType.Addr))
                {
                    stmts.Add(BoogieStatement.FromString(String.Format("assume $AllocAddr[{0}] == true || {0} != null_addr;", paramVariable)));
                }
            }

            return stmts;
        }

        protected override Expression ValueOfVariable(IVariable var)
        {
            return ReadAddr(var);
        }

        // the variable that represents var's address is $_var.name
        /*public override AddressExpression VarAddress(IVariable var)
        {
            return new AddressExpression(var.Type, String.Format("_{0}", var.Name));
        }*/

        public override Addressable AddressOf(InstanceFieldAccess instanceFieldAccess)
        {
            var expr = Expression.LoadInstanceFieldAddr(instanceFieldAccess.Field, ValueOfVariable(instanceFieldAccess.Instance));
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


        public override StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            var fieldAddr = AddressOf(instanceFieldAccess);
            var readValue = ReadAddr(fieldAddr);

            // dependiendo del type (del result?) indexo en el $memoryInt
            if (Helpers.IsGenericField(instanceFieldAccess.Field))
            {
                var boogieType = Helpers.GetBoogieType(result.Type);
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
        
        public override StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression expr)
        {
            StatementList stmts = new StatementList();

            var boogieType = expr.Type;
            if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals(Helpers.BoogieType.Object))
            {
                stmts.Add(AssumeInverseRelationUnionAndPrimitiveType(expr));
                stmts.Add(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr)));
            }
            else
                stmts.Add(WriteAddr(AddressOf(instanceFieldAccess), expr));

            return stmts;
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

        public override StatementList VariableAssignment(IVariable variableA, Expression expr)
        {
            return BoogieStatement.FromString($"{variableA} := {expr.Expr};");
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

            return ProcedureCall(boogieProcedure, argumentList.Select(v => v.Item1).ToList(), resultArguments, resultVariable);
        }

        public override Expression ReadAddr(IVariable var)
        {
            return BoogieVariable.FromDotNetVariable(var);
        }

        public override StatementList AllocAddr(IVariable var)
        {
            return BoogieStatement.Nop;
        }

        public override StatementList AllocObject(IVariable var, InstructionTranslator instTranslator)
        {
            return this.ProcedureCall(BoogieMethod.Alloc, new List<Expression> { }, BoogieVariable.FromDotNetVariable(var));
        }

        // in this memory addressing the value of a variable is the variable itself 
        protected override Expression ValueOfVariable(IVariable var)
        {
            return BoogieVariable.FromDotNetVariable(var);
        }

        public override StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression expr)
        {
            StatementList stmts = new StatementList();

            String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

            //var addr = AddressOf(instanceFieldAccess);
            //var writeAddr = WriteAddr(addr, value);

            if (!Settings.SplitFields)
            {
                if (!Helpers.IsBoogieRefType(expr.Type)) // int, bool, real
                {
                    stmts.Add(AssumeInverseRelationUnionAndPrimitiveType(expr));
                    stmts.Add(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr)));
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
                    stmts.Add(AssumeInverseRelationUnionAndPrimitiveType(expr));
                    //sb.AppendLine(VariableAssignment(heapAccess, PrimitiveType2Union(boogieType, value.Name)));
                    stmts.Add(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr)));
                }
                else
                    stmts.Add(WriteAddr(AddressOf(instanceFieldAccess), expr));
            }

            return stmts;
        }

        public override StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            StatementList stmts = new StatementList();

            String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

            var boogieType = Helpers.GetBoogieType(result.Type);

            if (!Settings.SplitFields)
            {

                if (!Helpers.IsBoogieRefType(result.Type)) // int, bool, real
                {
                    // example: Union2Int(Read(...))
                    var readFieldExpr = ReadFieldExpression.From(new InstanceField(instanceFieldAccess));
                    var expr = Expression.Union2PrimitiveType(boogieType, readFieldExpr);
                    stmts.Add(VariableAssignment(result, expr));
                }
                else
                {
                    var expr = ReadFieldExpression.From(new InstanceField(instanceFieldAccess));
                    stmts.Add(VariableAssignment(result, expr));
                }
            }
            else
            {
                var heapAccess = new InstanceField(instanceFieldAccess);

                //p_int:= F$ConsoleApplication3.Holds`1.x[$tmp2];
                if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals(Helpers.BoogieType.Ref))
                {
                    stmts.Add(VariableAssignment(result, Expression.Union2PrimitiveType(boogieType, this.ReadAddr(heapAccess))));
                }
                else
                    stmts.Add(VariableAssignment(result, this.ReadAddr(heapAccess)));
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
                return VariableAssignment(v.Var, expr);
            }
            else
            {
                throw new NotImplementedException();
            }
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
                else
                    singleton = new BoogieGeneratorAddr();
            }

            return singleton;
        }

        public abstract Expression ReadAddr(IVariable var);

        public abstract Expression ReadAddr(Addressable addr);
        
        public abstract StatementList DeclareLocalVariables(IList<IVariable> variables, Dictionary<string, BoogieVariable> temporalVariables);

        public abstract StatementList AllocLocalVariables(IList<IVariable> variables);

        public Addressable AddressOf(IValue value)
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
            else
                // arrays?
                throw new NotImplementedException();
        }

        public abstract Addressable AddressOf(InstanceFieldAccess instanceFieldAccess);
        public abstract Addressable AddressOf(StaticFieldAccess staticFieldAccess);
        public abstract Addressable AddressOf(IVariable var);
        
        public abstract StatementList WriteAddr(Addressable addr, Expression value);

        public abstract StatementList AllocAddr(IVariable var);
        public abstract StatementList AllocObject(IVariable var, InstructionTranslator instTranslator);

        protected abstract Expression ValueOfVariable(IVariable var);
        
        public StatementList AssumeInverseRelationUnionAndPrimitiveType(Expression expr)
        {
            var p2u = Expression.PrimitiveType2Union(expr);
            var p2u2p = Expression.Union2PrimitiveType(expr.Type, p2u);
            var eq = Expression.ExprEquals(p2u2p, expr);
            return BoogieStatement.Assume(eq);
        }

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
        public StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value)
        {
            return WriteInstanceField(instanceFieldAccess, ReadAddr(value));
        }
        public abstract StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression value);

        public abstract StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result);
        protected List<(Expression, IVariable)> ComputeArguments(MethodCallInstruction instruction, InstructionTranslator instTranslator)
        {
            var copyArgs = new List<(Expression, IVariable)>();

            var unspecializedMethod = Helpers.GetUnspecializedVersion(instruction.Method);
            Contract.Assume(
                unspecializedMethod.Parameters.Count() == instruction.Arguments.Count() ||
                unspecializedMethod.Parameters.Count() + 1 == instruction.Arguments.Count());
            // Instance methods, passing 'this'
            if (unspecializedMethod.Parameters.Count() != instruction.Arguments.Count())
            {
                var iVariable = instruction.Arguments.ElementAt(0);
                copyArgs.Add((ReadAddr(iVariable), iVariable));
            }
            for (int i = 0; i < instruction.Method.Parameters.Count(); ++i)
            {
                int arg_i =
                    unspecializedMethod.Parameters.Count() == instruction.Arguments.Count() ?
                    i : i + 1;
                var paramType = unspecializedMethod.Parameters.ElementAt(i).IsByReference && Settings.NewAddrModelling ? Helpers.BoogieType.Addr : Helpers.GetBoogieType(unspecializedMethod.Parameters.ElementAt(i).Type);
                var argType = Helpers.GetBoogieType(instruction.Arguments.ElementAt(arg_i).Type);
                if (!paramType.Equals(argType))
                {
                    // TODO(rcastano): try to reuse variables.
                    var tempBoogieVar = instTranslator.GetFreshVariable(Helpers.GetBoogieType(Backend.Types.Instance.PlatformType.SystemObject), "$temp_var_");
                    
                    // intended output: String.Format("\t\t{0} := {2}2Union({1});", localVar, instruction.Arguments.ElementAt(arg_i), argType)
                    instTranslator.AddBoogie(VariableAssignment(tempBoogieVar, Expression.PrimitiveType2Union(ReadAddr(instruction.Arguments.ElementAt(arg_i)))));

                    copyArgs.Add((tempBoogieVar, null));
                }
                else
                {
                    var iVariable = instruction.Arguments.ElementAt(arg_i);
                    copyArgs.Add((ReadAddr(iVariable), iVariable));
                }
            }
            return copyArgs;
        }
        public StatementList ProcedureCall(BoogieMethod procedure, List<Expression> argumentList, BoogieVariable resultVariable = null)
        {
            List<BoogieVariable> resultArgList = new List<BoogieVariable>();
            if (resultVariable != null)
            {
                resultArgList.Add(resultVariable);
            }
            return ProcedureCall(procedure, argumentList, resultArgList, resultVariable);
        }
        protected StatementList ProcedureCall(BoogieMethod procedure, List<Expression> argumentList, List<BoogieVariable> resultArguments, BoogieVariable resultVariable = null)
        {
            StatementList stmts = new StatementList();
            var boogieProcedureName = procedure.Name;
            var arguments = String.Join(",", argumentList.Select(v => v.Expr));
            if (resultArguments.Count > 0)
            {
                var resultArgumentsStr = String.Join(",", resultArguments.Select(v => v.Expr));
                stmts.Add(BoogieStatement.FromString(string.Format("call {0} := {1}({2});", resultArgumentsStr, boogieProcedureName, arguments)));
                if (Settings.NewAddrModelling)
                {
                    Contract.Assert(resultArguments.Count == 1);
                    Contract.Assert(resultArguments.Contains(resultVariable));
                }
                return stmts;
            }
            else
                return BoogieStatement.FromString(string.Format("call {0}({1});", boogieProcedureName, arguments));
        }

        public abstract StatementList ProcedureCall(IMethodReference procedure, MethodCallInstruction methodCallInstruction, InstructionTranslator instTranslator, BoogieVariable resultVariable = null);

        public StatementList ProcedureCall(IMethodReference procedure, InstructionTranslator instructionTranslator, MethodCallInstruction methodCallInstruction)
        {
            Contract.Assume(methodCallInstruction != null);
            StatementList stmts = new StatementList();
            IVariable resultVariable = null;
            if (methodCallInstruction.HasResult)
            {
                Contract.Assume(methodCallInstruction.Result != null);
                resultVariable = methodCallInstruction.Result;
            }
            if (Settings.NewAddrModelling) {
                BoogieVariable boogieResVar = null;
                if (methodCallInstruction.HasResult)
                {
                    boogieResVar = instructionTranslator.GetFreshVariable(Helpers.GetBoogieType(resultVariable.Type));
                }
                stmts.Add(ProcedureCall(procedure, methodCallInstruction, instructionTranslator, boogieResVar));
                if (methodCallInstruction.HasResult)
                {
                    stmts.Add(WriteAddr(AddressOf(resultVariable), boogieResVar));
                }
                return stmts;
            } else
            {
                if (methodCallInstruction.HasResult)
                {
                    return ProcedureCall(procedure, methodCallInstruction, instructionTranslator, BoogieVariable.GetAssignedVar(methodCallInstruction));
                } else
                {
                    return ProcedureCall(procedure, methodCallInstruction, instructionTranslator);
                }
                
            }
        }

        public abstract StatementList VariableAssignment(IVariable variableA, Expression expr);
        public abstract StatementList VariableAssignment(IVariable variableA, IValue value);

        public StatementList VariableAssignment(BoogieVariable variableA, Expression expr)
        {
            return BoogieStatement.VariableAssignment(variableA, expr);
        }

        public StatementList HavocResult(DefinitionInstruction instruction)
        {
            return BoogieStatement.FromString(String.Format("havoc {0};", instruction.Result));
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

        public StatementList Goto(string label)
        {
            return BoogieStatement.FromString(String.Format("\t\tgoto {0};", label));
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
            return AssumeTypeConstructor(ReadAddr(arg).Expr, type.ToString());
        }

        public StatementList AssumeTypeConstructor(string arg, string type)
        {
            return BoogieStatement.FromString(String.Format("assume $TypeConstructor($DynamicType({0})) == T${1};", arg, type));
        }

        public StatementList Assert(IVariable cond)
        {
            Contract.Assume(Helpers.GetBoogieType(cond.Type).Equals(Helpers.BoogieType.Bool));
            return BoogieStatement.Assert(ReadAddr(cond));
        }

        public StatementList LocationAttributes(string sourceFile, int sourceLine)
        {
            return BoogieStatement.FromString($"assert {{:sourceFile \"{sourceFile}\"}} {{:sourceLine \"{sourceLine}\"}} true;");
        }
        public BoogieStatement Assume(IVariable cond)
        {
            return BoogieStatement.Assume(ReadAddr(cond));
        }

        public abstract Expression NullObject();

        public BoogieBlock If(Expression condition, StatementList body)
        {
            BoogieBlock stmts = new BoogieBlock();

            stmts.Add(BoogieStatement.FromString(String.Format("if ({0})", condition.Expr)));
            stmts.Add(BoogieStatement.FromString("{"));
            stmts.Add(body);
            stmts.Add(BoogieStatement.FromString("}"));

            return stmts;
        }
        // TODO(rcastano): This should have its own type, something along the lines
        // of ElseBlock.
        public BoogieBlock Else( StatementList body)
        {
            BoogieBlock stmts = new BoogieBlock();

            stmts.Add(BoogieStatement.FromString("else"));
            stmts.Add(BoogieStatement.FromString("{"));
            stmts.Add(body);
            stmts.Add(BoogieStatement.FromString("}"));

            return stmts;
        }


        public BoogieBlock ElseIf(Expression condition, StatementList body)
        {
            BoogieBlock stmts = new BoogieBlock();

            stmts.Add(BoogieStatement.FromString(String.Format("else if ({0})", condition.Expr)));
            stmts.Add(BoogieStatement.FromString("{"));
            stmts.Add(body);
            stmts.Add(BoogieStatement.FromString("}"));

            return stmts;
        }

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
        
        public StatementList CallReadArrayElement(IVariable resultVariable, Expression array, Expression index, InstructionTranslator instructionTranslator)
        {
            StatementList stmts = new StatementList();
            if (Settings.NewAddrModelling)
            {
                BoogieVariable boogieResVar = null;
                if (resultVariable != null)
                {
                    boogieResVar = instructionTranslator.GetFreshVariable(Helpers.GetBoogieType(resultVariable.Type));
                }
                stmts.Add(CallReadArrayElement(boogieResVar, array, index));
                if (resultVariable != null)
                {
                    stmts.Add(WriteAddr(AddressOf(resultVariable), boogieResVar));
                }
                return stmts;
            }
            else
            {
                string resultVariableStr = null;
                if (resultVariable != null)
                {
                    resultVariableStr = resultVariable.Name;
                }
                return CallReadArrayElement(BoogieVariable.FromDotNetVariable(resultVariable), array, index);
            }
        }
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

        public StatementList Return()
        {
            return BoogieStatement.FromString("return;");
        }

        public StatementList BoxFrom(IVariable op1, ConvertInstruction convertInstruction, InstructionTranslator instructionTranslator)
        {
            var boogieType = Helpers.GetBoogieType(op1.Type);
            if (Helpers.IsBoogieRefType(boogieType))
                boogieType = Helpers.BoogieType.Union;

            var boxFromProcedure = BoogieMethod.BoxFrom(boogieType);
            var args = new List<IVariable> { op1 };
            var result = convertInstruction.Result;

            if (Settings.NewAddrModelling)
            {
                var tempBoogieVar = instructionTranslator.GetFreshVariable(Helpers.GetBoogieType(result.Type));
                var resultArguments = new List<BoogieVariable> { tempBoogieVar};
                return ProcedureCall(boxFromProcedure, args.Select(v => ReadAddr(v)).ToList(), resultArguments, tempBoogieVar);
            }
            else
            {
                var resultBoogieVar = BoogieVariable.GetAssignedVar(convertInstruction);
                var resultArguments = new List<BoogieVariable> { resultBoogieVar };
                return ProcedureCall(boxFromProcedure, args.Select(v => ReadAddr(v)).ToList(), resultArguments, resultBoogieVar);
            }
        }
    }
}
