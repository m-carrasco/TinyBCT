using Backend;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    partial class BoogieProcedureTemplate
    {
        string methodName;
        StatementList instructions;
        StatementList localVariables;
        string attr;
        string parametersWithTypes;
        string returnTypeIfAny;
        bool isExtern;

        public BoogieProcedureTemplate(string pMethodName, 
                                        string pAttr, 
                                        StatementList pLocalVariables,
                                        StatementList pInstructions, 
                                        string pParametersWithTypes, 
                                        string pReturnTypeIfAny,
                                        bool pIsExtern)
        {
            methodName = pMethodName;
            attr = pAttr;
            localVariables = pLocalVariables;
            instructions = pInstructions;
            parametersWithTypes = pParametersWithTypes;
            returnTypeIfAny = pReturnTypeIfAny;
            isExtern = pIsExtern;
        }

        public StatementList LocalVariablesDeclaration() { return localVariables; }
        public StatementList Instructions() { return instructions; }
        public string ProcedureName() { return methodName; }
        public string ParametersWithType() { return parametersWithTypes; }
        public string ReturnTypeIfAny() { return returnTypeIfAny;  }
        public string Attributes() { return attr; }
    }
}
