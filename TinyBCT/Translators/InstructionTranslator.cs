using Backend;
using Backend.Model;
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
    // one instance is created for each translated method
    class InstructionTranslator
    {
        private ClassHierarchyAnalysis CHA;
        // Diego: No longer required
        // public Dictionary<string, int> labels = new Dictionary<string, int>();

        // store for extern method called
        // since those methods do not have bodies, they are not translated
        // we need to declared them if they are called
        public static ISet<IMethodReference> ExternMethodsCalled = new HashSet<IMethodReference>();
        // store for methods that have not been found yet but have been called
        // Also necessary to declare them if they end up being missing
        public static ISet<IMethodReference> PotentiallyMissingMethodsCalled = new HashSet<IMethodReference>();
        // Type definitions might not be present in the dll because they are instantiations of generic types,
        // for example, List<int>. For these types, we will need to add the appropriate constants and axioms
        // in Boogie.
        public static ISet<ITypeDefinition> mentionedClasses = new HashSet<ITypeDefinition>();

        Translation translation;
        Instruction lastInstruction = null;
        // while translating instructions some variables may be removed
        // for example for delegates there are instructions that are no longer used
        public ISet<IVariable> RemovedVariables { get; } = new HashSet<IVariable>();
        public ISet<IVariable> AddedVariables { get; } = new HashSet<IVariable>();

        // counters used for new variables 
        private int delegateInvokations = 0;
        private int virtualInvokations = 0;

        private IPrimarySourceLocation prevSourceLocation;

        protected IMethodDefinition method;

        public InstructionTranslator(ClassHierarchyAnalysis CHA, IMethodDefinition method)
        {
            this.CHA = CHA;
            this.method = method;
        }

        public string Translate(IList<Instruction> instructions, int idx)
        {
            SetState(instructions, idx);
            var currentInstruction = instructions[idx];
            translation.AddLabel(currentInstruction);
            translation.AddLineNumbers(currentInstruction, prevSourceLocation);
            if (currentInstruction.Location != null)
                prevSourceLocation = currentInstruction.Location;
            instructions[idx].Accept(translation);
            lastInstruction = instructions[idx];

            return translation.Result();
        }

        

        void SetState(IList<Instruction> instructions, int idx)
        {
            if (DelegateInvokeTranslation.IsDelegateInvokeTranslation(instructions,idx))
                translation = new DelegateInvokeTranslation(this);
            else if (DelegateCreationTranslation.IsDelegateCreationTranslation(instructions, idx))
                translation = new DelegateCreationTranslation(this);
            else
                translation = new SimpleTranslation(this);
        }

        abstract class Translation : InstructionVisitor
        {
            protected InstructionTranslator instTranslator;
            public Translation(InstructionTranslator p)
            {
                instTranslator = p;
            }

            internal void AddLabel(Instruction instr)
            { 
                string label = instr.Label;
                if(!String.IsNullOrEmpty(label))
                    sb.AppendLine(String.Format("\t{0}:", label));                    
            }

            protected StringBuilder sb = new StringBuilder();
            public string Result()
            {
                //return sb.ToString();
                return sb.ToString().Replace("<>", "__");
            }

            internal void AddLineNumbers(Instruction instr, IPrimarySourceLocation prevSourceLocation)
            {
                IPrimarySourceLocation location = instr.Location;

                if (location == null && prevSourceLocation != null)
                {
                    location = prevSourceLocation;
                }
                if(location!=null)
                { 
                    var fileName = location.SourceDocument.Name;
                    var sourceLine = location.StartLine;
                    sb.AppendLine(String.Format("\t\t assert {{:sourceFile \"{0}\"}} {{:sourceLine {1} }} true;", fileName, sourceLine));
                    //     assert {:first} {:sourceFile "C:\Users\diegog\source\repos\corral\AddOns\AngelicVerifierNull\test\c#\As\As.cs"} {:sourceLine 23} true;
                }
                else 
                {
                    sb.AppendLine(String.Format("\t\t assert {{:sourceFile \"{0}\"}} {{:sourceLine {1} }} true;", "Empty", 0));
                }
            }
        }

        // translates each instruction independently 
        class SimpleTranslation : Translation
        {
            public SimpleTranslation(InstructionTranslator p) : base(p)
            {
            }

            public override void Visit(NopInstruction instruction)
            {
                //addLabel(instruction);
            }

            public override void Visit(BinaryInstruction instruction)
            {
                //addLabel(instruction);

                IVariable left = instruction.LeftOperand;
                IVariable right = instruction.RightOperand;

                // Binary operations get translated into method calls:
                // ops:
                //     + : System.String.Concat$System.String$System.String
                System.Diagnostics.Contracts.Contract.Assume(
                    !left.Type.TypeCode.Equals(PrimitiveTypeCode.String) && !right.Type.TypeCode.Equals(PrimitiveTypeCode.String));

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
                //addLabel(instruction);
                sb.Append(String.Format("\t\tgoto {0};", instruction.Target));
            }

            public override void Visit(ReturnInstruction instruction)
            {
                //addLabel(instruction);
                if (instruction.HasOperand)
                    sb.Append(String.Format("\t\tr := {0};", instruction.Operand.Name));
            }

            public override void Visit(LoadInstruction instruction)
            {
                //addLabel(instruction);
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

                    // code generated from (x => x * x) may have a singleton
                    // we will force to initialize the singleton every time it is used
                    // if it is not defined we will have a wrong delegate invokation
                    // the invariant for this is : null || correct delegate
                    // * null case:
                    //      it is correctly initialized
                    // * correct delegate
                    //      * correct invokation
                    // this problem arise becuase static constructors are not called
                    // also there is no invariant for static variables
                    if (staticFieldAccess.Type.ResolvedType.IsDelegate &&
                        staticFieldAccess.Field.ContainingType.IsCompilerGenerated()) 
                    {
                        sb.AppendLine(String.Format("\t\t{0} := null;", FieldTranslator.GetFieldName(staticFieldAccess.Field)));
                    }

                    sb.Append(String.Format("\t\t{0} := {1};", instruction.Result, FieldTranslator.GetFieldName(staticFieldAccess.Field)));
                }
                else if (instruction.Operand is StaticMethodReference) // delegates handling
                {
                    // see DelegateTranslation
                }
                else if (instruction.Operand is Reference)
                {
                    // Reference loading only found when using "default" keyword.
                    // Ignoring translation, the actual value referenced is used by accessing
                    // instTranslator.lastInstruction [see Visit(InitializeObjectInstruction instruction)]
                    // TODO(rcastano): check that loaded references are only used in the assumed context.
                }
                else
                {
                    string operand = instruction.Operand.Type.TypeCode.Equals(PrimitiveTypeCode.String) ?
                        Helpers.Strings.fixStringLiteral(instruction.Operand) :
                        instruction.Operand.ToString();
                    sb.Append(String.Format("\t\t{0} := {1};", instruction.Result, operand));
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

            private void DynamicDispatch(MethodCallInstruction instruction, string arguments)
            {
                var calless = Helpers.PotentialCalleesUsingCHA(instruction, Traverser.CHA);

                // not sure what to do in zero case
                Contract.Assert(calless.Count > 0);

                var getTypeVar = new LocalVariable(String.Format("DynamicDispatch_Type_{0}", instTranslator.virtualInvokations));
                getTypeVar.Type = Types.Instance.PlatformType.SystemObject; // must be translated to Ref
                var receiver = instruction.Arguments[0];

                instTranslator.virtualInvokations = instTranslator.virtualInvokations + 1;
                instTranslator.AddedVariables.Add(getTypeVar);

                sb.AppendLine(String.Format("\t\tcall {0} := System.Object.GetType({1});", getTypeVar, receiver));

                // example:if ($tmp6 == T$DynamicDispatch.Dog())

                sb.AppendLine(String.Format("\t\tif ($Subtype({0},T${1}()))", getTypeVar, Helpers.GetNormalizedType(calless.First().ContainingType)));
                sb.AppendLine("\t\t{");

                var firstSignature = Helpers.GetMethodName(calless.First());

                if (instruction.HasResult)
                {
                    //         call $tmp0 := DynamicDispatch.Mammal.Breathe(a);
                    sb.AppendLine(String.Format("\t\t\tcall {0} := {1}({2});", instruction.Result, firstSignature, arguments));

                }
                else
                {
                    sb.AppendLine(String.Format("\t\t\tcall {0}({1});", firstSignature, arguments));
                }

                sb.AppendLine("\t\t}");

                int i = 0;
                foreach (var impl in calless)
                {
                    // first and last invocation are not handled in this loop
                    if (i == 0 || i == calless.Count - 1)
                    {
                        i++;
                        continue;
                    }

                    sb.AppendLine(String.Format("\t\telse if ($Subtype({0},T${1}()))", getTypeVar, Helpers.GetNormalizedType(impl.ContainingType)));
                    sb.AppendLine("\t\t{");

                    var midSignature = Helpers.GetMethodName(impl);

                    if (instruction.HasResult)
                    {
                        //         call $tmp0 := DynamicDispatch.Mammal.Breathe(a);
                        sb.AppendLine(String.Format("\t\t\tcall {0} := {1}({2});", instruction.Result, midSignature, arguments));

                    }
                    else
                    {
                        sb.AppendLine(String.Format("\t\t\tcall {0}({1});", midSignature, arguments));
                    }

                    sb.AppendLine("\t\t}");
                    i++;
                }

                if (calless.Count > 1) // last element
                {
                    //sb.AppendLine(String.Format("\t\telse ({0} == {1})", getTypeVar, Helpers.GetNormalizedType(calless.Last().ContainingType)));
                    sb.AppendLine(String.Format("\t\telse", getTypeVar, Helpers.GetNormalizedType(calless.Last().ContainingType)));
                    sb.AppendLine("\t\t{");

                    var lastSignature = Helpers.GetMethodName(calless.Last());

                    if (instruction.HasResult)
                    {
                        //         call $tmp0 := DynamicDispatch.Mammal.Breathe(a);
                        sb.AppendLine(String.Format("\t\t\tcall {0} := {1}({2});", instruction.Result, lastSignature, arguments));

                    }
                    else
                    {
                        sb.AppendLine(String.Format("\t\t\tcall {0}({1});", lastSignature, arguments));
                    }

                    sb.AppendLine("\t\t}");
                }
                if (Helpers.IsExternal(instruction.Method.ResolvedMethod) || instruction.Method.ResolvedMethod.IsAbstract)
                    ExternMethodsCalled.Add(instruction.Method);
            }
            public override void Visit(MethodCallInstruction instruction)
            {
                // This is check is done because an object creation is splitted into two TAC instructions
                // This prevents to add the same instruction tag twice
                // DIEGO: Removed after fix in analysis framewlrk 
                // if (!Helpers.IsConstructor(instruction.Method))
                //addLabel(instruction);

                var arguments = string.Join(", ", instruction.Arguments);

                var methodName = instruction.Method.ContainingType.FullName() + "." + instruction.Method.Name.Value;

                if (methodName == "System.Diagnostics.Contracts.Contract.Assert")
                {
                    sb.Append(String.Format("\t\t assert {0};", arguments));
                    return;
                } else if (methodName == "System.Diagnostics.Contracts.Contract.Assume")
                {
                    sb.Append(String.Format("\t\t assume {0};", arguments));
                    return;
                } else if (instruction.Operation == MethodCallOperation.Virtual)
                {
                    DynamicDispatch(instruction, arguments);
                    return;
                }

                var signature = Helpers.GetMethodName(instruction.Method);

                if (instruction.HasResult)
                    sb.Append(String.Format("\t\tcall {0} := {1}({2});", instruction.Result, signature, arguments));
                else
                    sb.Append(String.Format("\t\tcall {0}({1});", signature, arguments));
                
                if (Helpers.IsExternal(instruction.Method.ResolvedMethod))
                    ExternMethodsCalled.Add(instruction.Method);
                // Important not to add methods to both sets.
                else if (Helpers.IsCurrentlyMissing(instruction.Method.ResolvedMethod))
                    PotentiallyMissingMethodsCalled.Add(instruction.Method);
            }

            public override void Visit(ConditionalBranchInstruction instruction)
            {
                //addLabel(instruction);

                IVariable leftOperand = instruction.LeftOperand;
                IInmediateValue rightOperand = instruction.RightOperand;

                // Binary operations get translated into method calls:
                // ops:
                //     ==: System.String.op_Equality$System.String$System.String
                //     !=: ? TODO(rcastano): check
                System.Diagnostics.Contracts.Contract.Assume(
                    !leftOperand.Type.TypeCode.Equals(PrimitiveTypeCode.String) && !rightOperand.Type.TypeCode.Equals(PrimitiveTypeCode.String));

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

                //addLabel(instruction);
                sb.AppendLine(String.Format("\t\tcall {0}:= Alloc();", instruction.Result));
                var type = Helpers.GetNormalizedType(instruction.AllocationType);
                InstructionTranslator.mentionedClasses.Add(instruction.AllocationType.ResolvedType);
                sb.AppendLine(String.Format("\t\tassume $DynamicType({0}) == T${1}();", instruction.Result, type));
                sb.AppendLine(String.Format("\t\tassume $TypeConstructor($DynamicType({0})) == T${1};", instruction.Result, type));
            }

            public override void Visit(StoreInstruction instruction)
            {
                //addLabel(instruction);

                var op = instruction.Operand; // what it is stored
                var instanceFieldAccess = instruction.Result as InstanceFieldAccess; // where it is stored
                string opStr = op.ToString();
                if (op.Type.TypeCode.Equals(PrimitiveTypeCode.String))
                {
                    opStr = Helpers.Strings.fixStringLiteral(op);
                }

                if (instanceFieldAccess != null)
                {
                    String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

                    if (Helpers.GetBoogieType(op.Type).Equals("int"))
                    {
                        sb.AppendLine(String.Format("\t\tassume Union2Int(Int2Union({0})) == {0};", opStr));
                        sb.Append(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, String.Format("Int2Union({0})", opStr)));
                    }
                    else if (Helpers.GetBoogieType(op.Type).Equals("Ref"))
                    {
                        //sb.AppendLine(String.Format("\t\tassume Union2Int(Int2Union({0})) == {0};", opStr));
                        // Union y Ref son el mismo type, forman un alias.
                        sb.Append(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, /*String.Format("Int2Union({0})", opStr)*/opStr));
                    }
                    else
                    {
                        // Not supporting these cases yet
                        System.Diagnostics.Contracts.Contract.Assume(false);
                    }
                }
                else
                {
                    // static fields are considered global variables
                    var staticFieldAccess = instruction.Result as StaticFieldAccess;
                    if (staticFieldAccess != null)
                    {
                        String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
                        sb.Append(String.Format("\t\t{0} := {1};", fieldName, opStr));
                    }
                }
            }

            public override void Visit(ConvertInstruction instruction)
            {
                //addLabel(instruction);
                var source = instruction.Operand;
                var dest = instruction.Result;
                var type = instruction.ConversionType;
                System.Diagnostics.Contracts.Contract.Assume(!source.Type.TypeCode.Equals(PrimitiveTypeCode.String));

                sb.Append(String.Format("\t\t{0} := $As({1},T${2}());", dest, source, type.ToString()));
            }

            public override void Visit(InitializeObjectInstruction instruction)
            {
                //addLabel(instruction);
                Contract.Assume(instruction.Variables.Count == 1);
                foreach (var var in instruction.Variables)
                {
                    LoadInstruction loadInstruction = this.instTranslator.lastInstruction as LoadInstruction;
                    Contract.Assume(loadInstruction != null);
                    Backend.ThreeAddressCode.Values.Reference reference = (Backend.ThreeAddressCode.Values.Reference) loadInstruction.Operand;
                    IAssignableValue where = reference.Value as IAssignableValue;
                    Contract.Assume(where != null);
                    var instanceFieldAccess = where as InstanceFieldAccess; // where it is stored
                    string valueStr = "0";

                    if (instanceFieldAccess != null)
                    {
                        String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);
                        
                        sb.AppendLine(String.Format("\t\tassume Union2Int(Int2Union({0})) == {0};", valueStr));
                        sb.Append(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, String.Format("Int2Union({0})", valueStr)));
                    }
                    else
                    {
                        // static fields are considered global variables
                        var staticFieldAccess = where as StaticFieldAccess;
                        if (staticFieldAccess != null)
                        {
                            String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
                            sb.Append(String.Format("\t\t{0} := {1};", fieldName, valueStr));
                        }
                    }
                }
            }
        }

        // it is triggered when Load, CreateObject and MethodCall instructions are seen in this order.
        // Load must be of a static or virtual method (currently only static is supported)
        class DelegateCreationTranslation : Translation
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
            public static bool IsDelegateCreationTranslation(IList<Instruction> instructions, int idx)
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
            private static CreateObjectInstruction createObjIns = null;

            public DelegateCreationTranslation(InstructionTranslator p) : base(p) {}

            public override void Visit(LoadInstruction instruction)
            {
                // todo: modify when delegates for virtual method is implemented
                Contract.Assert(instruction.Operand is StaticMethodReference);
                loadIns = instruction;

                instTranslator.RemovedVariables.UnionWith(instruction.ModifiedVariables);

                // continues in CreateObjectInstruction
            }

            public override void Visit(CreateObjectInstruction instruction)
            {
                // we ensure that before this instruction there was a load instruction
                //Contract.Assert(loadIns != null);

                createObjIns = instruction;

                /*var loadDelegateStmt = loadIns.Operand as StaticMethodReference;
                var methodRef = loadDelegateStmt.Method;
                var methodId = DelegateStore.GetMethodIdentifier(methodRef);

                // do we have to type this reference?
                sb.AppendLine(String.Format("\t\tcall {0}:= CreateDelegate({1}, {2}, {3});", instruction.Result, methodId, "null", "Type0()"));*/
                // continues in MethodCallInstruction
            }

            public override void Visit(MethodCallInstruction instruction)
            {
                Contract.Assert(loadIns != null && createObjIns != null);
                //addLabel(instruction);

                var loadDelegateStmt = loadIns.Operand as StaticMethodReference;
                var methodRef = loadDelegateStmt.Method;
                var methodId = DelegateStore.GetMethodIdentifier(methodRef);

                DelegateStore.AddDelegatedMethodToGroup(Helpers.GetNormalizedType(instruction.Method.ContainingType), methodRef);

                // invoke the correct version of create delegate
                var normalizedType = Helpers.GetNormalizedType(instruction.Method.ContainingType);
                IVariable receiverObject = instruction.Arguments[1];
                sb.AppendLine(String.Format("\t\tcall {0}:= CreateDelegate_{1}({2}, {3}, {4});", createObjIns.Result, normalizedType, methodId, receiverObject, "Type0()"));

                loadIns = null;
                createObjIns = null;
            }
        }

        class DelegateInvokeTranslation : Translation
        {
            public DelegateInvokeTranslation(InstructionTranslator p) : base(p) {}

            public override void Visit(MethodCallInstruction instruction)
            {
                //addLabel(instruction);

                // this local variable will hold the InvokeDelegate result 
                // the intent is to translate its type to Union (or Ref they are alias)
                var localVar = new LocalVariable(String.Format("$delegate_res_{0}", instTranslator.delegateInvokations), false);
                instTranslator.delegateInvokations =  1 + instTranslator.delegateInvokations;
                localVar.Type = Types.Instance.PlatformType.SystemObject;
                instTranslator.AddedVariables.Add(localVar);

                // create if it doesnt exist yet
                DelegateStore.CreateDelegateGroup(Helpers.GetNormalizedType(instruction.Method.ContainingType));

                // todo: this depends on the arguments types
                // todo: this must  be generalized because different types can casted to union
                foreach (var argument in instruction.Arguments.Skip(1)) // first argument is the delegate object
                {
                    var argType = Helpers.GetBoogieType(argument.Type);
                    if (argType.Equals("Ref")) // Ref and Union are alias
                        continue;
                    argType = argType.First().ToString().ToUpper() + argType.Substring(1).ToLower();
                    sb.AppendLine(String.Format("\t\tassume Union2{0}({0}2Union({1})) == {1};", argType, argument));
                }

                //sb.AppendLine(String.Format("\t\tassume Union2Int(Int2Union({0})) == {0};", instruction.Arguments[1]));

                /* 
                    // example of desired code
                    var $res_invoke : Union;
                    L_0000:
                    L_0002:
                        $r1 := 1;
                    L_0003:
                        assume Union2Int(Int2Union(1)) == 1; // depends on the delegate type
                        call $res_invoke := InvokeDelegate(f,Int2Union($r1)); -> there can be more than 1 argument
                        r := Union2Int($res_invoke);
                */

                var arguments2Union = new List<String>();
                foreach (var argument in instruction.Arguments.Skip(1))
                {
                    var argType = Helpers.GetBoogieType(argument.Type);
                    argType = argType.First().ToString().ToUpper() + argType.Substring(1).ToLower();
                    if (argType.Equals("Ref")) // Ref and Union are alias
                    {
                        arguments2Union.Add(argument.ToString());
                    } else
                    {
                        arguments2Union.Add(String.Format("{0}2Union({1})", argType, argument.ToString()));
                    }
                }

                var arguments = arguments2Union.Count > 0 ? "," + String.Join(",",arguments2Union) : String.Empty;

                // invoke the correct version of invoke delegate
                var normalizedType = Helpers.GetNormalizedType(instruction.Method.ContainingType);
                if (instruction.HasResult)
                    sb.AppendLine(String.Format("\t\tcall {0} := InvokeDelegate_{1}({2} {3});", localVar, normalizedType, instruction.Arguments[0], arguments));
                else
                    sb.AppendLine(String.Format("\t\tcall InvokeDelegate_{1}({2} {3});", localVar, normalizedType, instruction.Arguments[0], arguments));

                if (instruction.HasResult)
                {
                    // the union depends on the type of the arguments
                    var argType = Helpers.GetBoogieType(instruction.Result.Type);
                    if (argType.Equals("Ref")) // Ref and Union are alias
                        sb.AppendLine(String.Format("\t\t{0} := {1};", instruction.Result, localVar));
                    else
                    {
                        argType = argType.First().ToString().ToUpper() + argType.Substring(1).ToLower();
                        sb.AppendLine(String.Format("\t\t{0} := Union2{2}({1});", instruction.Result, localVar, argType));
                    }
                }
            }

            public static bool IsDelegateInvokeTranslation(IList<Instruction> instructions, int idx)
            {
                MethodCallInstruction instruction = instructions[idx] as MethodCallInstruction;

                if (instruction == null)
                    return false;

                if (instruction.Method.ContainingType.ResolvedType.IsDelegate &&
                    instruction.Method.Name.Value.Equals("Invoke")) // better way?
                    return true;

                return false;
            }
        }
    }

    public class DelegateStore
    {
        static IDictionary<IMethodReference, string> methodIdentifiers =
                new Dictionary<IMethodReference, string>();

        public static IDictionary<string, ISet<IMethodReference>> MethodGrouping
            = new Dictionary<string, ISet<IMethodReference>>();

        public static void AddDelegatedMethodToGroup(string tRef, IMethodReference mRef)
        {
            if (MethodGrouping.ContainsKey(tRef))
                MethodGrouping[tRef].Add(mRef);
            else
            {
                MethodGrouping[tRef] = new HashSet<IMethodReference>();
                MethodGrouping[tRef].Add(mRef);
            }
        }

        public static  void CreateDelegateGroup(string containingType)
        {
            if (MethodGrouping.ContainsKey(containingType))
                return;

            MethodGrouping[containingType] = new HashSet<IMethodReference>();
        }

        public static string CreateDelegateMethod(String typeRef)
        {
            var sb = new StringBuilder();
            var normalizedType = typeRef;// Helpers.GetNormalizedType(typeRef);

            sb.AppendLine(String.Format("procedure {{:inline 1}} CreateDelegate_{0}(Method: int, Receiver: Ref, TypeParameters: Ref) returns(c: Ref);", normalizedType));
            sb.AppendLine(String.Format("implementation {{:inline 1}} CreateDelegate_{0}(Method: int, Receiver: Ref, TypeParameters: Ref) returns(c: Ref)", normalizedType));
            sb.AppendLine("{");
            sb.AppendLine("     call c := Alloc();");
            sb.AppendLine("     assume $RefToDelegateReceiver(Method, c) == Receiver;");
            sb.AppendLine("     assume $RefToDelegateTypeParameters(Method, c) == TypeParameters;");

            foreach (var method in MethodGrouping[typeRef])
            {
                var id = methodIdentifiers[method];
                sb.AppendLine(String.Format("     assume $RefToDelegateMethod({0}, c) <==> Method == {0};", id));
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string CreateDelegateMethod()
        {
            var sb = new StringBuilder();

            foreach (var typeRef in MethodGrouping.Keys)
                sb.AppendLine(CreateDelegateMethod(typeRef));

            return sb.ToString();
        }

        public static string InvokeDelegateMethod()
        {
            var sb = new StringBuilder();

            foreach (var typeRef in MethodGrouping.Keys)
                sb.AppendLine(InvokeDelegateMethod(typeRef));

            return sb.ToString();
        }

        public static string InvokeDelegateMethod(string typeRef)
        {
            var sb = new StringBuilder();
            var normalizedType = typeRef;//Helpers.GetNormalizedType(typeRef);

            var methodRef = MethodGrouping[typeRef].First(); // get a reference to a Method in the group. All should have the same return type and parameters type

            // we should parametrize the parameters and return's types
            // currently we are only supporting static int -> int methods

            var argInList = new List<string>();

            bool hasReturnVariable = methodRef.Type.TypeCode != PrimitiveTypeCode.Void;

            // for virtual methods we may have to change this to include the reference to the object receiving the message
            var parameters = hasReturnVariable ? ","+ String.Join(",", methodRef.Parameters.Select(v => String.Format("arg{0}$in", v.Index) + " : Ref")) : String.Empty;

            sb.AppendLine(String.Format("procedure {{:inline 1}} InvokeDelegate_{0}($this: Ref{1}) {2};", normalizedType, parameters, hasReturnVariable ? "returns ($r: Ref)" : String.Empty));
            sb.AppendLine(String.Format("implementation {{:inline 1}} InvokeDelegate_{0}($this: Ref{1}) {2}", normalizedType, parameters, hasReturnVariable ? "returns ($r: Ref)" : String.Empty));
            sb.AppendLine("{");

            // we declare a local for each parameter - local will not be union, will be the real type (boogie version)
            // the resultRealType variable is for the return value if any - then it will be casted to Union and will be the return value

            foreach (var v in methodRef.Parameters)
                sb.AppendLine(String.Format("\tvar local{0} : {1};", v.Index, Helpers.GetBoogieType(v.Type)));

            if (hasReturnVariable)
                sb.AppendLine(String.Format("\tvar resultRealType: {0};", Helpers.GetBoogieType(methodRef.Type)));

            foreach (var method in MethodGrouping[typeRef])
            {
                var id = methodIdentifiers[method];

                sb.AppendLine(String.Format("\tif ($RefToDelegateMethod({0}, $this))", id));
                sb.AppendLine("\t{");
                // every argument is casted to Union (if they are Ref it is not necessary, they are alias)
                foreach (var v in methodRef.Parameters)
                {
                    var argType = Helpers.GetBoogieType(v.Type);
                    if (argType.Equals("Ref")) // Ref and Union are alias
                        sb.AppendLine(String.Format("\t\tlocal{0} := arg{0}$in;", v.Index));
                    else {
                        argType = argType.First().ToString().ToUpper() + argType.Substring(1).ToLower();
                        sb.AppendLine(String.Format("\t\tlocal{0} := Union2{1}(arg{0}$in);", v.Index, argType));
                    }
                }

                // we may have to add the receiver object for virtual methods
                var args = new List<string>();
                if (method.CallingConvention.HasFlag(Microsoft.Cci.CallingConvention.HasThis))
                {
                    var receiverObject = String.Format("$RefToDelegateReceiver({0}, $this)", id);
                    args.Add(receiverObject);
                }
                foreach (var v in methodRef.Parameters)
                    args.Add(String.Format("local{0}", v.Index));

                if (hasReturnVariable)
                    sb.AppendLine(String.Format("\t\tcall resultRealType := {0}({1});", Helpers.GetMethodName(method), String.Join(",", args)));
                else
                    sb.AppendLine(String.Format("\t\tcall {0}({1});", Helpers.GetMethodName(method), String.Join(",", args)));

                if (hasReturnVariable)
                {
                    if (Helpers.GetBoogieType(methodRef.Type).Equals("Ref"))
                        sb.AppendLine("\t\t$r := resultRealType;");
                    else
                    {
                        var argType = Helpers.GetBoogieType(methodRef.Type);
                        argType = argType.First().ToString().ToUpper() + argType.Substring(1).ToLower();
                        sb.AppendLine(String.Format("\t\tassume Union2{0}({0}2Union(resultRealType)) == resultRealType;", argType));
                        sb.AppendLine(String.Format("\t\t$r := {0}2Union(resultRealType);", argType));
                    }
                }

                sb.AppendLine("\t\treturn;");
                sb.AppendLine("\t}");
            }

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
