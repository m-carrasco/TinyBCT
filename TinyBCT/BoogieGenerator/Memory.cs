using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Microsoft.Cci;
using TinyBCT.Translators;

namespace TinyBCT.Memory
{
    public interface IMemory
    {
        // out of the scope

        //Helpers.BoogieType GetBoogieTypeForProcedureParameter(IParameterTypeInformation parameter);
        //string GetFieldDefinition(IFieldReference fieldReference, string fieldName);
        //BoogieVariable GetProcedureResultVariable(MethodCallInstruction methodCallInstruction, InstructionTranslator instructionTranslator);
        //StatementList SetProcedureResultVariable(BoogieVariable procedureResult, IVariable finalVariable);
        //StatementList ProcedureCall(IMethodReference procedure, MethodCallInstruction methodCallInstruction, InstructionTranslator instTranslator, BoogieVariable resultVariable = null);

        // ******

        /*public Expression ReadAddr(IVariable addr)
        {
            return ReadAddr(AddressOf(addr));
        }*/

        /*public Addressable AddressOf(IValue value)
        {
            if (value is IReferenceable)
                return AddressOf(value as IReferenceable);
            else if (value is Reference)
                return AddressOf(value as Reference);
            else
                throw new NotImplementedException();

            // we are covering AddressOf for the following types
            // Reference, Dereference, IVariable, InstanceFieldAccess, StaticFieldAccess and ArrayElementAccess
        }*/

        /*public Addressable AddressOf(IReferenceable value)
        {
            if (value is InstanceFieldAccess)
            {
                return AddressOf(value as InstanceFieldAccess);
            }
            else if (value is StaticFieldAccess)
            {
                return AddressOf(value as StaticFieldAccess);
            }
            else if (value is IVariable)
            {
                return AddressOf(value as IVariable);
            }
            else if (value is ArrayElementAccess)
            {
                throw new NotImplementedException();
            }
            else if (value is Dereference dereference)
                return AddressOf(dereference.Reference);

            // I should have covered all possible cases
            throw new NotImplementedException();
        }*/

        /*public StatementList WriteStaticField(StaticFieldAccess staticFieldAccess, Expression expr)
        {
            return WriteAddr(AddressOf(staticFieldAccess), expr);
        }*/
        /*public StatementList WriteStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            return WriteStaticField(staticFieldAccess, ReadAddr(value));
        }*/
        /* StatementList ReadStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            StatementList stmts = new StatementList();

            var address = AddressOf(staticFieldAccess);

            stmts.Add(VariableAssignment(value, ReadAddr(address)));

            return stmts;
        }*/
        /*public StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value, InstructionTranslator instTranslator)
        {
            return WriteInstanceField(instanceFieldAccess, ReadAddr(value), instTranslator);
        }*/

        /*public StatementList CallReadArrayElement(BoogieVariable result, Expression array, Expression index)
        {
            var l = new List<Expression>();
            l.Add(array);
            l.Add(index);
            return ProcedureCall(BoogieMethod.ReadArrayElement, l, result);
        }*/

        /*StatementList CallWriteArrayElement(Expression array, Expression index, Expression value)
        {
            var l = new List<Expression>();
            l.Add(array);
            l.Add(index);
            l.Add(value);
            return ProcedureCall(BoogieMethod.WriteArrayElement, l);
        }*/

        //Addressable AddressOf(Reference reference) { return AddressOf(reference.Value); }

        Addressable AddressOf(IReferenceable value);//na
        Addressable AddressOf(IValue value);// na
        Addressable AddressOf(InstanceFieldAccess instanceFieldAccess);
        Addressable AddressOf(StaticFieldAccess staticFieldAccess);
        Addressable AddressOf(IVariable var);

        StatementList AllocAddr(BoogieVariable var);
        StatementList AllocAddr(IVariable var);
        StatementList AllocObject(BoogieVariable var);
        StatementList AllocObject(IVariable var, InstructionTranslator instTranslator);
        StatementList AllocLocalVariables(IList<IVariable> variables);

        StatementList AllocStaticVariables();
        StatementList AllocStaticVariable(IFieldReference field);
        StatementList DeclareLocalVariables(IList<IVariable> variables, Dictionary<string, BoogieVariable> temporalVariables);

        StatementList CallReadArrayElement(IVariable resultVariable, Expression array, Expression index, InstructionTranslator instructionTranslator);
        StatementList CallReadArrayElement(BoogieVariable result, Expression array, Expression index); // na
        StatementList CallWriteArrayElement(Expression array, Expression index, Expression value); //na

        Expression NullObject();

        Expression ReadAddr(IVariable addr); // na
        Expression ReadAddr(Addressable addr);
        StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result);
        Expression ReadInstanceField(InstanceFieldAccess instanceFieldAccess);
        StatementList ReadStaticField(StaticFieldAccess staticFieldAccess, IVariable value); //na

        StatementList WriteAddr(Addressable addr, Expression value);
        StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression value, InstructionTranslator instTranslator);
        StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value, InstructionTranslator instTranslator); //na
        StatementList WriteStaticField(StaticFieldAccess staticFieldAccess, Expression expr); //na
        StatementList WriteStaticField(StaticFieldAccess staticFieldAccess, IVariable value); //na

        StatementList VariableAssignment(IVariable variableA, IValue value);
        StatementList VariableAssignment(IVariable variableA, Expression expr);
    }

    public abstract class BaseMemory : IMemory
    {
        public static IMemory Instance()
        {
            if (Settings.MemoryModel == ProgramOptions.MemoryModelOption.SplitFields)
                return new BCTMemory();
            else if (Settings.MemoryModel == ProgramOptions.MemoryModelOption.Addresses)
                return new AddrMemory();
            else if (Settings.MemoryModel == ProgramOptions.MemoryModelOption.Mixed)
                return new MixedMemory();

            throw new NotImplementedException();
        }

        public BaseMemory(IMemory dispatcher = null)
        {
            if (dispatcher == null)
                dispatcher = this;
            this.dispatcher = dispatcher;
        }

        protected IMemory dispatcher;
        protected BoogieGenerator bg = BoogieGenerator.Instance();

        //public abstract Addressable AddressOf(IReferenceable value);
        //public abstract Addressable AddressOf(IValue value);
        public abstract Addressable AddressOf(InstanceFieldAccess instanceFieldAccess);
        public abstract Addressable AddressOf(StaticFieldAccess staticFieldAccess);
        public abstract Addressable AddressOf(IVariable var);
        public abstract StatementList AllocAddr(IVariable var);
        public abstract StatementList AllocLocalVariables(IList<IVariable> variables);
        public abstract StatementList AllocObject(BoogieVariable var);
        public abstract StatementList AllocObject(IVariable var, InstructionTranslator instTranslator);
        public abstract StatementList CallReadArrayElement(IVariable resultVariable, Expression array, Expression index, InstructionTranslator instructionTranslator);
        //public abstract StatementList CallReadArrayElement(BoogieVariable result, Expression array, Expression index);
        //public abstract StatementList CallWriteArrayElement(Expression array, Expression index, Expression value);
        public abstract StatementList DeclareLocalVariables(IList<IVariable> variables, Dictionary<string, BoogieVariable> temporalVariables);
        public abstract Expression NullObject();
        //public abstract Expression ReadAddr(IVariable addr);
        public abstract Expression ReadAddr(Addressable addr);
        public abstract StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result);
        public abstract Expression ReadInstanceField(InstanceFieldAccess instanceFieldAccess);
        //public abstract StatementList ReadStaticField(StaticFieldAccess staticFieldAccess, IVariable value);
        public abstract StatementList VariableAssignment(IVariable variableA, IValue value);
        public abstract StatementList WriteAddr(Addressable addr, Expression value);
        public abstract StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression value, InstructionTranslator instTranslator);
        //public abstract StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value, InstructionTranslator instTranslator);
        //public abstract StatementList WriteStaticField(StaticFieldAccess staticFieldAccess, Expression expr);
        //public abstract StatementList WriteStaticField(StaticFieldAccess staticFieldAccess, IVariable value);

        public Expression ReadAddr(IVariable addr)
        {
            return dispatcher.ReadAddr(dispatcher.AddressOf(addr));
        }

        public Addressable AddressOf(IValue value)
        {
            if (value is IReferenceable)
                return dispatcher.AddressOf(value as IReferenceable);
            else if (value is Reference reference)
                return dispatcher.AddressOf(reference.Value);
            else
                throw new NotImplementedException();

            // we are covering AddressOf for the following types
            // Reference, Dereference, IVariable, InstanceFieldAccess, StaticFieldAccess and ArrayElementAccess
        }

        public Addressable AddressOf(IReferenceable value)
        {
            if (value is InstanceFieldAccess)
            {
                return dispatcher.AddressOf(value as InstanceFieldAccess);
            }
            else if (value is StaticFieldAccess)
            {
                return dispatcher.AddressOf(value as StaticFieldAccess);
            }
            else if (value is IVariable)
            {
                return dispatcher.AddressOf(value as IVariable);
            }
            else if (value is ArrayElementAccess)
            {
                throw new NotImplementedException();
            }
            else if (value is Dereference dereference)
                return dispatcher.AddressOf(dereference.Reference);

            // I should have covered all possible cases
            throw new NotImplementedException();
        }

        public StatementList WriteStaticField(StaticFieldAccess staticFieldAccess, Expression expr)
        {
            return dispatcher.WriteAddr(dispatcher.AddressOf(staticFieldAccess), expr);
        }
        public StatementList WriteStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            return dispatcher.WriteStaticField(staticFieldAccess, dispatcher.ReadAddr(value));
        }
        public StatementList  ReadStaticField(StaticFieldAccess staticFieldAccess, IVariable value)
        {
            StatementList stmts = new StatementList();

            var address = dispatcher.AddressOf(staticFieldAccess);

            stmts.Add(bg.VariableAssignment(value, dispatcher.ReadAddr(address)));

            return stmts;
        }
        public StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable value, InstructionTranslator instTranslator)
        {
            return WriteInstanceField(instanceFieldAccess, ReadAddr(value), instTranslator);
        }

        public StatementList CallReadArrayElement(BoogieVariable result, Expression array, Expression index)
        {
            var l = new List<Expression>();
            l.Add(array);
            l.Add(index);
            return bg.ProcedureCall(BoogieMethod.ReadArrayElement, l, result);
        }

        public StatementList CallWriteArrayElement(Expression array, Expression index, Expression value)
        {
            var l = new List<Expression>();
            l.Add(array);
            l.Add(index);
            l.Add(value);
            return BoogieGenerator.Instance().ProcedureCall(BoogieMethod.WriteArrayElement, l);
        }

        public StatementList VariableAssignment(IVariable variableA, Expression expr)
        {
            return dispatcher.WriteAddr(dispatcher.AddressOf(variableA), expr);
        }

        public StatementList AllocStaticVariables()
        {
            StatementList stmts = new StatementList();
            foreach (IFieldReference field in FieldTranslator.GetFieldReferences())
            {
                if (field.IsStatic)
                    stmts.Add(dispatcher.AllocStaticVariable(field));
            }

            return stmts;
        }

        public abstract StatementList AllocStaticVariable(IFieldReference field);

        public StatementList AllocAddr(BoogieVariable var)
        {
            return bg.ProcedureCall(BoogieMethod.AllocAddr, new List<Expression> { }, var);
        }
    }

    public class AddrMemory : BaseMemory
    {
        public AddrMemory(IMemory dispatcher=null) : base(dispatcher)
        {
        }

        public override Expression NullObject()
        {
            return BoogieLiteral.NullObject;
        }

        // hides implementation in super class
        public override StatementList AllocAddr(IVariable var)
        {
            var resultBoogieVar = BoogieVariable.AddressVar(var);
            return dispatcher.AllocAddr(resultBoogieVar);
        }
        public override StatementList AllocObject(BoogieVariable boogieVar)
        {
            return bg.ProcedureCall(BoogieMethod.AllocObject, new List<Expression> { }, boogieVar);
        }
        public override StatementList AllocObject(IVariable var, InstructionTranslator instTranslator)
        {
            var freshVariable = instTranslator.GetFreshVariable(Helpers.GetBoogieType(var));
            var stmts = new StatementList();
            stmts.Add(dispatcher.AllocObject(freshVariable));
            stmts.Add(bg.VariableAssignment(var, freshVariable));
            return stmts;
        }

        // hides implementation in super class
        //public new string VariableAssignment(string variableA, string expr)
        //{
        //    return string.Format("{0} := {1};", variableA, expr);
        //}

        public override StatementList DeclareLocalVariables(IList<IVariable> variables, Dictionary<string, BoogieVariable> temporalVariables)
        {
            var stmts = new StatementList();
            foreach (var v in variables)
            {
                stmts.Add(BoogieStatement.VariableDeclaration(BoogieVariable.AddressVar(v)));
            }

            foreach (var kv in temporalVariables)
            {
                stmts.Add(BoogieStatement.VariableDeclaration(kv.Value));
            }

            return stmts;
        }

        public override Expression ReadAddr(Addressable addr)
        {
            if (addr is AddressExpression addrExpr)
            {
                var readExpr = ReadTypedMemory.From(addrExpr);
                return readExpr;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override StatementList WriteAddr(Addressable addr, Expression expr)
        {
            if (addr is AddressExpression addrExpr)
            {
                if (expr.Type.Equals(Helpers.BoogieType.Addr))
                {
                    // pointer = pointer does not require map indexing in boogie
                    // it is just a variable assignment
                    BoogieVariable v = new BoogieVariable(expr.Type, addrExpr.Expr.Expr);
                    return BoogieStatement.VariableAssignment(v, expr);
                }

                return TypedMemoryMapUpdate.ForKeyValue(addrExpr, expr);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override StatementList VariableAssignment(IVariable variableA, IValue value)
        {
            Constant cons = value as Constant;
            BoogieLiteral boogieConstant = null;
            if (cons != null)
            {
                boogieConstant = BoogieLiteral.FromDotNetConstant(cons);
            }


            var boogieType = Helpers.GetBoogieType(variableA);

            if (value is Constant)
            {
                if (boogieConstant != null)
                {
                    return bg.VariableAssignment(variableA, boogieConstant);
                }
                else
                {
                    throw new NotImplementedException();
                    // return WriteAddr(variableA, value.ToString());
                }

            }
            else if (value is IVariable && !(value.Type is IManagedPointerType))
            { // right operand is not a pointer (therefore left operand is not a pointer)

                return dispatcher.WriteAddr(dispatcher.AddressOf(variableA),dispatcher.ReadAddr(value as IVariable));
            }
            else if (value is Dereference)
            {
                var dereference = value as Dereference;
                var content = dispatcher.ReadAddr(dereference.Reference);
                return dispatcher.WriteAddr(dispatcher.AddressOf(variableA), content);
            }
            else if (value.Type is IManagedPointerType)
            {

                // if the right operand is a pointer also the left one is a pointer
                // there are two cases for value:
                // 1) value has the form &<something> (in analysis-net this is a Reference object)
                // 2) value is just a variable (static, instance, local, array element) with pointer type
                // for 1) we want to take the allocated address of something and assign it to the boogie variable of the left operand
                // for 2) we just want to make a boogie assignment between the boogie variables of the left and right operands

                // AddressOf will do the work to separate case 1) and 2)
                var addr = dispatcher.AddressOf(value) as AddressExpression;
                Contract.Assume(addr != null);
                return BoogieStatement.VariableAssignment(BoogieVariable.AddressVar(variableA), addr.Expr);
            }

            Contract.Assert(false);
            // This shouldn't be reachable.
            throw new NotImplementedException();
        }

        public override StatementList AllocLocalVariables(IList<IVariable> variables)
        {
            StatementList stmts = new StatementList();

            // we allocate an address for all local variables
            // except they are a pointer, we are assuming that you can't take the address of a pointer
            foreach (var v in variables)
                if (!(v.Type is IManagedPointerType))
                    stmts.Add(dispatcher.AllocAddr(v));

            // load values into stack space
            foreach (var paramVariable in variables.Where(v => v.IsParameter))
            {
                // paramValue are variables in the three address code
                // however in boogie they are treated as values
                // those values are loaded into the stack memory space

                /*
                 void foo(int x){
                 }

                 procedure foo(x : int){
                    var _x : Addr; // stack space (done in previous loop)
                    x_ := AllocAddr();

                    data(_x) := x; // we are doing this conceptually
                 }
                */
                var boogieParamVariable = BoogieParameter.FromDotNetVariable(paramVariable);

                if (paramVariable.Type is IManagedPointerType)
                {
                    stmts.Add(BoogieStatement.VariableAssignment(BoogieVariable.AddressVar(paramVariable), boogieParamVariable));
                    continue;
                }

                Addressable paramAddress = dispatcher.AddressOf(paramVariable);

                // boogie generator knows that must fetch paramVariable's address (_x and not x)
                stmts.Add(dispatcher.WriteAddr(paramAddress, boogieParamVariable));

                if (Helpers.GetBoogieType(paramVariable).Equals(Helpers.BoogieType.Object))
                {
                    stmts.Add(BoogieStatement.AllocObjectAxiom(paramVariable));
                }
                else if (Helpers.GetBoogieType(paramVariable).Equals(Helpers.BoogieType.Addr))
                {
                    stmts.Add(BoogieStatement.AllocAddrAxiom(paramVariable));
                }
            }

            return stmts;
        }

        public override Addressable AddressOf(InstanceFieldAccess instanceFieldAccess)
        {
            var expr = Expression.LoadInstanceFieldAddr(instanceFieldAccess.Field, dispatcher.ReadAddr(instanceFieldAccess.Instance));
            return new AddressExpression(instanceFieldAccess.Field.Type, expr);
        }

        public override Addressable AddressOf(StaticFieldAccess staticFieldAccess)
        {
            var address = new AddressExpression(staticFieldAccess.Field.Type, BoogieVariable.From(new StaticField(staticFieldAccess)));
            return address;
        }

        public override Addressable AddressOf(IVariable var)
        {
            return new AddressExpression(var.Type, BoogieVariable.AddressVar(var));
        }

        public override Expression ReadInstanceField(InstanceFieldAccess instanceFieldAccess)
        {
            var fieldAddr = dispatcher.AddressOf(instanceFieldAccess);
            var readValue = dispatcher.ReadAddr(fieldAddr);

            return readValue;
        }

        public override StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            var readValue = dispatcher.ReadInstanceField(instanceFieldAccess);

            // dependiendo del type (del result?) indexo en el $memoryInt
            if (Helpers.IsGenericField(instanceFieldAccess.Field))
            {
                var boogieType = Helpers.GetBoogieType(result);
                if (!boogieType.Equals(Helpers.BoogieType.Object))
                {
                    return bg.VariableAssignment(result, Expression.Union2PrimitiveType(boogieType, readValue));
                }
                else
                {
                    return bg.VariableAssignment(result, readValue);
                }
            }
            else
            {
                return bg.VariableAssignment(result, readValue);
            }
        }

        public override StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression expr, InstructionTranslator instTranslator)
        {
            StatementList stmts = new StatementList();

            var boogieType = expr.Type;
            if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals(Helpers.BoogieType.Object))
            {
                stmts.Add(Expression.AssumeInverseRelationUnionAndPrimitiveType(expr));
                stmts.Add(dispatcher.WriteAddr(dispatcher.AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr, instTranslator.Boogie())));
            }
            else
                stmts.Add(dispatcher.WriteAddr(dispatcher.AddressOf(instanceFieldAccess), expr));

            return stmts;
        }

        public override StatementList AllocStaticVariable(IFieldReference field)
        {
            StaticField p = new StaticField(new StaticFieldAccess(field));
            BoogieVariable bv = BoogieVariable.From(p);

            return dispatcher.AllocAddr(bv);
        }

        public override StatementList CallReadArrayElement(IVariable resultVariable, Expression array, Expression index, InstructionTranslator instructionTranslator)
        {
            StatementList stmts = new StatementList();

            BoogieVariable boogieResVar = null;
            if (resultVariable != null)
            {
                boogieResVar = instructionTranslator.GetFreshVariable(Helpers.GetBoogieType(resultVariable));
            }
            stmts.Add(dispatcher.CallReadArrayElement(boogieResVar, array, index));
            if (resultVariable != null)
            {
                stmts.Add(dispatcher.WriteAddr(dispatcher.AddressOf(resultVariable), boogieResVar));
            }
            return stmts;

        }
    }

    public class BCTMemory : BaseMemory
    {
        public BCTMemory(IMemory dispatcher=null) : base(dispatcher)
        {
        }

        public override Expression NullObject()
        {
            return BoogieLiteral.NullRef;
        }

        public override StatementList VariableAssignment(IVariable variableA, IValue value)
        {
            Constant cons = value as Constant;
            if (cons != null)
            {
                return bg.VariableAssignment(variableA, BoogieLiteral.FromDotNetConstant(cons));
            }
            else if (value is Dereference)
            {
                var dereference = value as Dereference;
                return VariableAssignment(variableA, dereference.Reference);
            }

            return bg.VariableAssignment(variableA, dispatcher.ReadAddr(dispatcher.AddressOf(value)));
        }

        public override StatementList AllocLocalVariables(IList<IVariable> variables)
        {
            return BoogieStatement.Nop;
        }

        public override StatementList AllocStaticVariable(IFieldReference field)
        {
            return BoogieStatement.Nop;
        }

        public override StatementList DeclareLocalVariables(IList<IVariable> variables, Dictionary<string, BoogieVariable> temporalVariables)
        {
            StatementList stmts = new StatementList();

            foreach (var v in variables.Where(v => !v.IsParameter))
            {
                stmts.Add(BoogieStatement.VariableDeclaration(v));
            }

            foreach (var kv in temporalVariables)
            {
                stmts.Add(BoogieStatement.VariableDeclaration(kv.Value));
            }

            return stmts;
        }

        public override StatementList AllocAddr(IVariable var)
        {
            return BoogieStatement.Nop;
        }
        public override StatementList AllocObject(BoogieVariable boogieVar)
        {
            return bg.ProcedureCall(BoogieMethod.Alloc, new List<Expression> { }, boogieVar);
        }
        public override StatementList AllocObject(IVariable var, InstructionTranslator instTranslator)
        {
            return this.AllocObject(BoogieVariable.FromDotNetVariable(var));
        }

        public override StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression expr, InstructionTranslator instTranslator)
        {
            StatementList stmts = new StatementList();

            String fieldName = FieldTranslator.GetFieldName(instanceFieldAccess.Field);

            //var addr = AddressOf(instanceFieldAccess);
            //var writeAddr = WriteAddr(addr, value);

            if (!Settings.SplitFieldsEnabled())
            {
                if (!Helpers.IsBoogieRefType(expr.Type)) // int, bool, real
                {
                    stmts.Add(Expression.AssumeInverseRelationUnionAndPrimitiveType(expr));
                    stmts.Add(dispatcher.WriteAddr(dispatcher.AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr, instTranslator.Boogie())));
                    //sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, PrimitiveType2Union(Helpers.GetBoogieType(value.Type), value.Name)));
                }
                else
                {
                    stmts.Add(dispatcher.WriteAddr(dispatcher.AddressOf(instanceFieldAccess), expr));
                    //sb.AppendLine(String.Format("\t\t$Heap := Write($Heap, {0}, {1}, {2});", instanceFieldAccess.Instance, fieldName, value.Name));
                }
            }
            else
            {
                var boogieType = expr.Type;
                // var heapAccess = String.Format("{0}[{1}]", fieldName, instanceFieldAccess.Instance);
                //F$ConsoleApplication3.Foo.p[f_Ref] := $ArrayContents[args][0];

                if (Helpers.IsGenericField(instanceFieldAccess.Field) && !boogieType.Equals(Helpers.BoogieType.Ref))
                {
                    stmts.Add(Expression.AssumeInverseRelationUnionAndPrimitiveType(expr));
                    //sb.AppendLine(VariableAssignment(heapAccess, PrimitiveType2Union(boogieType, value.Name)));
                    stmts.Add(dispatcher.WriteAddr(dispatcher.AddressOf(instanceFieldAccess), Expression.PrimitiveType2Union(expr, instTranslator.Boogie())));
                }
                else
                    stmts.Add(dispatcher.WriteAddr(dispatcher.AddressOf(instanceFieldAccess), expr));
            }

            return stmts;
        }

        public override Expression ReadInstanceField(InstanceFieldAccess instanceFieldAccess)
        {
            if (!Settings.SplitFieldsEnabled())
                return ReadFieldExpression.From(new InstanceField(instanceFieldAccess));
            else
                return dispatcher.ReadAddr(dispatcher.AddressOf(instanceFieldAccess));
        }

        public override StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            StatementList stmts = new StatementList();
            var boogieType = Helpers.GetBoogieType(result);

            var readFieldExpr = dispatcher.ReadInstanceField(instanceFieldAccess);

            if (!Settings.SplitFieldsEnabled())
            {
                if (!Helpers.IsBoogieRefType(Helpers.GetBoogieType(result))) // int, bool, real
                {
                    var expr = Expression.Union2PrimitiveType(boogieType, readFieldExpr);
                    stmts.Add(bg.VariableAssignment(result, expr));
                }
                else
                {
                    stmts.Add(bg.VariableAssignment(result, readFieldExpr));
                }
            }
            else
            {
                //p_int:= F$ConsoleApplication3.Holds`1.x[$tmp2];
                if (Helpers.IsGenericField(instanceFieldAccess.Field) &&
                     !boogieType.Equals(Helpers.BoogieType.Ref))
                {
                    stmts.Add(bg.VariableAssignment(result, Expression.Union2PrimitiveType(boogieType, readFieldExpr)));
                }
                else
                    stmts.Add(bg.VariableAssignment(result, readFieldExpr));
            }

            return stmts;
        }

        public override Expression ReadAddr(Addressable addr)
        {
            if (addr is InstanceField instanceField)
            {
                var fieldName = FieldTranslator.GetFieldName(instanceField.Field);
                var instanceName = instanceField.Instance.Name;
                if (Settings.SplitFieldsEnabled())
                {
                    return ReadFieldExpression.From(instanceField);
                }
                else
                {
                    return ReadHeapExpression.From(instanceField);
                }
            }
            else if (addr is StaticField staticField)
            {
                return ReadFieldExpression.From(staticField);
            }
            else if (addr is DotNetVariable v)
            {
                return BoogieVariable.FromDotNetVariable(v.Var);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override Addressable AddressOf(InstanceFieldAccess instanceFieldAccess)
        {
            return new InstanceField(instanceFieldAccess);
        }

        public override Addressable AddressOf(StaticFieldAccess staticFieldAccess)
        {
            return new StaticField(staticFieldAccess);
        }

        public override Addressable AddressOf(IVariable var)
        {
            return new DotNetVariable(var);
        }

        public override StatementList WriteAddr(Addressable addr, Expression expr)
        {
            if (addr is InstanceField instanceField)
            {
                var instanceName = instanceField.Instance;
                var fieldName = FieldTranslator.GetFieldName(instanceField.Field);
                if (Settings.SplitFieldsEnabled())
                {
                    return SplitFieldUpdate.ForKeyValue(instanceField, expr);
                }
                else
                {
                    return HeapUpdate.ForKeyValue(instanceField, expr);
                }

            }
            else if (addr is StaticField staticField)
            {
                var boogieVar = BoogieVariable.From(staticField);
                return bg.VariableAssignment(boogieVar, expr);
            }
            else if (addr is DotNetVariable v)
            {
                return BoogieStatement.VariableAssignment(BoogieVariable.FromDotNetVariable(v.Var), expr);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override StatementList CallReadArrayElement(IVariable resultVariable, Expression array, Expression index, InstructionTranslator instructionTranslator)
        {
            StatementList stmts = new StatementList();
            return dispatcher.CallReadArrayElement(BoogieVariable.FromDotNetVariable(resultVariable), array, index);
        }
    }

    public class MixedMemory : BaseMemory {
        public MixedMemory() : base(null)
        {
            memAddr = new AddrMemory(this);
            memBCT = new BCTMemory(this);
        }

        IMemory memAddr;
        IMemory memBCT;

        private bool RequiresAllocation(IValue value)
        {
            if (value is IReferenceable referenceable)
                return RequiresAllocation(referenceable);

            return false;
        }

        private bool RequiresAllocation(IReferenceable referenceable)
        {
            // we are assuming that a IManagedPointerType can not be a pointer of a pointer
            Contract.Assume(!(referenceable is IManagedPointerType) || !ReferenceFinder.IsReferenced(referenceable));

            return ReferenceFinder.IsReferenced(referenceable);
        }

        private bool RequiresAllocation(IFieldReference field)
        {
            // we are assuming that a IManagedPointerType can not be a pointer of a pointer
            Contract.Assume(!(field.Type is IManagedPointerType) || !ReferenceFinder.IsReferenced(field));

            return ReferenceFinder.IsReferenced(field);
        }

        public override Addressable AddressOf(InstanceFieldAccess instanceFieldAccess)
        {
            if (RequiresAllocation(instanceFieldAccess) || instanceFieldAccess.Type is IManagedPointerType)
                return memAddr.AddressOf(instanceFieldAccess);
            else
                return memBCT.AddressOf(instanceFieldAccess);
        }

        public override Addressable AddressOf(StaticFieldAccess staticFieldAccess)
        {
            if (RequiresAllocation(staticFieldAccess) || staticFieldAccess.Type is IManagedPointerType)
                return memAddr.AddressOf(staticFieldAccess);
            else
                return memBCT.AddressOf(staticFieldAccess);
        }

        public override Addressable AddressOf(IVariable var)
        {
            if (RequiresAllocation(var) || var.Type is IManagedPointerType)
                return memAddr.AddressOf(var);
            else
                return memBCT.AddressOf(var);
        }

        public override StatementList AllocAddr(IVariable var)
        {
            if (RequiresAllocation(var))
                return memAddr.AllocAddr(var);
            else
                return memBCT.AllocAddr(var);
        }

        public override StatementList AllocLocalVariables(IList<IVariable> variables)
        {
            StatementList stmts = new StatementList();

            // only allocate an address for variables that are referenced 
            foreach (var v in variables)
                if (RequiresAllocation(v))
                    stmts.Add(AllocAddr(v));

            foreach (var paramVariable in variables.Where(v => v.IsParameter && (RequiresAllocation(v) || (v.Type is IManagedPointerType))))
            {
                var boogieParamVariable = BoogieParameter.FromDotNetVariable(paramVariable);

                if (!RequiresAllocation(paramVariable))
                {
                    //BoogieVariable target = RequiresAllocation(paramVariable) || (paramVariable.Type is IManagedPointerType) ? 
                    //    BoogieVariable.AddressVar(paramVariable) : BoogieVariable.FromDotNetVariable(paramVariable);

                    BoogieVariable target = BoogieVariable.AddressVar(paramVariable);
                    stmts.Add(BoogieStatement.VariableAssignment(target, boogieParamVariable));
                    continue;
                }

                Addressable paramAddress = AddressOf(paramVariable);

                // boogie generator knows that must fetch paramVariable's address (_x and not x)
                stmts.Add(WriteAddr(paramAddress, boogieParamVariable));

                if (Helpers.GetBoogieType(paramVariable).Equals(Helpers.BoogieType.Object))
                {
                    stmts.Add(BoogieStatement.AllocObjectAxiom(paramVariable));
                }
                else if (Helpers.GetBoogieType(paramVariable).Equals(Helpers.BoogieType.Addr))
                {
                    stmts.Add(BoogieStatement.AllocAddrAxiom(paramVariable));
                }
            }

            return stmts;
        }

        public override StatementList AllocObject(BoogieVariable var)
        {
            // actually should be the same in both models
            return memAddr.AllocObject(var);
        }

        public override StatementList AllocObject(IVariable var, InstructionTranslator instTranslator)
        {
            if (RequiresAllocation(var))
                return memAddr.AllocObject(var, instTranslator);
            else
                return memBCT.AllocObject(var, instTranslator);
        }

        public override StatementList DeclareLocalVariables(IList<IVariable> variables, Dictionary<string, BoogieVariable> temporalVariables)
        {
            var stmts = new StatementList();
            foreach (var v in variables)
            {
                if (RequiresAllocation(v) || v.Type is IManagedPointerType)
                    stmts.Add(BoogieStatement.VariableDeclaration(BoogieVariable.AddressVar(v)));
                else if (!v.IsParameter)
                    stmts.Add(BoogieStatement.VariableDeclaration(v));
            }

            foreach (var kv in temporalVariables)
            {
                stmts.Add(BoogieStatement.VariableDeclaration(kv.Value));
            }

            return stmts;
        }

        public override Expression NullObject()
        {
            return memAddr.NullObject();
        }

        public override Expression ReadAddr(Addressable addr)
        {
            if (addr is AddressExpression)
            {
                return memAddr.ReadAddr(addr);
            }
            else if (addr is InstanceField)
            {
                return memBCT.ReadAddr(addr);
            }
            else if (addr is StaticField)
            {
                return memBCT.ReadAddr(addr);
            }
            else if (addr is DotNetVariable)
            {
                return memBCT.ReadAddr(addr);
            }

            throw new NotImplementedException();
        }

        public override Expression ReadInstanceField(InstanceFieldAccess instanceFieldAccess)
        {
            if (RequiresAllocation(instanceFieldAccess))
                return memAddr.ReadInstanceField(instanceFieldAccess);
            else
                return memBCT.ReadInstanceField(instanceFieldAccess);
        }

        public override StatementList ReadInstanceField(InstanceFieldAccess instanceFieldAccess, IVariable result)
        {
            // with split fields false, i guess we should cast to union always
            Contract.Assert(Settings.SplitFieldsEnabled());

            Expression readExpr = ReadInstanceField(instanceFieldAccess);

            if (Helpers.IsGenericField(instanceFieldAccess.Field))
            {
                if (!Helpers.IsBoogieRefType(result))
                {
                    var boogieType = Helpers.GetBoogieType(result);
                    return bg.VariableAssignment(result, Expression.Union2PrimitiveType(boogieType, readExpr));
                }
            }

            return bg.VariableAssignment(result, readExpr);
        }

        public override StatementList VariableAssignment(IVariable variableA, IValue value)
        {
            bool lhsAlloc = RequiresAllocation(variableA);

            Expression expression = GetExpressionFromIValue(value);

            //if (value.Type is IManagedPointerType)
            //{
            //    return memAddr.VariableAssignment(variableA, value);
            //}

            return dispatcher.VariableAssignment(variableA, expression);
            //if (lhsAlloc)
            //    return memAddr.VariableAssignment(variableA, expression);

            //if (!lhsAlloc)
            //    return memBCT.VariableAssignment(variableA, expression);

            //return null;
        }

        private Expression GetExpressionFromIValue(IValue value)
        {
            if (RequiresAllocation(value) || value is Dereference) // if true, value is in a subset of IReferenceable stuff
            {
                // something requires an allocation because it has been reference (it is referenceable)
                IReferenceable referenceable = value as IReferenceable;
                return memAddr.ReadAddr(memAddr.AddressOf(referenceable));
            }

            if (value.Type is IManagedPointerType)
            {
                AddressExpression addr = memAddr.AddressOf(value) as AddressExpression;
                return addr.Expr;
            }

            if (value is Constant constant)
                return BoogieLiteral.FromDotNetConstant(constant);

            // we use old memory model here because they were not referenced
            if (value is IReferenceable || value is Reference)
            {
                Addressable addressable = memBCT.AddressOf(value);
                return memBCT.ReadAddr(addressable);
            }

            throw new NotImplementedException();
        }

        public override StatementList WriteAddr(Addressable addr, Expression value)
        {
            if (addr is AddressExpression addrExpr)
            {
                return memAddr.WriteAddr(addr, value);
            }
            else if (addr is InstanceField instanceField)
            {
                return memBCT.WriteAddr(addr, value);
            }
            else if (addr is StaticField staticField)
            {
                return memBCT.WriteAddr(addr, value);
            }
            else if (addr is DotNetVariable dotNetVariable)
            {
                return memBCT.WriteAddr(addr, value);
            }

            throw new NotImplementedException();
        }

        public override StatementList WriteInstanceField(InstanceFieldAccess instanceFieldAccess, Expression value, InstructionTranslator instTranslator)
        {
            if (RequiresAllocation(instanceFieldAccess))
                return memAddr.WriteInstanceField(instanceFieldAccess, value, instTranslator);
            else
                return memBCT.WriteInstanceField(instanceFieldAccess, value, instTranslator);
        }

        public override StatementList CallReadArrayElement(IVariable resultVariable, Expression array, Expression index, InstructionTranslator instructionTranslator)
        {
            if (RequiresAllocation(resultVariable))
            {
                return memAddr.CallReadArrayElement(resultVariable, array, index, instructionTranslator);
            }
            else
                return memBCT.CallReadArrayElement(resultVariable, array, index, instructionTranslator);
        }

        public override StatementList AllocStaticVariable(IFieldReference field)
        {
            if (RequiresAllocation(field))
                return memAddr.AllocStaticVariable(field);
            else
                return memBCT.AllocStaticVariable(field);
        }
}

}
