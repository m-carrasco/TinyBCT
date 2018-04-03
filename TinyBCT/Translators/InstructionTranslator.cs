using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Visitors;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT.Translators
{
    class InstructionTranslator
    {
        // store for extern method called
        // since those methods do not have bodies, they are not translated
        // we need to declared them if they are called
        public static ISet<IMethodReference> ExternMethodsCalled = new HashSet<IMethodReference>();

        Translation translation;

        public string Translate(IList<Instruction> instructions, int idx)
        {
            SetState(instructions, idx);
            instructions[idx].Accept(translation);
            return translation.Result();
        }

        void SetState(IList<Instruction> instructions, int idx)
        {
            if (DelegateTranslation.IsDelegateTranslation(instructions, idx))
                translation = new DelegateTranslation();
            else
                translation = new SimpleTranslation();
        }

        abstract class Translation : InstructionVisitor
        {
            protected void addLabel(Instruction instr)
            {
                sb.AppendLine(String.Format("\t{0}:", instr.Label));
            }

            protected StringBuilder sb = new StringBuilder();
            public string Result()
            {
                //return sb.ToString();
                return sb.ToString().Replace("<>", "__");
            }
        }

        // translates each instruction independently 
        class SimpleTranslation : Translation
        {
            public override void Visit(NopInstruction instruction)
            {
                //addLabel(instruction);
                sb.Append(String.Format("\t{0}:", instruction.Label));
            }

            public override void Visit(BinaryInstruction instruction)
            {
                addLabel(instruction);

                IVariable left = instruction.LeftOperand;
                IVariable right = instruction.RightOperand;

                String operation = String.Empty;

                switch (instruction.Operation)
                {
                    case BinaryOperation.Add: operation = "+"; break;
                    case BinaryOperation.Sub: operation = "-"; break;
                    case BinaryOperation.Mul: operation = "*"; break;
                    case BinaryOperation.Div: operation = "/"; break;
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
                        // hack: I don't know why is saying > when is comparing referencies
                        if (!left.Type.IsValueType || right.Type.IsValueType)
                        {
                            operation = "!=";
                        }
                        break;
                    case BinaryOperation.Ge: operation = ">="; break;
                    case BinaryOperation.Lt: operation = "<"; break;
                    case BinaryOperation.Le: operation = "<="; break;
                }
                sb.Append(String.Format("\t\t{0} {1} {2} {3} {4};", instruction.Result, ":=", left, operation, right));
            }

            public override void Visit(UnconditionalBranchInstruction instruction)
            {
                addLabel(instruction);
                sb.Append(String.Format("\t\tgoto {0};", instruction.Target));
            }

            public override void Visit(ReturnInstruction instruction)
            {
                addLabel(instruction);
                if (instruction.HasOperand)
                    sb.Append(String.Format("\t\tr := {0};", instruction.Operand.Name));
            }

            public override void Visit(LoadInstruction instruction)
            {
                addLabel(instruction);
                if (instruction.Operand is InstanceFieldAccess) // memory access handling
                {
                    InstanceFieldAccess instanceFieldOp = instruction.Operand as InstanceFieldAccess;
                    String fieldName = FieldTranslator.GetFieldName(instanceFieldOp.Field);
                    if (Helpers.GetBoogieType(instanceFieldOp.Type).Equals("int"))
                        sb.Append(String.Format("\t\t{0} := Union2Int(Read($Heap,{1},{2}));", instruction.Result, instanceFieldOp.Instance, fieldName));
                    else if (Helpers.GetBoogieType(instanceFieldOp.Type).Equals("Ref"))
                        // Union and Ref are alias. There is no need of Union2Ref
                        sb.Append(String.Format("\t\t{0} := Read($Heap,{1},{2});", instruction.Result, instanceFieldOp.Instance, fieldName));
                }
                else if (instruction.Operand is StaticFieldAccess) // memory access handling
                {
                    // static fields are considered global variables
                    var staticFieldAccess = instruction.Operand as StaticFieldAccess;
                    sb.Append(String.Format("\t\t{0} := {1};", instruction.Result, FieldTranslator.GetFieldName(staticFieldAccess.Field)));
                }
                else if (instruction.Operand is StaticMethodReference) // delegates handling
                {
                    // see DelegateTranslation
                }
                else
                {
                    sb.Append(String.Format("\t\t{0} := {1};", instruction.Result, instruction.Operand));
                }
            }

            public override void Visit(TryInstruction instruction)
            {
                // now that we can type reference (so exceptions), we could implement try catch structures
                sb.Append("// TryInstruction not implemented yet.");
            }

            public override void Visit(FinallyInstruction instruction)
            {
                // now that we can type reference (so exceptions), we could implement try catch structures
                sb.Append("// FinallyInstruction not implemented yet.");
            }

            public override void Visit(MethodCallInstruction instruction)
            {
                // This is check is done because an object creation is splitted into two TAC instructions
                // This prevents to add the same instruction tag twice
                if (!Helpers.IsConstructor(instruction.Method))
                    addLabel(instruction);

                var signature = Helpers.GetMethodName(instruction.Method);
                var arguments = string.Join(", ", instruction.Arguments);

                var methodName = instruction.Method.ContainingType.FullName() + "." + instruction.Method.Name.Value;

                if (methodName == "System.Diagnostics.Contracts.Contract.Assert")
                {
                    sb.Append(String.Format("\t\t assert {0};", arguments));
                    return;
                }

                if (instruction.HasResult)
                    sb.Append(String.Format("\t\tcall {0} := {1}({2});", instruction.Result, signature, arguments));
                else
                    sb.Append(String.Format("\t\tcall {0}({1});", signature, arguments));

                if (Helpers.IsExternal(instruction.Method.ResolvedMethod))
                    ExternMethodsCalled.Add(instruction.Method);
            }

            public override void Visit(ConditionalBranchInstruction instruction)
            {
                addLabel(instruction);

                IVariable leftOperand = instruction.LeftOperand;
                IInmediateValue rightOperand = instruction.RightOperand;

                var operation = string.Empty;

                switch (instruction.Operation)
                {
                    case BranchOperation.Eq: operation = "=="; break;
                    case BranchOperation.Neq: operation = "!="; break;
                    case BranchOperation.Gt: operation = ">"; break;
                    case BranchOperation.Ge: operation = ">="; break;
                    case BranchOperation.Lt: operation = "<"; break;
                    case BranchOperation.Le: operation = "<="; break;
                }


                sb.AppendLine(String.Format("\t\tif ({0} {1} {2})", leftOperand, operation, rightOperand));
                sb.AppendLine("\t\t{");
                sb.AppendLine(String.Format("\t\t\tgoto {0};", instruction.Target));
                sb.Append("\t\t}");

            }

            public override void Visit(CreateObjectInstruction instruction)
            {
                // assume $DynamicType($tmp0) == T$TestType();
                //assume $TypeConstructor($DynamicType($tmp0)) == T$TestType;

                addLabel(instruction);
                sb.AppendLine(String.Format("\t\tcall {0}:= Alloc();", instruction.Result));
                var type = Helpers.GetNormalizedType(instruction.AllocationType);
                sb.AppendLine(String.Format("\t\tassume $DynamicType({0}) == T${1}();", instruction.Result, type));
                sb.AppendLine(String.Format("\t\tassume $TypeConstructor($DynamicType({0})) == T${1};", instruction.Result, type));
            }

            public override void Visit(StoreInstruction instruction)
            {
                addLabel(instruction);

                var op = instruction.Operand; // what it is stored
                var instanceFieldAccess = instruction.Result as InstanceFieldAccess; // where it is stored

                if (instanceFieldAccess != null)
                {
                    String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

                    if (Helpers.GetBoogieType(op.Type).Equals("int"))
                    {
                        sb.AppendLine(String.Format("\t\tassume Union2Int(Int2Union({0})) == {0};", op));
                        sb.Append(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, String.Format("Int2Union({0})", op)));
                    }
                    else if (Helpers.GetBoogieType(op.Type).Equals("Ref"))
                    {
                        //sb.AppendLine(String.Format("\t\tassume Union2Int(Int2Union({0})) == {0};", op));
                        // Union y Ref son el mismo type, forman un alias.
                        sb.Append(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, /*String.Format("Int2Union({0})", op)*/op));
                    }
                }
                else
                {
                    // static fields are considered global variables
                    var staticFieldAccess = instruction.Result as StaticFieldAccess;
                    if (staticFieldAccess != null)
                    {
                        String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
                        sb.Append(String.Format("\t\t{0} := {1};", fieldName, op));
                    }
                }
            }

            public override void Visit(ConvertInstruction instruction)
            {
                addLabel(instruction);
                var source = instruction.Operand;
                var dest = instruction.Result;
                var type = instruction.ConversionType;

                sb.Append(String.Format("\t\t{0} := $As({1},T${2}());", dest, source, type.ToString()));
            }
        }

        // it is triggered when Load, CreateObject and MethodCall instructions are seen in this order.
        // Load must be of a static or virtual method (currently only static is supported)
        class DelegateTranslation : Translation
        {
            // at the moment only delegates for static methods are supported
            private static bool IsMethodReference(Instruction ins)
            {
                LoadInstruction loadIns = ins as LoadInstruction;
                if (loadIns != null)
                    return loadIns.Operand is StaticMethodReference;
                return false;
            }

            // decides if the DelegateTranslation state is set for instruction translation
            public static bool IsDelegateTranslation(IList<Instruction> instructions, int idx)
            {
                // we are going to read LOAD CREATE METHOD
                if (idx + 2 <= instructions.Count - 1)
                {
                    if (instructions[idx] is LoadInstruction &&
                        instructions[idx + 1] is CreateObjectInstruction &&
                        instructions[idx + 2] is MethodCallInstruction)
                    {
                        if (IsMethodReference(instructions[idx]))
                            return true;
                    }
                }

                // we have just read LOAD, we are going to read CREATE and METHOD
                if (idx - 1 >= 0 && idx + 1 <= instructions.Count - 1)
                {
                    if (instructions[idx - 1] is LoadInstruction &&
                        instructions[idx] is CreateObjectInstruction &&
                        instructions[idx + 1] is MethodCallInstruction)
                    {
                        if (IsMethodReference(instructions[idx-1]))
                            return true;
                    }
                }

                // we have just read LOAD, CREATE and we are going to read METHOD
                if (idx - 2 >= 0)
                {
                    if (instructions[idx - 2] is LoadInstruction &&
                        instructions[idx - 1] is CreateObjectInstruction &&
                        instructions[idx] is MethodCallInstruction)
                    {
                        if (IsMethodReference(instructions[idx-2]))
                            return true;
                    }
                }

                return false;
            }

            private static LoadInstruction loadIns = null;

            public override void Visit(LoadInstruction instruction)
            {
                // todo: modify when delegates for virtual method is implemented
                Contract.Assert(instruction.Operand is StaticMethodReference);
                loadIns = instruction;
            }

            public override void Visit(CreateObjectInstruction instruction)
            {
                // we ensure that before this instruction there was a load instruction
                Contract.Assert(loadIns != null);

                addLabel(instruction);

                // {1} -> entero que identifica univocamente al método
                // {2} -> objeto que tiene el método, en caso de ser estatico es null
                // {3} -> todavía no me queda claro.

                /*
                    procedure {:inline 1} CreateDelegate(Method: int, Receiver: Ref, TypeParameters: Ref) returns (c: Ref);
                    implementation {:inline 1} CreateDelegate(Method: int, Receiver: Ref, TypeParameters: Ref) returns (c: Ref)
                    {
                        call c := Alloc();
                        assume $RefToDelegateReceiver(Method, c) == Receiver;
                        assume $RefToDelegateTypeParameters(Method, c) == TypeParameters;
                        // supongamos que las constantes unicas de los métodos registrados son M1 y M2.
                        assume $RefToDelegateMethod(M1, c) <==> Method == M1;
                        assume $RefToDelegateMethod(M2, c) <==> Method == M2;
                    }

                    function $RefToDelegateMethod(int, Ref) : bool;
                    function $RefToDelegateReceiver(int, Ref) : Ref;
                    function $RefToDelegateTypeParameters(int, Ref) : Type;

                    function Type0() : Ref;
                */

                var loadDelegateStmt = loadIns.Operand as StaticMethodReference;
                var methodRef = loadDelegateStmt.Method;
                var methodId = DelegateStore.GetMethodIdentifier(methodRef);

                // do we have to type this reference?
                sb.AppendLine(String.Format("\t\tcall {0}:= CreateDelegate({1}, {2}, {3});", instruction.Result, methodId, "null", "Type0()"));
            }

            public override void Visit(MethodCallInstruction instruction)
            {
                // for delegate nothing is done 
                // translation was performed in CreateObjectInstruction visit
                loadIns = null;

                // captures something like System.Void System.Func<System.Int32, System.Int32, Object>..ctor
                // or System.Void System.Func<whatever list of types>..ctor
                //string pattern = @"System\.Void System\.Func<([A-Za-z0-9|\.]+(\,\s)?)+>\.\.ctor.*";
                //if (Regex.IsMatch(instruction.Method.ToString(), pattern))
                //    return;

                // This is check is done because an object creation is splitted into two TAC instructions
                // This prevents to add the same instruction tag twice
                //if (!Helpers.IsConstructor(instruction.Method))
                //    addLabel(instruction);

                /* var signature = Helpers.GetMethodName(instruction.Method);
                var arguments = string.Join(", ", instruction.Arguments);

                var methodName = instruction.Method.ContainingType.FullName() + "." + instruction.Method.Name.Value;

                if (methodName == "System.Diagnostics.Contracts.Contract.Assert")
                {
                    sb.Append(String.Format("\t\t assert {0};", arguments));
                    return;
                }


                if (instruction.HasResult)
                    sb.Append(String.Format("\t\tcall {0} := {1}({2});", instruction.Result, signature, arguments));
                else
                    sb.Append(String.Format("\t\tcall {0}({1});", signature, arguments));

                if (Helpers.IsExternal(instruction.Method.ResolvedMethod))
                    ExternMethodsCalled.Add(instruction.Method);*/
            }

        }
    }

    public class DelegateStore
    {
        static IDictionary<IMethodReference, string> methodIdentifiers =
                new Dictionary<IMethodReference, string>();

        public static string CreateDelegateMethod()
        {
            var sb = new StringBuilder();

            sb.AppendLine("procedure {:inline 1} CreateDelegate(Method: int, Receiver: Ref, TypeParameters: Ref) returns(c: Ref);");
            sb.AppendLine("implementation {:inline 1} CreateDelegate(Method: int, Receiver: Ref, TypeParameters: Ref) returns(c: Ref)");
            sb.AppendLine("{");
            sb.AppendLine("     call c := Alloc();");
            sb.AppendLine("     assume $RefToDelegateReceiver(Method, c) == Receiver;");
            sb.AppendLine("     assume $RefToDelegateTypeParameters(Method, c) == TypeParameters;");

            foreach (var id in methodIdentifiers.Values)
                sb.AppendLine(String.Format("     assume $RefToDelegateMethod({0}, c) <==> Method == {0};", id));

            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string DefineMethodsIdentifiers()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var methodId in methodIdentifiers.Values)
                sb.AppendLine(String.Format("const unique {0}: int;", methodId));

            return sb.ToString();
        }

        public static string GetMethodIdentifier(IMethodReference methodRef)
        {
            if (methodIdentifiers.ContainsKey(methodRef))
                return methodIdentifiers[methodRef];

            var methodName = Helpers.GetMethodName(methodRef);
            var methodArity = Helpers.GetArityWithNonBoogieTypes(methodRef);

            // example:  cMain2.objectParameter$System.Object;
            var methodId = methodName + methodArity;

            methodIdentifiers.Add(methodRef, methodId);

            return methodId;
        }
    }
}
