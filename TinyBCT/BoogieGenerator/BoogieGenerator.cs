using Backend.ThreeAddressCode.Values;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyBCT.Translators;

namespace TinyBCT
{
    public class BoogieGenerator
    {
        public static string AssumeInverseRelationUnionAndPrimitiveType(string variable, string boogieType)
        {
            boogieType = boogieType[0].ToString().ToUpper() + boogieType.Substring(1);
            var e1 = string.Format("{0}2Union({1})", boogieType, variable);
            return string.Format("assume Union2{0}({1}) == {2};", boogieType, e1, variable);
        }
        public static string AssumeInverseRelationUnionAndPrimitiveType(IVariable variable)
        {
            var boogieType = Helpers.GetBoogieType(variable.Type);
            Contract.Assert(!String.IsNullOrEmpty(boogieType));

            return AssumeInverseRelationUnionAndPrimitiveType(variable.ToString(), boogieType);
        }

        public static string PrimitiveType2Union(IVariable value)
        {
            var boogieType = Helpers.GetBoogieType(value.Type);
            Contract.Assert(!string.IsNullOrEmpty(boogieType) && !boogieType.Equals("Ref"));

            return PrimitiveType2Union(boogieType, value.ToString());
        }
        public static string PrimitiveType2Union(string boogieType, string value)
        {
            // int -> Int, bool -> Bool
            boogieType = boogieType[0].ToString().ToUpper() + boogieType.Substring(1);
            return string.Format("{0}2Union({1})", boogieType, value);
        }

        public static string Union2PrimitiveType(IVariable value)
        {
            var boogieType = Helpers.GetBoogieType(value.Type);
            Contract.Assert(!string.IsNullOrEmpty(boogieType) && !boogieType.Equals("Ref"));

            return Union2PrimitiveType(boogieType, value.ToString());
        }
        public static string Union2PrimitiveType(string boogieType, string value)
        {
            // int -> Int, bool -> Bool
            boogieType = boogieType[0].ToString().ToUpper() + boogieType.Substring(1);
            return string.Format("Union2{0}({1})", boogieType, value);
        }

        public static string WriteStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            StringBuilder sb = new StringBuilder();

            string opStr = value.ToString();
            if (value.Type.TypeCode.Equals(PrimitiveTypeCode.String))
                opStr = Helpers.Strings.fixStringLiteral(value);

            String fieldName = FieldTranslator.GetFieldName(staticFieldAccess.Field);
            sb.Append(String.Format("\t\t{0} := {1};", fieldName, opStr));

            return sb.ToString();
        }

        public static string WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value)
        {
            StringBuilder sb = new StringBuilder();

            string opStr = value.ToString();
            if (value.Type.TypeCode.Equals(PrimitiveTypeCode.String))
                opStr = Helpers.Strings.fixStringLiteral(value);

            String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

            var boogieType = Helpers.GetBoogieType(value.Type);
            Contract.Assert(!string.IsNullOrEmpty(boogieType));

            if (!boogieType.Equals("Ref")) // int, bool, real
            {
                sb.AppendLine(AssumeInverseRelationUnionAndPrimitiveType(value));
                sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, PrimitiveType2Union(boogieType, opStr)));
            }
            else
            {
                sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, opStr));
            }

            return sb.ToString();
        }
    }
}
