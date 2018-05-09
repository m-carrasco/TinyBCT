using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT.Translators
{
    class FieldTranslator
    {
        IFieldReference fieldRef;
        private static IDictionary<IFieldReference, String> fieldNames = new Dictionary<IFieldReference, String>();

        public static IList<String> GetFieldDefinitions()
        {
            IList<String> values = new List<String>();

            foreach (var item in fieldNames)
            {
                if (item.Key.IsStatic) {
                    values.Add(String.Format("var {0}: {1};", item.Value, Helpers.GetBoogieType(item.Key.Type)));
                } else {
                    values.Add(String.Format("const unique {0} : Field;", item.Value));
                }
            }

            return values;
        }

        public static String GetFieldName(IFieldReference fieldRef)
        {
            if (fieldNames.ContainsKey(fieldRef))
                return fieldNames[fieldRef];

            FieldTranslator ft = new FieldTranslator(fieldRef);

            return ft.Translate();
        }

        public FieldTranslator(IFieldReference f)
        {
            fieldRef = f;
        }

        public String Translate()
        {
            var typeName = Helpers.GetNormalizedType(fieldRef.ContainingType);
            var fieldName = typeName + "." + fieldRef.Name.Value;
            var name = String.Format("F${0}", fieldName);
            name = Helpers.NormalizeStringForCorral(name);

            fieldNames.Add(fieldRef, name);
            return name;
        }
    }
}
