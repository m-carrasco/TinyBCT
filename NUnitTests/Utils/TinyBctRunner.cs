using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyBCT;

namespace NUnitTests
{
    class TinyBctRunner
    {
        public string Run(ProgramOptions options)
        {
            TinyBCT.Program.Start(options);
            return options.OutputFile;
        }
    }

    public class TestOptions : ProgramOptions
    {
        public TestOptions()
        {
            EmitLineNumbers = true;
        }

        public void UseCollectionStubs()
        {
            var pathToSelf = System.IO.Path.GetDirectoryName(typeof(TinyBCT.Program).Assembly.Location);
            var stubs = System.IO.Path.Combine(pathToSelf, "..", "..", "..", "TinyBCT", "Dependencies", "CollectionStubs.dll");
            GetInputFiles().Add(stubs);
        }

        public void UsePoirotStubs()
        {
            var pathToSelf = System.IO.Path.GetDirectoryName(typeof(TinyBCT.Program).Assembly.Location);
            var bplFile = System.IO.Path.Combine(pathToSelf, "..", "..", "..", "TinyBCT", "Dependencies", "poirot_stubs.bpl");
            SetBplFiles(new List<string>() { bplFile });
        }
    }
}
