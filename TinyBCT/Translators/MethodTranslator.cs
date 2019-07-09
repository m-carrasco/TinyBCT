using Backend;
using Backend.Analyses;
using Backend.Model;
using Backend.ThreeAddressCode.Values;
using Backend.Transformations;
using Backend.Utils;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyBCT.Translators;
using Disassembler = TinyBCT.Translators.Disassembler;

namespace TinyBCT
{
    class MethodTranslator
    {
        IMethodDefinition methodDefinition;
        MethodBody methodBody;
        ClassHierarchyAnalysis CHA;
        private ControlFlowGraph CFG;
        Assembly assembly;

        static bool whitelistContains(string name)
        {
            if (!Settings.DebugLargeDLL)
            {
                return true;
            }
            if (name.Contains("Analyzer1"))
                return true;
            if (name.Contains("Project"))
                return true;
            if (name.Contains("Diagnostic"))
                return true;

            if (name.Contains("MetadataReference"))
                return true;
            if (name.Contains("Location"))
                return true;
            if (name.Contains("Compilation"))
                return true;
            if (name.Contains("Document"))
                return true;

            return false;
        }
        // called from Traverser
        // set in Main
        public static void TranslateAssemblies(ISet<Assembly> assemblies, ClassHierarchyAnalysis CHA)
        {
            foreach (Assembly assembly in assemblies)
            {
                foreach (IMethodDefinition methodDefinition in assembly.GetAllDefinedMethods())
                {
                    if (!methodDefinition.IsExternal)
                    {
                        try
                        {
                            if (whitelistContains(methodDefinition.ContainingType.FullName()))
                            {
                                var disassembler = new Disassembler(assembly.Host, methodDefinition, assembly.PdbReader);
                                disassembler.Execute();
                                MethodBody mB = disassembler.MethodBody;
                                ControlFlowGraph cfg = disassembler.ControlFlowGraph;
                                // it would be faster to do this while we do 
                                // the global search for field references
                                if (Settings.MemoryModel == ProgramOptions.MemoryModelOption.Mixed)
                                {
                                    ReferenceFinder reference = new ReferenceFinder();
                                    reference.CollectLocalVariables(mB);
                                }
                                MethodTranslator methodTranslator = new MethodTranslator(methodDefinition, mB, CHA, cfg, assembly);
                                // todo: improve this piece of code
                                StreamWriter streamWriter = Program.streamWriter;
                                streamWriter.WriteLine(methodTranslator.Translate());
                                Helpers.addTranslatedMethod(methodDefinition);
                            }
                        }
                        catch (InvalidOperationException ex)
                        {
                            Console.WriteLine("WARNING: Exception thrown while translating method (omitting): " + BoogieMethod.From(methodDefinition).Name);
                            if (!Settings.SilentExceptionsForMethods)
                            {
                                throw ex;
                            }
                        }
                    }
                }
            }
        }

        public MethodTranslator(IMethodDefinition methodDefinition, MethodBody methodBody, ClassHierarchyAnalysis CHA, ControlFlowGraph cfg, Assembly assembly)
        {
            this.methodDefinition = methodDefinition;
            this.methodBody = methodBody;
            this.CHA = CHA;
            this.CFG = cfg;
            this.assembly = assembly;
        }

        StatementList TranslateInstructions(out Dictionary<string, BoogieVariable> temporalVariables)
        {
            InstructionTranslator instTranslator = new InstructionTranslator(this.CHA, methodBody, CFG);
            instTranslator.Translate();

            foreach (var v in instTranslator.RemovedVariables)
                methodBody.Variables.Remove(v);

            foreach (var v in instTranslator.AddedVariables)
                methodBody.Variables.Add(v);
            temporalVariables = instTranslator.temporalVariables;
            return instTranslator.Boogie();
        }

        StatementList TranslateLocalVariables(Dictionary<string, BoogieVariable> temporalVariables)
        {
            StatementList localVariablesStmts = new StatementList();
            var bg = BoogieGenerator.Instance();

            var allVariables = methodBody.Variables.Union(methodBody.Parameters).ToList();
            localVariablesStmts.Add(bg.DeclareLocalVariables(allVariables, temporalVariables));

            localVariablesStmts.Add(bg.AllocLocalVariables(allVariables));

            // replace for class generated by compiler
            return localVariablesStmts;
        }

        String TranslateAttr()
        {
            // commented entrypoint tag - this is only added to the wrappers
            // check StaticInitializer
            return /*Helpers.IsMain(methodDefinition) ? " {:entrypoint}" :*/ String.Empty;

        }

        String TranslateReturnTypeIfAny()
        {
            if (Settings.AddressesEnabled())
            {
                var returnVariables = new List<String>();
                if (!Helpers.GetMethodBoogieReturnType(methodDefinition).Equals(Helpers.BoogieType.Void))
                    returnVariables.Add(String.Format("$result : {0}", Helpers.GetMethodBoogieReturnType(methodDefinition)));

                return String.Format("returns ({0})", String.Join(",", returnVariables));
            } else
            {
                if (Helpers.GetMethodBoogieReturnType(methodDefinition).Equals(Helpers.BoogieType.Void) &&
                     (!methodDefinition.Parameters.Any(p => p.IsByReference || p.IsOut)))
                    return String.Empty;
                else
                {
                    var returnVariables = new List<String>();
                    returnVariables = methodDefinition.Parameters.Where(p => p.IsByReference).Select(p => String.Format("{0}$out : {1}", p.Name.Value, Helpers.GetBoogieType(p))).ToList();
                    if (!Helpers.GetMethodBoogieReturnType(methodDefinition).Equals(Helpers.BoogieType.Void))
                        returnVariables.Add(String.Format("$result : {0}", Helpers.GetMethodBoogieReturnType(methodDefinition)));

                    return String.Format("returns ({0})", String.Join(",", returnVariables));
                }
            }
        }

        public String Translate()
        {
            // instructions must be translated before local variables
            // modification to local variables can ocurr while instruction translation is done
            // for example when delegate creation is detected some local variables are deleted.
            var ins = TranslateInstructions(out Dictionary<string, BoogieVariable> temporalVariables);
            var localVariables = TranslateLocalVariables(temporalVariables);
            var methodName = BoogieMethod.From(methodDefinition).Name;
            var attr = TranslateAttr();
            Disassembler disassembler = new Disassembler(assembly.Host, methodDefinition, assembly.PdbReader);
            disassembler.Execute();
            var parametersWithTypes = Helpers.GetParametersWithBoogieType(disassembler.MethodBody.Parameters);
            var returnTypeIfAny = TranslateReturnTypeIfAny();

            var boogieProcedureTemplate = new BoogieProcedureTemplate(methodName, attr, localVariables, ins, parametersWithTypes, returnTypeIfAny, Helpers.IsExternal(methodDefinition));
            return boogieProcedureTemplate.TransformText();
        }

    }
}
