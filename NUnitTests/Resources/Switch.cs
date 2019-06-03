using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Switch
    {
        public void test1_NoBugs(int programNumber)
        {
            programNumber = -100;
            switch (programNumber)
            {
                case 1:
                    Contract.Assert(false);
                    break;
                case 2:
                    Contract.Assert(false);
                    break;
                case 3:
                    Contract.Assert(false);
                    break;
                case 4:
                    Contract.Assert(false);
                    break;
                case 5:
                    Contract.Assert(false);
                    break;
                case 6:
                    Contract.Assert(false);
                    break;
                case 7:
                    Contract.Assert(false);
                    break;
                case 8:
                    Contract.Assert(false);
                    break;
                default:
                    Contract.Assert(programNumber == -100);
                    break;
            }
        }

        public void test1_Bugged(int programNumber)
        {
            programNumber = -100;
            switch (programNumber)
            {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    break;
                case 8:
                    break;
                default:
                    Contract.Assert(programNumber != -100);
                    break;
            }
        }

        public void test2_NoBugs(int programNumber)
        {
            programNumber = 8;
            switch (programNumber)
            {
                case 1:
                    Contract.Assert(false);
                    break;
                case 2:
                    Contract.Assert(false);
                    break;
                case 3:
                    Contract.Assert(false);
                    break;
                case 4:
                    Contract.Assert(false);
                    break;
                case 5:
                    Contract.Assert(false);
                    break;
                case 6:
                    Contract.Assert(false);
                    break;
                case 7:
                    Contract.Assert(false);
                    break;
                case 8:
                    programNumber = programNumber + 8;
                    Contract.Assert(programNumber == 16);
                    break;
            }
        }

        public void test2_Bugged(int programNumber)
        {
            programNumber = 8;
            switch (programNumber)
            {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    break;
                case 8:
                    programNumber = programNumber + 8;
                    Contract.Assert(programNumber != 16);
                    break;
            }
        }

        public void test3_NoBugs(int programNumber)
        {
            programNumber = -100;
            switch (programNumber)
            {
                case 1:
                    Contract.Assert(false);
                    break;
                case 2:
                    Contract.Assert(false);
                    break;
                case 3:
                    Contract.Assert(false);
                    break;
                case 4:
                    Contract.Assert(false);
                    break;
                case 5:
                    Contract.Assert(false);
                    break;
                case 6:
                    Contract.Assert(false);
                    break;
                case 7:
                    Contract.Assert(false);
                    break;
                case 8:
                    Contract.Assert(false);
                    break;
                default:
                    programNumber = programNumber - 100;
                    break;
            }

            Contract.Assert(programNumber == -200);
        }

        public void test3_Bugged(int programNumber)
        {
            programNumber = -100;
            switch (programNumber)
            {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    break;
                case 8:
                    break;
                default:
                    programNumber = programNumber - 100;
                    break;
            }

            Contract.Assert(programNumber != -200);
        }

        public void test4_NoBugs(int programNumber)
        {
            programNumber = 8;
            switch (programNumber)
            {
                case 1:
                    Contract.Assert(false);
                    break;
                case 2:
                    Contract.Assert(false);
                    break;
                case 3:
                    Contract.Assert(false);
                    break;
                case 4:
                    Contract.Assert(false);
                    break;
                case 5:
                    Contract.Assert(false);
                    break;
                case 6:
                    Contract.Assert(false);
                    break;
                case 7:
                    Contract.Assert(false);
                    break;
                case 8:
                    programNumber = programNumber - 8;
                    break;
                default:
                    break;
            }

            Contract.Assert(programNumber == 0);
        }

        public void test4_Bugged(int programNumber)
        {
            programNumber = 8;
            switch (programNumber)
            {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    break;
                case 8:
                    programNumber = programNumber - 8;
                    break;
                default:
                    break;
            }

            Contract.Assert(programNumber != 0);
        }
    }
}
