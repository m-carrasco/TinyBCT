using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyBCT.Translators;

namespace TinyBCT
{
    class TypeVisitor : MetadataTraverser
    {
        private IMetadataHost host;
        private ISourceLocationProvider sourceLocationProvider;
        private StringBuilder sb = new StringBuilder();

        public TypeVisitor(IMetadataHost host, ISourceLocationProvider sourceLocationProvider)
        {
            this.host = host;
            this.sourceLocationProvider = sourceLocationProvider;
        }

        public override void TraverseChildren(ITypeDefinition typeDefinition)
        {
            FieldTranslator ft = new FieldTranslator(typeDefinition);


            // todo: improve this piece of code
            StreamWriter streamWriter = Program.streamWriter;
            streamWriter.WriteLine(ft.Translate());
        }

    }
}
