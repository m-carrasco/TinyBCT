using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Test")]
namespace TinyBCT.Translators
{
    class FieldTranslator
    {
        IFieldReference fieldRef;
        internal static IDictionary<IFieldReference, String> fieldNames = new Dictionary<IFieldReference, String>();

        public static ISet<String> GetFieldDefinitions()
        {
            ISet<String> values = new HashSet<String>();

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

            FieldTranslator ft = new FieldTranslator(fieldRef);

            return ft.BoogieNameForField();
        }

        public FieldTranslator(IFieldReference f)
        {
            fieldRef = f;
        }

        public String BoogieNameForField()
        {
            var typeName = Helpers.GetNormalizedType(fieldRef.ContainingType);
            var fieldName = typeName + "." + fieldRef.Name.Value;
            var name = String.Format("F${0}", fieldName);
            name = Helpers.Strings.NormalizeStringForCorral(name);

            fieldNames.Add(fieldRef, name);
            return name;
        }
    }
}
