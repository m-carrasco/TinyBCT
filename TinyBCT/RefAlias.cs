using Backend;
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Visitors;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{

    public class RefAlias : InstructionVisitor
    {
        public RefAlias(MethodBody mb) : base()
        {
            methodBody = mb;
        }

        MethodBody methodBody;

        public void Transform()
        {
            foreach (var ins in methodBody.Instructions)
                ins.Accept(this);

            foreach (var item in variableToParameter)
            {
                foreach (var ins in methodBody.Instructions)
                    ins.Replace(item.Key, item.Value);
            }
        }

        public IDictionary<IVariable, IVariable> variableToParameter 
            = new Dictionary<IVariable, IVariable>();

        public override void Visit(LoadInstruction instruction)
        {
            base.Visit(instruction);
            string name = instruction.Operand.ToString();
            var v = instruction.Operand as IVariable;

            if (instruction.Operand.Type is IManagedPointerType && instruction.Operand is Reference)
            {
                var r = instruction.Operand as Reference;
                variableToParameter.Add(instruction.Result, r.Variables.First());

            }

            if (instruction.HasResult && v != null && methodBody.Parameters.Contains(instruction.Operand) && IsRefArgument(v))
                variableToParameter.Add(instruction.Result, v);
        }

        public bool IsRefArgument(IVariable var)
        {
            var res = methodBody.MethodDefinition.Parameters.Where(p => p.Name.Value.Equals(var.Name) && (p.IsByReference || p.IsOut));

            Contract.Assert(res.Count() == 0 || res.Count() == 1);

            return res.Count() > 0;
        }
    }
}
