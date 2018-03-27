using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Visitors;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TinyBCT.Translators;

namespace TinyBCT
{
    class InstructionTranslator : InstructionVisitor
    {

        public static ISet<IMethodReference> ExternMethodsCalled = new HashSet<IMethodReference>();
        public string Result() { return  sb.ToString().Replace("<>","__"); }
        private StringBuilder sb = new StringBuilder();

        private void addLabel(Instruction instr)
        {
            sb.AppendLine(String.Format("\t{0}:", instr.Label));
        }

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
                /*case BinaryOperation.Rem: operation = "%"; break;
                case BinaryOperation.And: operation = "&"; break;
                case BinaryOperation.Or: operation = "|"; break;
                case BinaryOperation.Xor: operation = "^"; break;
                case BinaryOperation.Shl: operation = "<<"; break;
                case BinaryOperation.Shr: operation = ">>"; break;*/
                case BinaryOperation.Eq: operation = "=="; break;
                case BinaryOperation.Neq: operation = "!="; break;
                case BinaryOperation.Gt: operation = ">";
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

            /*
            // workaround for bug typing bools and integers
            // we need to tell if an integer is used as a boolean
            // temp fix until fixed from analysis-net framework.
            switch (instruction.Operation)
            {
                case BinaryOperation.Eq:
                case BinaryOperation.Neq:
                case BinaryOperation.Gt:
                case BinaryOperation.Ge:
                case BinaryOperation.Lt:
                case BinaryOperation.Le:
                    if ( (Helpers.GetBoogieType(left.Type).Equals("int") && Helpers.GetBoogieType(right.Type).Equals("bool")) ||
                        (Helpers.GetBoogieType(right.Type).Equals("int") && Helpers.GetBoogieType(left.Type).Equals("bool")))
                    {
                        var leftFixed = Helpers.GetBoogieType(left.Type).Equals("int") ? String.Format("Int2Bool({0})", left.ToString()) : left.ToString();
                        var rightFixed = Helpers.GetBoogieType(right.Type).Equals("int") ? String.Format("Int2Bool({0})", right.ToString()) : right.ToString();
                        sb.Append(String.Format("\t\t{0} {1} {2} {3} {4};", instruction.Result, ":=", leftFixed, operation, rightFixed));
						return;
					}
					break;
            }*/

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
            } else if (instruction.Operand is StaticFieldAccess) // memory access handling
            {
                // static fields are considered global variables
                var staticFieldAccess = instruction.Operand as StaticFieldAccess;
                sb.Append(String.Format("\t\t{0} := {1};", instruction.Result, FieldTranslator.GetFieldName(staticFieldAccess.Field)));
            } else if (instruction.Operand is StaticMethodReference) // delegates handling
            {
                // tracking of this references is done in DelegateTranslator
                //call $tmp0:= T$System.Func`2$CreateDelegate(cMain2.foo$System.Int32, null, Type0());
                //return;
            } else
            {
                sb.Append(String.Format("\t\t{0} := {1};", instruction.Result, instruction.Operand));
            }

            lastLoadInst = instruction;
        }


        public override void Visit(TryInstruction instruction)
        {
            sb.Append("// TryInstruction not implemented yet.");
        }

        public override void Visit(FinallyInstruction instruction)
        {
            sb.Append("// FinallyInstruction not implemented yet.");
        }

        public override void Visit(MethodCallInstruction instruction)
        {
            // captures something like System.Void System.Func<System.Int32, System.Int32, Object>..ctor
            // or System.Void System.Func<whatever list of types>..ctor
            string pattern = @"System\.Void System\.Func<([A-Za-z0-9|\.]+(\,\s)?)+>\.\.ctor.*";
            if (Regex.IsMatch(instruction.Method.ToString(), pattern) )
                return;

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

        /*
         * Se hace una primera pasada y se guarda una referencia de todos los metodos que aparecen en una StaticReference
         * de una LoadInstruction
         * 
         * Luego cuando se hace la traducción instrucción por instrucción, Load, CreateObjectInstruction y MethodCallInstruction
         * deberían ser reemplazados en Boogie por una invocación "CreateDelegate"  correspondiente
         * 
         * El CreateDelegate lo haré en el CreateObjectInstruction porque es la variable de este resultado que es usado 
         * para referenciar el objeto.
         * 
         * Borrar variables no usadas del Load y el MethodCallInstruction
         *  
         * Problemas encontrados:
         * ¿Como puedo saber si CreateObjectInstruction es de un objeto que apunta a un método?
         *  Solo se me ocurren maneras medio hack. No se si hay un tipo en CCI para este tipo de objetos.
         *  Por ejemplo si uso en C# un Func<int, int> aparece como "System.Func", pero si hago un delegate aparece con el nombre del delegate.
         *  ¿Como puedo capturar todos?
         *  
         * ¿Como puedo saber si MethodCallInstruction esta haciendo una invocación de un constructor de un objeto que apunta a un método?
         *  Si uso un System.Func no se crean objetos adicionales, pero si uso un Delegate si. ¿Tendré que traducir lo de los delegates? . Como puedo identificar cada uno de manera elegante?
         */

        private static LoadInstruction lastLoadInst = null;

        public override void Visit(CreateObjectInstruction instruction)
        {
            // TODO: we should find a better way to identify types that reference functions
            if (instruction.AllocationType.ToString().StartsWith("System.Func"))
            {
                // estamos asumiendo que antes de esto siempre hubo un LoadInstruction
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

                if (lastLoadInst.Operand is StaticMethodReference) // delegates handling
                {
                    var loadDelegateStmt = lastLoadInst.Operand as StaticMethodReference;
                    var methodRef = loadDelegateStmt.Method;
                    var methodId = DelegateTranslator.methodIdentifiers[methodRef];

                    sb.AppendLine(String.Format("call {0}:= CreateDelegate({1}, {2}, {3});", instruction.Result, methodId, "null", "Type0()"));
                }

                return;
            }

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
                    sb.Append(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, String.Format("Int2Union({0})",op)));
                } else if (Helpers.GetBoogieType(op.Type).Equals("Ref"))
                {
                    //sb.AppendLine(String.Format("\t\tassume Union2Int(Int2Union({0})) == {0};", op));
                    // Union y Ref son el mismo type, forman un alias.
                    sb.Append(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, /*String.Format("Int2Union({0})", op)*/op));
                }
            } else
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

            sb.Append(String.Format("\t\t{0} := $As({1},T${2}());", dest, source,type.ToString()));
        }

    }
}
