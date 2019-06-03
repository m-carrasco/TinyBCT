using System.Diagnostics.Contracts;


class Test {
  public static void Main(string[] args) {
      if (args.Length > 0) {
          Contract.Assert(args[0] != null);
      }
  }
  public static void Main2(string[] args) {
      Contract.Assert(args.Length == 0);
  }
}
