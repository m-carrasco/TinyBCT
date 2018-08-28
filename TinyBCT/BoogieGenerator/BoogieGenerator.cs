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
            return PrimitiveType2Union(expr.Type, expr.Expr);
        }
        public static Expression PrimitiveType2Union(IVariable value)
        {
            Contract.Assert(!Helpers.IsBoogieRefType(value.Type));
            var boogieType = Helpers.GetBoogieType(value.Type);
            return PrimitiveType2Union(boogieType, value.ToString());
        }
        public static Expression PrimitiveType2Union(Helpers.BoogieType boogieType, string value)
        {
            return new Expression(Helpers.BoogieType.Union, $"{boogieType.FirstUppercase()}2Union({value})");
        }
        public static Expression Union2PrimitiveType(Helpers.BoogieType boogieType, string value)
        {
            return new Expression(boogieType, $"Union2{boogieType.FirstUppercase()}({value})");
        }
        public static Expression Union2PrimitiveType(Helpers.BoogieType boogieType, Expression expr)
        {
            return new Expression(boogieType, $"Union2{boogieType.FirstUppercase()}({expr.Expr})");
        }
        public static Expression As(Expression expr, ITypeReference arg2)
        {
            var type = Helpers.GetNormalizedTypeFunction(arg2, InstructionTranslator.MentionedClasses);
            // TODO(rcastano): Fix for generics
            return new Expression(Helpers.BoogieType.Ref, $"$As({expr.Expr}, {type})");
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
        public static Expression NotEquals(Expression op1, Expression op2)
        {
            Contract.Assume(op1.Type.Equals(op2.Type));
            return new Expression(Helpers.BoogieType.Bool, $"({op1.Expr} != {op2.Expr})");
        }
        public static Expression LessThan(Expression op1, int index)
        {
            Contract.Assume(op1.Type.Equals(Helpers.BoogieType.Int));
            return new Expression(Helpers.BoogieType.Bool, $"({op1.Expr} < {index})");
        }
        static public Expression FromDotNetVariable(IVariable variable)
        {
            Contract.Requires(!variable.IsParameter);
            return new Expression(Helpers.GetBoogieType(variable.Type), BoogieVariable.AdaptNameToBoogie(variable.Name));
        }

        public override string ToString()
        {
            throw new Exception("This method should not be called, use property Expr.");
        }

        internal static Expression TEMPORARY_HACK_FromAddress(Addressable addr)
        {
            if (addr is AddressExpression addrExpr)
            {
                return new Expression(Helpers.GetBoogieType(addrExpr.Type), addrExpr.Expr);
            }
            throw new NotImplementedException();
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
        public static BoogieLiteral FromString(Constant constant)
        {
            return new BoogieLiteral(Helpers.BoogieType.Ref, Strings.fixStringLiteral(constant));
        }
        public static bool IsNullConst(Constant constant)
        {
            return
                constant != null &&
                constant.Type.Equals(Backend.Types.Instance.PlatformType.SystemObject) &&
                constant.ToString().Equals("null");
        }
        public static BoogieLiteral FromBool(Constant constant)
        {
            Contract.Assume(
                constant != null &&
                Helpers.GetBoogieType(constant.Type).Equals(Helpers.BoogieType.Bool));
            return new BoogieLiteral(Helpers.BoogieType.Bool, constant.ToString());
        }
        public static BoogieLiteral FromNull(Constant constant)
        {
            Contract.Assume(IsNullConst(constant));
            // The boogie type varies depending on the address encoding.
            var boogieType = Helpers.GetBoogieType(constant.Type);
            return new BoogieLiteral(boogieType, constant.ToString());
        }
        

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
        public static BoogieVariable GetTempVar(Helpers.BoogieType type, Dictionary<string, Helpers.BoogieType> usedVarNames)
        {
            int i = usedVarNames.Count-1;
            var name = String.Empty;
            do {
                ++i;
                name = $"$temp_{type.FirstUppercase()}_{i}";
            } while (usedVarNames.ContainsKey(name));
            usedVarNames.Add(name, type);
            return new BoogieVariable(type, name);
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


        public static readonly BoogieVariable ExceptionVar = new BoogieVariable(Helpers.BoogieType.Ref, "$Exception");
        public static readonly BoogieVariable ExceptionTypeVar = new BoogieVariable(Helpers.BoogieType.Ref, "$ExceptionType");
        public static readonly BoogieVariable ExceptionInCatchHandlerVar = new BoogieVariable(Helpers.BoogieType.Ref, "$ExceptionInCatchHandler");
        public static readonly BoogieVariable ExceptionInCatchHandlerTypeVar = new BoogieVariable(Helpers.BoogieType.Ref, "$ExceptionInCatchHandlerType");
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
            public string Expr { get { return $"{ReadFunction}({MemoryMap}, {Key.Expr})"; } }
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

    public class BoogieStatement {
        protected BoogieStatement(string stmt)
        {
            Stmt = stmt;
        }
        public readonly string Stmt;
    }
    public class MemoryMapUpdate : BoogieStatement
    {
        private MemoryMapUpdate(TemporaryClassToBuildExpression temp) : base(temp.Stmt) { }
        private class TemporaryClassToBuildExpression
        {
            public TemporaryClassToBuildExpression(AddressExpression key, string value)
            {
                Key = key;
                Value = value;
            }
            public string Stmt { get { return $"{MemoryMap} := {WriteFunction}({MemoryMap}, {Key.Expr}, {Value});"; } }
            public Helpers.BoogieType Type { get { return Helpers.GetBoogieType(Key.Type); } }
            private AddressExpression Key { get; }
            // TODO(rcastano): This should be "Expression Value"
            private string Value { get; }
            private string MemoryMap { get { return $"$memory{Type.FirstUppercase()}"; } }
            private string WriteFunction { get { return $"Write{Type.FirstUppercase()}"; } }
        }
        public static MemoryMapUpdate ForKeyValue(AddressExpression key, Expression value)
        {
            return ForKeyValue(key, value.Expr);
        }
        public static MemoryMapUpdate ForKeyValue(AddressExpression key, string value)
        {
            var supportedTypes = new HashSet<Helpers.BoogieType> { Helpers.BoogieType.Int, Helpers.BoogieType.Bool, Helpers.BoogieType.Object, Helpers.BoogieType.Real, Helpers.BoogieType.Addr };
            Contract.Assume(supportedTypes.Contains(Helpers.GetBoogieType(key.Type)));
            // TODO(rcastano): re-add later when value is of the right type (Expression)
            // Contract.Assume(Helpers.GetBoogieType(key.Type).Equals(value.Type));
            return new MemoryMapUpdate(new TemporaryClassToBuildExpression(key, value));
        }
    }

    public abstract class Addressable
    {

    }
    // TODO(rcastano): Change name. An instance of AddressExpression will not actually be an expression.
    // expr field is an expression that in boogie will be typed as Addr
    public class AddressExpression : Addressable
    {
        public AddressExpression(ITypeReference t, string e)
        {
            Type = t;
            Expr = e;
        }

        public override string ToString()
        {
            return Expr;
        }

        // make them only readable
        public ITypeReference Type;
        public string Expr;
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
        public override string AllocAddr(IVariable var)
        {
            return this.ProcedureCall("AllocAddr", new List<string>(), AddressOf(var).ToString());
        }

        public override string AllocObject(IVariable var, InstructionTranslator instTranslator)
        {
            var freshVariable = instTranslator.GetFreshVariable(Helpers.GetBoogieType(var.Type));
            var sb = new StringBuilder();
            sb.AppendLine(this.ProcedureCall("AllocObject", new List<string>(), freshVariable));
            sb.AppendLine(this.VariableAssignment(var, freshVariable));
            return sb.ToString();
        }

        // hides implementation in super class
        //public new string VariableAssignment(string variableA, string expr)
        //{
        //    return string.Format("{0} := {1};", variableA, expr);
        //}

        public override string DeclareLocalVariables(IList<IVariable> variables, ISet<IVariable> assignedInMethodCalls, Dictionary<string, Helpers.BoogieType> temporalVariables)
        {
            StringBuilder sb = new StringBuilder();
            variables.Select(v =>
                    String.Format("\tvar {0} : {1};", AddressOf(v), "Addr")
            ).ToList().ForEach(str => sb.AppendLine(str));

            assignedInMethodCalls.Select(v =>
                    String.Format("\tvar {0} : {1};", v, Helpers.GetBoogieType(v.Type))
            ).ToList().ForEach(str => sb.AppendLine(str));

            temporalVariables.Select(kv =>
                    String.Format("\tvar {0} : {1};", kv.Key, kv.Value)
            ).ToList().ForEach(str => sb.AppendLine(str));


            return sb.ToString();
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

        public override string WriteAddr(Addressable addr, Expression expr)
        {
            if (addr is AddressExpression)
            {
                var addrExpr = addr as AddressExpression;
                var boogieType = Helpers.GetBoogieType(addrExpr.Type);
                return MemoryMapUpdate.ForKeyValue(addrExpr, expr).Stmt;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override string VariableAssignment(IVariable variableA, Expression expr)
        {
            return WriteAddr(AddressOf(variableA), expr);
        }
        public override string VariableAssignment(IVariable variableA, IValue value)
        {
            Constant cons = value as Constant;
            BoogieLiteral boogieConstant = null;
            if (cons != null)
            {
                if (cons.Value is Single || cons.Value is Double || cons.Value is Decimal || TypeHelper.IsPrimitiveInteger(cons.Type))
                {
                    boogieConstant = BoogieLiteral.Numeric(cons);
                } else if (Helpers.GetBoogieType(cons.Type).Equals(Helpers.BoogieType.Bool))
                {
                    boogieConstant = BoogieLiteral.FromBool(cons);
                } else if (BoogieLiteral.IsNullConst(cons))
                {
                    boogieConstant = BoogieLiteral.FromNull(cons);
                }
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
                var addr = new AddressExpression(variableA.Type, ReadAddr(dereference.Reference).Expr);
                return WriteAddr(AddressOf(variableA), ReadAddr(addr));
            } else if (value is Reference)
            {
                var reference = value as Reference;
                var addr = AddressOf(reference.Value);
                Expression expr = Expression.TEMPORARY_HACK_FromAddress(addr);
                return WriteAddr(AddressOf(variableA), expr);
            }

            Contract.Assert(false);

            return "";
        }

        public override string AllocLocalVariables(IList<IVariable> variables)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var v in variables)
                sb.AppendLine(AllocAddr(v));

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
                sb.AppendLine(WriteAddr(paramAddress, boogieParamVariable));

                if (Helpers.GetBoogieType(paramVariable.Type).Equals(Helpers.BoogieType.Object))
                {
                    sb.AppendLine(String.Format("assume $AllocObject[{0}] == true || {0} != null_object;", paramVariable));
                } else if (Helpers.GetBoogieType(paramVariable.Type).Equals(Helpers.BoogieType.Addr))
                {
                    sb.AppendLine(String.Format("assume $AllocAddr[{0}] == true || {0} != null_addr;", paramVariable));
                }
            }

            return sb.ToString();
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
            String map = FieldTranslator.GetFieldName(instanceFieldAccess.Field);
            return new AddressExpression(instanceFieldAccess.Field.Type, string.Format("LoadInstanceFieldAddr({0}, {1})", map, ValueOfVariable(instanceFieldAccess.Instance).Expr));
        }

        public override Addressable AddressOf(StaticFieldAccess staticFieldAccess)
        {
            String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
            var address = new AddressExpression(staticFieldAccess.Field.Type, fieldName);
            return address;
        }

        public override Addressable AddressOf(IVariable var)
        {
            return new AddressExpression(var.Type, String.Format("_{0}", var.Name));
        }


        public override string ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            var fieldAddr = AddressOf(instanceFieldAccess);
            var readValue = ReadAddr(fieldAddr);

            // dependiendo del type (del result?) indexo en el $memoryInt
            if (Helpers.IsGenericField(instanceFieldAccess.Field))
            {
                var boogieType = Helpers.GetBoogieType(result.Type);
                if (!boogieType.Equals(Helpers.BoogieType.Object))
                {
                    return VariableAssignment(result, Expression.Union2PrimitiveType(boogieType, readValue.Expr));
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
        
        public override string WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression expr)
        {
            StringBuilder sb = new StringBuilder();

            var boogieType = expr.Type;
            if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals(Helpers.BoogieType.Object))
            {
                sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(expr));
                sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr)));
            }
            else
                sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), expr));

            return sb.ToString();
        }
    }

    public class BoogieGeneratorALaBCT : BoogieGenerator
    {
        public override Expression NullObject()
        {
            return BoogieLiteral.NullRef;
        }

        public override string VariableAssignment(IVariable variableA, IValue value)
        {
            Constant cons = value as Constant;
            if (cons != null && (cons.Value is Single || cons.Value is Double || cons.Value is Decimal))
            {   
                return VariableAssignment(variableA, BoogieLiteral.Numeric(cons));
            } else if (value is Dereference)
            {
                var dereference = value as Dereference;
                return VariableAssignment(variableA, dereference.Reference);
            }

            return VariableAssignment(variableA.ToString(), value.ToString());
        }

        public override string VariableAssignment(IVariable variableA, Expression expr)
        {
            return $"{variableA} := {expr.Expr};";
        }

        public override string AllocLocalVariables(IList<IVariable> variables)
        {
            return String.Empty;
        }

        public override string DeclareLocalVariables(IList<IVariable> variables, ISet<IVariable> assignedInMethodCalls, Dictionary<string, Helpers.BoogieType> temporalVariables)
        {
            StringBuilder sb = new StringBuilder();

            variables.Where(v => !v.IsParameter)
            .Select(v =>
                    String.Format("\tvar {0} : {1};", v.Name, Helpers.GetBoogieType(v.Type))
            ).ToList().ForEach(str => sb.AppendLine(str));

            temporalVariables.Select(kv =>
                    String.Format("\tvar {0} : {1};", kv.Key, kv.Value)
            ).ToList().ForEach(str => sb.AppendLine(str));

            return sb.ToString();
        }

        public override Expression ReadAddr(IVariable var)
        {
            return BoogieVariable.FromDotNetVariable(var);
        }

        public override string AllocAddr(IVariable var)
        {
            return String.Empty;
        }

        public override string AllocObject(IVariable var, InstructionTranslator instTranslator)
        {
            return this.ProcedureCall("Alloc", new List<string>(), AddressOf(var).ToString());
        }

        // in this memory addressing the value of a variable is the variable itself 
        protected override Expression ValueOfVariable(IVariable var)
        {
            return BoogieVariable.FromDotNetVariable(var);
        }

        public override string WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression expr)
        {
            StringBuilder sb = new StringBuilder();

            String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

            //var addr = AddressOf(instanceFieldAccess);
            //var writeAddr = WriteAddr(addr, value);

            if (!Settings.SplitFields)
            {
                if (!Helpers.IsBoogieRefType(expr.Type)) // int, bool, real
                {
                    sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(expr));
                    sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr)));
                    //sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, PrimitiveType2Union(Helpers.GetBoogieType(value.Type), value.Name)));
                }
                else
                {
                    sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), expr));
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
                    sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(expr));
                    //sb.AppendLine(VariableAssignment(heapAccess, PrimitiveType2Union(boogieType, value.Name)));
                    sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr)));
                }
                else
                    sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), expr));
            }

            return sb.ToString();
        }

        public override string ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            StringBuilder sb = new StringBuilder();

            String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

            var boogieType = Helpers.GetBoogieType(result.Type);

            if (!Settings.SplitFields)
            {

                if (!Helpers.IsBoogieRefType(result.Type)) // int, bool, real
                {
                    // example: Union2Int(Read(...))
                    var expr = Expression.Union2PrimitiveType(boogieType, String.Format("Read($Heap,{0},{1})", instanceFieldAccess.Instance, fieldName));
                    sb.AppendLine(VariableAssignment(result, expr));
                }
                else
                {
                    var expr = ReadFieldExpression.From(new InstanceField(instanceFieldAccess));
                    sb.AppendLine(VariableAssignment(result, expr));
                }
            }
            else
            {
                var heapAccess = new InstanceField(instanceFieldAccess);

                //p_int:= F$ConsoleApplication3.Holds`1.x[$tmp2];
                if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals(Helpers.BoogieType.Ref))
                {
                    sb.AppendLine(VariableAssignment(result, Expression.Union2PrimitiveType(boogieType, this.ReadAddr(heapAccess))));
                }
                else
                    sb.AppendLine(VariableAssignment(result, this.ReadAddr(heapAccess)));
            }

            return sb.ToString();
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

        public override string WriteAddr(Addressable addr, Expression expr)
        {
            if (addr is InstanceField instanceField)
            {
                var instanceName = instanceField.Instance;
                var fieldName = FieldTranslator.GetFieldName(instanceField.Field);
                if (Settings.SplitFields)
                {
                    return $"{fieldName}[{instanceName}] := {expr.Expr};";
                }
                else
                {
                    // return $"Read($Heap,{instanceName},{fieldName})";
                    return $"$Heap := Write($Heap, {instanceName}, {fieldName}, {expr.Expr});";
                }

            }
            else if (addr is StaticField staticField)
            {
                var fieldName = FieldTranslator.GetFieldName(staticField.Field);
                return VariableAssignment(fieldName, expr.Expr);
            }
            else if (addr is DotNetVariable v)
            {
                var varName = v.Var.Name;
                return VariableAssignment(varName, expr.Expr);
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
        
        public abstract string DeclareLocalVariables(IList<IVariable> variables, ISet<IVariable> assignedInMethodCalls, Dictionary<string, Helpers.BoogieType> temporalVariables);

        public abstract string AllocLocalVariables(IList<IVariable> variables);

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
        
        public abstract string WriteAddr(Addressable addr, Expression value);

        public abstract string AllocAddr(IVariable var);
        public abstract string AllocObject(IVariable var, InstructionTranslator instTranslator);

        protected abstract Expression ValueOfVariable(IVariable var);

        public string AssumeInverseRelationUnionAndPrimitiveType(string variable, Helpers.BoogieType boogieType)
        {
            var e1 = string.Format("{0}2Union({1})", boogieType.FirstUppercase(), variable);
            return string.Format("assume Union2{0}({1}) == {2};", boogieType.FirstUppercase(), e1, variable);
        }

        public string AssumeInverseRelationUnionAndPrimitiveType(IVariable variable)
        {
            var boogieType = Helpers.GetBoogieType(variable.Type);

            return AssumeInverseRelationUnionAndPrimitiveType(variable.ToString(), boogieType);
        }
        public string AssumeInverseRelationUnionAndPrimitiveType(Expression expr)
        {
            return AssumeInverseRelationUnionAndPrimitiveType(expr.Expr, expr.Type);
        }

        public string WriteStaticField(StaticFieldAccess staticFieldAccess, Expression expr)
        {
            return WriteAddr(AddressOf(staticFieldAccess), expr);
        }
        public string WriteStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            return WriteStaticField(staticFieldAccess, ReadAddr(value));
        }

        public string ReadStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            StringBuilder sb = new StringBuilder();

            var address = AddressOf(staticFieldAccess);

            sb.Append(VariableAssignment(value, ReadAddr(address)));
            //sb.Append(VariableAssignment(value, fieldName));

            return sb.ToString();
        }
        public string WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value)
        {
            return WriteInstanceField(instanceFieldAccess, ReadAddr(value));
        }
        public abstract string WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression value);

        public abstract string ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result);
        
        private string ProcedureCall(IMethodReference procedure, List<IVariable> argumentList, string resultVariableStr = null)
        {
            StringBuilder sb = new StringBuilder();
            var boogieProcedureName = Helpers.GetMethodName(procedure);

            int s = procedure.IsStatic ? 0 : 1;
            var resultArguments = new List<String>();

            // using inheritance this should be moved to the boogie generator a la bct
            if (!Settings.NewAddrModelling)
            {
                // check behavior with out arguments
                var referencedIndexes = procedure.Parameters.Where(p => p.IsByReference).Select(p => p.Index + s);

                foreach (var i in referencedIndexes)
                    resultArguments.Add(ValueOfVariable(argumentList[i]).Expr);
            }

            if (resultVariableStr != null)
            {
                resultArguments.Add(resultVariableStr);
            }

            var arguments = String.Join(",", argumentList.Select(v => ReadAddr(v).Expr));
            if (resultArguments.Count > 0)
            {
                sb.Append(string.Format("call {0} := {1}({2});", String.Join(",", resultArguments), boogieProcedureName, arguments));
                if (Settings.NewAddrModelling)
                {
                    Contract.Assert(resultArguments.Count == 1);
                    Contract.Assert(resultArguments.Contains(resultVariableStr));
                }
                return sb.ToString();
            }
            else
                return string.Format("call {0}({1});", boogieProcedureName, arguments);
        }
        public string ProcedureCall(IMethodReference procedure, List<IVariable> argumentList, BoogieVariable resultVariable = null)
        {
            string resultVariableStr = null;
            if (resultVariable != null)
            {
                resultVariableStr = resultVariable.Expr;
            }
            return ProcedureCall(procedure, argumentList, resultVariableStr);
        }

        public string ProcedureCall(IMethodReference procedure, List<IVariable> argumentList, InstructionTranslator instructionTranslator, IVariable resultVariable = null)
        {
            StringBuilder sb = new StringBuilder();
            
            if (Settings.NewAddrModelling) {
                BoogieVariable boogieResVar = null;
                if (resultVariable != null)
                {
                    boogieResVar = instructionTranslator.GetFreshVariable(Helpers.GetBoogieType(resultVariable.Type));
                }
                sb.AppendLine(ProcedureCall(procedure, argumentList, boogieResVar));
                if (resultVariable != null)
                {
                    sb.AppendLine(WriteAddr(AddressOf(resultVariable), boogieResVar));
                }
                return sb.ToString();
            } else
            {
                string resultVariableStr = null;
                if (resultVariable != null)
                {
                    resultVariableStr = resultVariable.Name;
                }
                return ProcedureCall(procedure, argumentList, resultVariableStr);
            }
        }

        public string ProcedureCall(string boogieProcedureName, List<string> argumentList, BoogieVariable resultVariable)
        {
            var resultArguments = new List<string>();
            if (resultVariable != null)
                resultArguments.Add(resultVariable.Expr);

            return ProcedureCall(boogieProcedureName, argumentList, resultArguments);
        }
        public string ProcedureCall(string boogieProcedureName, List<string> argumentList, string resultVariable = null)
        {
            var resultArguments = new List<string>();
            if (resultVariable != null)
                resultArguments.Add(resultVariable);

            return ProcedureCall(boogieProcedureName, argumentList, resultArguments);
        }

        public string ProcedureCall(string boogieProcedureName, List<string> argumentList, IList<string> resultVariables )
        {
            var arguments = String.Join(",", argumentList);
            if (resultVariables.Count > 0)
                return string.Format("call {0} := {1}({2});", String.Join(",",resultVariables), boogieProcedureName, arguments);
            else
                return string.Format("call {0}({1});", boogieProcedureName, arguments);
        }

        public abstract string VariableAssignment(IVariable variableA, Expression expr);
        public abstract string VariableAssignment(IVariable variableA, IValue value);

        public string VariableAssignment(BoogieVariable variableA, Expression expr)
        {
            return $"{variableA.Expr} := {expr.Expr};";
        }

        public string VariableAssignment(string variableA, string expr)
        {
            return string.Format("{0} := {1};", variableA, expr);
        }

        public string HavocResult(DefinitionInstruction instruction)
        {
            return String.Format("havoc {0};", instruction.Result);
        }

        public string BranchOperationExpression(IVariable op1, IInmediateValue op2, BranchOperation branchOperation)
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

            return BranchOperationExpression(ReadAddr(op1).Expr, op2 is Constant ? op2.ToString() : ReadAddr(AddressOf(op2)).Expr, operation);
        }

        public string BranchOperationExpression(string op1, string op2, string operation)
        {
            return string.Format("{0} {1} {2}", op1, operation, op2);
        }

        public string UnaryOperationExpression(IVariable var, UnaryOperation unaryOperation)
        {
            Contract.Assume(IsSupportedUnaryOperation(unaryOperation));
            // currently just neg is supported, equivalent to * -1
            return String.Format("-{0}", var.Name);
        }
        public bool IsSupportedUnaryOperation(UnaryOperation op)
        {
            return UnaryOperation.Neg.Equals(op);
        }

    public string Goto(string label)
        {
            return String.Format("\t\tgoto {0};", label);
        }

        public string DynamicType(IVariable reference)
        {
            return DynamicType(reference.Name);
        }

        public string DynamicType(string reference)
        {
            return String.Format("$DynamicType({0})", reference);
        }

        public string AssumeDynamicType(IVariable reference, ITypeReference type)
        {
            return AssumeDynamicType(ReadAddr(reference).Expr, type);
        }

        public string AssumeDynamicType(string name, ITypeReference type)
        {
            return String.Format("assume $DynamicType({0}) == {1};", name, Helpers.GetNormalizedTypeFunction(type, InstructionTranslator.MentionedClasses));
        }

        public string TypeConstructor(string type)
        {
            return String.Format("$TypeConstructor({0})", type);
        }

        public string AssumeTypeConstructor(IVariable arg, ITypeReference type)
        {
            return AssumeTypeConstructor(ReadAddr(arg).Expr, type.ToString());
        }

        public string AssumeTypeConstructor(string arg, string type)
        {
            return String.Format("assume $TypeConstructor($DynamicType({0})) == T${1};", arg, type);
        }

        public string Assert(IVariable cond)
        {
            return Assert(ReadAddr(cond).Expr);
        }

        public string Assert(string cond)
        {
            return String.Format("assert {0};", cond);
        }

        public string Assume(IVariable cond)
        {
            return Assume(ReadAddr(cond).Expr);
        }

        public string Assume(string cond)
        {
            return String.Format("assume {0};", cond);
        }

        public abstract Expression NullObject();

        public string If(string condition, string body)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(String.Format("if ({0})", condition));
            sb.AppendLine("{");
            sb.AppendLine(body);
            sb.AppendLine("}");

            return sb.ToString();
        }

        public string Else( string body)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("else");
            sb.AppendLine("{");
            sb.AppendLine(body);
            sb.AppendLine("}");

            return sb.ToString();
        }


        public string ElseIf(string condition, string body)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(String.Format("else if ({0})", condition));
            sb.AppendLine("{");
            sb.AppendLine(body);
            sb.AppendLine("}");

            return sb.ToString();
        }

        public string Subtype(IVariable var, ITypeReference type)
        {
            return Subtype(var.Name, type);
        }

        public string Subtype(string var, ITypeReference type)
        {
            return string.Format("$Subtype({0}, {1})", var, Helpers.GetNormalizedTypeFunction(type, InstructionTranslator.MentionedClasses));
        }

        public string AssumeArrayLength(IVariable array, string length)
        {
            return Assume(String.Format("{0} == {1}", ArrayLength(array), length));
        }

        public string ArrayLength(IVariable arr)
        {
            return String.Format("$ArrayLength({0})", arr.Name);
        }

        public string ArrayLength(string arr)
        {
            return String.Format("$ArrayLength({0})", arr);
        }

        public string ReadArrayElement(string array, string index)
        {
            return string.Format("$ReadArrayElement({0}, {1})", array, index);
        }

        public string ReadArrayElement(IVariable array, IVariable index)
        {
            return ReadArrayElement(array.Name, index.Name);
        }

        public string CallReadArrayElement(string result, string array, string index)
        {
            var l = new List<string>();
            l.Add(array);
            l.Add(index);
            return ProcedureCall("$ReadArrayElement", l, result);
        }

        public string CallReadArrayElement(IVariable result, IVariable array, IVariable index)
        {
            return CallReadArrayElement(result.Name, array.Name, index.Name);
        }

        public string WriteArrayElement(string array, string index, string value)
        {
            return string.Format("$WriteArrayElement({0}, {1}, {2})", array, index, value);
        }

        public string WriteArrayElement(IVariable array, IVariable index, IVariable value)
        {
            return WriteArrayElement(array.Name, index.Name, value.Name);
        }

        public string CallWriteArrayElement(string array, string index, string value)
        {
            var l = new List<string>();
            l.Add(array);
            l.Add(index);
            l.Add(value);
            return ProcedureCall("$WriteArrayElement", l);
        }

        public string CallWriteArrayElement(IVariable array, IVariable index, IVariable value)
        {
            return CallWriteArrayElement(array.Name, index.Name, value.Name);
        }

        public string Return()
        {
            return "return;";
        }

        public string Negation(string b)
        {
            return String.Format("!{0}", b);
        }

        public string BoxFrom(IVariable op1, IVariable result)
        {
            var boogieType = Helpers.GetBoogieType(op1.Type);
            if (boogieType.Equals(Helpers.BoogieType.Ref))
                boogieType = Helpers.BoogieType.Union;
            
            var boxFromProcedure = String.Format("$BoxFrom{0}", boogieType.FirstUppercase());
            var args = new List<string>();
            args.Add(op1.Name);

            return ProcedureCall(boxFromProcedure, args, result.Name);
        }
    }
}
