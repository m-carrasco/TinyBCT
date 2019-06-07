// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend;
using System.IO;
using TinyBCT.Translators;
using Backend.Model;
using Backend.ThreeAddressCode.Values;

namespace TinyBCT
{
    public class Program 
    {
        public static void ResetStaticVariables()
        {
            TinyBCT.BoogieGenerator.singleton = null;

            TinyBCT.Helpers.methodsTranslated = new System.Collections.Generic.HashSet<string>();
            TinyBCT.BoogieLiteral.Strings.stringLiterals = new System.Collections.Generic.HashSet<string>();
            TinyBCT.Helpers.Strings.specialCharacters = new Dictionary<Char, int>() { { ' ', 0 } };

            TinyBCT.Translators.InstructionTranslator.CalledMethods = new System.Collections.Generic.HashSet<Microsoft.Cci.IMethodReference>();
            TinyBCT.Translators.InstructionTranslator.MentionedClasses = new HashSet<ITypeReference>();
            TinyBCT.Translators.FieldTranslator.fieldNames = new Dictionary<IFieldReference, String>();

            TinyBCT.Translators.DelegateStore.methodIdentifiers = new Dictionary<IMethodReference, TinyBCT.DelegateExpression>(new DelegateStore.IMethodReferenceCmp());
            TinyBCT.Translators.DelegateStore.MethodGrouping = new Dictionary<string, ISet<IMethodReference>>();

            TinyBCT.Translators.TypeDefinitionTranslator.classes = new HashSet<ITypeDefinition>();
            TinyBCT.Translators.TypeDefinitionTranslator.parents = new HashSet<ITypeDefinition>();
            TinyBCT.Translators.TypeDefinitionTranslator.normalizedTypeStrings = new HashSet<string>();

            TinyBCT.Translators.StaticInitializer.mainMethods = new HashSet<IMethodDefinition>();
            TinyBCT.Translators.StaticInitializer.staticConstructors = new HashSet<IMethodDefinition>();

            TinyBCT.ImmutableArguments.MethodToMapping = new Dictionary<MethodBody, IDictionary<IVariable, IVariable>>();

            TinyBCT.ReferenceFinder.ResetFieldReferences();
            TinyBCT.Translators.TypeDefinitionTranslator.ParametricTypes = new HashSet<string>();
        }

        public static StreamWriter streamWriter;

        private static string SetupOutputFile()
        {
            var outputResultPath = Path.ChangeExtension(Settings.OutputFile, "bpl");
            streamWriter = File.CreateText(outputResultPath);
            return outputResultPath;
        }

        public static void CreateAllAsyncMethods(StreamWriter sw)
        {
            StatementList stmts = new StatementList();

            Action<StatementList> fStmt = (s => stmts.Add(s));
            Action<string> fStr = (s => fStmt(BoogieStatement.FromString(s)));

            var taskVar = BoogieVariable.GetTempVar(Helpers.ObjectType(), null, "task");
            var objectTypeStr = Helpers.ObjectType().FirstUppercase();
            fStr($"var resultAsync : [{objectTypeStr}] {objectTypeStr};");
            fStr($"var builder2task: [{objectTypeStr}] {objectTypeStr};");
            fStr($"procedure {{:extern}} System.Threading.Tasks.Task`1.get_Result(this : {objectTypeStr}) returns ($result : {objectTypeStr}) {{");
            fStr("$result := resultAsync[this];");
            fStr("}");
            fStr("");
            fStr($"procedure {{:extern}} System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.Create() returns ($result : {objectTypeStr}) {{");
            fStmt(BoogieStatement.VariableDeclaration(taskVar));
            var bg = BoogieGenerator.Instance();
            fStmt(bg.AllocObject(taskVar));
            fStmt(BoogieStatement.Assume(Expression.IsTask(taskVar)));
            var resultVar = BoogieVariable.ResultVar(Helpers.ObjectType());
            fStmt(bg.AllocObject(resultVar));
            
            fStmt(BoogieStatement.Assume(Expression.IsAsyncTaskMethodBuilder(resultVar)));
            fStr($"builder2task[$result] := {taskVar.Expr};");
            fStr("}");
            fStr($"procedure {{:extern}} System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.get_Task(this : {objectTypeStr}) returns ($result : {objectTypeStr}) {{");
            fStr("$result := builder2task[this];");
            fStr("}");
            fStr($"procedure {{:extern}} System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.SetResult$`0(this : {objectTypeStr},result : {objectTypeStr}) {{");
            fStr($"var task : {objectTypeStr};");
            fStr("task := builder2task[this];");
            fStr($"resultAsync[task] := result;");
            fStr("}");
            foreach (var stmt in stmts)
            {
                sw.WriteLine(stmt.Stmt);
            }
            CreateAsyncStartMethod(sw);
        }
        public static void CreateAsyncStartMethod(StreamWriter sw)
        {
            StatementList stmts = new StatementList();

            var boogieGetTypeRes = BoogieVariable.GetTempVar(Helpers.ObjectType(), null /* need dict with used var names*/, prefix: "asyncType");
            var stateMachineVar = BoogieVariable.GetTempVar(Helpers.ObjectType(), null /* need dict with used var names*/, prefix: "stateMachineCopy");
            stmts.Add(BoogieStatement.VariableDeclaration(stateMachineVar));
            stmts.Add(BoogieStatement.VariableDeclaration(boogieGetTypeRes));
            stmts.Add(BoogieStatement.FromString($"{stateMachineVar.Expr} := stateMachine;"));
            foreach (var asyncMethod in Helpers.asyncMoveNexts)
            {
                var asyncType = asyncMethod.ContainingTypeDefinition;
                var bg = BoogieGenerator.Instance();
                stmts.Add(bg.ProcedureCall(BoogieMethod.GetTypeMethod, new List<Expression> { stateMachineVar }, boogieGetTypeRes));
                StatementList ifStmts = new StatementList();
                ifStmts.Add(BoogieGenerator.Instance().ProcedureCall(BoogieMethod.From(asyncMethod), new List<Expression> { stateMachineVar }));
                
                stmts.Add(BoogieStatement.If(Expression.Subtype(boogieGetTypeRes, asyncType), ifStmts));
            }
            stmts.Add(BoogieStatement.FromString($"v0$out := {stateMachineVar.Expr};"));
            
            sw.WriteLine(@"procedure {:extern} System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1.Start``1$``0$(this : Ref,stateMachine : Ref) returns (v0$out : Ref) {");
            foreach (var stmt in stmts)
            {
                sw.WriteLine(stmt.Stmt);
            }
            sw.WriteLine("}");
        }
        
        public static void Main(string[] args){
            ProgramOptions options = Settings.CreateProgramOptions(args);
            Start(options);
        }

        public static void Start(ProgramOptions programOptions){

            Console.WriteLine(programOptions);
            Settings.SetProgramOptions(programOptions);
            var outputPath = SetupOutputFile();

            Prelude.Write(); // writes prelude.bpl content into the output file

            using (var host = new PeReader.DefaultHost())
            {
                #region Load assemblies
                ISet<Assembly> inputAssemblies = new HashSet<Assembly>();
                foreach (string inputFile in Settings.InputFiles)
                {
                    Assembly assembly = new Assembly(host);
                    assembly.Load(inputFile);
                    inputAssemblies.Add(assembly);
                }
                #endregion

                #region Execute CHA
                var CHAnalysis = new ClassHierarchyAnalysis(host);
                CHAnalysis.Analyze();
                #endregion

                #region Initialize host types
                Types.Initialize(host);
                #endregion

                // TODO(diegog): Analysis not integrated yet
                // This can be used to obtain the allocated types and delegates
                //var allocationsAndDelelegatesAnalysis = new TypesAndDelegatesCollector(host);

                #region Write three address code for debugging
                TACWriter.WriteTAC(inputAssemblies);
                #endregion

                #region Look for references (used in mixed memory model)
                if (Settings.MemoryModel == ProgramOptions.MemoryModelOption.Mixed)
                    ReferenceFinder.TraverseForFields(inputAssemblies);
                #endregion

                #region Translate defined types and add axioms about subtyping
                TypeDefinitionTranslator.TranslateTypes(inputAssemblies);
                #endregion

                #region Translate defined methods
                MethodTranslator.TranslateAssemblies(inputAssemblies, CHAnalysis);
                #endregion

                #region Create main wrapper with static fields initialization and static constructors calls
                StaticInitializer.SearchStaticConstructorsAndMain(inputAssemblies);
                streamWriter.WriteLine(StaticInitializer.CreateInitializeGlobals());
                streamWriter.WriteLine(StaticInitializer.CreateMainWrappers());
                streamWriter.WriteLine(StaticInitializer.CreateStaticVariablesAllocProcedure());
                streamWriter.WriteLine(StaticInitializer.CreateDefaultValuesStaticVariablesProcedure());
                streamWriter.WriteLine(StaticInitializer.CreateStaticConstructorsCallsProcedure());
                #endregion

                #region Translate types that are referenced but not defined in the input assemblies
                TypeDefinitionTranslator.DefineUndeclaredSuperClasses();
                TypeDefinitionTranslator.ParametricTypeDeclarations();
                #endregion

                #region Translate string constants
                BoogieLiteral.Strings.WriteStringConsts(streamWriter);
                #endregion

                #region Translate delegates
                streamWriter.WriteLine(DelegateStore.DefineMethodsIdentifiers());
                streamWriter.WriteLine(DelegateStore.CreateDelegateMethod());
                streamWriter.WriteLine(DelegateStore.InvokeDelegateMethod());
                #endregion

                // CreateAllAsyncMethods(streamWriter);
                #region Heuristic to catch getters & setters. If they are in our input assemblies we generate a body using a field associated to that property
                IEnumerable<IMethodReference> usedProperties = new List<IMethodReference>();
                if (Settings.StubGettersSetters)
                {
                    if (Settings.StubGettersSetters)
                    {
                        GetterSetterStub getterSetterStub = new GetterSetterStub();
                        usedProperties = getterSetterStub.Stub(inputAssemblies, streamWriter);
                    }
                }
                #endregion

                streamWriter.WriteLine(StringTranslator.StringConcatStub());
                streamWriter.WriteLine(StringTranslator.StringEqualsStub());
                streamWriter.WriteLine(StringTranslator.StringOpEqualityStub());
                streamWriter.WriteLine(StringTranslator.StringOpInequalityStub());
                streamWriter.WriteLine(StringTranslator.StringFunctions());

                #region Translate called methods as extern (bodyless methods or methods not present in our input assemblies)

                var externMethods = InstructionTranslator.CalledMethods.Except(inputAssemblies.GetAllDefinedMethods().Where(m => m.Body.Size > 0)).Except(usedProperties);
                externMethods = externMethods.Where(m => !StringTranslator.GetBoogieNamesForStubs().Contains(BoogieMethod.From(m).Name));
                foreach (var methodRef in externMethods)
                {
                    var head = Helpers.GetExternalMethodDefinition(Helpers.GetUnspecializedVersion(methodRef));
                    streamWriter.WriteLine(head);
                }

                #endregion

                #region Translate class fields
                // we declare read or written fields
                foreach (var field in FieldTranslator.GetFieldDefinitions())
                    streamWriter.WriteLine(field);
                #endregion

                streamWriter.Close();

                #region Append bpl input files
                foreach (var bplInputFile in Settings.BplInputFiles)
                {
                    var output = new FileStream(outputPath, FileMode.Append, FileAccess.Write);
 
                    using (var inputStream = File.OpenRead(bplInputFile))
                    {
                        inputStream.CopyTo(output);
                    }

                    output.Close();
                }
                #endregion
            }

        }
    }
}
