using Backend;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Test")]
namespace TinyBCT.Translators
{
    class StaticInitializer
    {
        internal static ISet<IMethodDefinition> mainMethods 
            = new HashSet<IMethodDefinition>();
        internal static ISet<IMethodDefinition> staticConstructors 
            = new HashSet<IMethodDefinition>();

        public static void IMethodDefinitionTraverse(IMethodDefinition mD, IMetadataHost host, ISourceLocationProvider sourceLocationProvider)
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
            StringBuilder sb
            = new StringBuilder();
            foreach (var mainMethod in mainMethods)
            {
                var methodName = BoogieMethod.From(mainMethod).Name;
                var parameters = Helpers.GetParametersWithBoogieType(mainMethod);
                var returnType = Helpers.GetMethodBoogieReturnType(mainMethod).Equals(Helpers.BoogieType.Void) ? String.Empty : ("returns ($result :" + Helpers.GetMethodBoogieReturnType(mainMethod) + ")");
                sb.AppendLine(String.Format("procedure {{:entrypoint}} $Main_Wrapper_{0}({1}) {2}", methodName, parameters, returnType));
                sb.AppendLine("{");

                var variables = String.Empty;
                IMethodDefinition methodDef = mainMethod as IMethodDefinition;
                if (methodDef != null)
                    variables = String.Join(",", methodDef.Parameters.Select(v => v.Name));
                else
                    variables = String.Join(",", mainMethod.Parameters.Select(v => String.Format("param{0}", v.Index)));

                if (mainMethod.CallingConvention.HasFlag(Microsoft.Cci.CallingConvention.HasThis))
                    variables = String.Format("this", mainMethod.ParameterCount > 0 ? "," : String.Empty, parameters);

                variables = Helpers.Strings.NormalizeStringForCorral(variables);

                sb.AppendLine("\tcall $initialize_globals();");
                if (String.IsNullOrEmpty(returnType))
                    sb.AppendLine(String.Format("\tcall {0}({1});", methodName, variables));
                else
                    sb.AppendLine(String.Format("\tcall $result := {0}({1});", methodName, variables));

                sb.AppendLine("\tif ($Exception != null)");
                sb.AppendLine("\t{");
                sb.AppendLine("\t\treturn;");
                sb.AppendLine("\t}");

                sb.AppendLine("}");

            }

            return sb.ToString();
        }

        public static string CreateInitializeGlobals()
        {
            StringBuilder sb
            = new StringBuilder();
            sb.AppendLine("procedure $initialize_globals()");
            sb.AppendLine("{");
            sb.AppendLine("\t//this procedure initializes global exception variables and calls static constructors");
            sb.AppendLine("\t$Exception := null;");
            sb.AppendLine("\t$ExceptionType := null;");
            sb.AppendLine("\t$ExceptionInCatchHandler := null;");
            sb.AppendLine("\t$ExceptionInCatchHandlerType := null;");

            foreach (var staticConstructor in staticConstructors)
            {
                var signature = BoogieMethod.From(staticConstructor).Name;
                sb.AppendLine(String.Format("\tcall {0}();", signature));
                sb.AppendLine("\tif ($Exception != null)");
                sb.AppendLine("\t{");
                sb.AppendLine("\t\treturn;");
                sb.AppendLine("\t}");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
