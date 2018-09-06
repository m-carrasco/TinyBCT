using Backend;
using Backend.Analyses;
using Backend.Model;
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Test")]
namespace TinyBCT
{
    public static class Helpers
    {
        internal static ISet<string> methodsTranslated = new HashSet<string>();
        public static bool IsInstructionImplemented(Instruction inst)
        {
            if (inst is MethodCallInstruction ||
                inst is LoadInstruction ||
                inst is UnconditionalBranchInstruction ||
                inst is BinaryInstruction ||
                inst is NopInstruction ||
                inst is ReturnInstruction ||
                inst is ConditionalBranchInstruction ||
                inst is CreateObjectInstruction ||
                inst is StoreInstruction ||
                inst is FinallyInstruction ||
                inst is TryInstruction || 
                inst is CatchInstruction ||
                inst is ThrowInstruction ||
                inst is ConvertInstruction ||
                inst is InitializeObjectInstruction ||
                inst is CreateArrayInstruction ||
                inst is UnaryInstruction ||
                inst is SwitchInstruction ||
                inst is LoadTokenInstruction)
                return true;

            return false;
        }

        public static uint SizeOf(PrimitiveTypeCode type)
        {
            uint elementTypeBytes = 0;
            switch (type)
            {
                case PrimitiveTypeCode.Boolean:
                    elementTypeBytes = 1;
                    break;
                case PrimitiveTypeCode.Char:
                    elementTypeBytes = 2;
                    break;
                case PrimitiveTypeCode.Float32:
                    elementTypeBytes = 4;
                    break;
                case PrimitiveTypeCode.Float64:
                    elementTypeBytes = 8;
                    break;
                case PrimitiveTypeCode.Int32:
                case PrimitiveTypeCode.UInt32:
                    elementTypeBytes = 4;
                    break;
                default:
                    // complete on demand
                    // i guess there are some primitive types that do not have a fixed size
                    // pointers may depend of architecture
                    Contract.Assert(false);
                    break;
            }
            return elementTypeBytes;
        }

        public class BoogieType {
            private BoogieType(ConstValue c)
            {
                Const = c;
            }
            public static readonly BoogieType Int = new BoogieType(ConstValue.Int);
            public static readonly BoogieType Bool = new BoogieType(ConstValue.Bool);
            public static readonly BoogieType Real = new BoogieType(ConstValue.Real);
            public static readonly BoogieType Object = new BoogieType(ConstValue.Object);
            public static readonly BoogieType Ref = new BoogieType(ConstValue.Ref);
            public static readonly BoogieType Addr = new BoogieType(ConstValue.Addr);
            public static readonly BoogieType Union = new BoogieType(ConstValue.Union);
            public static readonly BoogieType Void = new BoogieType(ConstValue.Void);
            private ConstValue Const { get; }
            private enum ConstValue { Int, Bool, Real, Object, Ref, Addr, Union, Void };
            public string FirstUppercase()
            {
                return Const.ToString();
            }
            public override string ToString()
            {
                if (Const == ConstValue.Int)
                    return "int";
                if (Const == ConstValue.Real)
                    return "real";
                if (Const == ConstValue.Bool)
                    return "bool";
                return Const.ToString();
            }
        };
        public static BoogieType GetBoogieType(ITypeReference type)
        {
            INamedTypeDefinition namedType = type as INamedTypeDefinition;
            if (namedType != null && type.IsEnum)
                return GetBoogieType(namedType.UnderlyingType);

            if (TypeHelper.IsPrimitiveInteger(type) || type.TypeCode.Equals(PrimitiveTypeCode.Char)  /*|| type.TypeCode.Equals(PrimitiveTypeCode.UIntPtr)*/)
                return BoogieType.Int;

            // not sure about this
            // appeared when accessing anArray.Length
            if (type.TypeCode.Equals(PrimitiveTypeCode.UIntPtr))
                return BoogieType.Int;

            if (type.TypeCode.Equals(PrimitiveTypeCode.Boolean))
                return BoogieType.Bool;

            if (type.TypeCode.Equals(PrimitiveTypeCode.Float32))
                return BoogieType.Real;

            if (type.TypeCode.Equals(PrimitiveTypeCode.Float64))
                return BoogieType.Real;

            if (type.TypeCode.Equals(PrimitiveTypeCode.String))
                return Settings.NewAddrModelling ? BoogieType.Object : BoogieType.Ref;

            if (type.TypeCode.Equals(PrimitiveTypeCode.NotPrimitive))
                return Settings.NewAddrModelling ? BoogieType.Object : BoogieType.Ref;

            if (type.TypeCode.Equals(PrimitiveTypeCode.Reference) && Settings.NewAddrModelling)
                return BoogieType.Addr;

            if (type.TypeCode.Equals(PrimitiveTypeCode.Reference) && !Settings.NewAddrModelling)
            {
                // manuel: we type a reference accordingly to its target type
                // we just support reference writes/reads for arguments
                // void foo (ref int x) -> writes/reads to x are supported
                // {
                // ....
                // ref int y = ...; -> writes/reads to y are not supported
                // ...
                // }
                // this is how bct works.
                // x is considered as a variable with the target type and then is returned and assigned after the invokation
                // call arg1 := foo(arg1);

                IManagedPointerType t = type as IManagedPointerType;
                Contract.Assume(t != null);
                return GetBoogieType(t.TargetType);
            }

            // void type will return null
            if (Types.Instance.PlatformType.SystemVoid.Equals(type))
                return BoogieType.Void;

            Contract.Assert(false);
            throw new Exception("Invalid program state reached");
        }
        public static IMethodReference GetUnspecializedVersion(IMethodReference method)
        {
            return MemberHelper.UninstantiateAndUnspecialize(method);
            //var specializedMethodReference = (method as ISpecializedMethodReference);
            //if (specializedMethodReference != null)
            //{
            //    return (method as ISpecializedMethodReference).UnspecializedVersion;
            //}
            //return method;
        }

        public static Helpers.BoogieType GetMethodBoogieReturnType(IMethodReference methodDefinition)
        {
            return GetBoogieType(methodDefinition.Type);
        }

        public static String GetExternalMethodDefinition(IMethodReference methodRef)
        {
            var methodName = BoogieMethod.From(methodRef).Name;
            if (Helpers.IsCurrentlyMissing(methodRef))
            {
                // TODO(rcastano): Add logger. Print this as INFO or WARNING level.
                Console.WriteLine("WARNING: Creating non-deterministic definition for missing method: " + methodName);
            }
            var parameters = Helpers.GetParametersWithBoogieType(methodRef);
            var returnType = String.Empty;

            // manuel: i can't check for p.IsOut with a method reference, i need a method definition
            // i think that is not possible if it is extern.
            if (Helpers.GetMethodBoogieReturnType(methodRef).Equals(Helpers.BoogieType.Void) && !Settings.NewAddrModelling && !methodRef.Parameters.Any(p => p.IsByReference))
            {
                returnType = String.Empty;
            } else
            {
                var returnVariables = new List<String>();
                returnVariables = methodRef.Parameters.Where(p => p.IsByReference && !Settings.NewAddrModelling).Select(p => String.Format("v{0}$out : {1}", p.Index, Helpers.GetBoogieType(p.Type))).ToList();
                if (!Helpers.GetMethodBoogieReturnType(methodRef).Equals(Helpers.BoogieType.Void))
                    returnVariables.Add(String.Format("$result : {0}", Helpers.GetMethodBoogieReturnType(methodRef)));

                returnType = String.Format("returns ({0})", String.Join(",", returnVariables));
            }

            var t = new BoogieProcedureTemplate(methodName, " {:extern} ", String.Empty, String.Empty, parameters, returnType, true);

            return t.TransformText();
        }

        public class SubtypeComparer : IComparer<IMethodReference>
        {
            public int Compare(IMethodReference a, IMethodReference b)
            {
                if (IsSubtypeOrImplements(a.ContainingType, b.ContainingType))
                {
                    return 0;
                }
                else
                    return 1;
            }
        }
        public static bool IsSubtypeOrImplements(ITypeReference t1, ITypeReference t2)
        {
            return (Type1DerivesFromOrIsTheSameAsType2ForGenerics(t1.ResolvedType, t2.ResolvedType)) 
                || Type1ImplementsType2ForGenerics(t1.ResolvedType,t2);
        }
        public static bool Type1DerivesFromOrIsTheSameAsType2ForGenerics(ITypeReference t1, ITypeReference t2)
        {
            // TODO: Check if generics
            var resolvedT1 = TypeHelper.UninstantiateAndUnspecialize(t1).ResolvedType;
            var unspecializedT2 = TypeHelper.UninstantiateAndUnspecialize(t2);
            if (TypeHelper.TypesAreEquivalent(resolvedT1, unspecializedT2))
                return true;
            //if(resolvedT1.InternedKey == unspecializedT2.InternedKey)
            //    return true;
            foreach (var b in resolvedT1.BaseClasses)
            {
                if (Type1DerivesFromOrIsTheSameAsType2ForGenerics(b, t2))
                    return true;
            }
            return false;
        }

        public static bool Type1ImplementsType2ForGenerics(ITypeReference t1, ITypeReference t2)
        {
            // TODO: Check if generics
            var resolvedT1 = TypeHelper.UninstantiateAndUnspecialize(t1).ResolvedType;
            var unspecializedT2 = TypeHelper.UninstantiateAndUnspecialize(t2);

            foreach (var i in resolvedT1.Interfaces)
            {
                var unspecializedInterface = TypeHelper.UninstantiateAndUnspecialize(i);
                if (unspecializedInterface.InternedKey == unspecializedT2.InternedKey)
                    return true;
                if (Type1ImplementsType2ForGenerics(i.ResolvedType, unspecializedT2))
                    return true;
            }
            foreach (var b in resolvedT1.BaseClasses)
            {
                if (Type1ImplementsType2ForGenerics(b.ResolvedType, unspecializedT2))
                    return true;
            }
            return false;
        }

        public static IList<IMethodReference> PotentialCalleesUsingCHA(IMethodReference unsolvedCallee, IVariable receiver,  
                                                                       MethodCallOperation operation,  ClassHierarchyAnalysis CHA)
        {
            var result = new List<IMethodReference>();
            // var unsolvedCallee = invocation.Method;
            if(unsolvedCallee.Name.Value.Contains("GetEnumerator"))
            {

            }
            switch(operation)
            {
                case MethodCallOperation.Static:
                    result.Add(unsolvedCallee);
                    break;
                case MethodCallOperation.Virtual:
                    //var receiver = invocation.Arguments[0];
                    var type = (receiver.Type is IGenericTypeInstanceReference) ? (receiver.Type as IGenericTypeInstanceReference).GenericType : receiver.Type;
                    if(type is IManagedPointerTypeReference)
                    {
                        type = (type as IManagedPointerTypeReference).TargetType;
                        type = TypeHelper.UninstantiateAndUnspecialize(type);
                        //type = (type is IGenericTypeInstanceReference) ? (type as IGenericTypeInstanceReference).GenericType : type;

                    }
                    var calleeTypes = new List<ITypeReference>(CHA.GetAllSubtypes(type));
                    //// Hack to get the type from CollectionStubs 
                    //var typeCHA = CHA.Types.SingleOrDefault(t => t.ToString() == type.ToString());
                    //if (typeCHA != null)
                    //    type = typeCHA;

                    calleeTypes.Add(type);
                    var candidateCalless = calleeTypes.Select(t => t.FindMethodImplementationForGenerics(unsolvedCallee)).Where(t => t!=null);
                    var candidateCalless2 = calleeTypes.Select(t => t.FindMethodImplementation(unsolvedCallee)).Where(t => t != null);
                    candidateCalless = candidateCalless.Union(candidateCalless2);
                    foreach (var candidate in candidateCalless) // improved this
                    {
                        if (!result.Contains(candidate)) 
                            result.Add(candidate);
                    }
                    //result.AddRange(candidateCalless);
                    break;
            }

            result.Sort(new SubtypeComparer()); // improved this
            return result;
        }


        public static IMethodReference FindMethodImplementation(this ITypeReference receiverType, IMethodReference method)
        {
            var originalReceiverType = receiverType;
            IMethodReference result = null;
            if(method.ToString().Contains("Current"))
            {

            }
            if (method.ToString().Contains("GetEnumerator"))
            {

            }

            while (receiverType != null && IsSubtypeOrImplements(receiverType, method.ContainingType ))
            {
                var receiverTypeDef = receiverType.ResolvedType;
                if (receiverTypeDef == null) break;

                //                var matchingMethod = receiverTypeDef.Methods.SingleOrDefault(m => MemberHelper.GetMemberSignature(m,NameFormattingOptions.PreserveSpecialNames)
                //                                                                                .EndsWith(MemberHelper.GetMemberSignature(method,NameFormattingOptions.PreserveSpecialNames)));
                var matchingMethods = receiverTypeDef.Methods.Where(m => m.Name.Value==method.Name.Value
                                                                                  && ParametersAreCompatible(m,method));

                if(matchingMethods.Count()>1)
                {
                    matchingMethods = receiverTypeDef.Methods.Where(m => m.Name.UniqueKey== method.Name.UniqueKey
                                                                                                      && MemberHelper.SignaturesAreEqual(m, method));
                }

                var matchingMethod = matchingMethods.SingleOrDefault();

                if (matchingMethod != null)
                {
                    result = matchingMethod;
                    return result;
                }
                else
                {
                    receiverType = receiverTypeDef.BaseClasses.SingleOrDefault();
                }

            }
            if(result==null)
            {

            }

            return result;
        }
        public static IMethodReference FindMethodImplementationForGenerics(this ITypeReference receiverType, IMethodReference method)
        {
            var originalReceiverType = receiverType;
            IMethodReference result = null;
            if (method.ToString().Contains("Current"))
            {

            }
            if (method.ToString().Contains("GetEnumerator"))
            {

            }

            while (receiverType != null && IsSubtypeOrImplements(receiverType, method.ContainingType))
            {
                var receiverTypeDef = receiverType.ResolvedType;
                if (receiverTypeDef == null) break;

                //var matchingMethod = receiverTypeDef.Methods.SingleOrDefault(m => m.Name.UniqueKey == method.Name.UniqueKey && MemberHelper.SignaturesAreEqual(m, method));
                var unspecializedMethod = Helpers.GetUnspecializedVersion(method);
                var matchingMethods = receiverTypeDef.Methods.Where(m => m.Name.Value == unspecializedMethod.Name.Value && ParametersAreCompatible(m, unspecializedMethod));

                if(matchingMethods.Count()>1)
                {
                    matchingMethods = receiverTypeDef.Methods.Where(m => m.Name.UniqueKey == unspecializedMethod.Name.UniqueKey && MemberHelper.SignaturesAreEqual(m, unspecializedMethod));
                }

                var matchingMethod = matchingMethods.SingleOrDefault();

                if (matchingMethod != null)
                {
                    result = matchingMethod;
                    return result;
                }
                else
                {
                    receiverType = receiverTypeDef.BaseClasses.SingleOrDefault();
                }

            }
            if (result == null)
            {

            }

            return result;
        }


        public static bool ParametersAreCompatible(IMethodReference m1, IMethodReference m2)
        {
            m1 = GetUnspecializedVersion(m1);

            m2 = GetUnspecializedVersion(m2);
            if (m1.ParameterCount != m2.ParameterCount)
                return false;
            for(var i=0; i<m1.ParameterCount; i++)
            {
                var m1Pi = TypeHelper.UninstantiateAndUnspecialize(m1.Parameters.ElementAt(i).Type);
                var m2Pi = TypeHelper.UninstantiateAndUnspecialize(m2.Parameters.ElementAt(i).Type);
                if (!TypeEquals(m1Pi, m2Pi) && !Type1DerivesFromOrIsTheSameAsType2ForGenerics(m1Pi, m2Pi))
                    return false;

            }
            return true;
        }

        public static bool TypeEquals(this ITypeReference type1, ITypeReference type2)
        {
            return TypeHelper.TypesAreEquivalent(type1, type2);
        }


        /*
        public static String GetMethodDefinition(IMethodReference methodRef, bool IsExtern)
        {
            var methodName = Helpers.GetMethodName(methodRef);
            var arguments = Helpers.GetParametersWithBoogieType(methodRef);
            var returnType = Helpers.GetMethodBoogieReturnType(methodRef);

            var head = String.Empty;

            if (methodRef.Type.TypeCode != PrimitiveTypeCode.Void)
                head = String.Format("procedure {5} {0} {1}({2}) returns (r : {3}){4}", 
                            IsExtern ? " {:extern}" : String.Empty, 
                            methodName, 
                            arguments, 
                            returnType, 
                            IsExtern ? ";" : String.Empty, 
                            IsMain(methodRef) ? " {:entrypoint}" : String.Empty);
            else
                head = String.Format("procedure {4} {0}  {1}({2}){3}", IsExtern ? " {:extern}" : String.Empty,
                                                                        methodName, 
                                                                        arguments, 
                                                                        IsExtern ? ";" : String.Empty, 
                                                                        IsMain(methodRef) ? " {:entrypoint}" : String.Empty);

            return head;
        }*/

        public static bool IsMain(IMethodReference methodRef)
		{
			if (methodRef.Name == null|| !methodRef.IsStatic) return false;
			return methodRef.Name.Value=="Main";
		}

        // name of procedures should be followed by the C# types of the arguments
        // void foo(int i) should be like foo$int(...)
        // this function returns $int
        public static String GetArityWithNonBoogieTypes(IMethodReference methodRef)
        {
            return String.Join("", GetUnspecializedVersion(methodRef).Parameters.Select(v => "$" + TypeHelper.UninstantiateAndUnspecialize(v.Type)));
        }

        public static String GetParametersWithBoogieType(IMethodReference methodRef)
        {
            var parameters = String.Empty;
            IMethodDefinition methodDef = methodRef as IMethodDefinition;
            if (methodDef != null)
                parameters =  String.Join(",", methodDef.Parameters.Select(v => v.Name + " : " + (Settings.NewAddrModelling && v.IsByReference ? Helpers.BoogieType.Addr : GetBoogieType(v.Type))));
            else
                parameters = String.Join(",", methodRef.Parameters.Select(v => String.Format("param{0}", v.Index) + " : " + (Settings.NewAddrModelling && v.IsByReference ? Helpers.BoogieType.Addr : GetBoogieType(v.Type))));

            if (methodRef.CallingConvention.HasFlag(Microsoft.Cci.CallingConvention.HasThis))
                parameters = String.Format("this : Ref{0}{1}", methodRef.ParameterCount > 0 ? "," : String.Empty, parameters);

            parameters = Strings.NormalizeStringForCorral(parameters);
            return parameters;
        }

        public static bool IsCompiledGeneratedClass(this INamedTypeDefinition typeAsClassResolved)
        {
            var result = typeAsClassResolved != null && typeAsClassResolved.Attributes.Any(attrib => attrib.Type.GetName().Equals("CompilerGeneratedAttribute"));
            return result;
        }

        public static bool IsCompilerGenerated(this ITypeReference type)
        {
            var resolvedClass = type.ResolvedType as INamedTypeDefinition;

            if (resolvedClass != null)
            {
                return resolvedClass.IsCompiledGeneratedClass();
            }
            return false;
        }

        public static Boolean IsCurrentlyMissing(IMethodReference methodReference)
        {
            // The value of this condition can change throughout the execution of the translation.
            // For that reason, it should be called at the end of the translation again to confirm
            // the method is actually missing from the binary.
            return !methodsTranslated.Contains(BoogieMethod.From(methodReference).Name);
        }

        // workaround
        public static Boolean IsExternal(IMethodDefinition methodDefinition)
        {
            if (methodDefinition.IsConstructor)
            {
                var methodName = BoogieMethod.From(methodDefinition).Name;
                if (methodName.Equals("System.Object.#ctor"))
                    return true;
            }

            if (methodDefinition.IsExternal)
                return true;

            return false;
        }

        public static void addTranslatedMethod(IMethodDefinition methodDefinition)
        {
            methodsTranslated.Add(BoogieMethod.From(methodDefinition).Name);
        }
        public static string GetNormalizedTypeFunction(
            ITypeReference originalType, ISet<ITypeReference> mentionedClasses,
            IEnumerable<ITypeReference> typeArguments = null,
            Func<ITypeReference, Boolean> forceRecursion = null)
        {
            if (forceRecursion == null)
            {
                forceRecursion = (t => !(t is IGenericTypeParameter));
            }
            var type = originalType;
            bool callRecursively = typeArguments == null;

            mentionedClasses.Add(type);

            if (type is INamespaceTypeReference || type is INestedTypeReference)
            { 
                type = TypeHelper.GetInstanceOrSpecializedNestedType(type.ResolvedType);
            }

            if (type is IGenericTypeInstanceReference)
            {
                var instanciatedType = type as IGenericTypeInstanceReference;
                if (instanciatedType.GenericArguments.Count() > 0)
                {
                    typeArguments = typeArguments ?? instanciatedType.GenericArguments;
                    Func<ITypeReference, string> f = null;
                    if (callRecursively)
                    {
                        f = (t => Helpers.GetNormalizedTypeFunction(t, mentionedClasses));
                    }
                    else
                    {
                        f = (t => !forceRecursion(t) ? t.GetName() : Helpers.GetNormalizedTypeFunction(t, mentionedClasses));
                    }
                    var typeArgsString = String.Join(",", typeArguments.Select(t => f(t)));
                    foreach (var t in typeArguments)
                    {
                        mentionedClasses.Add(t);
                    }
                    var typeName = GetNormalizedType(instanciatedType.GenericType);
                    return String.Format("T${0}({1})", typeName, typeArgsString);
                }
                else
                {
                    return "T$" + GetNormalizedType(type) + "()";
                }
            } else
            {
                return "T$" + GetNormalizedType(type) + "()";
            }
        }
            public static string GetNormalizedType(ITypeReference type)
        {
            type = TypeHelper.UninstantiateAndUnspecialize(type);
            var result = TypeHelper.GetTypeName(type, NameFormattingOptions.UseGenericTypeNameSuffix | NameFormattingOptions.OmitTypeArguments);
            var namedTypeReference = (type as INamedTypeReference);
            if (namedTypeReference != null)
            {
                result = TypeHelper.GetTypeName(namedTypeReference, NameFormattingOptions.UseGenericTypeNameSuffix);
            }
            // Do this well 
            result = result.Replace('<', '$').Replace('>', '$').Replace(", ", "$"); // for example containing type for delegates
            result = Strings.NormalizeStringForCorral(result);

            return result;
        }

        public static bool IsGenericField(IFieldReference field)
        {
            var containingType = TypeHelper.UninstantiateAndUnspecialize(field.ContainingType).ResolvedType;

            if (containingType.FullName().Equals("Microsoft.Cci.DummyTypeReference"))
            {
                // if we cannot know if this is a generic field
                // we will consider it is and it will be declared with Union type
                return true;
            }
            var potentiallyGenericField = containingType.Fields.Single(f => field.Name == f.Name);
            return potentiallyGenericField.Type is IGenericTypeParameter;
        }

        // this is just a wrapper to know where is it called for delegates
        // Diego wants to change how to group delegates in the future
        // do not call this method if it is not for delegates
        public static string GetNormalizedTypeForDelegates(ITypeReference type)
        {
            return GetNormalizedType(type);
        }

        /// <summary>
        /// Normalize Methdod Definitions taken from original BCT
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string CreateUniqueMethodName(IMethodReference method) {
            // We remove all type instances 
            var unspecializedMethod = GetUnspecializedVersion(method);
            //var containingTypeName = TypeHelper.GetTypeName(method.ContainingType, NameFormattingOptions.None);
            var s = MemberHelper.GetMethodSignature(unspecializedMethod, NameFormattingOptions.DocumentationId);
            s = s.Substring(2);
            s = s.TrimEnd(')');
            s = TurnStringIntoValidIdentifier(s);
            return s;
        }

        public static string TurnStringIntoValidIdentifier(string s)
        {

            // Do this specially just to make the resulting string a little bit more readable.
            // REVIEW: Just let the main replacement take care of it?
            s = s.Replace("[0:,0:]", "2DArray"); // TODO: Do this programmatically to handle arbitrary arity
            s = s.Replace("[0:,0:,0:]", "3DArray");
            s = s.Replace("[0:,0:,0:,0:]", "4DArray");
            s = s.Replace("[0:,0:,0:,0:,0:]", "5DArray");
            s = s.Replace("[]", "array");

            // The definition of a Boogie identifier is from BoogiePL.atg.
            // Just negate that to get which characters should be replaced with a dollar sign.

            // letter = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".
            // digit = "0123456789".
            // special = "'~#$^_.?`".
            // nondigit = letter + special.
            // ident =  [ '\\' ] nondigit {nondigit | digit}.

            s = Regex.Replace(s, "[^A-Za-z0-9'~#$^_.?`]", "$");

            s = GetRidOfSurrogateCharacters(s);
            return s;
        }

        /// <summary>
        /// Unicode surrogates cannot be handled by Boogie.
        /// http://msdn.microsoft.com/en-us/library/dd374069(v=VS.85).aspx
        /// </summary>
        private static string GetRidOfSurrogateCharacters(string s)
        {
            //  TODO this is not enough! Actually Boogie cannot support UTF8
            var cs = s.ToCharArray();
            var okayChars = new char[cs.Length];
            for (int i = 0, j = 0; i < cs.Length; i++)
            {
                if (Char.IsSurrogate(cs[i])) continue;
                okayChars[j++] = cs[i];
            }
            var raw = String.Concat(okayChars);
            return raw.Trim(new char[] { '\0' });
        }

        public static bool IsConstructor(IMethodReference method)
        {
            return method.Name.Value == ".ctor";
        }

        public static Boolean IsBoogieRefType(Helpers.BoogieType type)
        {
            return type.Equals(Helpers.BoogieType.Ref) || type.Equals(Helpers.BoogieType.Object) || type.Equals(Helpers.BoogieType.Union) || type.Equals(Helpers.BoogieType.Addr);
        }
        public static Boolean IsBoogieRefType(ITypeReference r)
        {
            return IsBoogieRefType(Helpers.GetBoogieType(r));
        }

        public static class Strings
        {
            internal static IDictionary<Char, int> specialCharacters = new Dictionary<Char, int>() { { ' ', 0 } };
            internal static readonly Regex illegalBoogieCharactersRegex = new Regex(@"[^a-zA-Z_.$#'`~^\?]");
            public static bool ContainsIllegalCharacters(string s)
            {
                return illegalBoogieCharactersRegex.Match(s).Success;
            }
            public static string ReplaceIllegalChars(string s)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < s.Length; ++i)
                {
                    // Using '#' as escape character.
                    if (s[i] == '#')
                    {
                        sb.Append("##");
                    }
                    else if (!illegalBoogieCharactersRegex.Match(s[i].ToString()).Success)
                    {
                        sb.Append(s[i]);
                    }
                    else
                    {
                        if (!specialCharacters.ContainsKey(s[i]))
                        {
                            specialCharacters.Add(s[i], specialCharacters.Count);
                        }
                        sb.Append("#" + specialCharacters[s[i]].ToString() + "#");
                    }
                }
                return sb.ToString();
            }
            public static string ReplaceSpaces(string s)
            {
                return s.Replace(" ", "#" + specialCharacters[' '].ToString() + "#");
            }
            
            public static string NormalizeStringForCorral(string s)
            {
                return s.Replace("::", ".")// for example: static fields
                    .Replace("<>", "__")  // class compiled generated
                    .Replace('<', '$').Replace('>', '$').Replace(", ", "$").Replace("=", "$").Replace("[]", "$Array$");

                //return s; // .Replace('<', '_').Replace('>', '_');
            }
            
            public static Expression FixStringLiteral(IValue v, BoogieGenerator bg)
            {
                string vStr = v.ToString();
                if (v is Constant cons)
                {
                    return BoogieLiteral.FromString(cons);
                } else if (v is IVariable variable)
                {
                    return bg.ReadAddr(variable);
                } else
                {
                    throw new NotImplementedException();
                }
            }

            // TODO(rcastano): pick better name
            public static string GetBinaryMethod(BinaryOperation op)
            {
                string method = "";
                switch (op)
                {
                    case BinaryOperation.Eq: method = "System.String.op_Equality$System.String$System.String"; break;
                    case BinaryOperation.Neq: method = "System.String.op_Inequality$System.String$System.String"; break;
                    case BinaryOperation.Add: method = "System.String.Concat$System.String$System.String"; break;
                }
                return method;
            }
            
            public static string GetBinaryMethod(BranchOperation op)
            {
                string method = "";
                switch (op)
                {
                    case BranchOperation.Eq: method = "System.String.op_Equality$System.String$System.String"; break;
                    case BranchOperation.Neq: method = "System.String.op_Inequality$System.String$System.String"; break;
                    default:
                        Contract.Assert(false);
                        break;
                }
                return method;
            }
        }
    }
	public static class Extensions
	{
		public static string FullName(this ITypeReference tref)
		{
			return TypeHelper.GetTypeName(tref, NameFormattingOptions.Signature | NameFormattingOptions.TypeParameters);
		}
		public static string GetName(this ITypeReference tref)
		{
			if (tref is INamedTypeReference)
				return (tref as INamedTypeReference).Name.Value;

			return TypeHelper.GetTypeName(tref, NameFormattingOptions.OmitContainingType | NameFormattingOptions.OmitContainingNamespace | NameFormattingOptions.SmartTypeName);
		}
	}
}
