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

        public override string ToString()
        {
            throw new Exception("This method should not be called, use property Expr.");
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
        public static BoogieLiteral String(Constant constant)
        {
            throw new NotImplementedException();
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
        static public BoogieVariable FromDotNetVariable(IVariable variable)
        {
            Contract.Requires(!variable.IsParameter);
            return new BoogieVariable(Helpers.GetBoogieType(variable.Type), AdaptNameToBoogie(variable.Name));
        }

        static protected string AdaptNameToBoogie(string name)
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

        public InstanceField(IFieldReference field, IVariable instance)
        {
            Field = field;
            Instance = instance;
        }
    }

    public class StaticField : Addressable
    {
        public IFieldReference Field { get; }
        public ITypeReference Type { get; }

        public StaticField(IFieldReference field, ITypeReference type)
        {
            Field = field;
            Type = type;
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
        public override string NullObject()
        {
            return BoogieLiteral.NullObject.Expr;
        }

        // hides implementation in super class
        public override string AllocAddr(IVariable var)
        {
            return this.ProcedureCall("AllocAddr", new List<string>(), AddressOf(var).ToString());
        }

        public override string AllocObject(IVariable var, ISet<IVariable> shouldCreateValueVariable)
        {
            shouldCreateValueVariable.Add(var);
            var sb = new StringBuilder();
            sb.AppendLine(this.ProcedureCall("AllocObject", new List<string>(), var.Name));
            sb.AppendLine(this.VariableAssignment(var, var.Name));
            return sb.ToString();
        }

        // hides implementation in super class
        //public new string VariableAssignment(string variableA, string expr)
        //{
        //    return string.Format("{0} := {1};", variableA, expr);
        //}

        public override string DeclareLocalVariables(IList<IVariable> variables, ISet<IVariable> assignedInMethodCalls)
        {
            StringBuilder sb = new StringBuilder();
            variables.Select(v =>
                    String.Format("\tvar {0} : {1};", AddressOf(v), "Addr")
            ).ToList().ForEach(str => sb.AppendLine(str));

            assignedInMethodCalls.Select(v =>
                    String.Format("\tvar {0} : {1};", v, Helpers.GetBoogieType(v.Type))
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
            return WriteAddr(addr, expr.Expr);
        }
        public override string WriteAddr(Addressable addr, String value)
        {
            if (addr is AddressExpression)
            {
                var addrExpr = addr as AddressExpression;
                var boogieType = Helpers.GetBoogieType(addrExpr.Type);

                if (boogieType.Equals(Helpers.BoogieType.Int))
                    return VariableAssignment("$memoryInt", String.Format("{0}({1},{2},{3})", "WriteInt", "$memoryInt", addrExpr.Expr, value));
                else if (boogieType.Equals(Helpers.BoogieType.Bool))
                    return VariableAssignment("$memoryBool", String.Format("{0}({1},{2},{3})", "WriteBool", "$memoryBool", addrExpr.Expr, value));
                else if (boogieType.Equals(Helpers.BoogieType.Object))
                    return VariableAssignment("$memoryObject", String.Format("{0}({1},{2},{3})", "WriteObject", "$memoryObject", addrExpr.Expr, value));
                else if (boogieType.Equals(Helpers.BoogieType.Real))
                    return VariableAssignment("$memoryReal", String.Format("{0}({1},{2},{3})", "WriteReal", "$memoryReal", addrExpr.Expr, value));
                else if (boogieType.Equals(Helpers.BoogieType.Addr))
                    return VariableAssignment("$memoryAddr", String.Format("{0}({1},{2},{3})", "WriteAddr", "$memoryAddr", addrExpr.Expr, value));

                Contract.Assert(false);
                return "";
            } else
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
            if (cons != null && (cons.Value is Single || cons.Value is Double || cons.Value is Decimal || TypeHelper.IsPrimitiveInteger(cons.Type)))
            {
                boogieConstant = BoogieLiteral.Numeric(cons);
            }

            var boogieType = Helpers.GetBoogieType(variableA.Type);

            if (value is Constant)
            {
                if (boogieConstant != null)
                {
                    return VariableAssignment(variableA, boogieConstant);
                } else
                {
                    return WriteAddr(variableA, value.ToString());
                }
                
            } else if (value is IVariable)
            {
                return WriteAddr(variableA, ValueOfVariable(value as IVariable));
            } else if (value is Dereference)
            {
                var dereference = value as Dereference;
                // read addr of the reference
                // index that addr into the corresponding 'heap'
                var addr = new AddressExpression(variableA.Type, ReadAddr(dereference.Reference).Expr);
                return WriteAddr(variableA, ReadAddr(addr).Expr);
            } else if (value is Reference)
            {
                var reference = value as Reference;
                var addr = AddressOf(reference.Value);
                return WriteAddr(variableA, addr.ToString());
            }

            Contract.Assert(false);

            return "";
        }

        public override string VariableAssignment(IVariable variableA, string expr)
        {
            return WriteAddr(variableA, expr);
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

        protected override string ValueOfVariable(IVariable var)
        {
            return ReadAddr(var).Expr;
        }

        // the variable that represents var's address is $_var.name
        /*public override AddressExpression VarAddress(IVariable var)
        {
            return new AddressExpression(var.Type, String.Format("_{0}", var.Name));
        }*/

        public override Addressable AddressOf(InstanceFieldAccess instanceFieldAccess)
        {
            String map = FieldTranslator.GetFieldName(instanceFieldAccess.Field);
            return new AddressExpression(instanceFieldAccess.Field.Type, string.Format("LoadInstanceFieldAddr({0}, {1})", map, ValueOfVariable(instanceFieldAccess.Instance)));
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

        public override string WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value)
        {
            StringBuilder sb = new StringBuilder();

            var boogieType = Helpers.GetBoogieType(value.Type);
            if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals(Helpers.BoogieType.Object))
            {
                sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(value));
                sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(boogieType, ReadAddr(value).Expr)));
            }
            else
                sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), ReadAddr(value)));

            return sb.ToString();
        }
    }

    public class BoogieGeneratorALaBCT : BoogieGenerator
    {
        public override string NullObject()
        {
            return BoogieLiteral.NullRef.Expr;
        }

        public override string VariableAssignment(IVariable variableA, IValue value)
        {
            Constant cons = value as Constant;
            if (cons != null && (cons.Value is Single || cons.Value is Double || cons.Value is Decimal))
            {   
                return VariableAssignment(variableA.ToString(), BoogieLiteral.Numeric(cons).Expr);
            } else if (value is Dereference)
            {
                var dereference = value as Dereference;
                return VariableAssignment(variableA, dereference.Reference);
            }

            return VariableAssignment(variableA.ToString(), value.ToString());
        }

        public override string VariableAssignment(IVariable variableA, Expression expr)
        {
            return VariableAssignment(BoogieVariable.FromDotNetVariable(variableA), expr);
        }
        public override string VariableAssignment(IVariable variableA, string expr)
        {
            return VariableAssignment(variableA.ToString(), expr);
        }

        public override string AllocLocalVariables(IList<IVariable> variables)
        {
            return String.Empty;
        }

        public override string DeclareLocalVariables(IList<IVariable> variables, ISet<IVariable> assignedInMethodCalls)
        {
            StringBuilder sb = new StringBuilder();

            variables.Where(v => !v.IsParameter)
            .Select(v =>
                    String.Format("\tvar {0} : {1};", v.Name, Helpers.GetBoogieType(v.Type))
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

        public override string AllocObject(IVariable var, ISet<IVariable> _shouldCreateValueVariable)
        {
            return this.ProcedureCall("Alloc", new List<string>(), AddressOf(var).ToString());
        }

        // in this memory addressing the value of a variable is the variable itself 
        protected override string ValueOfVariable(IVariable var)
        {
            return var.Name;
        }

        public override string WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value)
        {
            StringBuilder sb = new StringBuilder();

            String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

            //var addr = AddressOf(instanceFieldAccess);
            //var writeAddr = WriteAddr(addr, value);

            if (!Settings.SplitFields)
            {
                if (!Helpers.IsBoogieRefType(value.Type)) // int, bool, real
                {
                    sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(value));
                    sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(Helpers.GetBoogieType(value.Type), value.Name)));
                    //sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, PrimitiveType2Union(Helpers.GetBoogieType(value.Type), value.Name)));
                }
                else
                {
                    sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), value.Name));
                    //sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, value.Name));
                }
            }
            else
            {
                var boogieType = Helpers.GetBoogieType(value.Type);
               // var heapAccess = String.Format("{0}[{1}]", fieldName, instanceFieldAccess.Instance);
                //F$ConsoleApplication3.Foo.p[f_Ref] := $ArrayContents[args][0];

                if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals(Helpers.BoogieType.Ref))
                {
                    sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(value));
                    //sb.AppendLine(VariableAssignment(heapAccess, PrimitiveType2Union(boogieType, value.Name)));
                    sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(boogieType, value.Name)));
                }
                else
                    sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), value.Name));
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
                    var expr = String.Format("Read($Heap,{0},{1})", instanceFieldAccess.Instance, fieldName);
                    sb.AppendLine(VariableAssignment(result, expr));
                }
            }
            else
            {
                var heapAccess = string.Format("{0}[{1}]", fieldName, instanceFieldAccess.Instance.Name);

                //p_int:= F$ConsoleApplication3.Holds`1.x[$tmp2];
                if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals(Helpers.BoogieType.Ref))
                {
                    sb.AppendLine(VariableAssignment(result, Expression.Union2PrimitiveType(boogieType, heapAccess)));
                }
                else
                    sb.AppendLine(VariableAssignment(result, heapAccess));
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
            return new InstanceField(instanceFieldAccess.Field, instanceFieldAccess.Instance);
        }

        public override Addressable AddressOf(StaticFieldAccess staticFieldAccess)
        {
            return new StaticField(staticFieldAccess.Field, staticFieldAccess.Field.Type);
        }

        public override Addressable AddressOf(IVariable var)
        {
            return new DotNetVariable(var);
        }

        public override string WriteAddr(Addressable addr, Expression expr)
        {
            return WriteAddr(addr, expr.Expr);
        }
        public override string WriteAddr(Addressable addr, string value)
        {
            if (addr is InstanceField instanceField)
            {
                var instanceName = instanceField.Instance;
                var fieldName = FieldTranslator.GetFieldName(instanceField.Field);
                if (Settings.SplitFields)
                {
                    return $"{fieldName}[{instanceName}] := {value};";
                }
                else
                {
                    // return $"Read($Heap,{instanceName},{fieldName})";
                    return $"$Heap := Write($Heap, {instanceName}, {fieldName}, {value});";
                }
                
            }
            else if (addr is StaticField staticField)
            {
                var fieldName = FieldTranslator.GetFieldName(staticField.Field);
                return VariableAssignment(fieldName, value);
            }
            else if (addr is DotNetVariable v)
            {
                var varName = v.Var.Name;
                return VariableAssignment(varName, value);
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

        public abstract string DeclareLocalVariables(IList<IVariable> variables, ISet<IVariable> assignedInMethodCalls);

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

        public string WriteAddr(IVariable addr, IValue value)
        {
            return WriteAddr(AddressOf(addr), value.ToString());
        }

        public string WriteAddr(IVariable addr, String value)
        {
            return WriteAddr(AddressOf(addr), value.ToString());
        }

        public string WriteAddr(Addressable addr, IValue value)
        {
            return WriteAddr(addr, value.ToString());
        }

        public abstract string WriteAddr(Addressable addr, string value);
        public abstract string WriteAddr(Addressable addr, Expression value);

        public abstract string AllocAddr(IVariable var);
        public abstract string AllocObject(IVariable var, ISet<IVariable> shouldCreateValueVariable);

        protected abstract string ValueOfVariable(IVariable var);

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

        public string WriteStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            StringBuilder sb = new StringBuilder();

            //string opStr = value.ToString();
            //if (value.Type.TypeCode.Equals(PrimitiveTypeCode.String))
            //    opStr = Helpers.Strings.fixStringLiteral(value);

            String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
            sb.Append(WriteAddr(AddressOf(staticFieldAccess), ReadAddr(value)));
            return sb.ToString();
        }

        public string ReadStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            StringBuilder sb = new StringBuilder();

            var address = AddressOf(staticFieldAccess);

            sb.Append(VariableAssignment(value, ReadAddr(address)));
            //sb.Append(VariableAssignment(value, fieldName));

            return sb.ToString();
        }

        public abstract string WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value);

        public abstract string ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result);

        public string ProcedureCall(IMethodReference procedure, List<IVariable> argumentList, ISet<IVariable> shouldCreateValueVariable, IVariable resultVariable = null)
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
                    resultArguments.Add(ValueOfVariable(argumentList[i]));
            }

            if (resultVariable != null)
            {
                resultArguments.Add(resultVariable.Name);
                shouldCreateValueVariable.Add(resultVariable);
            }

            var arguments = String.Join(",", argumentList.Select(v => ReadAddr(v).Expr));
            if (resultArguments.Count > 0)
            {
                sb.Append(string.Format("call {0} := {1}({2});", String.Join(",", resultArguments), boogieProcedureName, arguments));
                if (Settings.NewAddrModelling)
                {
                    Contract.Assert(resultArguments.Count == 1);
                    Contract.Assert(resultArguments.Contains(resultVariable.Name));
                    sb.Append(WriteAddr(resultVariable, resultVariable.Name));
                }
                return sb.ToString();
            }
            else
                return string.Format("call {0}({1});", boogieProcedureName, arguments);

            //return ProcedureCall(boogieProcedureName, argumentList.Select(v => ValueOfVariable(v)).ToList(), resultArguments);
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
            StringBuilder sb = new StringBuilder();

            var arguments = String.Join(",", argumentList);
            if (resultVariables.Count > 0)
                return string.Format("call {0} := {1}({2});", String.Join(",",resultVariables), boogieProcedureName, arguments);
            else
                return string.Format("call {0}({1});", boogieProcedureName, arguments);
        }

        public abstract string VariableAssignment(IVariable variableA, Expression expr);
        public abstract string VariableAssignment(IVariable variableA, IValue value);
        public abstract string VariableAssignment(IVariable variableA, string expr);
        public string NullOrZero(ITypeReference type)
        {
            if (TypeHelper.IsPrimitiveInteger(type))
            {
                return "0";
            }
            else if (type.TypeCode.Equals(TypeCode.Single) || type.TypeCode.Equals(TypeCode.Double))
            {
                return "0.0";
            }
            else
            {
                return "null";
            }
        }

        public string VariableAssignment(BoogieVariable variableA, Expression expr)
        {
            return $"{variableA.Expr} := {expr.Expr};";
        }

        public string VariableAssignment(string variableA, string expr)
        {
            return string.Format("{0} := {1};", variableA, expr);
        }

        public static bool IsSupportedBinaryOperation(BinaryOperation binaryOperation, IVariable op1, IVariable op2)
        {
            switch (binaryOperation)
            {
                case BinaryOperation.And:
                case BinaryOperation.Or:
                    return Helpers.GetBoogieType(op1.Type).Equals(Helpers.BoogieType.Bool) && Helpers.GetBoogieType(op2.Type).Equals(Helpers.BoogieType.Bool);
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

        public string BinaryOperationExpression(IVariable op1, IVariable op2, BinaryOperation binaryOperation)
        {
            Contract.Assume(IsSupportedBinaryOperation(binaryOperation, op1, op2));
            string operation = String.Empty;
            switch (binaryOperation)
            {
                case BinaryOperation.Add: operation = "+"; break;
                case BinaryOperation.Sub: operation = "-"; break;
                case BinaryOperation.Mul: operation = "*"; break;
                case BinaryOperation.Div:
                    {
                        if (Helpers.GetBoogieType(op1.Type).Equals(Helpers.GetBoogieType(op2.Type)) &&
                            Helpers.GetBoogieType(op1.Type).Equals(Helpers.BoogieType.Int))
                            operation = "div";
                        else
                            operation = "/";
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
                case BinaryOperation.Gt:
                    operation = ">";
                    var leftAsConstant = op1 as Constant;
                    var rightAsConstant = op2 as Constant;
                    if (leftAsConstant != null && rightAsConstant != null)
                    {
                        // There was a bug when comparing references, checking that this doesn't show up again.
                        Contract.Assume(leftAsConstant.Value != null && rightAsConstant.Value != null);
                    }
                    break;
                case BinaryOperation.Ge: operation = ">="; break;
                case BinaryOperation.Lt: operation = "<"; break;
                case BinaryOperation.Le: operation = "<="; break;
                case BinaryOperation.Rem: operation = "mod";break;
                case BinaryOperation.And:
                    {
                        Contract.Assert(Helpers.GetBoogieType(op1.Type).Equals(Helpers.BoogieType.Bool));
                        Contract.Assert(Helpers.GetBoogieType(op2.Type).Equals(Helpers.BoogieType.Bool));
                        operation = "&&";
                        break;
                    }
                case BinaryOperation.Or:
                    {
                        Contract.Assert(Helpers.GetBoogieType(op1.Type).Equals(Helpers.BoogieType.Bool));
                        Contract.Assert(Helpers.GetBoogieType(op2.Type).Equals(Helpers.BoogieType.Bool));
                        operation = "||";
                        break;
                    }
                default:
                    Contract.Assert(false);
                    break;
            }

            return BinaryOperationExpression(ReadAddr(op1).Expr, ReadAddr(op2).Expr, operation);
        }

        public string BinaryOperationExpression(string op1, string op2, string operation)
        {
            return string.Format("{0} {1} {2}", op1, operation, op2);
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

        public abstract string NullObject();

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

        public string As(IVariable arg1, ITypeReference arg2)
        {
            // TODO(rcastano): Fix for generics
            return String.Format("$As({0},{1})", ReadAddr(arg1).Expr, Helpers.GetNormalizedTypeFunction(arg2, InstructionTranslator.MentionedClasses));
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
