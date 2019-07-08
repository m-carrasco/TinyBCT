using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Transformations;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT.Translators
{
    class TypesAndDelegatesCollector
    {
        IMetadataHost host;

        public ISet<ITypeReference> AllocatedTypes { get; private set; }
        public ISet<IMethodReference> Delegates { get; private set; }

        public TypesAndDelegatesCollector(IMetadataHost host)
        {
            this.host = host;
            AllocatedTypes = new HashSet<ITypeReference>();
            Delegates = new HashSet<IMethodReference>();
        }

        public void Analyze()
        {
            foreach(var inputFile in Settings.InputFiles)
            {
                using(var assembly = new Assembly(host))
                {
                    assembly.Load(inputFile);
                    // analysis-net setup
                    var methodVisitor = new MethodVisitor(host, assembly.PdbReader);
                    methodVisitor.TraverseChildren(assembly.Module.ContainingAssembly);
                    AllocatedTypes.UnionWith(methodVisitor.AllocatedTypes);
                    Delegates.UnionWith(methodVisitor.Delegates);
                }
            }
        }

    }

    public class MethodVisitor : MetadataTraverser
    {
        private IMetadataHost host;
        private ISourceLocationProvider sourceLocationProvider;
        public ISet<ITypeReference> AllocatedTypes { get; private set; }
        public ISet<IMethodReference> Delegates { get; private set; }

        public MethodVisitor(IMetadataHost host, ISourceLocationProvider sourceLocationProvider)
        {
            this.host = host;
            this.sourceLocationProvider = sourceLocationProvider;
            AllocatedTypes = new HashSet<ITypeReference>();
            Delegates = new HashSet<IMethodReference>();
        }

        public override void TraverseChildren(IAssembly assembly)
        {
            base.TraverseChildren(assembly);
        }

        public override void TraverseChildren(ITypeDefinition typeDefinition)
        {
            base.TraverseChildren(typeDefinition);
        }
        public override void TraverseChildren(IMethodDefinition methodDefinition)
        {
            //var signature = MemberHelper.GetMethodSignature(methodDefinition, NameFormattingOptions.Signature | NameFormattingOptions.ParameterName);
            //System.Console.WriteLine(signature);

            if (methodDefinition.IsAbstract || methodDefinition.IsExternal) return;

            var disassembler = new TinyBCT.Translators.Disassembler(host, methodDefinition, sourceLocationProvider);
            disassembler.Execute();
            foreach(var instruction in disassembler.MethodBody.Instructions)
            {
                var visitor = new AllocAndDelegateFinderVisitor(this);
                instruction.Accept(visitor);
            }
        }
    }
    class AllocAndDelegateFinderVisitor : Backend.Visitors.InstructionVisitor
    {

        //private Instruction instruction;
        private MethodVisitor methodVisitor;
        public AllocAndDelegateFinderVisitor(MethodVisitor methodVisitor)
        {
            this.methodVisitor = methodVisitor;
        }
        public override void Visit(CreateObjectInstruction instruction)
        {
            methodVisitor.AllocatedTypes.Add(instruction.AllocationType);
        }
        public override void Visit(CreateArrayInstruction instruction)
        {
            methodVisitor.AllocatedTypes.Add(instruction.ElementType);
        }
        public override void Visit(LoadInstruction instruction)
        {
            var operand = instruction.Operand;
			if (operand is VirtualMethodReference)
            {
                // DIEGO TODO: Maybe I will need to add the instance 
                var loadDelegateStmt = operand as VirtualMethodReference;
                var methodRef = loadDelegateStmt.Method;
                var instance = loadDelegateStmt.Instance;
                methodVisitor.Delegates.Add(methodRef);

            }
            else if (operand is StaticMethodReference)
            {
                var loadDelegateStmt = operand as StaticMethodReference;
                var methodRef = loadDelegateStmt.Method;
                 methodVisitor.Delegates.Add(methodRef);
            }
        }
    }
}

