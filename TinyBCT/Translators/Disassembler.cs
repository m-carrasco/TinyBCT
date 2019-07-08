using System;
using Backend;
using Backend.Analyses;
using Backend.Model;
using Backend.Transformations;
using Backend.Utils;
using Microsoft.Cci;

namespace TinyBCT.Translators
{
    public class Disassembler
    {
        private ISourceLocationProvider sourceLocationProvider;

        public MethodBody MethodBody { get; private set; }
        public ControlFlowGraph ControlFlowGraph { get; private set; }

        private IMetadataHost host;
        private IMethodDefinition methodDefinition;

        public Disassembler(IMetadataHost host, IMethodDefinition methodDefinition, ISourceLocationProvider sourceLocationProvider)
        {
            this.host = host;
            this.methodDefinition = methodDefinition;
            this.sourceLocationProvider = sourceLocationProvider;
        }

        public void Execute()
        {
            var disassembler = new Backend.Transformations.Disassembler(host, methodDefinition, sourceLocationProvider);
            var methodBody = disassembler.Execute();
            MethodBody = methodBody;

            var cfAnalysis = new ControlFlowAnalysis(methodBody);
            ControlFlowGraph = cfAnalysis.GenerateExceptionalControlFlow();

            var splitter = new WebAnalysis(ControlFlowGraph, methodBody.MethodDefinition);
            splitter.Analyze();
            splitter.Transform();

            methodBody.UpdateVariables();

            var typeAnalysis = new TypeInferenceAnalysis(ControlFlowGraph, methodBody.MethodDefinition.Type);
            typeAnalysis.Analyze();

            //var forwardCopyAnalysis = new ForwardCopyPropagationAnalysis(Traverser.CFG);
            //forwardCopyAnalysis.Analyze();
            //forwardCopyAnalysis.Transform(methodBody);

            //var backwardCopyAnalysis = new BackwardCopyPropagationAnalysis(Traverser.CFG);
            //backwardCopyAnalysis.Analyze();
            //backwardCopyAnalysis.Transform(methodBody);

            // TinyBCT transformations

            var fieldInitialization = new FieldInitialization(methodBody);
            fieldInitialization.Transform();

            if (!Settings.AddressesEnabled())
            {
                var refAlias = new RefAlias(methodBody);
                refAlias.Transform();
            }

            // execute this after RefAlias! 
            var immutableArguments = new ImmutableArguments(methodBody);
            immutableArguments.Transform();

            methodBody.RemoveUnusedLabels();
        }

    }
}
