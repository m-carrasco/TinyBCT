using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Cci;
using TinyBCT.Translators;

namespace TinyBCT
{
    public class GetterSetterStub
    {
        // this method returns all method references that have been stubbed
        public IEnumerable<IMethodReference> Stub(ISet<Assembly> inputAssemblies, StreamWriter streamWriter)
        {
            PropertiesFinder propertiesFinder = new PropertiesFinder();
            var nonExternMethods = inputAssemblies.GetAllDefinedMethods().Where(m => m.Body.Size > 0);
            var usedProperties = propertiesFinder.FindPropertiesCalls(inputAssemblies).Except(nonExternMethods);
            foreach (var property in usedProperties)
            {
                var proc = property.Name.Value.StartsWith("get_") ? GetProcedureStub(property) : SetProcedureStub(property);
                streamWriter.WriteLine(proc);
            }

            return usedProperties;
        }

        public string GetFieldDef(IMethodReference method)
        {
            FieldTranslator field = new FieldTranslator();
            var boogieName = field.BoogieNameForField(method.ContainingType, method.Name.Value);
            var boogieType = Helpers.GetBoogieType(method.Type);
            return String.Format("var {0} : [Ref]{1};", boogieName, boogieType);
        }

        public string GetProcedureStub(IMethodReference method)
        {
            FieldTranslator field = new FieldTranslator();
            var boogieName = field.BoogieNameForField(method.ContainingType, method.Name.Value);
            var boogieType = Helpers.GetBoogieType(method.Type);

            var get = new StatementList();
            get.Add(BoogieStatement.FromString("$result := " + boogieName + "[obj];"));

            var t = new BoogieProcedureTemplate(BoogieMethod.From(method).Name, "", StatementList.Empty, get, "obj : Ref", String.Format(" returns  ( $result : {0})", boogieType.ToString()), false);
            return t.TransformText();
        }

        public string SetProcedureStub(IMethodReference method)
        {
            FieldTranslator field = new FieldTranslator();
            var boogieName = field.BoogieNameForField(method.ContainingType, method.Name.Value);
            var paramType = Helpers.GetBoogieType(method.Parameters.ElementAt(1));

            var get = new StatementList();
            get.Add(BoogieStatement.FromString(boogieName + "[obj] := " + "val"));

            var t = new BoogieProcedureTemplate(BoogieMethod.From(method).Name, "", StatementList.Empty, get, "obj : Ref, val : " + paramType, String.Empty, false);

            return t.TransformText();
        }

    }
}
