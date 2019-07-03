using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TinyBCT.Comparers;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Test")]
namespace TinyBCT.Translators
{
    class FieldTranslator
    {
        internal static IDictionary<IFieldReference, String> fieldNames = FieldTranslator.NewFieldReferenceMap();

        public static IDictionary<IFieldReference, String> NewFieldReferenceMap()
        {
            return new Dictionary<IFieldReference, String>(new IFieldReferenceComparer());
        }

        public static IEnumerable<IFieldReference> GetFieldReferences()
        {
            var d = ImmutableDictionary.ToImmutableDictionary(fieldNames);
            return d.Keys;
        }

        public static ISet<String> GetFieldDefinitions()
        {
            ISet<String> values = new HashSet<String>();

            var r = fieldNames.Where(kv => kv.Value.Contains("F$TinyBCT.Extensions.$ReadAsAsyncStub$d__0`1.__u__1"));

            foreach (var item in fieldNames)
            {
                var def = BoogieGenerator.Instance().GetFieldDefinition(item.Key, item.Value);
                values.Add(def);
            }

            return values;
        }

        public static String GetFieldName(IFieldReference fieldRef)
        {
            if (fieldNames.ContainsKey(fieldRef))
                return fieldNames[fieldRef];

            FieldTranslator ft = new FieldTranslator();

            var name = ft.BoogieNameForField(fieldRef.ContainingType, fieldRef.Name.Value);
            fieldNames.Add(fieldRef, name);
            return name;
        }

        public String BoogieNameForField(ITypeReference containingType, string fName)
        {
            var typeName = Helpers.GetNormalizedType(containingType);
            var fieldName = typeName + "." + fName;
            var name = String.Format("F${0}", fieldName);
            name = Helpers.Strings.NormalizeStringForCorral(name);

            return name;
        }
    }
}
