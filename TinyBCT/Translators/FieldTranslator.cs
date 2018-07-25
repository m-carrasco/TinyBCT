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
                if (!Settings.NewAddrModelling)
                {
                    if (item.Key.IsStatic)
                    {
                        values.Add(String.Format("var {0}: {1};", item.Value, Helpers.GetBoogieType(item.Key.Type)));
                    }
                    else
                    {
                        if (!Settings.SplitFields)
                            values.Add(String.Format("const unique {0} : Field;", item.Value));
                        else
                        {
                            if (Helpers.IsGenericField(item.Key))
                            {
                                values.Add(String.Format("var {0} : [Ref]Union;", item.Value));
                            }
                            else
                            {
                                var boogieType = Helpers.GetBoogieType(item.Key.Type);
                                Contract.Assert(!string.IsNullOrEmpty(boogieType));
                                values.Add(String.Format("var {0} : [Ref]{1};", item.Value, boogieType));
                            }
                        }
                    }
                } else
                {
                    if (item.Key.IsStatic)
                    {
                        values.Add(String.Format("var {0}: {1};", item.Value, "Addr"));
                    }
                    else
                    {
                        values.Add(String.Format("var {0} : InstanceFieldAddr;", item.Value));
                    }
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
            name = Helpers.Strings.NormalizeStringForCorral(name);

            fieldNames.Add(fieldRef, name);
            return name;
        }
    }
}
