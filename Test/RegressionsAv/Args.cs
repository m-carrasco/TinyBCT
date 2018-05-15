using System.Diagnostics.Contracts;


class Test {
  public static void Main(string[] args) {
      if (args.Length > 0) {
          Contract.Assert(args[0] != null);
      }
  }
}
