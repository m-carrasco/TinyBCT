using Backend;
using Backend.ThreeAddressCode.Values;
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
        internal static IEnumerable<IMethodDefinition> mainMethods;
        internal static IEnumerable<IMethodDefinition> staticConstructors;

        public static void SearchStaticConstructorsAndMain(ISet<Assembly> assemblies)
        {
            staticConstructors = assemblies.GetAllDefinedMethods().Where(m => m.IsStaticConstructor);
            mainMethods = assemblies.GetAllDefinedMethods().Where(m => Helpers.IsMain(m));
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

                sb.AppendLine("\tcall $allocate_static_fields();");
                sb.AppendLine("\tcall $default_values_static_fields();");
                sb.AppendLine("\tcall $initialize_globals();");
                sb.AppendLine("\tcall $call_static_constructors();");
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
            sb.AppendLine("procedure {:ProgramInitialization} $initialize_globals()");
            sb.AppendLine("{");
            sb.AppendLine("\t//this procedure initializes global exception variables and calls static constructors");
            sb.AppendLine("\t$Exception := null;");
            sb.AppendLine("\t$ExceptionType := null;");
            sb.AppendLine("\t$ExceptionInCatchHandler := null;");
            sb.AppendLine("\t$ExceptionInCatchHandlerType := null;");

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static BoogieLiteral GetDefaultConstant(Helpers.BoogieType boogieType)
        {
            if (boogieType == Helpers.BoogieType.Int)
            {
                Constant zero = new Constant(0) { Type = Types.Instance.PlatformType.SystemInt32 };
                return BoogieLiteral.Numeric(zero);
            }

            if (boogieType == Helpers.BoogieType.Bool)
            {
                return BoogieLiteral.False; 
            }

            if (boogieType == Helpers.BoogieType.Real)
            {
                Constant zero = new Constant(0F) { Type = Types.Instance.PlatformType.SystemFloat32 };
                return BoogieLiteral.Numeric(zero);
            }

            if (boogieType == Helpers.BoogieType.Object)
            {
                return BoogieLiteral.NullObject;
            }

            // address should not require a default value, you create one the same time you are referencing something.
            throw new NotImplementedException("Unexpected type to initialize");
        }

        public static string CreateDefaultValuesStaticVariablesProcedure()
        {
            #region Create body of the procedure
            StatementList body = new StatementList();

            foreach (IFieldReference field in FieldTranslator.GetFieldReferences())
            {
                if (field.IsStatic)
                {
                    BoogieGenerator bg = BoogieGenerator.Instance();
                    StaticFieldAccess staticFieldAccess = new StaticFieldAccess(field);

                    body.Add(bg.WriteStaticField(staticFieldAccess, GetDefaultConstant(Helpers.GetBoogieType(field.Type))));
                }
            }
            #endregion

            string procedureName = "$default_values_static_fields";
            string attributes = String.Empty;
            StatementList localVariables = new StatementList();
            String parametersWithTypes = String.Empty;
            String returnTypeIfAny = String.Empty;

            BoogieProcedureTemplate temp = new BoogieProcedureTemplate(procedureName, attributes, localVariables, body, parametersWithTypes, returnTypeIfAny, false);
            return temp.TransformText();
        }

        public static string CreateStaticVariablesAllocProcedure()
        {
            string procedureName = "$allocate_static_fields";
            string attributes = String.Empty;
            StatementList body = BoogieGenerator.Instance().AllocStaticVariables();
            StatementList localVariables = new StatementList();
            String parametersWithTypes = String.Empty;
            String returnTypeIfAny = String.Empty;

            BoogieProcedureTemplate temp = new BoogieProcedureTemplate(procedureName, attributes, localVariables, body, parametersWithTypes, returnTypeIfAny, false);
            return temp.TransformText();
        }

        public static string CreateStaticConstructorsCallsProcedure()
        {
            StatementList body = new StatementList();

            foreach (var staticConstructor in staticConstructors)
            {
                var ctor = BoogieMethod.From(staticConstructor);
                body.Add(BoogieGenerator.Instance().ProcedureCall(ctor, new List<Expression>(), null));
            }

            string procedureName = "$call_static_constructors";
            string attributes = String.Empty;
            StatementList localVariables = new StatementList();
            String parametersWithTypes = String.Empty;
            String returnTypeIfAny = String.Empty;

            BoogieProcedureTemplate temp = new BoogieProcedureTemplate(procedureName, attributes, localVariables, body, parametersWithTypes, returnTypeIfAny, false);
            return temp.TransformText();
        }
    }
}
