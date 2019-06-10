using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Backend.ThreeAddressCode.Values;

namespace TinyBCT.Translators
{
    public class StringTranslator
    {
        public StatementList CallStringProcedure(BoogieMethod method, IVariable op1, IInmediateValue op2, BoogieVariable boogieVariable)
        {
            Contract.Assert(!(op2 is Constant c) || c.Value == null);

            var arg1 = BoogieGenerator.Instance().ReadAddr(op1);
            Expression arg2 = null;

            if (op2 is Constant constant && constant.Value == null)
            {
                arg2 = BoogieGenerator.Instance().NullObject();
            } else if(op2 is IVariable variable)
            {
                arg2 = BoogieGenerator.Instance().ReadAddr(variable);
            } else
            {
                throw new NotImplementedException();
            }

            var arguments = new List<Expression>() { arg1, arg2 };
            return BoogieGenerator.Instance().ProcedureCall(method, arguments, boogieVariable);
        }

        public StatementList CallAllocLiteral(IVariable result, Constant constant, InstructionTranslator instructionTranslator)
        {
            Contract.Assert(constant.Value is String);

            var statements = new StatementList();
            statements.Add(BoogieGenerator.Instance().AllocObject(result, instructionTranslator));

            if (Settings.Z3Strings)
            {
                var obj = BoogieGenerator.Instance().ReadAddr(result);
                var literal = BoogieLiteral.FromDotNetConstant(constant);
                var function = InvokeObjectToString(obj);
                var equal = Expression.BinaryOperationExpression(function, literal, Backend.ThreeAddressCode.Instructions.BinaryOperation.Eq);
                statements.Add(BoogieStatement.Assume(equal));
            }
            
            return statements;
        }

        public Expression InvokeObjectToString(Expression obj)
        {
            return new Expression(Helpers.BoogieType.StringLiteral, String.Format("ObjectToString({0})", obj.Expr));
        }

        // this is useful because we can prevent them to be defined as extern
        public static ISet<string> GetBoogieNamesForStubs()
        {
            return new HashSet<string>()
            {
                BoogieMethod.StringFormat1.Name,
                BoogieMethod.StringFormat2.Name,
                BoogieMethod.StringEquals.Name,
                BoogieMethod.StringEquality.Name,
                BoogieMethod.StringInequality.Name,
                BoogieMethod.StringConcat.Name
            };
        }

        public static string StringConcatStub()
        {
            string methodName = BoogieMethod.StringConcat.Name;
            string attr = String.Empty;
            StatementList localVariables = new StatementList();
            StatementList instructions = new StatementList();

            if (Settings.Z3Strings)
            {
                localVariables.Add(BoogieStatement.FromString("var obj : Object;"));
                localVariables.Add(BoogieStatement.FromString("var val : string;"));

                instructions.Add(BoogieStatement.FromString("val := concat(ObjectToString(a$in), ObjectToString(b$in));"));
                instructions.Add(BoogieStatement.FromString("call obj := Alloc();"));
                instructions.Add(BoogieStatement.FromString("assume ObjectToString(obj) == val;"));
                instructions.Add(BoogieStatement.FromString("$result:= obj;"));
            }

            string parameterTypes = "a$in: Object, b$in: Object";
            string returnTypes = "returns ($result: Object)";
            bool isExtern = false;

            BoogieProcedureTemplate procedure =
                new BoogieProcedureTemplate(methodName, attr, localVariables, instructions, parameterTypes, returnTypes, isExtern);

            return procedure.TransformText();
        }

        public static string StringEqualsStub()
        {
            string methodName = BoogieMethod.StringEquals.Name;
            string attr = String.Empty;
            StatementList localVariables = new StatementList();
            StatementList instructions = new StatementList();

            if (Settings.Z3Strings)
            {
                instructions.Add(BoogieStatement.FromString("if (a$in != null && b$in == null)"));
                instructions.Add(BoogieStatement.FromString("{"));
                instructions.Add(BoogieStatement.FromString("  $result:= false;"));
                instructions.Add(BoogieStatement.FromString("} else"));
                instructions.Add(BoogieStatement.FromString("{"));
                instructions.Add(BoogieStatement.FromString("  $result:= (ObjectToString(a$in) == ObjectToString(b$in));"));
                instructions.Add(BoogieStatement.FromString("}"));
            }

            string parameterTypes = "a$in: Object, b$in: Object";
            string returnTypes = "returns ($result: bool)";
            bool isExtern = false;

            BoogieProcedureTemplate procedure =
                new BoogieProcedureTemplate(methodName, attr, localVariables, instructions, parameterTypes, returnTypes, isExtern);

            return procedure.TransformText();
        }

        public static string StringOpEqualityStub()
        {
            string methodName = BoogieMethod.StringEquality.Name;
            string attr = String.Empty;
            StatementList localVariables = new StatementList();
            StatementList instructions = new StatementList();

            if (Settings.Z3Strings)
            {
                instructions.Add(BoogieStatement.FromString("if (a$in != null && b$in == null)"));
                instructions.Add(BoogieStatement.FromString("{"));
                instructions.Add(BoogieStatement.FromString("  $result:= false;"));
                instructions.Add(BoogieStatement.FromString("} else"));
                instructions.Add(BoogieStatement.FromString("{"));
                instructions.Add(BoogieStatement.FromString("  $result:= (ObjectToString(a$in) == ObjectToString(b$in));"));
                instructions.Add(BoogieStatement.FromString("}"));
            }

            string parameterTypes = "a$in: Object, b$in: Object";
            string returnTypes = "returns ($result: bool)";
            bool isExtern = false;

            BoogieProcedureTemplate procedure =
                new BoogieProcedureTemplate(methodName, attr, localVariables, instructions, parameterTypes, returnTypes, isExtern);

            return procedure.TransformText();
        }

        public static string StringOpInequalityStub()
        {
            string methodName = BoogieMethod.StringInequality.Name;
            string attr = String.Empty;
            StatementList localVariables = new StatementList();
            StatementList instructions = new StatementList();

            if (Settings.Z3Strings)
            {
                instructions.Add(BoogieStatement.FromString("if (a$in != null && b$in == null)"));
                instructions.Add(BoogieStatement.FromString("{"));
                instructions.Add(BoogieStatement.FromString("  $result:= true;"));
                instructions.Add(BoogieStatement.FromString("} else"));
                instructions.Add(BoogieStatement.FromString("{"));
                instructions.Add(BoogieStatement.FromString("  $result:= (ObjectToString(a$in) != ObjectToString(b$in));"));
                instructions.Add(BoogieStatement.FromString("}"));
            }

            string parameterTypes = "a$in: Object, b$in: Object";
            string returnTypes = "returns ($result: bool)";
            bool isExtern = false;

            BoogieProcedureTemplate procedure =
                new BoogieProcedureTemplate(methodName, attr, localVariables, instructions, parameterTypes, returnTypes, isExtern);

            return procedure.TransformText();
        }

        public static string StringFormat1()
        {
            string methodName = BoogieMethod.StringFormat1.Name;
            string attr = String.Empty;
            StatementList localVariables = new StatementList();
            StatementList instructions = new StatementList();

            if (Settings.Z3Strings)
            {
                localVariables.Add(BoogieStatement.FromString("var res : string;"));
                localVariables.Add(BoogieStatement.FromString("var obj : Object;"));

                instructions.Add(BoogieStatement.FromString("res := replace(ObjectToString(param0), \"{0}\", ObjectToString(param1));"));
                instructions.Add(BoogieStatement.FromString("call obj := Alloc();"));
                instructions.Add(BoogieStatement.FromString("assume ObjectToString(obj) == res;"));
                instructions.Add(BoogieStatement.FromString("$result := obj;"));
            }

            string parameterTypes = "param0 : Object,param1 : Object";
            string returnTypes = "returns ($result : Object)";
            bool isExtern = false;

            BoogieProcedureTemplate procedure =
                new BoogieProcedureTemplate(methodName, attr, localVariables, instructions, parameterTypes, returnTypes, isExtern);

            return procedure.TransformText();
        }

        public static string StringFormat2()
        {
            string methodName = BoogieMethod.StringFormat2.Name;
            string attr = String.Empty;
            StatementList localVariables = new StatementList();
            StatementList instructions = new StatementList();

            if (Settings.Z3Strings)
            {
                localVariables.Add(BoogieStatement.FromString("var res : string;"));
                localVariables.Add(BoogieStatement.FromString("var obj : Object;"));

                instructions.Add(BoogieStatement.FromString("res := replace(ObjectToString(param0), \"{0}\", ObjectToString(param1));"));
                instructions.Add(BoogieStatement.FromString("res:= replace(res, \"{1}\", ObjectToString(param2));"));
                instructions.Add(BoogieStatement.FromString("call obj := Alloc();"));
                instructions.Add(BoogieStatement.FromString("assume ObjectToString(obj) == res;"));
                instructions.Add(BoogieStatement.FromString("$result := obj;"));
            }

            string parameterTypes = "param0 : Object,param1 : Object,param2 : Object";
            string returnTypes = "returns ($result : Object)";
            bool isExtern = false;

            BoogieProcedureTemplate procedure =
                new BoogieProcedureTemplate(methodName, attr, localVariables, instructions, parameterTypes, returnTypes, isExtern);

            return procedure.TransformText();
        }

        public static string StringFunctions()
        {
            if (Settings.Z3Strings)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("function ObjectToString(obj: Object) : string;");
                stringBuilder.AppendLine("function {:builtin \"str.++\"} concat(string, string): string;");
                stringBuilder.AppendLine("function {:builtin \"str.replace\"} replace(string, string, string): string;");
                return stringBuilder.ToString();
            } else
                return string.Empty;

        }
    }
}
