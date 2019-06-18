using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Cci;

namespace TinyBCT
{
    public class AsyncStubs
    {
        public AsyncStubs(ISet<Assembly> inputAssemblies)
        {
            this.stateMachinesTypes = inputAssemblies.GetAllDefinedTypes().Where(t => t.Interfaces.Any( i => i.FullName().Contains("System.Runtime.CompilerServices.IAsyncStateMachine")));
        }

        public string AsyncMethodBuilderStartStub()
        {
            StatementList localVars = new StatementList();
            StatementList instructions = new StatementList();
            var param0 = new BoogieVariable(Helpers.BoogieType.Addr, "param0");
            var moveNextMethods = stateMachinesTypes.Select(t => t.Members.Where(m => m.Name.Value.Contains("MoveNext")).First()).Cast<IMethodDefinition>();
            var ifCases = moveNextMethods.Select(m => Invoke(m, param0));

            foreach (var ifCase in ifCases)
                instructions.Add(ifCase);

            var procedureTemplate = new BoogieProcedureTemplate("System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start``1$``0$", "", localVars, instructions, "this : Ref,param0: Addr", String.Empty, false);
            return procedureTemplate.TransformText();
        }

        public string AsyncStubsScheduleTask()
        {
            StatementList localVars = new StatementList();
            localVars.Add(BoogieStatement.FromString("var state : bool;"));

            StatementList instructions = new StatementList();
            var param0 = new BoogieVariable(Helpers.BoogieType.Object, "sm");


            var procIsCompleted = BoogieMethod.AsyncStubsTaskAwaiterIsCompleted;
            var awaiterVar = new BoogieVariable(Helpers.BoogieType.Object, "awaiter");
            var stateVar = new BoogieVariable(Helpers.BoogieType.Bool, "state");
            var argsList = new List<Expression>();
            argsList.Add(awaiterVar);
            var resList = new List<BoogieVariable>();
            resList.Add(stateVar);
            var callIsCompleted = BoogieStatement.ProcedureCall(procIsCompleted, argsList, resList, stateVar);

            var assume = BoogieStatement.Assume(Expression.BinaryOperationExpression(stateVar, new Expression(Helpers.BoogieType.Bool, "true"), Backend.ThreeAddressCode.Instructions.BinaryOperation.Eq));

            var yield = BoogieStatement.FromString("yield;");

            instructions.Add(callIsCompleted);
            instructions.Add(assume);
            instructions.Add(yield);

            var moveNextMethods = stateMachinesTypes.Select(t => t.Members.Where(m => m.Name.Value.Contains("MoveNext")).First()).Cast<IMethodDefinition>();
            var ifCases = moveNextMethods.Select(m => Invoke(m, param0));

            foreach (var ifCase in ifCases)
                instructions.Add(ifCase);

            var procedureTemplate = new BoogieProcedureTemplate("$AsyncStubs$ScheduleTask", "", localVars, instructions, "awaiter : Object, sm : Object", String.Empty, false);
            return procedureTemplate.TransformText();
        }

        private StatementList Invoke(IMethodDefinition member, BoogieVariable receiver)
        {
            Expression receiverObject = receiver;
            if (receiver.Type.Equals(Helpers.BoogieType.Addr))
            {
                AddressExpression addrExpr = new AddressExpression(member.ContainingType, receiver);
                receiverObject = BoogieGenerator.Instance().ReadAddr(addrExpr);
            }

            Expression subtype = Expression.Subtype(receiverObject, member.ContainingType);

            StatementList body = new StatementList();
            List<Expression> argumentList = new List<Expression>();
            argumentList.Add(receiverObject);
            body.Add(BoogieGenerator.Instance().ProcedureCall(BoogieMethod.From(member), argumentList));
            body.Add(BoogieStatement.ReturnStatement);
            var ifExpr = BoogieStatement.If(subtype, body);

            return ifExpr;
        }

        // compiler generated state machine types implementing System.Runtime.CompilerServices.IAsyncStateMachine
        IEnumerable<ITypeDefinition> stateMachinesTypes;
    }
}
