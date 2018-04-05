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
            var diff = parents.Except(classes);

            foreach (var c in diff)
            {
                var typeName = Helpers.GetNormalizedType(c);

                // already in prelude
                if (typeName.Equals("T$System.Object()"))
                    continue;

                var superClass = c.BaseClasses.SingleOrDefault();
                sb.AppendLine(String.Format("function T${0}() : Ref;", typeName));
                sb.AppendLine(String.Format("const unique T${0} : int;", typeName));
                sb.AppendLine(String.Format("axiom $TypeConstructor(T${0}()) == T${0};", typeName));

                sb.AppendLine("axiom(forall $T: Ref:: { " + String.Format(" $Subtype(T${0}()", typeName) +
                    ", $T) } $Subtype(T$" + string.Format("{0}(), $T) <==> T${0}() == $T || $Subtype({1}, $T));", typeName, "T$System.Object()"));
            }

            StreamWriter streamWriter = Program.streamWriter;
            streamWriter.WriteLine(sb);
        }

        public string Translate()
        {
            StringBuilder sb = new StringBuilder();
            var typeName = Helpers.GetNormalizedType(typeDef);
            var superClass = typeDef.BaseClasses.SingleOrDefault();
            sb.AppendLine(String.Format("function T${0}() : Ref;", typeName));
            sb.AppendLine(String.Format("const unique T${0} : int;", typeName));
            sb.AppendLine(String.Format("axiom $TypeConstructor(T${0}()) == T${0};", typeName));
            if (superClass != null)
            {
                sb.AppendLine("axiom(forall $T: Ref:: { " + String.Format(" $Subtype(T${0}()", typeName) +
                    ", $T) } $Subtype(T$" + string.Format("{0}(), $T) <==> T${0}() == $T || $Subtype(T${1}(), $T));", typeName, Helpers.GetNormalizedType(superClass)));

               parents.Add(superClass.ResolvedType);
            }

            // todo: improve this piece of code
            //StreamWriter streamWriter = Program.streamWriter;
            //streamWriter.WriteLine(sb);

            classes.Add(typeDef);
            return sb.ToString();
        }
    }
}
