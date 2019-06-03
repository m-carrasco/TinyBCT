using System;
using System.Diagnostics.Contracts;
public class A {
    public static void Bar(int n) {
        Contract.Assert(n==5);
    }

    public static void Foo(int n) {
    }
}
class Test {
    public static void Main() {
        Action<int> new_delegate = delegate (int x) { A.Bar(x); };
        new_delegate(5);
    }
}