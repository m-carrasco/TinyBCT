using System.Collections.Generic;
using Backend.ThreeAddressCode.Instructions;
using Microsoft.Cci;
using System.Linq;
using Backend.Transformations;
using Backend;
using Backend.Analyses;
using Backend.Model;

namespace TinyBCT
{
    public class PropertiesFinder
    {
        private bool IsGetOrSetProp(MethodCallInstruction m)
        { 
            if (!m.Method.Type.Equals(Types.Instance.PlatformType.SystemVoid) && m.Method.Name.Value.StartsWith("get_"))
                return true;

            if (m.Method.Type.Equals(Types.Instance.PlatformType.SystemVoid) && m.Method.Name.Value.StartsWith("set_"))
                return true;

            return false;
        }
        public IEnumerable<IMethodReference> FindPropertiesCalls(IEnumerable<Instruction> methodBody)
        {
            return methodBody.Where(i => i is MethodCallInstruction m && IsGetOrSetProp(m)).Cast<MethodCallInstruction>().Select(i => i.Method);
        }

        public IEnumerable<IMethodReference> FindPropertiesCalls(ISet<Assembly> assemblies)
        {
            var allDefinedMethods = assemblies.GetAllDefinedMethods();
            var definedMethodsWithBody = allDefinedMethods.Where(m => m.Body != null && m.Body.Size > 0);

            Function<IMethodDefinition, MethodBody> disassemble = (method) =>
            {
                var disassembler = new TinyBCT.Translators.Disassembler(assemblies.First().Host, method, assemblies.First().PdbReader);
                disassembler.Execute();
                return disassembler.MethodBody;
            };

            var methodBodies = definedMethodsWithBody.Select(m => disassemble(m).Instructions);

            return methodBodies.SelectMany(body => FindPropertiesCalls(body));
        }
    }
}
