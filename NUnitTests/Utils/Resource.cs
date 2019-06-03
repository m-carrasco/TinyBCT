using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitTests.Utils
{
    class Resource
    {
        public static string GetResourceAsString(string resourceName)
        {
            var sourceStream = System.Reflection.Assembly.GetAssembly(typeof(Resource)).GetManifestResourceStream(resourceName);
            System.IO.StreamReader streamReader = new System.IO.StreamReader(sourceStream);
            var source = streamReader.ReadToEnd();
            return source;
        }
    }
}
