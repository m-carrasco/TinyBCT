using Backend;
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    // the intent of this class is to transform a method that follows the next pattern:
    // void foo(Arg arg1){
    // ...
    // arg1 = some value // corral doesn't like this - expects immutable arguments
    // }
    
    // TODO: check behaviour of this code when "ref" feature is implemented
    // void foo(ref a){...}
    class ImmutableArguments : InstructionVisitor
    {
        public ImmutableArguments(MethodBody mb) : base()
        {
            methodBody = mb;
        }

        MethodBody methodBody;
        List<LoadInstruction> newLoads = new List<LoadInstruction>();
        IDictionary<IVariable, IVariable> argumentToNewVariable = new Dictionary<IVariable, IVariable>();

        public void Transform()
        {
            foreach (var ins in methodBody.Instructions)
                ins.Accept(this);

            foreach (var ins in methodBody.Instructions)
            {
                foreach (var item in argumentToNewVariable)
                    ins.Replace(item.Key, item.Value);
            }

            foreach (var ins in newLoads)
                methodBody.Instructions.Insert(0, ins);
        }

        private IVariable AddNewLocalVariable(IVariable var)
        {
            string variableName = String.Format("$immutableArgument_{0}", var.Name);
            var tempVar = new LocalVariable(variableName, false, methodBody.MethodDefinition);
            tempVar.Type = var.Type;
            methodBody.Variables.Add(tempVar);
            return tempVar;
        }

        public override void Visit(LoadInstruction instruction)
        {
            base.Visit(instruction);

            // method argument is assigned
            // thus violating corral's requirements
            if (methodBody.Parameters.Contains(instruction.Result))
            {
                // creates new copy of the assigned argument
                // creates a load instruction for to set original argument value to the new local copy
                // the new local copy will replace the use of the argument

                var newVar = AddNewLocalVariable(instruction.Result);
                var newLoad = new LoadInstruction(0, newVar, instruction.Result);
                newLoads.Add(newLoad);
                argumentToNewVariable.Add(instruction.Result, newVar);
            }
        }
    }
}
