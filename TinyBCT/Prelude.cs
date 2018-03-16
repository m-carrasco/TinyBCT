using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    class Prelude
    {
        public static void Write()
        {
            var tinyBCTExeFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var streamReader = new StreamReader(Path.Combine(tinyBCTExeFolder, @"prelude.bpl"));
            var streamWriter = Program.streamWriter;
            // prelude
            streamWriter.WriteLine(streamReader.ReadToEnd());
            streamReader.Close();
        }
    }
}
