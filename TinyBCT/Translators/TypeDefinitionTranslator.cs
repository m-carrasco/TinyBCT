using Backend.Utils;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT.Translators
{
    public class TypeDefinitionTranslator
    {
        public static ISet<ITypeDefinition> classes = new HashSet<ITypeDefinition>();

        // there can be super class types that are not defined in the dll
        // for instance MulticastDelegate
        // once every type definition is processed we check if the super classes were declared 
        // we will declare the difference between parents and classes sets
        public static ISet<ITypeDefinition> parents = new HashSet<ITypeDefinition>();
        
        // Already added, collisions can occur with instantiations
        // of types involving generics.
        public static ISet<string> normalizedTypeStrings = new HashSet<string>();

        INamedTypeDefinition typeDef;
        public TypeDefinitionTranslator(INamedTypeDefinition namedTypeDefinition)
        {
            typeDef = namedTypeDefinition;
        }

        // called from Traverser
        // set in Main
        public static void TranslateTypes(ISet<Assembly> assemblies)
        {
            foreach (INamedTypeDefinition type in assemblies.GetAllDefinedTypes())
            {
                TypeDefinitionTranslator t = new TypeDefinitionTranslator(type);
                // todo: improve this piece of code
                StreamWriter streamWriter = Program.streamWriter;
                streamWriter.WriteLine(t.Translate());
            }
        }

        // this is experimental 
        // we are not sure these axioms improve anything at all
        public static void TypeAxioms()
        {
            StringBuilder sb = new StringBuilder();
            // todo: improve this piece of code
            foreach (var c1 in TypeDefinitionTranslator.classes)
            {
                foreach (var c2 in TypeDefinitionTranslator.classes.Where(c => c != c1))
                {
                    if (!TypeHelper.Type1DerivesFromOrIsTheSameAsType2(c1, c2))
                    {
                        var tn1 = Helpers.GetNormalizedType(c1);
                        var tn2 = Helpers.GetNormalizedType(c2);

                        // axiom(forall $T: Ref:: { $Subtype($T, T1$() } $Subtype($T, $T1) ==> ! $Subtype($T, T2$))
                        sb.AppendLine("axiom(forall $T: Ref:: { " + String.Format("$Subtype($T, T${0}())", tn1)
                             + "} " + String.Format("$Subtype($T, T${0}()) ==>!$Subtype($T, T${1}()));", tn1, tn2));
                    }
                }
            }
            // todo: improve this piece of code
            StreamWriter streamWriter = Program.streamWriter;
            streamWriter.WriteLine(sb);
        }

        // we are missing information of the hierarchy
        // currently this code is defining undeclared classes but there can be information missing
        // todo : improve
        public static void DefineUndeclaredSuperClasses()
        {
            StringBuilder sb = new StringBuilder();


            HashSet<ITypeReference> diff = new HashSet<ITypeReference>();
            diff.UnionWith(InstructionTranslator.MentionedClasses);
            diff.UnionWith(parents);
            diff.ExceptWith(classes);

            InstructionTranslator.MentionedClasses.Clear();
            parents.Clear();

            while (diff.Any()) 
            {
                var c = diff.ElementAt(0);
                diff.Remove(c);
                var typeName = Helpers.GetNormalizedType(c);
                var typeName2 = Helpers.GetNormalizedTypeFunction(c, InstructionTranslator.MentionedClasses);
                if (normalizedTypeStrings.Contains(typeName))
                    continue;

                normalizedTypeStrings.Add(typeName);
                // already in prelude
                if (typeName.Equals("System.Object"))
                    continue;


                GenerateTypeDefinition(sb, c);

                if (InstructionTranslator.MentionedClasses.Any())
                {
                    diff.UnionWith(InstructionTranslator.MentionedClasses);
                    InstructionTranslator.MentionedClasses.Clear();
                }
                if (parents.Any())
                {
                    diff.UnionWith(parents);
                    parents.Clear();
                }

                //var argsString = String.Empty;
                //if (c is IGenericTypeInstanceReference)
                //{
                //    System.Diagnostics.Contracts.Contract.Assume(false);
                //    var instanciatedType = c as IGenericTypeInstanceReference;
                //    var typeArguments = instanciatedType.GenericArguments;
                //    argsString = String.Join(",", typeArguments.Select(t => t.ToString() + " : Ref"));
                //}
                //sb.AppendLine(String.Format("function T${0}({1}) : Ref;", typeName, argsString));
                //sb.AppendLine(String.Format("const unique T${0} : int;", typeName));

                //sb.AppendLine("axiom(forall $T: Ref:: { " + String.Format(" $Subtype(T${0}()", typeName) +
                //    ", $T) } $Subtype(T$" + string.Format("{0}(), $T) <==> T${0}() == $T || $Subtype({1}, $T));", typeName, "T$System.Object()"));
            }

            StreamWriter streamWriter = Program.streamWriter;
            streamWriter.WriteLine(sb);
        }

        public string Translate()
        {
            StringBuilder sb = new StringBuilder();
            var typeName = Helpers.GetNormalizedType(typeDef);
            if (normalizedTypeStrings.Contains(typeName))
                return "";

            normalizedTypeStrings.Add(typeName);
            GenerateTypeDefinition(sb, typeDef);

            // todo: improve this piece of code
            //StreamWriter streamWriter = Program.streamWriter;
            //streamWriter.WriteLine(sb);

            classes.Add(typeDef);
            return sb.ToString();
        }
        private static IEnumerable<ITypeDefinition> GetArgumentsTypes(ITypeDefinition typeDefinition)
        {
            IEnumerable<ITypeDefinition> argTypes = null;
            if (typeDefinition is IGenericTypeInstanceReference)
            {
                var instanciatedType = typeDefinition as IGenericTypeInstanceReference;
                argTypes = instanciatedType.GenericArguments.Select(t => t.ResolvedType);
            }
            return argTypes;
        }

        // the function returns true if the given type reference depends in a type parameter
        // not necessarily the type must be declared as class Type<T> 
        // it could be nested and implicitly depend on a generic parameter 
        // it could inherit a generic parameter
        // there are some situations that can only be resolved if the definition is present
        private static bool IsParametericType(ITypeReference typeReference)
        {
            if (typeReference is IGenericTypeInstanceReference)
                return true;

            INestedTypeReference nestedType = typeReference as INestedTypeReference;
            if (nestedType != null && !(nestedType is Dummy))
            { 
                // check Inner example in TestAxiomsGenerics2
                // it is a nested type reference, and also a INamedTypeReference but GenericParameterCount is zero and DoesNotInheritGenericParameters is true
                var res = !nestedType.ResolvedType.DoesNotInheritGenericParameters; //|| IsParametericType(nestedType.ResolvedType.BaseClasses.First());

                return res;
            }

            INamedTypeReference namedType = typeReference as INamedTypeReference;

            if (namedType != null)
            {
                return namedType.GenericParameterCount > 0;

            }

            return false;
        }

        public static int CountArguments(ITypeReference typeReference)
        {
            if (typeReference is IArrayTypeReference arrayTypeReference)
            {
                return 1;
            }
                

            if (typeReference is IGenericTypeInstanceReference genericType)
            {
                return genericType.GenericArguments.Count();
            }

            if (typeReference is INamedTypeReference namedType)
            {
                return namedType.GenericParameterCount;
            }

            return 0;
        }

        public static void ParametricTypeDeclarations()
        {
            var types = ParametricTypes.Select(t => ParametricTypeDeclaration(t));
            foreach (var t in types)
                Program.streamWriter.WriteLine(t);
        }

        public static string ParametricTypeDeclaration(string type)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(String.Format("function {0}() : Ref;", type));
            sb.AppendLine(String.Format("const unique {0} : int;", type));
            sb.AppendLine(String.Format("axiom $TypeConstructor({0}()) == {0};", type));
            sb.AppendLine(String.Format("axiom(forall  $T: Ref:: {{  $Subtype({0}(), $T) }} $Subtype({0}(), $T) <==> ({0}() == $T || $Subtype(T$System.Object(), $T)));", type));
            return sb.ToString();
        }

        public static ISet<string> ParametricTypes = new HashSet<string>();

        private static IEnumerable<string> GetParametricTypes(int count)
        {
            var ids = Enumerable.Range(0, count);
            var types = ids.Select(i => String.Format("T$T$_{0}", i));
            ParametricTypes.AddRange(types);
            return types;
        }

        public static string GetAbstractFunctionType(ITypeReference typeReference)
        {
            String typeName = Helpers.GetNormalizedType(typeReference);
            int numberArgs = CountArguments(typeReference);
            IEnumerable<string> parameters = GetParametricTypes(numberArgs).Select(t => String.Format("{0}()", t));
            var abstractType = String.Format("T${0}({1})", typeName, String.Join(",", parameters));

            return abstractType;
        }

        private static IEnumerable<string> GetQuantifierVariables(int count)
        {
            var ids = Enumerable.Range(0, count);
            return ids.Select(i => "T" + i);
        }
    
        private static string TypeConstructorAxiom(ITypeReference type)
        {
            string typeName = Helpers.GetNormalizedType(type);
            string abstractType = GetAbstractFunctionType(type);
            return String.Format("axiom $TypeConstructor({0}) == T${1};", abstractType, typeName);
        }

        private static string AxiomAllInstanceTypesSubtypeAbstractType(ITypeReference typeReference)
        {
            var abstractType = GetAbstractFunctionType(typeReference);
            string typeName = Helpers.GetNormalizedType(typeReference);
            IEnumerable<string> quantifiedVarNames = GetQuantifierVariables(CountArguments(typeReference));

            var quantifierDecl = String.Join(",", quantifiedVarNames.Select(t => t + ": Ref"));
            var quantifiedType = String.Format("T${0}({1})", typeName, String.Join(",", quantifiedVarNames));

            return String.Format("axiom(forall {0} :: {{  $Subtype({1}, {2}) }} $Subtype({1}, {2}) );", quantifierDecl, quantifiedType, abstractType);
        }

        private static string SubtypeIfParentsSubtypeOrIsSameTypeOrAbstractType(ITypeReference typeReference, IEnumerable<ITypeReference> superTypes)
        {
            IList<string> quantifiedVarNames = GetQuantifierVariables(CountArguments(typeReference)).ToList();
            var abstractFunctionType = GetAbstractFunctionType(typeReference); // if it is not a parametrized type it returns the same type
            string typeName = Helpers.GetNormalizedType(typeReference);

            var subtyping = superTypes.Select(t => String.Format("$Subtype({0},$T_)", GetAbstractFunctionType(t))).ToList();
            subtyping.Add(String.Format("{0} == $T_", abstractFunctionType));
            string subtypingOr = String.Join(" || ", subtyping);


            var quantifiedType = String.Format("T${0}({1})", typeName, String.Join(",", quantifiedVarNames));
            quantifiedVarNames.Add("$T_"); // we dont want this variable in the quantified type (that's why it is added after)
            var quantifiedDecl = String.Join(",", quantifiedVarNames.Select(t => t + ": Ref"));

            return String.Format("axiom(forall {0} :: {{  $Subtype({1}, $T_) }} ", quantifiedDecl, quantifiedType) +
                    String.Format("$Subtype({0}, $T_) <==> ({1}));", quantifiedType, subtypingOr);
        }

        private static string TypeFunction(ITypeReference typeReference)
        {
            string typeName = Helpers.GetNormalizedType(typeReference);
            int numberArgs = CountArguments(typeReference);
            var funcParams = Enumerable.Range(0, numberArgs).Select(idx => String.Format("t{0} : Ref", idx));
            String argsString = String.Join(",", funcParams);

            return String.Format("function T${0}({1}) : Ref;", typeName, argsString);
        }

        private static string TypeConstant(ITypeReference typeReference)
        {
            string typeName = Helpers.GetNormalizedType(typeReference);
            return String.Format("const unique T${0} : int;", typeName);
        }

        public static void GenerateTypeDefinition(StringBuilder sb, ITypeReference typeReference)
        {
            InstructionTranslator.MentionedClasses.Add(typeReference);

            #region Type function & constant declaration
            sb.AppendLine(TypeFunction(typeReference));
            sb.AppendLine(TypeConstant(typeReference));
            #endregion

            #region TypeConstructor axioms and special subtyping for parametric types
            if (CountArguments(typeReference) > 0)
                sb.AppendLine(AxiomAllInstanceTypesSubtypeAbstractType(typeReference));

            sb.AppendLine(TypeConstructorAxiom(typeReference));
            #endregion

            #region Subtyping axioms
            ITypeDefinition typeDefinition = typeReference.ResolvedType;
            var superClass = typeDefinition.BaseClasses.SingleOrDefault();

            // superClass is empty for interfaces and the object type
            if (superClass == null || typeDefinition is Dummy)
                superClass = Backend.Types.Instance.PlatformType.SystemObject;

            var superTypes = new List<ITypeReference>();
            superTypes.Add(superClass);
            InstructionTranslator.MentionedClasses.AddRange(superTypes);

            sb.AppendLine(SubtypeIfParentsSubtypeOrIsSameTypeOrAbstractType(typeReference, superTypes));
            sb.AppendLine();
            #endregion
        }
    }
}
