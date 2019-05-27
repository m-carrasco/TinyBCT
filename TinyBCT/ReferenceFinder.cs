using System;
using Backend.ThreeAddressCode.Instructions;
using Backend.Visitors;
using Backend.ThreeAddressCode.Values;
using System.Collections.Generic;
using Microsoft.Cci;
using Backend.Transformations;
using Backend;
using Backend.Analyses;
using System.Diagnostics.Contracts;

namespace TinyBCT
{
    public class ReferenceFinder
    {
        // this set has the result of the last analysis
        static ISet<IReferenceable> ReferencedSet = new HashSet<IReferenceable>();
        static ISet<IFieldReference> FieldReferencedSet = new HashSet<IFieldReference>();

        public static void AddReference(IReferenceable referenceable)
        {
            Contract.Assert(!(referenceable is StaticFieldAccess) && !(referenceable is InstanceFieldAccess));
            // this is just to be aware when this happens, we should support arrays!
            Contract.Assert(!(referenceable is ArrayElementAccess));
            ReferencedSet.Add(referenceable);
        }

        public static void AddReference(IFieldReference field)
        {
            FieldReferencedSet.Add(field);
        }

        static public bool IsReferenced(IReferenceable referenceable)
        {
            if (referenceable is StaticFieldAccess staticFieldAccess)
                return IsReferenced(staticFieldAccess.Field);
            else if (referenceable is InstanceFieldAccess instanceFieldAccess)
                return IsReferenced(instanceFieldAccess.Field);
            else
            {
                return ReferencedSet.Contains(referenceable);
            }
        }

        static public bool IsReferenced(IFieldReference field)
        {
            return FieldReferencedSet.Contains(field);
        }

        static public void ResetFieldReferences()
        {
            FieldReferencedSet.Clear();
        }

        public void CollectFields(MethodBody methodBody)
        {
            FieldsCollector f = new FieldsCollector();
            f.Visit(methodBody);
        }

        public void CollectLocalVariables(MethodBody methodBody)
        {
            ReferencedSet = new HashSet<IReferenceable>();
            (new VariablesCollector()).Visit(methodBody);
        }

        public static void TraverseForFields(ISet<Assembly> assemblies)
        {
            foreach (Assembly assembly in assemblies)
            {
                foreach (IMethodDefinition methodDefinition in assembly.GetAllDefinedMethods())
                {
                    var disassembler = new Disassembler(assembly.Host, methodDefinition, assembly.PdbReader);
                    MethodBody methodBody = disassembler.Execute();

                    var cfAnalysis = new ControlFlowAnalysis(methodBody);
                    var cfg = cfAnalysis.GenerateExceptionalControlFlow();

                    var splitter = new WebAnalysis(cfg, methodBody.MethodDefinition);
                    splitter.Analyze();
                    splitter.Transform();

                    methodBody.UpdateVariables();

                    var typeAnalysis = new TypeInferenceAnalysis(cfg, methodBody.MethodDefinition.Type);
                    typeAnalysis.Analyze();

                    ReferenceFinder reference = new ReferenceFinder();
                    reference.CollectFields(methodBody);
                }
            }
        }

        public class VariablesCollector : InstructionVisitor
        {
            public override void Visit(LoadInstruction instruction)
            {
                if (instruction.Operand is Reference reference)
                {
                    // Reference object is an expression of the form &<...>
                    // <...> is the pointedObj
                    IReferenceable pointedObj = reference.Value;

                    if (pointedObj is InstanceFieldAccess ||
                        pointedObj is StaticFieldAccess)
                        return;

                    ReferenceFinder.AddReference(pointedObj);
                }
            }

        }

        public class FieldsCollector : InstructionVisitor
        {
            public override void Visit(LoadInstruction instruction)
            {
                if (instruction.Operand is Reference reference)
                {
                    // Reference object is an expression of the form &<...>
                    // <...> is the pointedObj
                    IReferenceable pointedObj = reference.Value;

                    if (pointedObj is StaticFieldAccess staticFieldAccess)
                        FieldReferencedSet.Add(staticFieldAccess.Field);
                    else if (pointedObj is InstanceFieldAccess instanceFieldAccess)
                        FieldReferencedSet.Add(instanceFieldAccess.Field);

                }
            }
        }
    }
}

