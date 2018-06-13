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
        int instructionIndex = 0;

        public void Transform()
        {
            foreach (var ins in methodBody.Instructions)
            {
                ins.Accept(this);
                instructionIndex++;
            }

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

            /*
                // we want to detect code like this
                // and use in the method call i and j
                // in boogie the method will overwrite their values
             
                local Int32 i;
                local Int32 j;
                local Int32* $r2;
                local Int32* $r3;

                $r2 = &i;
                $r3 = &j;
                RefKeyword::Ref($r2, $r3);

                // expected boogie (bct works like this)
                call i, j := RefKeyword::Ref(i, j);
                // we copy their values and then return a new value
             */

            if (instruction.Operand.Type is IManagedPointerType && 
                instruction.Operand is Reference && 
                IsInMethodCallAsRef(instruction.Result))
            {
                var r = instruction.Operand as Reference;
                variableToParameter.Add(instruction.Result, r.Variables.First());

            }


            /*
                this check is used to detect ref/out parameters

                parameter Int32* a;
                parameter Int32* b;

                local Int32* $r0;
                local Int32 $r1;
                local Int32* $r2;
                local Int32 $r3;

                LABEL_0:
                        $r0 = a;
                        $r1 = 10;
                        *$r0 = $r1;
                LABEL_1:
                        $r2 = b;
                        $r3 = 11;
                        *$r2 = $r3;
                        return;

                // we will ignore pointers and use them as their target types
                // that's why the aliasing in LABEL_0 and LABEL_1 wont work
                // we need to replace every use of $r0 by a and $r2 by b

                // in boogie we return the new values of a and b (read previous comment)
             */

            if (instruction.HasResult && v != null && methodBody.Parameters.Contains(instruction.Operand) && IsRefArgument(v))
                variableToParameter.Add(instruction.Result, v);
        }

        public bool IsInMethodCallAsRef(IVariable var)
        {
            // if we found a method call instruction that uses var as a ref parameter, then return true
            // manuel: test behavior with out parameters

            for (int i = instructionIndex+1; i < methodBody.Instructions.Count(); i++)
            {
                MethodCallInstruction methodCallIns 
                    = methodBody.Instructions[instructionIndex] as MethodCallInstruction;

                if (methodCallIns != null && methodCallIns.Arguments.IndexOf(var) != -1)
                {
                    // add one because of the "this" argument
                    var idx = methodCallIns.Arguments.IndexOf(var) + (!methodCallIns.Method.IsStatic ? 1 : 0);

                    // not sure what happens to out parameters
                    if (methodCallIns.Method.Parameters.ElementAt(idx).IsByReference)
                        return true;
                }
            }

            return false;
        }

        public bool IsRefArgument(IVariable var)
        {
            var res = methodBody.MethodDefinition.Parameters.Where(p => p.Name.Value.Equals(var.Name) && (p.IsByReference || p.IsOut));

            Contract.Assert(res.Count() == 0 || res.Count() == 1);

            return res.Count() > 0;
        }
    }
}
