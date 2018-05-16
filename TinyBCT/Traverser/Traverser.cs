// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend;
using Backend.Analyses;
using Backend.Serialization;
using Backend.ThreeAddressCode;
using Backend.Transformations;
using System.IO;
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using TinyBCT.Translators;
using Backend.Model;
using Backend.Utils;

namespace TinyBCT
{
	class Traverser : MetadataTraverser
	{
		private IMetadataHost host;
		private ISourceLocationProvider sourceLocationProvider;
        //private ISet<INamedTypeDefinition> classes = new HashSet<INamedTypeDefinition>();

        // FIX: I have issues with the use of actions that do not allow my to pass this as paramater
        public static ClassHierarchyAnalysis CHA;
        public static ControlFlowGraph CFG; // ugly - the thing is that if labels were erased we can't create cfg

        public Traverser(IMetadataHost host, ISourceLocationProvider sourceLocationProvider, ClassHierarchyAnalysis CHAnalysis)
		{
			this.host = host;
			this.sourceLocationProvider = sourceLocationProvider;
            CHA = CHAnalysis;
		}

        // not used yet - under development
        // there are some issues related to the parameter renaming
        // we modified the methodBody but we should also do the same with the IMethodDef
        private void inmutableArguments(MethodBody methodBody)
        {
            // corral interprets parameter as inmutable references
            // parameters are renamed, and local versions of them are created
            // procedure foo(a : int) { ... } -> procedure foo($a_param : int) { var a : int; a := $a_param; ...}

            var newParameters = new List<IVariable>();
            var oldParamToNewLocal = new Dictionary<IVariable, IVariable>();
            var newLocalToNewParam = new Dictionary<IVariable, IVariable>();
            foreach (var variable in methodBody.Parameters)
            {
                // note: analysis-net changed and required to pass a method reference in the LocalVariable constructor
                var newParameter = new LocalVariable(String.Format("${0}_param", variable.Name), true, methodBody.MethodDefinition);
                newParameters.Add(newParameter);
                newParameter.Type = variable.Type;
                // note: analysis-net changed and required to pass a method reference in the LocalVariable constructor
                var newLocal = new LocalVariable(variable.Name, false, methodBody.MethodDefinition);
                newLocal.Type = variable.Type;
                oldParamToNewLocal.Add(variable, newLocal);
                methodBody.Variables.Add(newLocal);
                newLocalToNewParam.Add(newLocal, newParameter);
            }

            methodBody.Parameters.Clear();
            foreach (var variable in newParameters)
                methodBody.Parameters.Add(variable);

            foreach (var instruction in methodBody.Instructions)
                foreach (KeyValuePair<IVariable, IVariable> oldParamNewParam in oldParamToNewLocal)
                    instruction.Replace(oldParamNewParam.Key, oldParamNewParam.Value);

            uint i = (uint)methodBody.Instructions.Count * 5; // hack - otherwise we should re assing every label - *5 in order to avoid collisions
            foreach (KeyValuePair<IVariable, IVariable> oldParamNewLocal in oldParamToNewLocal)
            {
                var ins = new LoadInstruction(i, oldParamNewLocal.Value, newLocalToNewParam[oldParamNewLocal.Value]);
                // pisar ins.Label = "bct_LABELID"
                methodBody.Instructions.Insert(0, ins);
                i++;
            }
        }

        private void transformBody(MethodBody methodBody)
        {
            var cfAnalysis = new ControlFlowAnalysis(methodBody);
            //var cfg = cfAnalysis.GenerateNormalControlFlow();
            CFG = cfAnalysis.GenerateExceptionalControlFlow();

            var splitter = new WebAnalysis(CFG, methodBody.MethodDefinition);
            splitter.Analyze();
            splitter.Transform();

            methodBody.UpdateVariables();

            var typeAnalysis = new TypeInferenceAnalysis(CFG);
            typeAnalysis.Analyze();

            //var forwardCopyAnalysis = new ForwardCopyPropagationAnalysis(CFG);
            //forwardCopyAnalysis.Analyze();
            //forwardCopyAnalysis.Transform(methodBody);

            //var backwardCopyAnalysis = new BackwardCopyPropagationAnalysis(CFG);
            //backwardCopyAnalysis.Analyze();
            //backwardCopyAnalysis.Transform(methodBody);

            methodBody.RemoveUnusedLabels();
        }

        private List<System.Action<INamedTypeDefinition>> namedTypeDefinitionActions
            = new List<System.Action<INamedTypeDefinition>>();

        public void AddNamedTypeDefinitionAction(System.Action<INamedTypeDefinition> a)
        {
            namedTypeDefinitionActions.Add(a);
        }

        public override void TraverseChildren(INamedTypeDefinition namedTypeDefinition)
        {
            // TypeDefinitionTranslator handles this type of boogie code.
            //function T$TestType() : Ref;
            //const unique T$TestType: int;
            //axiom $TypeConstructor(T$TestType()) == T$TestType;
            // axiom (forall $T: Ref :: { $Subtype(T$Test(), $T) } $Subtype(T$Test(), $T) <==> T$Test() == $T || $Subtype(T$System.Object(), $T));

            foreach (var action in namedTypeDefinitionActions)
                action(namedTypeDefinition);

            base.TraverseChildren(namedTypeDefinition);
        }

		public override void TraverseChildren(IAssembly assembly)
		{
			base.TraverseChildren(assembly);
			/*StringBuilder sb = new StringBuilder();
			// todo: improve this piece of code
			foreach (var c1 in TypeDefinitionTranslator.classes)
			{
				foreach (var c2 in TypeDefinitionTranslator.classes.Where(c => c != c1))
				{
                    if (!TypeHelper.Type1DerivesFromOrIsTheSameAsType2(c1, c2))
					{
						var tn1 = Helpers.GetNormalizedType(c1);
						var tn2 = Helpers.GetNormalizedType(c2);

						// axiom(forall $T: Ref:: { $Subtype($T, T1$() } $Subtype($T, $T1) ==> ! $Subtype($T, T2$))
						sb.AppendLine("axiom(forall $T: Ref:: { " + String.Format("$Subtype($T, T${0}())", tn1)
							 + "} " + String.Format("$Subtype($T, T${0}()) ==>!$Subtype($T, T${1}()));", tn1, tn2));
					}
				}
			}
			// todo: improve this piece of code
			StreamWriter streamWriter = Program.streamWriter;
			streamWriter.WriteLine(sb);*/
		}

        private List<System.Action<IMethodDefinition,MethodBody>> methodDefinitionActions 
            = new List<System.Action<IMethodDefinition, MethodBody>>();

        public void AddMethodDefinitionAction(System.Action<IMethodDefinition, MethodBody> a)
        {
            methodDefinitionActions.Add(a);
        }

        public override void TraverseChildren(IMethodDefinition methodDefinition)
        {
            // if it is external, its definition will be translated only if it is called
            // that case is handled on the method call instruction translation
            // calling Dissasembler on a external method will raise an exception.
            if (!methodDefinition.IsExternal)
            {
                var disassembler = new Disassembler(host, methodDefinition, sourceLocationProvider);
                var methodBody = disassembler.Execute();
                transformBody(methodBody);

                foreach (var action in methodDefinitionActions)
                    action(methodDefinition, methodBody);
            }

            base.TraverseChildren(methodDefinition);
        }
    }
}
