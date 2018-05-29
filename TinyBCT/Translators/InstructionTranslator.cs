using Backend;
using Backend.Model;
using Backend.ThreeAddressCode;
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
        private MethodBody methodBody; // currently used to get ExceptionsInformation
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
        public static ISet<ITypeReference> MentionedClasses = new HashSet<ITypeReference>();

        Translation translation;
        Instruction lastInstruction = null;
        // while translating instructions some variables may be removed
        // for example for delegates there are instructions that are no longer used
        public ISet<IVariable> RemovedVariables { get; } = new HashSet<IVariable>();
        public ISet<IVariable> AddedVariables { get; } = new HashSet<IVariable>();

        private IPrimarySourceLocation prevSourceLocation;

        protected IMethodDefinition method;

        public InstructionTranslator(ClassHierarchyAnalysis CHA, MethodBody methodBody)
        {
            this.CHA = CHA;
            this.method = methodBody.MethodDefinition;
            this.methodBody = methodBody;
        }

        private StringBuilder sb = new StringBuilder();
        public void AddBoogie(string boogie)
        {
            sb.AppendLine(boogie);
        }
        public string Boogie()
        {
            return sb.ToString().Replace("<>", "__");
        }

        public void Translate(/*IList<Instruction> instructions, int idx*/)
        {
            var instructions = methodBody.Instructions;
            for (int idx = 0; idx < instructions.Count(); idx++)
            {
                SetState(instructions, idx);
                var currentInstruction = instructions[idx];
                translation.AddLabel(currentInstruction);
                translation.AddLineNumbers(currentInstruction, prevSourceLocation);
                if (currentInstruction.Location != null)
                    prevSourceLocation = currentInstruction.Location;
                instructions[idx].Accept(translation);
                lastInstruction = instructions[idx];
            }
        }

        void SetState(IList<Instruction> instructions, int idx)
        {
           if (DelegateInvokeTranslation.IsDelegateInvokeTranslation(instructions, idx))
                translation = new DelegateInvokeTranslation(this);
            else if (DelegateCreationTranslation.IsDelegateCreationTranslation(instructions, idx))
                translation = new DelegateCreationTranslation(this);
            else if (ExceptionTranslation.IsExceptionTranslation(instructions, idx))
                translation = new ExceptionTranslation(this);
            else if (ArrayTranslation.IsArrayTranslation(instructions, idx))
                translation = new ArrayTranslation(this);
            else
                translation = new SimpleTranslation(this);
        }

        protected abstract class Translation : InstructionVisitor
        {
            protected InstructionTranslator instTranslator;
            public Translation(InstructionTranslator p)
            {
                instTranslator = p;
            }

            protected void AddBoogie(string boogie)
            {
                instTranslator.AddBoogie(boogie);
            }

            internal void AddLabel(Instruction instr)
            { 
                string label = instr.Label;
                if(!String.IsNullOrEmpty(label))
                   AddBoogie(String.Format("\t{0}:", label));                    
            }

            internal void AddLineNumbers(Instruction instr, IPrimarySourceLocation prevSourceLocation)
            {
                if (!Settings.EmitLineNumbers)
                    return;
                IPrimarySourceLocation location = instr.Location;

                if (location == null && prevSourceLocation != null)
                {
                    location = prevSourceLocation;
                }
                if(location!=null)
                { 
                    var fileName = location.SourceDocument.Name;
                    var sourceLine = location.StartLine;
                    AddBoogie(String.Format("\t\t assert {{:sourceFile \"{0}\"}} {{:sourceLine {1} }} true;", fileName, sourceLine));
                    //     assert {:first} {:sourceFile "C:\Users\diegog\source\repos\corral\AddOns\AngelicVerifierNull\test\c#\As\As.cs"} {:sourceLine 23} true;
                }
                else 
                {
                    AddBoogie(String.Format("\t\t assert {{:sourceFile \"{0}\"}} {{:sourceLine {1} }} true;", "Empty", 0));
                }
            }

            protected void DynamicDispatch(IMethodReference method, IVariable receiver, MethodCallOperation operation, Action<IMethodReference> actionOnPotentialCallee)
            {
                var calless = Helpers.PotentialCalleesUsingCHA(method, receiver, operation, Traverser.CHA);

                if (calless.Count > 1)
                {
                    // note: analysis-net changed and required to pass a method reference in the LocalVariable constructor
                    var getTypeVar = AddNewLocalVariableToMethod("DynamicDispatch_Type_", Types.Instance.PlatformType.SystemObject);

                    AddBoogie(String.Format("\t\tcall {0} := System.Object.GetType({1});", getTypeVar, receiver));

                    // example:if ($tmp6 == T$DynamicDispatch.Dog())

                    AddBoogie(String.Format("\t\tif ($Subtype({0},T${1}()))", getTypeVar, Helpers.GetNormalizedType(calless.First().ContainingType)));
                    AddBoogie("\t\t{");

                    actionOnPotentialCallee(calless.First());
                    AddBoogie("\t\t}");

                    int i = 0;
                    foreach (var impl in calless)
                    {
                        MentionedClasses.Add(impl.ContainingType);
                        // first  invocation is not handled in this loop
                        if (i == 0)
                        {
                            i++;
                            continue;
                        }

                        AddBoogie(String.Format("\t\telse if ($Subtype({0},T${1}()))", getTypeVar, Helpers.GetNormalizedType(impl.ContainingType)));
                        AddBoogie("\t\t{");

                        actionOnPotentialCallee(impl);

                        AddBoogie("\t\t}");
                        i++;
                    }

                    AddBoogie(String.Format("\t\telse", getTypeVar, Helpers.GetNormalizedType(calless.Last().ContainingType)));
                    AddBoogie("\t\t{");
                    AddBoogie("\t\t assert false;");
                    AddBoogie("\t\t}");
                }
                else
                {
                    actionOnPotentialCallee(method);
                    SimpleTranslation.AddToExternalMethods(method);
                }

                if (Helpers.IsExternal(method.ResolvedMethod) || method.ResolvedMethod.IsAbstract)
                    SimpleTranslation.AddToExternalMethods(method);
            }

            public IVariable AddNewLocalVariableToMethod(string name, ITypeReference type, bool isParameter = false)
            {
                string variableName = String.Format("{0}{1}", name, instTranslator.AddedVariables.Count);
                var tempVar = new LocalVariable(variableName, isParameter, instTranslator.method);
                tempVar.Type = type;
                instTranslator.AddedVariables.Add(tempVar);

                return tempVar;
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
            }

            public override void Visit(BinaryInstruction instruction)
            {
                //addLabel(instruction);

                IVariable left = instruction.LeftOperand;
                IVariable right = instruction.RightOperand;


                if (left.Type.TypeCode.Equals(PrimitiveTypeCode.String) ||
                    right.Type.TypeCode.Equals(PrimitiveTypeCode.String))
                {
                    string methodName = Helpers.Strings.GetBinaryMethod(instruction.Operation);

                    var tempVar = AddNewLocalVariableToMethod("tempVarStringBinOp_", instruction.Result.Type);
                    var arguments = new List<string>();
                    arguments.Add(Helpers.Strings.fixStringLiteral(left));
                    arguments.Add(Helpers.Strings.fixStringLiteral(right));
                    AddBoogie(BoogieGenerator.Instance().ProcedureCall(methodName, arguments, tempVar.ToString()));
                    AddBoogie(BoogieGenerator.Instance().VariableAssignment(instruction.Result, tempVar.ToString()));
                } else
                {
                    // TODO(rcastano): refactor this. Ideally, there might be different BoogieGenerators,
                    // each making slightly different translations, for example, bitvector representation
                    // of integers as opposed to Boogie int.
                    // When that happens, this should most likely be encapsulated within BoogieGenerator.
                    if (BoogieGenerator.IsSupportedBinaryOperation(instruction.Operation))
                    {
                        var exp = BoogieGenerator.Instance().BinaryOperationExpression(left, right, instruction.Operation);
                        var assignment = BoogieGenerator.Instance().VariableAssignment(instruction.Result.ToString(), exp);
                        AddBoogie(assignment);
                    }
                    else
                    {
                        AddBoogie(BoogieGenerator.Instance().HavocResult(instruction));
                    }
                }
            }

            public override void Visit(UnconditionalBranchInstruction instruction)
            {
                if (instruction.IsLeaveProtectedBlock)
                {
                    // this is a special goto statement
                    // it is used to exit a try or a catch

                    // we should check if there is a finally statement in the same try where this instruction is located
                    // if there is one, we jump to it.

                    // remember that there can be a finally when there are no catches or viceversa

                    var branchOffset = instruction.Offset;

                    // check if it is inside a catch handler
                    var catchContaining = from pb in instTranslator.methodBody.ExceptionInformation
                                     where
                                        branchOffset >= Convert.ToInt32(pb.Handler.Start.Substring(2),16) 
                                        && branchOffset <= Convert.ToInt32(pb.Handler.End.Substring(2), 16)
                                        && pb.Handler.Kind == ExceptionHandlerBlockKind.Catch
                                           orderby Convert.ToInt32(pb.Start.Substring(2), 16) descending
                                           select pb;

                    // check if it is in a try
                    var tryContaining = from pb in instTranslator.methodBody.ExceptionInformation
                                          where
                                             branchOffset >= Convert.ToInt32(pb.Start.Substring(2), 16)
                                             && branchOffset <= Convert.ToInt32(pb.End.Substring(2), 16)
                                             //&& pb.Handler.Kind != ExceptionHandlerBlockKind.Catch
                                          orderby Convert.ToInt32(pb.Start.Substring(2), 16) descending
                                          select pb;

                    ProtectedBlock containingBlock = null;

                    if (catchContaining.Count() > 0 && tryContaining.Count() == 0)
                        containingBlock = catchContaining.First();
                    else if (catchContaining.Count() == 0 && tryContaining.Count() > 0)
                        containingBlock = tryContaining.First();
                    else if (catchContaining.Count() > 0 && tryContaining.Count() > 0)
                    {
                        var cStart = Convert.ToInt32(catchContaining.First().Start.Substring(2), 16);
                        var tStart = Convert.ToInt32(tryContaining.First().Start.Substring(2), 16);
                        containingBlock = cStart > tStart ? catchContaining.First() : tryContaining.First();
                    }

                    // we know where the protected block starts, we look for a finally handler in the same level.
                    var target = from pb in instTranslator.methodBody.ExceptionInformation
                                 where
                                     pb.Start == containingBlock.Start && pb.Handler.Kind == ExceptionHandlerBlockKind.Finally
                                 orderby Convert.ToInt32(pb.Start.Substring(2), 16) descending
                                 select pb.Handler.Start;

                    if (target.Count() > 0) // is there a finally?
                    {
                        AddBoogie(BoogieGenerator.Instance().Goto(target.First()));
                        return;
                    }

                }

                AddBoogie(BoogieGenerator.Instance().Goto(instruction.Target));
            }

            public override void Visit(ReturnInstruction instruction)
            {
                if (instruction.HasOperand)
                    AddBoogie(BoogieGenerator.Instance().VariableAssignment("$result", instruction.Operand.Name));
            }

            public override void Visit(LoadInstruction instruction)
            {
                var instructionOperand = instruction.Operand;
                if (instructionOperand is Reference)
                {
                    // Reference loading only found when using "default" keyword.
                    // Ignoring translation, the actual value referenced is used by accessing
                    // instTranslator.lastInstruction [see Visit(InitializeObjectInstruction instruction)]
                    // TODO(rcastano): check that loaded references are only used in the assumed context.
                    instructionOperand = (instructionOperand as Reference).Value;
                }
                if (instructionOperand is InstanceFieldAccess) // memory access handling
                {
                    InstanceFieldAccess instanceFieldOp = instructionOperand as InstanceFieldAccess;
                    AddBoogie(BoogieGenerator.Instance().ReadInstanceField(instanceFieldOp, instruction.Result));
                }
                else if (instructionOperand is StaticFieldAccess) // memory access handling
                {
                    // static fields are considered global variables
                    var staticFieldAccess = instructionOperand as StaticFieldAccess;

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
                        AddBoogie(BoogieGenerator.Instance().VariableAssignment(FieldTranslator.GetFieldName(staticFieldAccess.Field), "null"));
                    }

                    AddBoogie(BoogieGenerator.Instance().ReadStaticField(staticFieldAccess, instruction.Result));
                }

                else if (instructionOperand is StaticMethodReference) // delegates handling
                {
                    // see DelegateTranslation
                }
                else
                {
                    string operand = instructionOperand.Type.TypeCode.Equals(PrimitiveTypeCode.String) ?
                        Helpers.Strings.fixStringLiteral(instructionOperand) :
                        instructionOperand.ToString();
                    AddBoogie(BoogieGenerator.Instance().VariableAssignment(instruction.Result, operand));
                }

                if (instruction.Result is IVariable &&
                    instruction.Result.Type.TypeCode.Equals(PrimitiveTypeCode.String))
                {
                    MentionedClasses.Add(instruction.Result.Type);
                    AddBoogie(BoogieGenerator.Instance().AssumeDynamicType(instruction.Result, instruction.Result.Type));
                    AddBoogie(BoogieGenerator.Instance().AssumeTypeConstructor(BoogieGenerator.Instance().DynamicType(instruction.Result), instruction.Result.Type));
                }
            }

            public static void AddToExternalMethods(IMethodReference method)
            {
                ExternMethodsCalled.Add(method.ResolvedMethod);
            }

            private void CallMethod(MethodCallInstruction instruction, List<IVariable> arguments, IMethodReference callee)
            {
                var signature = Helpers.GetMethodName(callee);

                if (instruction.HasResult)
                {
                    //         call $tmp0 := DynamicDispatch.Mammal.Breathe(a);
                    // the union depends on the type of the arguments
                    var resType = Helpers.GetBoogieType(instruction.Result.Type);
                    var methodType = Helpers.GetMethodBoogieReturnType(Helpers.GetUnspecializedVersion(callee));
                    if (methodType.Equals(resType) || resType.Equals("Ref")) // Ref and Union are alias
                    {
                        AddBoogie(BoogieGenerator.Instance().ProcedureCall(callee, arguments, instruction.Result));
                    }
                    else
                    {
                        // TODO(rcastano): reuse variable
                        var localVar = AddNewLocalVariableToMethod("$temp_var_", Types.Instance.PlatformType.SystemObject, false);
                        AddBoogie(BoogieGenerator.Instance().ProcedureCall(callee, arguments, localVar));
                        resType = resType.First().ToString().ToUpper() + resType.Substring(1).ToLower();
                        AddBoogie(BoogieGenerator.Instance().VariableAssignment(instruction.Result, BoogieGenerator.Instance().Union2PrimitiveType(resType, localVar.Name)));
                    }
                }
                else
                {
                    AddBoogie(BoogieGenerator.Instance().ProcedureCall(callee, arguments));
                }
            }

            public override void Visit(MethodCallInstruction instruction)
            {
                // This is check is done because an object creation is splitted into two TAC instructions
                // This prevents to add the same instruction tag twice
                // DIEGO: Removed after fix in analysis framewlrk 
                // if (!Helpers.IsConstructor(instruction.Method))
                //addLabel(instruction);
                //var arguments = "";
                List<string> toAppend = new List<string>();

                var methodName = instruction.Method.ContainingType.FullName() + "." + instruction.Method.Name.Value;
                var copyArgs = ComputeArguments(instruction, toAppend);

                foreach (var line in toAppend)
                {
                    AddBoogie(line);
                }

                if (methodName == "System.Diagnostics.Contracts.Contract.Assert")
                {
                    AddBoogie(BoogieGenerator.Instance().Assert(instruction.Variables.First()));
                    return;
                }
                else if (methodName == "System.Diagnostics.Contracts.Contract.Assume")
                {
                    AddBoogie(BoogieGenerator.Instance().Assume(instruction.Variables.First()));
                    return;
                }
                // Diego: BUGBUG. Some non-virtual but non-static call (i.e., on particular instances) are categorized as static!
                // Our examples are compiled agains mscorlib generic types and we need to replace some method with colection stubs
                // All the generics treatment is performed in Dynamic dispatch, so we are not converting some invocations 
                else if (instruction.Operation == MethodCallOperation.Virtual)
                {
                    Action<IMethodReference> onPotentialCallee = (potential => CallMethod(instruction, copyArgs, potential));
                    DynamicDispatch(instruction.Method, instruction.Arguments.Count > 0 ? instruction.Arguments[0] : null, instruction.Method.IsStatic ? MethodCallOperation.Static : MethodCallOperation.Virtual, onPotentialCallee);
                    ExceptionTranslation.HandleExceptionAfterMethodCall(instruction);
                    return;
                }

                var signature = Helpers.GetMethodName(instruction.Method);

                CallMethod(instruction, copyArgs, instruction.Method);

                ExceptionTranslation.HandleExceptionAfterMethodCall(instruction);

                if (Helpers.IsExternal(instruction.Method.ResolvedMethod))
                    AddToExternalMethods(instruction.Method);
                // Important not to add methods to both sets.
                else if (Helpers.IsCurrentlyMissing(instruction.Method.ResolvedMethod))
                    PotentiallyMissingMethodsCalled.Add(Helpers.GetUnspecializedVersion(instruction.Method));
            }

            private List<IVariable> ComputeArguments(MethodCallInstruction instruction,  List<string> toAppend)
            {
                var copyArgs = new List<IVariable>();

                var unspecializedMethod = Helpers.GetUnspecializedVersion(instruction.Method);
                Contract.Assume(
                    unspecializedMethod.Parameters.Count() == instruction.Arguments.Count() ||
                    unspecializedMethod.Parameters.Count() + 1 == instruction.Arguments.Count());
                // Instance methods, passing 'this'
                if (unspecializedMethod.Parameters.Count() != instruction.Arguments.Count())
                {
                    copyArgs.Add(instruction.Arguments.ElementAt(0));
                }
                for (int i = 0; i < instruction.Method.Parameters.Count(); ++i)
                {
                    int arg_i =
                        unspecializedMethod.Parameters.Count() == instruction.Arguments.Count() ?
                        i : i + 1;
                    var paramType = Helpers.GetBoogieType(unspecializedMethod.Parameters.ElementAt(i).Type);
                    var argType = Helpers.GetBoogieType(instruction.Arguments.ElementAt(arg_i).Type);
                    if (!paramType.Equals(argType))
                    {
                        // TODO(rcastano): try to reuse variables.
                        var localVar = AddNewLocalVariableToMethod("$temp_var_", Types.Instance.PlatformType.SystemObject, false);

                        var bg = BoogieGenerator.Instance();
                        // intended output: String.Format("\t\t{0} := {2}2Union({1});", localVar, instruction.Arguments.ElementAt(arg_i), argType)
                        toAppend.Add(bg.VariableAssignment(localVar, bg.PrimitiveType2Union(instruction.Arguments.ElementAt(arg_i))));

                        copyArgs.Add(localVar);
                    }
                    else
                    {
                        copyArgs.Add(instruction.Arguments.ElementAt(arg_i));
                    }
                }
                return copyArgs;
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

                AddBoogie(String.Format("\t\tif ({0} {1} {2})", leftOperand, operation, rightOperand));
                AddBoogie("\t\t{");
                AddBoogie(String.Format("\t\t\tgoto {0};", instruction.Target));
                AddBoogie("\t\t}");

            }

            public override void Visit(CreateObjectInstruction instruction)
            {
                // assume $DynamicType($tmp0) == T$TestType();
                //assume $TypeConstructor($DynamicType($tmp0)) == T$TestType;

                //addLabel(instruction);
                AddBoogie(String.Format("\t\tcall {0}:= Alloc();", instruction.Result));
                var type = Helpers.GetNormalizedType(TypeHelper.UninstantiateAndUnspecialize(instruction.AllocationType));
                InstructionTranslator.MentionedClasses.Add(instruction.AllocationType);
                AddBoogie(String.Format("\t\tassume $DynamicType({0}) == T${1}();", instruction.Result, type));
                AddBoogie(String.Format("\t\tassume $TypeConstructor($DynamicType({0})) == T${1};", instruction.Result, type));
            }

            public override void Visit(StoreInstruction instruction)
            {
                var instanceFieldAccess = instruction.Result as InstanceFieldAccess; // where it is stored
                if (instanceFieldAccess != null)
                {
                    var p = BoogieGenerator.Instance().WriteInstanceField(instanceFieldAccess, instruction.Operand);
                    AddBoogie(p);
                }
                else
                {
                    // static fields are considered global variables
                    var staticFieldAccess = instruction.Result as StaticFieldAccess;
                    if (staticFieldAccess != null)
                    {
                        var p = BoogieGenerator.Instance().WriteStaticField(staticFieldAccess, instruction.Operand);
                        AddBoogie(p);
                    }
                    else
                        Contract.Assume(false);
                }
            }

            public override void Visit(ConvertInstruction instruction)
            {
                //addLabel(instruction);
                var source = instruction.Operand;
                var dest = instruction.Result;
                var type = instruction.ConversionType;
                System.Diagnostics.Contracts.Contract.Assume(!source.Type.TypeCode.Equals(PrimitiveTypeCode.String));

                AddBoogie(String.Format("\t\t{0} := $As({1},T${2}());", dest, source, type.ToString()));
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
                    //IAssignableValue where = reference.Value as IAssignableValue;
                    var where = reference.Value;
                    Contract.Assume(where != null);

                    var instanceFieldAccess = where as InstanceFieldAccess; // where it is stored
                    string valueStr = "0";

                    if (instanceFieldAccess != null)
                    {
                        String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);
                        
                        AddBoogie(String.Format("\t\tassume Union2Int(Int2Union({0})) == {0};", valueStr));
                        AddBoogie(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, String.Format("Int2Union({0})", valueStr)));
                    }
                    else 
                    {
                        // static fields are considered global variables
                        var staticFieldAccess = where as StaticFieldAccess;
                        if (staticFieldAccess != null)
                        {
                            String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
                            AddBoogie(String.Format("\t\t{0} := {1};", fieldName, valueStr));
                        }
                        else if(where is IVariable)
                        {
                            // Diego BUG BUG
                            // We need to handle default(T) properly
                            var name = where.ToString();
                            AddBoogie(String.Format("\t\t{0} := {1};", name, valueStr));
                        }
                        else
                        {
                            Contract.Assert(false, "Not supported value");
                        }
                    }
                }
            }
        }

        protected class ArrayTranslation : Translation
        {
            public ArrayTranslation(InstructionTranslator p) : base(p)
            {
            }

            // hacky solution - I want to know if we are dealing with only a length instruction or length + convert
            private static bool onlyLengthNoConvert = false;

            public static bool IsArrayTranslation(IList<Instruction> instructions, int idx)
            {
                Instruction ins = instructions[idx];

                if (ins is CreateArrayInstruction)
                    return true;

                var load = ins as LoadInstruction;
                if (load != null && (load.Operand is ArrayElementAccess))
                    return true;

                // Length Access returns an uint
                // we will avoid the conversion from uint to int by directly returning the length

                // Load/Length Convert
                if (load != null && (load.Operand is ArrayLengthAccess) &&
                    idx+1 <= instructions.Count-1 && instructions[idx+1] is ConvertInstruction)
                    return true;

                // Load/Length Convert
                if (ins is ConvertInstruction && idx-1 >= 0 && instructions[idx-1] is LoadInstruction)
                {
                    var op = (instructions[idx - 1] as LoadInstruction).Operand as ArrayLengthAccess;
                    if (op != null)
                        return true;
                }

                // no convert instruction is also acceptable.
                if (load != null && (load.Operand is ArrayLengthAccess))
                {
                    onlyLengthNoConvert = true;
                    return true;
                }

                var store = ins as StoreInstruction;
                if (store != null && (store.Result is ArrayElementAccess /*|| store.Result is ArrayLengthAccess*/))
                    return true;

                return false;
            }

            public override void Visit(CreateArrayInstruction instruction)
            {
                /*
                    call $tmp0 := Alloc();
                    assume $ArrayLength($tmp0) == 1 * 510;
                    assume (forall $tmp1: int :: $ArrayContents[$tmp0][$tmp1] == null);
                */

                AddBoogie(String.Format("call {0} := Alloc();", instruction.Result));
                AddBoogie(String.Format("assume $ArrayLength({0}) == {1};", instruction.Result, string.Join<IVariable>(" * ",instruction.UsedVariables.ToArray())));
                AddBoogie(String.Format("assume (forall $tmp1: int :: $ArrayContents[{0}][$tmp1] == null);",instruction.Result));
            }

            private static LoadInstruction arrayLengthAccess = null;

            public override void Visit(ConvertInstruction instruction)
            {
                Contract.Assert(arrayLengthAccess != null);

                var op = arrayLengthAccess.Operand as ArrayLengthAccess;
                AddBoogie(String.Format("{0} := $ArrayLength({1});", instruction.Result, op.Instance));
                arrayLengthAccess = null;
            }

            public override void Visit(LoadInstruction instruction)
            {
                ArrayElementAccess elementAccess = instruction.Operand as ArrayElementAccess;
                ArrayLengthAccess lengthAccess = instruction.Operand as ArrayLengthAccess;

                if (lengthAccess != null)
                {
                    if (!onlyLengthNoConvert)
                    {
                        // Length Access returns an uint
                        // we will avoid the conversion from ptr to int by directly returning the length
                        // check ConvertInstruction
                        arrayLengthAccess = instruction;
                    }
                    else
                    {
                        AddBoogie(String.Format("{0} := $ArrayLength({1});", instruction.Result, lengthAccess.Instance));
                        onlyLengthNoConvert = false;
                    }

                    return;
                }

                if (elementAccess != null){

                    // multi-dimensional arrays (note that it is not an array of arrays)
                    // analysis-net is not working correctly with them
                    Contract.Assert(elementAccess.Indices.Count == 1);

                    var argType = Helpers.GetBoogieType(elementAccess.Type);
                    Contract.Assert(!String.IsNullOrEmpty(argType));
                    ReadArrayContent(instruction.Result, elementAccess.Array, elementAccess.Indices, argType);
                    return;
                }
            }

            // result is the original result variable of the instruction
            private void ReadArrayContent(IVariable insResult, IVariable array, IList<IVariable> indexes, string boogieType)
            {
                if (indexes.Count == 1)
                {
                    // we access the last array of the chain - this one returns the element we want
                    // that element could be a Union (Ref alias) or a non Union type. In the last case we must perform a cast.
                    if (boogieType.Equals("Ref"))
                    {
                        AddBoogie(String.Format("call {0} := $ReadArrayElement({1}, {2});",  insResult, array, indexes.First()));
                    }
                    else
                    {
                        // Store Union element and then cast it to the correct type
                        boogieType = boogieType.First().ToString().ToUpper() + boogieType.Substring(1).ToLower();

                        var tempVar = AddNewLocalVariableToMethod("$arrayElement", Types.Instance.PlatformType.SystemObject);
                        AddBoogie(String.Format("call {0} := $ReadArrayElement({1}, {2});", tempVar, array, indexes.First()));
                        AddBoogie(String.Format("{0} := Union2{2}({1});", insResult, tempVar, boogieType));
                    }
                } else
                {
                    var tempVar = AddNewLocalVariableToMethod("$arrayElement", Types.Instance.PlatformType.SystemObject);
                    AddBoogie(String.Format("call {0} := $ReadArrayElement({1}, {2});", insResult, array, indexes.First()));
                    ReadArrayContent(insResult, tempVar, indexes.Skip(1).ToList(), boogieType);
                }
            }

            public override void Visit(StoreInstruction instruction)
            {
                // multi-dimensional arrays are not supported yet - analysis-net is not working correctly
                ArrayElementAccess res = instruction.Result as ArrayElementAccess;

                Contract.Assert(res != null);
                var argType = Helpers.GetBoogieType(res.Type);
                Contract.Assert(!String.IsNullOrEmpty(argType));

                if (!argType.Equals("Ref")) // Ref and Union are alias
                {
                    /*
                        assume Union2Int(Int2Union(0)) == 0;
                        $ArrayContents := $ArrayContents[$ArrayContents[a][1] := $ArrayContents[$ArrayContents[a][1]][1 := Int2Union(0)]];
                    */
                    argType = argType.First().ToString().ToUpper() + argType.Substring(1).ToLower();

                    AddBoogie(String.Format("assume Union2{0}({0}2Union({1})) == {1};", argType, instruction.Operand));
                    AddBoogie(String.Format("call $WriteArrayElement({0}, {1}, {3}2Union({2}));", res.Array, res.Indices[0], instruction.Operand, argType));
                } else
                    AddBoogie(String.Format("call $WriteArrayElement({0}, {1}, {2});", res.Array, res.Indices[0], instruction.Operand));
            }
        }

        protected class ExceptionTranslation : Translation
        {
            public ExceptionTranslation(InstructionTranslator p) : base(p)
            {
            }

            public static bool IsExceptionTranslation(IList<Instruction> instructions, int idx)
            {
                if (!Settings.Exceptions)
                    return false;

                // hack to get endfinally
                var nopInstruction = instructions[idx] as NopInstruction;
                if (nopInstruction != null)
                {
                    return nopInstruction.IsEndFinally;
                }

                var unconditionalBranchInstruction = instructions[idx] as UnconditionalBranchInstruction;
                if (unconditionalBranchInstruction != null)
                {
                    return unconditionalBranchInstruction.IsLeaveProtectedBlock;
                }

                if (instructions[0] is ThrowInstruction || instructions[0] is CatchInstruction || instructions[0] is FinallyInstruction)
                    return true;

                return false;
            }

            public override void Visit(NopInstruction instruction)
            {
                // hack to get endfinally
                //if (instruction.IsEndFinally)
                //{
                    /*
                     The endfinally instruction transfers control out of a finally or fault block in the usual manner. 
                     This mean that if the finally block was being executed as a result of a leave statement in a try block, 
                     then execution continues at the next statement following the finally block. 
                     If, on the other hand, the finally block was being executed as a result of an exception having been thrown, 
                     then execution will transfer to the next suitable block of exception handling code. 

                        taken from: "Expert .NET 1.1 Programming"
                     */

                    // only encode behaviour if there was an unhandled exception
                    AddBoogie("\t\tif ($Exception != null)");
                    AddBoogie("\t\t{");
                    var target = GetThrowTarget(instruction);
                    if (String.IsNullOrEmpty(target))
                        AddBoogie("\t\t\treturn;");
                    else
                        AddBoogie(String.Format("\t\t\tgoto {0};", target));
                    AddBoogie("\t\t}");

                //}
                //addLabel(instruction);
            }

            public override void Visit(UnconditionalBranchInstruction instruction)
            {
                //if (instruction.IsLeaveProtectedBlock)
                //{
                    // this is a special goto statement
                    // it is used to exit a try or a catch

                    // we should check if there is a finally statement in the same try where this instruction is located
                    // if there is one, we jump to it.

                    // remember that there can be a finally when there are no catches or viceversa

                    var branchOffset = instruction.Offset;

                    // check if it is inside a catch handler
                    var catchContaining = from pb in instTranslator.methodBody.ExceptionInformation
                                          where
                                             branchOffset >= Convert.ToInt32(pb.Handler.Start.Substring(2), 16)
                                             && branchOffset <= Convert.ToInt32(pb.Handler.End.Substring(2), 16)
                                             && pb.Handler.Kind == ExceptionHandlerBlockKind.Catch
                                          orderby Convert.ToInt32(pb.Start.Substring(2), 16) descending
                                          select pb;

                    // check if it is in a try
                    var tryContaining = from pb in instTranslator.methodBody.ExceptionInformation
                                        where
                                           branchOffset >= Convert.ToInt32(pb.Start.Substring(2), 16)
                                           && branchOffset <= Convert.ToInt32(pb.End.Substring(2), 16)
                                        //&& pb.Handler.Kind != ExceptionHandlerBlockKind.Catch
                                        orderby Convert.ToInt32(pb.Start.Substring(2), 16) descending
                                        select pb;

                    ProtectedBlock containingBlock = null;

                    if (catchContaining.Count() > 0 && tryContaining.Count() == 0)
                        containingBlock = catchContaining.First();
                    else if (catchContaining.Count() == 0 && tryContaining.Count() > 0)
                        containingBlock = tryContaining.First();
                    else if (catchContaining.Count() > 0 && tryContaining.Count() > 0)
                    {
                        var cStart = Convert.ToInt32(catchContaining.First().Start.Substring(2), 16);
                        var tStart = Convert.ToInt32(tryContaining.First().Start.Substring(2), 16);
                        containingBlock = cStart > tStart ? catchContaining.First() : tryContaining.First();
                    }

                    // we know where the protected block starts, we look for a finally handler in the same level.
                    var target = from pb in instTranslator.methodBody.ExceptionInformation
                                 where
                                     pb.Start == containingBlock.Start && pb.Handler.Kind == ExceptionHandlerBlockKind.Finally
                                 orderby Convert.ToInt32(pb.Start.Substring(2), 16) descending
                                 select pb.Handler.Start;

                    if (target.Count() > 0) // is there a finally?
                    {
                        AddBoogie(String.Format("\t\tgoto {0};", target.First()));
                        return;
                    }

                //}

                AddBoogie(String.Format("\t\tgoto {0};", instruction.Target));
            }

            public override void Visit(TryInstruction instruction)
            {
                // nothing is done for this type of instruciton
            }

            public override void Visit(CatchInstruction instruction)
            {
                AddBoogie(String.Format("\t\tif (!$Subtype($ExceptionType, T${0}()))", Helpers.GetNormalizedType(instruction.ExceptionType)));
                AddBoogie("\t\t{");
                // we jump to next catch handler, finally handler or exit method with return.
                var nextHandler = GetNextHandlerIfCurrentCatchNotMatch(instruction);//GetNextExceptionHandlerLabel(instTranslator.methodBody.ExceptionInformation, instruction.Label);
                if (String.IsNullOrEmpty(nextHandler))
                    AddBoogie("\t\t\treturn;");
                else
                    AddBoogie(String.Format("\t\t\tgoto {0};", nextHandler));

                AddBoogie("\t\t}");

                //AddBoogie(String.Format());

                // Exception is handled we reset global variables
                AddBoogie(String.Format("\t\t{0} := $Exception;", instruction.Result));
                AddBoogie("\t\t$Exception := null;");
                AddBoogie("\t\t$ExceptionType := null;");
            }

            public override void Visit(ThrowInstruction instruction)
            {
                AddBoogie(String.Format("\t\tcall $ExceptionType := System.Object.GetType({0});", instruction.Operand));
                AddBoogie(String.Format("\t\t$Exception := {0};", instruction.Operand));

                var target = GetThrowTarget(instruction);

                if (String.IsNullOrEmpty(target))
                    AddBoogie("\t\treturn;");
                else
                    AddBoogie(String.Format("\t\tgoto {0};", target));
            }

            public override void Visit(FinallyInstruction instruction)
            {
                // we need to modify the last instruction
            }

            // if $Exception is not null, the method call instruction is handled as if it was a throw instruction
            // nearest catch handler is searched otherwise exit method.
            public static string HandleExceptionAfterMethodCall(Instruction ins)
            {
                if (!Settings.Exceptions)
                    return string.Empty;

                var sb = new StringBuilder();
                sb.AppendLine("\t\tif ($Exception != null)");
                sb.AppendLine("\t\t{");
                var label = GetThrowTarget(ins);
                if (String.IsNullOrEmpty(label))
                    sb.AppendLine("\t\t\treturn;");
                else
                    sb.AppendLine(String.Format("\t\t\tgoto {0};", label));
                sb.AppendLine("\t\t}");
                return sb.ToString();
            }

            // retrieves all succesors blocks of the block that contains the throw instruction (it could be a method call instruction)
            // we want to jump to the nearest catch handler, so we get the block with the nearest label.
            // if there is none, we should exit the method
            protected static string GetThrowTarget(Instruction instruction)
            {
                var node = Traverser.CFG.Nodes.Where((x => x.Instructions.Contains(instruction))).First();
                var successors = node.Successors.Where(block => block.Instructions.Any(ins => ins is CatchInstruction || ins is FinallyInstruction) && block.Instructions.First().Label != null);

                if (successors.Count() == 0)
                {
                    return string.Empty;
                }

                var offsetToBlock = successors.ToLookup(block => block.Instructions.First().Offset);
                var minOffset = successors.Select(block => block.Instructions.First().Offset).Min();

                return offsetToBlock[minOffset].First().Instructions.First().Label;
            }

            // next handler if current catch is not compatible with the exception type
            protected  string GetNextHandlerIfCurrentCatchNotMatch(Instruction instruction)
            {
                var node = Traverser.CFG.Nodes.Where((x => x.Instructions.Contains(instruction))).First();

                // predecessors of a catch is a try - exceptional cfg add edges to the block that make a try not the catch blocks.
                var successors = node.Predecessors.SelectMany(n => n.Successors).Where(n => n != node && n.Kind != CFGNodeKind.Exit);

                var possibleHandlers = successors.Where(block => block.Instructions.Any(ins => ins is CatchInstruction || ins is FinallyInstruction)
                                                                            && block.Instructions.First().Label != null && // is target of something
                                                                            block.Instructions.First().Offset > instruction.Offset); // handlers after this one.

                if (possibleHandlers.Count() == 0)
                {
                    return string.Empty;
                }

                var offsetToBlock = possibleHandlers.ToLookup(block => block.Instructions.First().Offset);
                var minOffset = possibleHandlers.Select(block => block.Instructions.First().Offset).Min();

                return offsetToBlock[minOffset].First().Instructions.First().Label;
            }
        }

        // it is triggered when Load, CreateObject and MethodCall instructions are seen in this order.
        // Load must be of a static or virtual method (currently only static is supported)
        class DelegateCreationTranslation : Translation
        {
            private static bool IsMethodReference(Instruction ins)
            {
                LoadInstruction loadIns = ins as LoadInstruction;
                if (loadIns != null)
                    return loadIns.Operand is StaticMethodReference || loadIns.Operand is VirtualMethodReference;
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
                Contract.Assert(instruction.Operand is StaticMethodReference || instruction.Operand is VirtualMethodReference);
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
                AddBoogie(String.Format("\t\tcall {0}:= CreateDelegate({1}, {2}, {3});", instruction.Result, methodId, "null", "Type0()"));*/
                // continues in MethodCallInstruction
            }

            public override void Visit(MethodCallInstruction instruction)
            {
                Contract.Assert(loadIns != null && createObjIns != null);
                //addLabel(instruction);

                // it can be VirtualMethodReference or StaticMethodReference
                var loadDelegateStmt = (loadIns.Operand as IFunctionReference) ;
                var methodRef = loadDelegateStmt.Method;
                IVariable receiverObject = instruction.Arguments[1];

                Action<IMethodReference> d = (potentialMethod) =>
                {
                    var methodId = DelegateStore.GetMethodIdentifier(potentialMethod);

                    // instruction.method.containingtype is the instantiated type
                    // i group them by their uninstanciated type
                    DelegateStore.AddDelegatedMethodToGroup(instruction.Method.ContainingType, potentialMethod);
                    // invoke the correct version of create delegate
                    var normalizedType = Helpers.GetNormalizedTypeForDelegates(instruction.Method.ContainingType);
                    AddBoogie(String.Format("\t\tcall {0}:= CreateDelegate_{1}({2}, {3}, {4});", createObjIns.Result, normalizedType, methodId, receiverObject, "Type0()"));
                    ExceptionTranslation.HandleExceptionAfterMethodCall(instruction);
                };

                DynamicDispatch(methodRef, receiverObject, methodRef.IsStatic ? MethodCallOperation.Static : MethodCallOperation.Virtual, d);

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
                // note: analysis-net changed and required to pass a method reference in the LocalVariable constructor
                var localVar = AddNewLocalVariableToMethod("$delegate_res_", Types.Instance.PlatformType.SystemObject, false);

                // create if it doesnt exist yet
                // i want the specialized type
                DelegateStore.CreateDelegateGroup(instruction.Method.ContainingType);

                foreach (var argument in instruction.Arguments.Skip(1)) // first argument is the delegate object
                {
                    var argType = Helpers.GetBoogieType(argument.Type);
                    if (argType.Equals("Ref")) // Ref and Union are alias
                        continue;
                    argType = argType.First().ToString().ToUpper() + argType.Substring(1).ToLower();
                    AddBoogie(String.Format("\t\tassume Union2{0}({0}2Union({1})) == {1};", argType, argument));
                }

                //AddBoogie(String.Format("\t\tassume Union2Int(Int2Union({0})) == {0};", instruction.Arguments[1]));

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
                var normalizedType = Helpers.GetNormalizedTypeForDelegates(instruction.Method.ContainingType);
                if (instruction.HasResult)
                    AddBoogie(String.Format("\t\tcall {0} := InvokeDelegate_{1}({2} {3});", localVar, normalizedType, instruction.Arguments[0], arguments));
                else
                    AddBoogie(String.Format("\t\tcall InvokeDelegate_{1}({2} {3});", localVar, normalizedType, instruction.Arguments[0], arguments));

                if (instruction.HasResult)
                {
                    // the union depends on the type of the arguments
                    var argType = Helpers.GetBoogieType(instruction.Result.Type);
                    if (argType.Equals("Ref")) // Ref and Union are alias
                        AddBoogie(String.Format("\t\t{0} := {1};", instruction.Result, localVar));
                    else
                    {
                        argType = argType.First().ToString().ToUpper() + argType.Substring(1).ToLower();
                        AddBoogie(String.Format("\t\t{0} := Union2{2}({1});", instruction.Result, localVar, argType));
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
        internal static IDictionary<IMethodReference, string> methodIdentifiers =
                new Dictionary<IMethodReference, string>();

        // we use a string as a key because hashing ITypeReference is not working as expected

        public static IDictionary<string, ISet<IMethodReference>> MethodGrouping
            = new Dictionary<string, ISet<IMethodReference>>();

        public static IDictionary<string, ITypeReference> MethodGroupType
            = new Dictionary<string, ITypeReference>();

        // this method will receive potential calles which have their types uninstanciated
        public static void AddDelegatedMethodToGroup(ITypeReference tRef, IMethodReference mRef)
        {
            var k = Helpers.GetNormalizedTypeForDelegates(tRef);
            if (MethodGrouping.ContainsKey(k))
                MethodGrouping[k].Add(mRef);
            else
            {
                MethodGrouping[k] = new HashSet<IMethodReference>();
                MethodGrouping[k].Add(mRef);
                MethodGroupType[k] = tRef;
            }
        }

        public static  void CreateDelegateGroup(ITypeReference tRef)
        {
            var k = Helpers.GetNormalizedTypeForDelegates(tRef);
            if (MethodGrouping.ContainsKey(k))
                return;

            MethodGrouping[k] = new HashSet<IMethodReference>();
            MethodGroupType[k] = tRef;
        }

        public static string CreateDelegateMethod(string typeRef)
        {
            var sb = new StringBuilder();
            var normalizedType = typeRef;

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

        public static string InvokeDelegateMethod(string groupTypeString)
        {
            var sb = new StringBuilder();

            var invokeArity = MethodGroupType[groupTypeString].ResolvedType.Methods.Single(m => m.Name.ToString().Equals("Invoke"));
            
            // we should parametrize the parameters and return's types
            // currently we are only supporting static int -> int methods
            var argInList = new List<string>();
            bool hasReturnVariable = invokeArity.Type.TypeCode != PrimitiveTypeCode.Void;
            // for virtual methods we may have to change this to include the reference to the object receiving the message
            var parameters = invokeArity.ParameterCount > 0 ? ","+ String.Join(",", invokeArity.Parameters.Select(v => String.Format("arg{0}$in", v.Index) + " : Ref")) : String.Empty;

            sb.AppendLine(string.Format("procedure {{:inline 1}} InvokeDelegate_{0}($this: Ref{1}) {2};", groupTypeString, parameters, hasReturnVariable ? "returns ($r: Ref)" : string.Empty));
            sb.AppendLine(string.Format("implementation {{:inline 1}} InvokeDelegate_{0}($this: Ref{1}) {2}", groupTypeString, parameters, hasReturnVariable ? "returns ($r: Ref)" : string.Empty));
            sb.AppendLine("{");

            // we declare a local for each parameter - local will not be union, will be the real type (boogie version)
            // the resultRealType variable is for the return value if any - then it will be casted to Union and will be the return value
            foreach (var v in invokeArity.Parameters)
                sb.AppendLine(String.Format("\tvar local{0} : {1};", v.Index, Helpers.GetBoogieType(v.Type)));

            if (hasReturnVariable)
                sb.AppendLine(String.Format("\tvar resultRealType: {0};", Helpers.GetBoogieType(invokeArity.Type)));

            foreach (var m in MethodGrouping[groupTypeString])
            {
                var id = methodIdentifiers[m];

                var method = Helpers.GetUnspecializedVersion(m);
                sb.AppendLine(String.Format("\tif ($RefToDelegateMethod({0}, $this))", id));
                sb.AppendLine("\t{");

                // we may have to add the receiver object for virtual methods
                var args = new List<string>();
                if (method.CallingConvention.HasFlag(Microsoft.Cci.CallingConvention.HasThis))
                {
                    var receiverObject = String.Format("$RefToDelegateReceiver({0}, $this)", id);
                    args.Add(receiverObject);
                }

                // every argument is casted to Union (if they are Ref it is not necessary, they are alias)
                // we need the unspecialized version because that's how generics are handled
                foreach (var v in method.Parameters)
                {
                    var argType = Helpers.GetBoogieType(v.Type);
                    Contract.Assert(!String.IsNullOrEmpty(argType));
                    //if (argType.Equals("Ref")) // Ref and Union are alias
                    //    AddBoogie(String.Format("\t\tlocal{0} := arg{0}$in;", v.Index));
                    if (!argType.Equals("Ref"))
                    {
                        argType = argType.First().ToString().ToUpper() + argType.Substring(1).ToLower();
                        sb.AppendLine(String.Format("\t\tlocal{0} := Union2{1}(arg{0}$in);", v.Index, argType));
                        args.Add(String.Format("local{0}", v.Index));
                    }
                    else
                    {
                        args.Add(String.Format("arg{0}$in", v.Index));
                    }
                }

                if (hasReturnVariable)
                {
                    Contract.Assert(!String.IsNullOrEmpty(Helpers.GetBoogieType(method.Type)));

                    if (Helpers.GetBoogieType(method.Type).Equals("Ref"))
                    {
                        sb.AppendLine(String.Format("\t\tcall $r := {0}({1});", Helpers.GetMethodName(method), String.Join(",", args)));
                    } else
                    {
                        var argType = Helpers.GetBoogieType(method.Type);
                        Contract.Assert(!String.IsNullOrEmpty(argType));
                        argType = argType.First().ToString().ToUpper() + argType.Substring(1).ToLower();
                        if (!argType.Equals("Ref"))
                        {
                            sb.AppendLine(String.Format("\t\tcall resultRealType := {0}({1});", Helpers.GetMethodName(method), String.Join(",", args)));
                            sb.AppendLine(String.Format("\t\tassume Union2{0}({0}2Union(resultRealType)) == resultRealType;", argType));
                            sb.AppendLine(String.Format("\t\t$r := {0}2Union(resultRealType);", argType));
                        } else
                        {
                            sb.AppendLine(String.Format("\t\tcall $r := {0}({1});", Helpers.GetMethodName(method), String.Join(",", args)));
                        }

                    }
                } else
                {
                    sb.AppendLine(String.Format("\t\tcall {0}({1});", Helpers.GetMethodName(method), String.Join(",", args)));
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

        // This method has a clone? GetMethodName
        public static string GetMethodIdentifier(IMethodReference methodRef)
        {
            var methodId = Helpers.CreateUniqueMethodName(methodRef);

            if (methodIdentifiers.ContainsKey(methodRef))
                return methodIdentifiers[methodRef];

            //var methodName = Helpers.GetMethodName(methodRef);
            //var methodArity = Helpers.GetArityWithNonBoogieTypes(methodRef);

            //// example:  cMain2.objectParameter$System.Object;
            //var methodId = methodName + methodArity;

            methodIdentifiers.Add(methodRef, methodId);

            return methodId;
        }
    }
}
