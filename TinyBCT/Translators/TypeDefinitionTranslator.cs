using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT.Translators
{
    class TypeDefinitionTranslator
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
        public static void TypeDefinitionTranslatorTraverse(INamedTypeDefinition typeDef)
        {
            TypeDefinitionTranslator t = new TypeDefinitionTranslator(typeDef);
            // todo: improve this piece of code
            StreamWriter streamWriter = Program.streamWriter;
            streamWriter.WriteLine(t.Translate());
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


                GenerateTypeDefinition(sb, c.ResolvedType, typeName);

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
            GenerateTypeDefinition(sb, typeDef, typeName);

            // todo: improve this piece of code
            //StreamWriter streamWriter = Program.streamWriter;
            //streamWriter.WriteLine(sb);

            classes.Add(typeDef);
            return sb.ToString();
        }

        public static void GenerateTypeDefinition(StringBuilder sb, ITypeDefinition typeDefinition, string typeName)
        {

            var superClass = typeDefinition.BaseClasses.SingleOrDefault();

            var argsString = String.Empty;
            typeDefinition = TypeHelper.UninstantiateAndUnspecialize(typeDefinition).ResolvedType;
            if (typeDefinition is INamespaceTypeReference || typeDefinition is INestedTypeReference || typeDefinition is IGenericTypeInstance)
            {
                typeDefinition = TypeHelper.GetInstanceOrSpecializedNestedType(typeDefinition.ResolvedType);
            }

            if (typeDefinition is IGenericTypeInstanceReference)
            {
                var instanciatedType = typeDefinition as IGenericTypeInstanceReference;
                var typeArgs = instanciatedType.GenericArguments;
                argsString = String.Join(",", typeArgs.Select(t => t.ToString() + " : Ref"));
            }
            
            sb.AppendLine(String.Format("function T${0}({1}) : Ref;", typeName, argsString));
            sb.AppendLine(String.Format("const unique T${0} : int;", typeName));
            
            if (superClass == null)
            {
                superClass = Backend.Types.Instance.PlatformType.SystemObject;
            }

            argsString = "";
            IEnumerable<ITypeReference> typeArguments = null;
            if (typeDefinition is IGenericTypeInstanceReference)
            {
                var instanciatedType = typeDefinition as IGenericTypeInstanceReference;
                typeArguments = instanciatedType.GenericArguments;
                argsString = String.Join(",", typeArguments.Select(t => t.ToString() + " : Ref")) + ", ";
                var callWithQuantifiedVars =
                Helpers.GetNormalizedTypeFunction(typeDefinition, InstructionTranslator.MentionedClasses, typeArguments);
                var callWithGenericsTypes = Helpers.GetNormalizedTypeFunction(typeDefinition, InstructionTranslator.MentionedClasses);
                /// subtype(generic($T), generic(T$T()) 
                sb.AppendLine(
                String.Format("axiom(forall {0} :: {{  $Subtype({1}, {2}) }} $Subtype({1}, {2}) );", argsString.Substring(0, argsString.Length - 2), callWithQuantifiedVars, callWithGenericsTypes));
                sb.AppendLine(String.Format("axiom $TypeConstructor({0}) == T${1};", callWithGenericsTypes, typeName));

            }
            else
            {
                sb.AppendLine(String.Format("axiom $TypeConstructor(T${0}()) == T${0};", typeName));
            }
            var funcCall = Helpers.GetNormalizedTypeFunction(typeDefinition, InstructionTranslator.MentionedClasses, typeArguments: typeArguments);

            var superClassFuncCall = Helpers.GetNormalizedTypeFunction(superClass, InstructionTranslator.MentionedClasses, typeArguments);
            sb.AppendLine(
                String.Format("axiom(forall {0} $T: Ref:: {{  $Subtype({1}, $T) }} ", argsString, funcCall) +
                String.Format("$Subtype({0}, $T) <==> {0} == $T || $Subtype({1}, $T));", funcCall, superClassFuncCall));

            parents.Add(superClass.ResolvedType);
            
            sb.AppendLine();
        }
    }
}
