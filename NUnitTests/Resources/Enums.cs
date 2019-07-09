using System;
namespace NUnitTests.Resources
{
    public enum Day { Sat, Sun, Mon, Tue, Wed, Thu, Fri };

    public class Enums
    {
        public string M2(Day d)
        {
            return d.ToString();
        }

        public string M3(ref Day d)
        {
            return d.ToString();
        }
    }
}
