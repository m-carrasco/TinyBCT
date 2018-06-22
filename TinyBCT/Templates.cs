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
        string instructions;
        string localVariables;
        string attr;
        string parametersWithTypes;
        string returnTypeIfAny;
        bool isExtern;
        string typeFunction;

        public BoogieProcedureTemplate(string pMethodName, 
                                        string pAttr, 
                                        string pLocalVariables, 
                                        string pInstructions, 
                                        string pParametersWithTypes, 
                                        string pReturnTypeIfAny,
                                        bool pIsExtern,
                                        string pTypeFunction)
        {
            methodName = pMethodName;
            attr = pAttr;
            localVariables = pLocalVariables;
            instructions = pInstructions;
            parametersWithTypes = pParametersWithTypes;
            returnTypeIfAny = pReturnTypeIfAny;
            isExtern = pIsExtern;
            typeFunction = pTypeFunction;
        }

        public string LocalVariablesDeclaration() { return localVariables; }
        public string Instructions() { return instructions; }
        public string ProcedureName() { return methodName; }
        public string ParametersWithType() { return parametersWithTypes; }
        public string ReturnTypeIfAny() { return returnTypeIfAny;  }
        public string Attributes() { return attr; }
        public string TypeFunction() { return typeFunction; }
    }
}
