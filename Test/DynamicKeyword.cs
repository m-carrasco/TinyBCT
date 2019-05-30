using System;
namespace Test
{
    public class DynamicKeyword
    {
        public DynamicKeyword() { }

        public static void Main()
        {
            Named n = new Named();
            var s = GetName(n);
        }

        public static string GetName(dynamic named)
        {
            return named.Name;
        }

        class Named
        {
            public string Name;
        }
    }
}
