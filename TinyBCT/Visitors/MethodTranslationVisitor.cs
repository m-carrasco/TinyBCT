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

namespace TinyBCT
{
	class MethodTranslationVisitor : MetadataTraverser
	{
		private IMetadataHost host;
		private ISourceLocationProvider sourceLocationProvider;

		public MethodTranslationVisitor(IMetadataHost host, ISourceLocationProvider sourceLocationProvider)
		{
			this.host = host;
			this.sourceLocationProvider = sourceLocationProvider;
		}

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
                var newParameter = new LocalVariable(String.Format("${0}_param", variable.Name), true);
                newParameters.Add(newParameter);
                newParameter.Type = variable.Type;
                var newLocal = new LocalVariable(variable.Name, false);
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
                methodBody.Instructions.Insert(0, ins);
                i++;
            }
        }

        private void transformBody(MethodBody methodBody)
        {
            var cfAnalysis = new ControlFlowAnalysis(methodBody);
            var cfg = cfAnalysis.GenerateNormalControlFlow();

            var splitter = new WebAnalysis(cfg);
            splitter.Analyze();
            splitter.Transform();

            methodBody.UpdateVariables();

            var typeAnalysis = new TypeInferenceAnalysis(cfg);
            typeAnalysis.Analyze();

            var forwardCopyAnalysis = new ForwardCopyPropagationAnalysis(cfg);
            forwardCopyAnalysis.Analyze();
            forwardCopyAnalysis.Transform(methodBody);

            var backwardCopyAnalysis = new BackwardCopyPropagationAnalysis(cfg);
            backwardCopyAnalysis.Analyze();
            backwardCopyAnalysis.Transform(methodBody);
        }

        private void checkNotImplementedInstructions(MethodBody methodBody)
        {
            // check if there is no implemented instruction
            if (methodBody.Instructions.Any(ins => !Helpers.IsInstructionImplemented(ins)))
            {
                Console.WriteLine("************" + methodBody.MethodDefinition.Name + "************");
                foreach (var ins in methodBody.Instructions)
                {
                    if (!Helpers.IsInstructionImplemented(ins))
                        Console.WriteLine(String.Format("{0} ------> {2} {1}", ins, ins.GetType(), "not implemented"));
                    else
                        Console.WriteLine(ins);
                }
            }
        }

        public override void TraverseChildren(IMethodDefinition methodDefinition)
		{
            var disassembler = new Disassembler(host, methodDefinition, sourceLocationProvider);
            var methodBody = disassembler.Execute();

            transformBody(methodBody);

            checkNotImplementedInstructions(methodBody);

            MethodTranslator methodTranslator = new MethodTranslator(methodDefinition, methodBody);
       
            // todo: improve this piece of code
            StreamWriter streamWriter = Program.streamWriter;
            streamWriter.WriteLine(methodTranslator.Translate());
        }
	}
}
