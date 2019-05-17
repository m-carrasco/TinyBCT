using System;
using Backend.ThreeAddressCode.Instructions;
using Backend.Visitors;
using Backend.ThreeAddressCode.Values;
using System.Collections.Generic;
using Microsoft.Cci;

namespace TinyBCT
{
    public class ReferenceFinder : InstructionVisitor
    {
        // this set has the result of the last analysis
        static ISet<IReferenceable> ReferencedSet;
        static ISet<IFieldReference> FieldReferencedSet;

        ISet<IReferenceable> referencedSet;
        ISet<IFieldReference> fieldReferencedSet;

        public override void Visit(LoadInstruction instruction) {
            if (instruction.Operand is Reference reference)
            {
                // Reference object is an expression of the form &<...>
                // <...> is the pointedObj
                IReferenceable pointedObj = reference.Value;
                referencedSet.Add(pointedObj);

                // redundancy just to have faster lookups
                if (pointedObj is StaticFieldAccess staticFieldAccess)
                    fieldReferencedSet.Add(staticFieldAccess.Field);
                else if (pointedObj is InstanceFieldAccess instanceFieldAccess)
                    fieldReferencedSet.Add(instanceFieldAccess.Field);
            }
        }

        public override void Visit(IInstructionContainer container)
        {
            referencedSet = new HashSet<IReferenceable>();
            fieldReferencedSet = new HashSet<IFieldReference>();

            base.Visit(container);

            ReferencedSet = referencedSet;
            FieldReferencedSet = fieldReferencedSet;
        }

        static public bool IsReferenced(IReferenceable referenceable)
        {
            return ReferencedSet.Contains(referenceable);
        }

        static public bool IsReferenced(IFieldReference field)
        {
            return FieldReferencedSet.Contains(field);
        }
    }
}
