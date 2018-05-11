using Backend;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT.Translators
{
    class StaticInitializer
    {
        private static StringBuilder sb 
            = new StringBuilder();
        private static ISet<IMethodDefinition> mainMethods 
            = new HashSet<IMethodDefinition>();
        private static ISet<IMethodDefinition> staticConstructors 
            = new HashSet<IMethodDefinition>();

        public static void IMethodDefinitionTraverse(IMethodDefinition mD, MethodBody mB)
        {
            if (mD.IsStaticConstructor)
            {
                staticConstructors.Add(mD);
            } else if (Helpers.IsMain(mD))
            {
                mainMethods.Add(mD);
            }
        }

        public static string CreateMainWrappers()
        {

            foreach (var mainMethod in mainMethods)
            {
                var methodName = Helpers.GetMethodName(mainMethod);
                var parameters = Helpers.GetParametersWithBoogieType(mainMethod);
                var returnType = Helpers.GetMethodBoogieReturnType(mainMethod) == null ? String.Empty : ("returns ($result :" + Helpers.GetMethodBoogieReturnType(mainMethod) + ")");
                sb.AppendLine(String.Format("procedure $Main_Wrapper_{0}({1}) {2}",methodName, parameters, returnType));
                sb.AppendLine("{");

                var variables = String.Empty;
                IMethodDefinition methodDef = mainMethod as IMethodDefinition;
                if (methodDef != null)
                    variables = String.Join(",", methodDef.Parameters.Select(v => v.Name));
                else
                    variables = String.Join(",", mainMethod.Parameters.Select(v => String.Format("param{0}", v.Index)));

                if (mainMethod.CallingConvention.HasFlag(Microsoft.Cci.CallingConvention.HasThis))
                    variables = String.Format("this", mainMethod.ParameterCount > 0 ? "," : String.Empty, parameters);

                variables = Helpers.NormalizeStringForCorral(variables);
                sb.AppendLine(String.Format("\tcall {0}({1});", methodName, variables));


                sb.AppendLine("\tif ($Exception != null)");
                sb.AppendLine("\t\treturn;");

                sb.AppendLine("\t}");

            }

            return sb.ToString();
        }

        public static string CreateInitializeGlobals()
        {
            sb.AppendLine("procedure $initialize_globals()");
            sb.AppendLine("{");
            sb.AppendLine("\t//this procedure initializes global exception variables and calls static constructors");
            sb.AppendLine("\t$Exception := null;");
            sb.AppendLine("\t$ExceptionType := null");

            foreach (var staticConstructor in staticConstructors)
            {
                var signature = Helpers.GetMethodName(staticConstructor);
                sb.AppendLine(String.Format("\tcall {0}();", signature));
                sb.AppendLine("\tif ($Exception != null)");
                sb.AppendLine("\t\treturn;");
            }

            sb.AppendLine("\t}");
            return sb.ToString();
        }
    }
}
