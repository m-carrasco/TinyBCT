using System;
using System.IO;

namespace Shared.Billing
{
    public static class ActiveListParser
    {
        /* precondition:
         * name == STRING1_STRING2_STRING3_activeList.txt
         * myBlob != null && StreamReader(myBlob).ReadToEnd() != null
         */       
        public static ActiveList Parse(string name, Stream myBlob)
        {
            // name has this format CLIENTCODE_YEAR_MONTH_activeList.txt
            /*  myBlob has this format
                99050555745;Ka Aa;A
                29120458762;Kb Ab;A
                39091666028;Kc Ac;B
                77050929111;Kd Ad;A
                76091166752;Ke Ae;A
                97031653569;Kf Af;B
                35060205229;Kg Ag;A
                38112669875;Kh Ah;B
                13102408939;Kh Ah;A
            */

            using (StreamReader sr = new StreamReader(myBlob))
            {
                var dataLines = sr.ReadToEnd();
                var parts = name.Split(new char[] { '_' });
                return new ActiveList
                {
                    CustomerCode = parts[0],
                    Year = int.Parse(parts[1]),
                    Month = int.Parse(parts[2]),
                    DataLines = dataLines.Split(Environment.NewLine.ToCharArray())
                };
            }
        }

        /* postcondition:
         * result.CustomerCode == STRING1
         * result.Year == STRING2
         * result.Month == STRING3
         * join_array(result.DataLines, result.DataLines.Size-1) == myBlob

        //actually we could ask something stronger about the array: every element terminates with Environment.NewLine or there is only one element with no Environment.NewLine
        function join_array(array, i){
            if (i < 0)
               string.empty           
            else if (i == 0)
                array[0]
            else
                z3_concat(join_array(array, i-1), array[i]))
        }       
         */
    }

    public class ActiveList
    {
        public string CustomerCode { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string[] DataLines { get; set; }
    }
}
