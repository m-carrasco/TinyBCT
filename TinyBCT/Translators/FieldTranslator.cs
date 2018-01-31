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
        ITypeDefinition typeDef;
        public static IDictionary<IFieldReference, String> fieldNames = new Dictionary<IFieldReference, String>();

        public FieldTranslator(ITypeDefinition t)
        {
            typeDef = t;
        }

        public String Translate()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var field in typeDef.Fields)
            {
                var name = String.Format("F${0}", field.ToString());
                sb.AppendLine(String.Format("const unique {0} : Field;", name));

                fieldNames.Add(field, name);
            }

            return sb.ToString();
        }

    }
}
