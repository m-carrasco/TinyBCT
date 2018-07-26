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
    // expr field is an expression that in boogie will be typed as Addr
    public class AddressExpression
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
    public class AddressExpressionUnionHeap : AddressExpression
    {
        public IFieldReference Field;
        public IVariable Instance;

        public AddressExpressionUnionHeap(ITypeReference t, string e) : base(t, e)
        {
        }
    }

    // TODO: improve inheritance
    // BoogieGenerator class should not have memory specific methods
    public class BoogieGeneratorAddr : BoogieGenerator
    {
        // hides implementation in super class
        public override string AllocAddr(IVariable var)
        {
            return this.ProcedureCall("AllocAddr", new List<string>(), AddressOf(var).ToString());
        }

        // hides implementation in super class
        //public new string VariableAssignment(string variableA, string expr)
        //{
        //    return string.Format("{0} := {1};", variableA, expr);
        //}

        public override string DeclareLocalVariables(IList<IVariable> variables)
        {
            StringBuilder sb = new StringBuilder();
            variables.Select(v =>
                    String.Format("\tvar {0} : {1};", AddressOf(v), "Addr")
            ).ToList().ForEach(str => sb.AppendLine(str));

            return sb.ToString();
        }

        public override string ReadAddr(IVariable addr)
        {
            return ReadAddr(AddressOf(addr));
        }

        public override string ReadAddr(AddressExpression addr)
        {
            var boogieType = Helpers.GetBoogieType(addr.Type);

            if (boogieType.Equals("int"))
                return String.Format("ReadInt({0}, {1})", "$memoryInt", addr.Expr);
            else if (boogieType.Equals("bool"))
                return String.Format("ReadBool({0}, {1})", "$memoryBool", addr.Expr);
            else if (boogieType.Equals("Object"))
                return String.Format("ReadObject({0}, {1})", "$memoryObject", addr.Expr);
            else if (boogieType.Equals("real"))
                return String.Format("ReadReal({0}, {1})", "$memoryReal", addr.Expr);
            else if (boogieType.Equals("Addr"))
                return String.Format("ReadAddr({0}, {1})", "$memoryAddr", addr.Expr);

            Contract.Assert(false);
            return "";
        }

        // it may have to be public but i have not found an example yet.
        public override string WriteAddr(AddressExpression addr, String value)
        {
            var boogieType = Helpers.GetBoogieType(addr.Type);

            if (boogieType.Equals("int"))
                return VariableAssignment("$memoryInt", String.Format("{0}({1},{2},{3})", "WriteInt", "$memoryInt", addr.Expr, value));
            else if (boogieType.Equals("bool"))
                return VariableAssignment("$memoryBool", String.Format("{0}({1},{2},{3})", "WriteBool", "$memoryBool", addr.Expr, value));
            else if (boogieType.Equals("Object"))
                return VariableAssignment("$memoryObject", String.Format("{0}({1},{2},{3})", "WriteObject", "$memoryObject", addr.Expr, value));
            else if (boogieType.Equals("real"))
                return VariableAssignment("$memoryReal", String.Format("{0}({1},{2},{3})", "WriteReal", "$memoryReal", addr.Expr, value));
            else if (boogieType.Equals("Addr"))
                return VariableAssignment("$memoryAddr", String.Format("{0}({1},{2},{3})", "WriteAddr", "$memoryAddr", addr.Expr, value));

            Contract.Assert(false);
            return "";
        }

        public override string VariableAssignment(IVariable variableA, IValue value)
        {
            Constant cons = value as Constant;
            if (cons != null && (cons.Value is Single || cons.Value is Double || cons.Value is Decimal))
            {
                string formatedFloatValue = base.FormatFloatValue(cons);
            }

            var boogieType = Helpers.GetBoogieType(variableA.Type);

            if (value is Constant)
            {
                return WriteAddr(variableA, value);
            } else if (value is IVariable)
            {
                return WriteAddr(variableA, ValueOfVariable(value as IVariable));
            } else if (value is Dereference)
            {
                var dereference = value as Dereference;
                // read addr of the reference
                // index that addr into the corresponding 'heap'
                var addr = new AddressExpression(variableA.Type, ReadAddr(dereference.Reference));
                return WriteAddr(variableA, ReadAddr(addr));
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

                Constant constantValue = new Constant(paramVariable);
                constantValue.Type = paramVariable.Type;

                // boogie generator knows that must fetch paramVariable's address (_x and not x)
                sb.AppendLine(VariableAssignment(paramVariable, constantValue));
            }

            return sb.ToString();
        }

        protected override string ValueOfVariable(IVariable var)
        {
            return ReadAddr(var);
        }

        // the variable that represents var's address is $_var.name
        /*public override AddressExpression VarAddress(IVariable var)
        {
            return new AddressExpression(var.Type, String.Format("_{0}", var.Name));
        }*/

        public override AddressExpression AddressOf(InstanceFieldAccess instanceFieldAccess)
        {
            String map = FieldTranslator.GetFieldName(instanceFieldAccess.Field);
            return new AddressExpression(instanceFieldAccess.Field.Type, string.Format("LoadInstanceFieldAddr({0}, {1})", map, ValueOfVariable(instanceFieldAccess.Instance)));
        }

        public override AddressExpression AddressOf(StaticFieldAccess staticFieldAccess)
        {
            String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
            var address = new AddressExpression(staticFieldAccess.Field.Type, fieldName);
            return address;
        }

        public override AddressExpression AddressOf(IVariable var)
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
                if (!boogieType.Equals("Object"))
                {
                    return VariableAssignment(result, Union2PrimitiveType(boogieType, readValue));
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
            if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals("Object"))
            {
                sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(value));
                sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), PrimitiveType2Union(boogieType, ReadAddr(value))));
            }
            else
                sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), ReadAddr(value)));

            return sb.ToString();
        }
    }

    public class BoogieGeneratorALaBCT : BoogieGenerator
    {
        public override string VariableAssignment(IVariable variableA, IValue value)
        {
            Constant cons = value as Constant;
            if (cons != null && (cons.Value is Single || cons.Value is Double || cons.Value is Decimal))
            {
                // default string representation of floating point types is not suitable for boogie
                // boogie wants dot instead of ,
                // "F" forces to add decimal part
                string str = FormatFloatValue(cons);

                return VariableAssignment(variableA.ToString(), str);
            } else if (value is Dereference)
            {
                var dereference = value as Dereference;
                return VariableAssignment(variableA, dereference.Reference);
            }

            return VariableAssignment(variableA.ToString(), value.ToString());
        }

        public override string VariableAssignment(IVariable variableA, string expr)
        {
            return VariableAssignment(variableA.ToString(), expr);
        }

        public override string AllocLocalVariables(IList<IVariable> variables)
        {
            return String.Empty;
        }

        public override string DeclareLocalVariables(IList<IVariable> variables)
        {
            StringBuilder sb = new StringBuilder();

            variables.Where(v => !v.IsParameter)
            .Select(v =>
                    String.Format("\tvar {0} : {1};", v.Name, Helpers.GetBoogieType(v.Type))
            ).ToList().ForEach(str => sb.AppendLine(str));

            return sb.ToString();
        }

        public override string ReadAddr(IVariable var)
        {
            return String.Empty;
        }

        public override string AllocAddr(IVariable var)
        {
            return String.Empty;
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
                    sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), PrimitiveType2Union(Helpers.GetBoogieType(value.Type), value.Name)));
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
                Contract.Assert(!string.IsNullOrEmpty(boogieType));
               // var heapAccess = String.Format("{0}[{1}]", fieldName, instanceFieldAccess.Instance);
                //F$ConsoleApplication3.Foo.p[f_Ref] := $ArrayContents[args][0];

                if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals("Ref"))
                {
                    sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(value));
                    //sb.AppendLine(VariableAssignment(heapAccess, PrimitiveType2Union(boogieType, value.Name)));
                    sb.AppendLine(WriteAddr(AddressOf(instanceFieldAccess), PrimitiveType2Union(boogieType, value.Name)));
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
            Contract.Assert(!string.IsNullOrEmpty(boogieType));

            if (!Settings.SplitFields)
            {

                if (!Helpers.IsBoogieRefType(result.Type)) // int, bool, real
                {
                    // example: Union2Int(Read(...))
                    var expr = Union2PrimitiveType(boogieType, String.Format("Read($Heap,{0},{1})", instanceFieldAccess.Instance, fieldName));
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
                if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals("Ref"))
                {
                    sb.AppendLine(VariableAssignment(result, Union2PrimitiveType(boogieType, heapAccess)));
                }
                else
                    sb.AppendLine(VariableAssignment(result, heapAccess));
            }

            return sb.ToString();
        }

        public override string ReadAddr(AddressExpression addr)
        {
            if (addr is AddressExpressionUnionHeap)
            {
                var a = addr as AddressExpressionUnionHeap;
                return String.Format("Read($Heap,{0},{1})", a.Instance, FieldTranslator.GetFieldName(a.Field));
            } else
                return addr.Expr;
        }

        public override AddressExpression AddressOf(InstanceFieldAccess instanceFieldAccess)
        {
            if (Settings.SplitFields)
            {
                String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);
                return new AddressExpression(instanceFieldAccess.Field.Type, String.Format("{0}[{1}]", fieldName, instanceFieldAccess.Instance.Name));
            } else
            {
                var a = new AddressExpressionUnionHeap(instanceFieldAccess.Type, "");
                a.Field = instanceFieldAccess.Field;
                a.Instance = instanceFieldAccess.Instance;
                return a;
            }
        }

        public override AddressExpression AddressOf(StaticFieldAccess staticFieldAccess)
        {
            String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
            var address = new AddressExpression(staticFieldAccess.Field.Type, fieldName);
            return address;
        }

        public override AddressExpression AddressOf(IVariable var)
        {
            return new AddressExpression(var.Type, var.Name);
        }

        public override string WriteAddr(AddressExpression addr, string value)
        {
            if (addr is AddressExpressionUnionHeap)
            {
                var a = addr as AddressExpressionUnionHeap;
                var fieldName = FieldTranslator.GetFieldName(a.Field);
                return String.Format("$Heap := Write($Heap, {0}, {1}, {2});", a.Instance, fieldName, value);
            } else
            {
                return VariableAssignment(addr.Expr, value);
            }
        }
    }

    public abstract class BoogieGenerator
    {
        private static BoogieGenerator singleton;

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

        protected string FormatFloatValue(Constant cons)
        {
            // default string representation of floating point types is not suitable for boogie
            // boogie wants dot instead of ,
            // "F" forces to add decimal part
            string str = "";
            if (cons.Value is Single)
            {
                Single v = (Single)cons.Value;
                str = v.ToString("F").Replace(",", ".");
            }
            else if (cons.Value is Double)
            {
                Double v = (Double)cons.Value;
                str = v.ToString("F").Replace(",", ".");
            }
            else if (cons.Value is Decimal)
            {
                Decimal v = (Decimal)cons.Value;
                str = v.ToString("F").Replace(",", ".");
            } else
            {
                Contract.Assert(false);
            }

            return str;
        }

        public abstract string ReadAddr(AddressExpression addr);

        public abstract string DeclareLocalVariables(IList<IVariable> variables);

        public abstract string AllocLocalVariables(IList<IVariable> variables);

        public AddressExpression AddressOf(IValue value)
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

        public abstract AddressExpression AddressOf(InstanceFieldAccess instanceFieldAccess);
        public abstract AddressExpression AddressOf(StaticFieldAccess staticFieldAccess);
        public abstract AddressExpression AddressOf(IVariable var);

        public string WriteAddr(IVariable addr, IValue value)
        {
            return WriteAddr(AddressOf(addr), value.ToString());
        }

        public string WriteAddr(IVariable addr, String value)
        {
            return WriteAddr(AddressOf(addr), value.ToString());
        }

        public string WriteAddr(AddressExpression addr, IValue value)
        {
            return WriteAddr(addr, value.ToString());
        }

        public abstract string WriteAddr(AddressExpression addr, string value);

        public abstract string ReadAddr(IVariable var);

        public abstract string AllocAddr(IVariable var);

        protected abstract string ValueOfVariable(IVariable var);

        public string AssumeInverseRelationUnionAndPrimitiveType(string variable, string boogieType)
        {
            boogieType = boogieType[0].ToString().ToUpper() + boogieType.Substring(1);
            var e1 = string.Format("{0}2Union({1})", boogieType, variable);
            return string.Format("assume Union2{0}({1}) == {2};", boogieType, e1, variable);
        }

        public string AssumeInverseRelationUnionAndPrimitiveType(IVariable variable)
        {
            var boogieType = Helpers.GetBoogieType(variable.Type);
            Contract.Assert(!String.IsNullOrEmpty(boogieType));

            return AssumeInverseRelationUnionAndPrimitiveType(variable.ToString(), boogieType);
        }

        public string PrimitiveType2Union(IVariable value)
        {
            Contract.Assert(!Helpers.IsBoogieRefType(value.Type));
            var boogieType = Helpers.GetBoogieType(value.Type);
            return PrimitiveType2Union(boogieType, value.ToString());
        }

        public string PrimitiveType2Union(string boogieType, string value)
        {
            // int -> Int, bool -> Bool
            boogieType = boogieType[0].ToString().ToUpper() + boogieType.Substring(1);
            return string.Format("{0}2Union({1})", boogieType, value);
        }

        public string Union2PrimitiveType(IVariable value)
        {
            Contract.Assert(Helpers.IsBoogieRefType(value.Type));
            var boogieType = Helpers.GetBoogieType(value.Type);

            return Union2PrimitiveType(boogieType, value.ToString());
        }
        public string Union2PrimitiveType(string boogieType, string value)
        {
            // int -> Int, bool -> Bool
            boogieType = boogieType[0].ToString().ToUpper() + boogieType.Substring(1);
            return string.Format("Union2{0}({1})", boogieType, value);
        }

        public string WriteStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            StringBuilder sb = new StringBuilder();

            //string opStr = value.ToString();
            //if (value.Type.TypeCode.Equals(PrimitiveTypeCode.String))
            //    opStr = Helpers.Strings.fixStringLiteral(value);

            String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
            WriteAddr(AddressOf(staticFieldAccess), ReadAddr(value));
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

        public string ProcedureCall(IMethodReference procedure, List<IVariable> argumentList, IVariable resultVariable = null)
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
                resultArguments.Add(ValueOfVariable(resultVariable));

            return ProcedureCall(boogieProcedureName, argumentList.Select(v => ValueOfVariable(v)).ToList(), resultArguments);
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

        public abstract string VariableAssignment(IVariable variableA, IValue value);
        public abstract string VariableAssignment(IVariable variableA, string expr);

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
                    return Helpers.GetBoogieType(op1.Type) == "bool" && Helpers.GetBoogieType(op2.Type) == "bool";
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

            return BranchOperationExpression(op1.Name, op2.ToString(), operation);
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
                            Helpers.GetBoogieType(op1.Type).Equals("int"))
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
                        Contract.Assert(Helpers.GetBoogieType(op1.Type) == "bool");
                        Contract.Assert(Helpers.GetBoogieType(op2.Type) == "bool");
                        operation = "&&";
                        break;
                    }
                case BinaryOperation.Or:
                    {
                        Contract.Assert(Helpers.GetBoogieType(op1.Type) == "bool");
                        Contract.Assert(Helpers.GetBoogieType(op2.Type) == "bool");
                        operation = "||";
                        break;
                    }
                default:
                    Contract.Assert(false);
                    break;
            }

            return BinaryOperationExpression(op1.Name, op2.Name, operation);
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
            return AssumeDynamicType(reference.Name, type);
        }

        public string AssumeDynamicType(string name, ITypeReference type)
        {
            return String.Format("assume $DynamicType({0}) == {1};", name, Helpers.GetNormalizedTypeFunction(type, InstructionTranslator.MentionedClasses));
        }

        public string TypeConstructor(string type)
        {
            return String.Format("$TypeConstructor({0})", type);
        }

        public string AssumeTypeConstructor(string arg, ITypeReference type)
        {
            return AssumeTypeConstructor(arg, type.ToString());
        }

        public string AssumeTypeConstructor(string arg, string type)
        {
            return String.Format("assume $TypeConstructor($DynamicType({0})) == T${1};", arg, type);
        }

        public string Assert(IVariable cond)
        {
            return Assert(cond.Name);
        }

        public string Assert(string cond)
        {
            return String.Format("assert {0};", cond);
        }

        public string Assume(IVariable cond)
        {
            return Assume(cond.Name);
        }

        public string Assume(string cond)
        {
            return String.Format("assume {0};", cond);
        }

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
            return String.Format("$As({0},{1})", arg1, Helpers.GetNormalizedTypeFunction(arg2, InstructionTranslator.MentionedClasses));
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
            boogieType = boogieType[0].ToString().ToUpper() + boogieType.Substring(1);
            if (boogieType.Equals("Ref"))
                boogieType = "Union";

            var boxFromProcedure = String.Format("$BoxFrom{0}", boogieType);
            var args = new List<string>();
            args.Add(op1.Name);

            return ProcedureCall(boxFromProcedure, args, result.Name);
        }
    }
}
