using Backend;
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    class FieldInitialization
    {
        public FieldInitialization(MethodBody methodBody)
        {
            this.methodBody = methodBody;
        }

        MethodBody methodBody;

        // some field initialization can be missing in constructors
        // example:
        /*
         class Foo{
                int x; // this initialization is not added by the compiler
                int y = y;
            }
         */

        // therefore we are adding the initialization for each field at the top of every constructor.

        public void Transform()
        {
            // these variables hold the default value
            IDictionary<Helpers.BoogieType, LocalVariable > boogieTypeToLocalVariable =
                new Dictionary<Helpers.BoogieType, LocalVariable>();

            // they are the same stored in boogieTypeToLocalVariable
            IList<LocalVariable> variables 
                = new List<LocalVariable>();

            // assignment of constants
            IList<Instruction> instructions
                 = new List<Instruction>();

            CreateLocalVariablesWithDefaultValues(boogieTypeToLocalVariable, variables, instructions);

            // we need to initialize local variables.

            foreach (var lv in methodBody.Variables)
            {
                if (lv.IsParameter || 
                    // in the delegate handling this type of variables are not used
                    // calling get boogie type will crash
                    lv.Type is Microsoft.Cci.Immutable.FunctionPointerType) 
                    continue;

                var varBoogieType = Helpers.GetBoogieType(lv.Type);
                IVariable initialValue = boogieTypeToLocalVariable[varBoogieType];
                var storeInstruction = new LoadInstruction(0, lv, initialValue);
                storeInstruction.Label = String.Empty;
                instructions.Add(storeInstruction);
            }


            var fields = methodBody.MethodDefinition.ContainingTypeDefinition.Fields;
            if (methodBody.MethodDefinition.IsStaticConstructor)
            {
                foreach (IFieldDefinition field in fields.Where(f => f.IsStatic))
                {
                    var fieldBoogieType = Helpers.GetBoogieType(field.Type);
                    IVariable initialValue = boogieTypeToLocalVariable[fieldBoogieType];
                    var staticAccess = new StaticFieldAccess(field);
                    var storeInstruction = new StoreInstruction(0, staticAccess, initialValue);
                    storeInstruction.Label = String.Empty;
                    instructions.Add(storeInstruction);
                }
            } else if (methodBody.MethodDefinition.IsConstructor)
            {
                var thisVariable = methodBody.Parameters[0];

                foreach (IFieldDefinition field in fields.Where(f => !f.IsStatic))
                {
                    var fieldBoogieType = Helpers.GetBoogieType(field.Type);
                    IVariable initialValue = boogieTypeToLocalVariable[fieldBoogieType];
                    var instanceAccess = new InstanceFieldAccess(thisVariable, field);
                    var storeInstruction = new StoreInstruction(0, instanceAccess, initialValue);
                    storeInstruction.Label = String.Empty;
                    instructions.Add(storeInstruction);
                }
            }

            methodBody.Variables.UnionWith(variables);
            int idx = 0;
            foreach (var i in instructions)
            {
                methodBody.Instructions.Insert(idx, i);
                idx++;
            }
        }

        private void CreateLocalVariablesWithDefaultValues(IDictionary<Helpers.BoogieType, LocalVariable> boogieTypeToLocalVariable, IList<LocalVariable> variables, IList<Instruction> instructions)
        {
            // int

            var defaultInt = new LocalVariable("$defaultIntValue", false, methodBody.MethodDefinition);
            defaultInt.Type = Types.Instance.PlatformType.SystemInt32;
            Contract.Assert(Helpers.GetBoogieType(defaultInt.Type).Equals(Helpers.BoogieType.Int));

            var intAssign = new LoadInstruction(0, defaultInt, new Constant(0) { Type = defaultInt.Type });
            intAssign.Label = String.Empty;

            variables.Add(defaultInt);
            instructions.Add(intAssign);

            boogieTypeToLocalVariable.Add(Helpers.BoogieType.Int, defaultInt);

            // real

            var defaultReal = new LocalVariable("$defaultRealValue", false, methodBody.MethodDefinition);
            defaultReal.Type = Types.Instance.PlatformType.SystemFloat32;
            Contract.Assert(Helpers.GetBoogieType(defaultReal.Type).Equals(Helpers.BoogieType.Real));

            var realAssing = new LoadInstruction(0, defaultReal, new Constant(0F) { Type = defaultReal.Type });
            realAssing.Label = String.Empty;

            variables.Add(defaultReal);
            instructions.Add(realAssing);

            boogieTypeToLocalVariable.Add(Helpers.BoogieType.Real, defaultReal);

            // bool

            var defaultBool = new LocalVariable("$defaultBoolValue", false, methodBody.MethodDefinition);
            defaultBool.Type = Types.Instance.PlatformType.SystemBoolean;
            Contract.Assert(Helpers.GetBoogieType(defaultBool.Type).Equals(Helpers.BoogieType.Bool));

            var boolAssign = new LoadInstruction(0, defaultBool, new Constant(false) { Type = defaultBool.Type });
            boolAssign.Label = String.Empty;

            variables.Add(defaultBool);
            instructions.Add(boolAssign);

            boogieTypeToLocalVariable.Add(Helpers.BoogieType.Bool, defaultBool);

            // Ref

            var defaultRef = new LocalVariable("$defaultRef", false, methodBody.MethodDefinition);
            defaultRef.Type = Types.Instance.PlatformType.SystemObject;
            Contract.Assert(Helpers.GetBoogieType(defaultRef.Type).Equals(Helpers.BoogieType.Ref));

            var refAssign  = new LoadInstruction(0, defaultRef, new Constant(null) { Type = defaultRef.Type });
            refAssign.Label = String.Empty;

            variables.Add(defaultRef);
            instructions.Add(refAssign);

            boogieTypeToLocalVariable.Add(Helpers.BoogieType.Ref, defaultRef);
        }
    }
}
