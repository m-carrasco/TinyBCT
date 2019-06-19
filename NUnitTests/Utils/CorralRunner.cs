using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnitTests.Utils
{
    class CorralRunner
    {
        public class CorralOptions
        {
            public Nullable<int> ExecutionContextBound { get; set; } = null;
            public string MainProcedure { get; set; } = null;
            public Nullable<int> RecursionBound { get; set; } = 10;
            public bool TrackAllVars { get; set; } = false;
            public string Track { get; set; } = null;
            public bool UseArrayTheory { get; set; } = false;
            public Nullable<int> TimeLimit { get; set; } = null;
            public string Flags { get; set; } = null;
            public bool PrintBoogieExt { get; set; } = false;
            public Nullable<bool> Cooperative { get; set; } = null;
            public string InputBplFile { get; set; } = null;

            // returns cmd line to execute corral
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(InputBplFile + " ");

                if (ExecutionContextBound != null)
                    sb.Append("/k:" + ExecutionContextBound.ToString() + " ");

                if (MainProcedure != null)
                    sb.Append("/main:" + MainProcedure + " ");

                if (RecursionBound != null)
                    sb.Append("/recursionBound:" + RecursionBound.ToString() + " ");

                if (TrackAllVars)
                    sb.Append("/trackAllVars ");

                if (Track != null)
                    sb.Append("/track:" + Track + " ");

                if (UseArrayTheory)
                    sb.Append("/useArrayTheory ");

                if (TimeLimit != null)
                    sb.Append("/timeLimit:" + TimeLimit.ToString() + " ");

                if (Flags != null)
                    sb.Append("/flags:" + Flags + " ");

                if (PrintBoogieExt)
                    sb.Append("/printBoogieExt ");

                if (Cooperative != null && Cooperative == true)
                    sb.Append("/cooperative ");

                return sb.ToString();
            }
        }

        public static string rootTinyBCT = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
        public static string corralPath = Path.Combine(rootTinyBCT, "..", "corral", "bin", "Debug", "corral.exe");
        public class CorralResult
        {
            public class CorralOutputException : Exception { }

            private string output;
            private string err;
            private string cmd;
            public CorralResult(string pOutput, string pErr, string pCmd)
            {
                output = pOutput;
                err = pErr;
                cmd = pCmd;
            }
            private static void SanityCheck(string output, string err, bool checkSyntaxError = true)
            {
                bool result = true;
                result = result && !output.Equals("");
                if (checkSyntaxError)
                {
                    result = result && err.Equals("");
                    result = result && !output.ToUpper().Contains(": ERROR:");
                }
                if (!result)
                {
                    throw new CorralOutputException();
                }
            }
            public bool AssertionFails()
            {
                SanityCheck(this.output, this.err);
                return output.Contains("Program has a potential bug: True bug");
            }
            public bool NoBugs()
            {
                SanityCheck(this.output, this.err);
                return output.Contains("Program has no bugs") && !AssertionFails();
            }
            public bool SyntaxErrors()
            {
                SanityCheck(this.output, this.err, checkSyntaxError: false);
                return this.output.Contains(": error:") || this.err.Contains("Parse errors");
            }
            public bool NameResolutionErrors()
            {
                SanityCheck(this.output, this.err, checkSyntaxError: false);
                return this.output.Contains("name resolution error");
            }
            public string getOutput()
            {
                SanityCheck(this.output, this.err);
                return output;
            }
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("stdout:");
                sb.AppendLine(output);
                sb.AppendLine("stderr:");
                sb.AppendLine(err);
                return sb.ToString();
            }
        }

        public CorralResult Run(CorralOptions corralOptions)
        {
            System.Diagnostics.Contracts.Contract.Assert(System.IO.File.Exists(corralOptions.InputBplFile));

            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = corralPath;
            pProcess.StartInfo.Arguments = corralOptions.ToString();
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.RedirectStandardError = true;
            var cmd = corralPath + " " + pProcess.StartInfo.Arguments;
            pProcess.Start();
            string output = pProcess.StandardOutput.ReadToEnd();
            string err = pProcess.StandardError.ReadToEnd();
            pProcess.WaitForExit();
            pProcess.Dispose();
            return new CorralResult(output, err, cmd);
        }
    }
}
