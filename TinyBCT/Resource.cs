using System;
namespace TinyBCT
{
    public class Resource
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
