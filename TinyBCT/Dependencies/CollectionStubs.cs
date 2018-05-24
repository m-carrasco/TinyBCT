//Only parts of Poirot.cs Stubs.cs that influence a collection

using System.Diagnostics.Contracts;
using System.Collections;
using System.Collections.Generic;

namespace Poirot {
  using System;

  public class Poirot {
    public static extern int NondetInt();
    public static extern string NondetString();
    public static extern Object NondetObject();
  }

}


namespace System {
  public class Random {
    public int Next() {
      int x = Poirot.Poirot.NondetInt();
      Contract.Assume(0 <= x);
      return x;
    }
    public int Next(int a) {
      int x = Poirot.Poirot.NondetInt();
      Contract.Assume(a <= x);
      return x;
    }
    public int Next(int a, int b) {
      int x = Poirot.Poirot.NondetInt();
      Contract.Assume(a <= x && x < b);
      return x;
    }
  }

}

namespace System.Collections.Generic
{
    public abstract class List<T> : IEnumerable<T>
    {
	struct Enumerator : IEnumerator<T> {
	      List<T> parent;
	      int iter;
	      public Enumerator(List<T> l) { 
	          parent = l; 
		  iter = -1; 
	      }
	      public bool MoveNext() { 
	          iter = iter + 1; 
		  return (iter < parent.Count);
	      }
	      public T Current { get { return parent[iter]; } }
	      public void Dispose() {}
	      public void Reset() { iter = -1; }
              object System.Collections.IEnumerator.Current { get { return this.Current; } }
	}

        private List(bool dontCallMe) { }
        public IEnumerator<T> GetEnumerator() { return new Enumerator(this); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
	public abstract T this[int index] { get; set; }
	public abstract int Count { get; }
    }

    public abstract class HashSet<T> : IEnumerable<T>
    {
	struct Enumerator : IEnumerator<T> {
	      HashSet<T> parent;
	      int iter;
	      T currentElem;
	      HashSet<T> currentSet;
	      public Enumerator(HashSet<T> h) { 
	          parent = h; 
		  iter = -1; 
		  currentSet = parent.New();
		  currentElem = default(T);
	      }
	      public bool MoveNext() { 
	          iter = iter + 1; 
		  if (iter >= parent.Count) 
		      return false;
		  currentElem = parent.Random();
		  Contract.Assume(parent.Contains(currentElem) && !currentSet.Contains(currentElem));
		  currentSet.Add(currentElem);
		  return true;
	      }
	      public T Current { get { return currentElem; } }
	      public void Dispose() {}
	      public void Reset() {
	          iter = -1;
   		      currentSet = parent.New();
          }
          object System.Collections.IEnumerator.Current { get { return this.Current; } }
	}

        private HashSet(bool dontCallMe) { }
        public IEnumerator<T> GetEnumerator() { return new Enumerator(this); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
	public abstract int Count { get; }
	public abstract bool Add(T t);	
	public abstract bool Contains(T t);
	public abstract T Random();
	public abstract HashSet<T> New();
    }
}

namespace System.Linq {
     public static class Enumerable {
        public static int Sum(this IEnumerable<int> source) {
	    int sum = 0;
	    var e = source.GetEnumerator();
	    while (e.MoveNext()) {
	        sum += e.Current;
	    }
	    return sum;
	}

        public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector) {
	    int sum = 0;
	    var e = source.GetEnumerator();
	    while (e.MoveNext()) {
	        sum += selector(e.Current);
	    }
	    return sum;
	}

	class SelectEnumerable<T,U> : IEnumerable<U> {
	      private IEnumerable<T> origEnum;
	      private Func<T,U> func;
	      public SelectEnumerable(IEnumerable<T> origEnum, Func<T,U> func) { this.origEnum = origEnum; this.func = func; }
	      public IEnumerator<U> GetEnumerator() { return new SelectEnumerator<T,U>(this.origEnum.GetEnumerator(), this.func); }
              System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
	}

	class SelectEnumerator<T,U> : IEnumerator<U> {
              private IEnumerator<T> currentEnumerator;
	      private Func<T,U> func;
	      public SelectEnumerator(IEnumerator<T> origEnum, Func<T,U> func) { this.currentEnumerator = origEnum; this.func = func; }
	      public bool MoveNext() { return this.currentEnumerator.MoveNext(); }
	      public U Current { get { return this.func(this.currentEnumerator.Current); } }
	      public void Dispose() {}
	      public void Reset() { this.currentEnumerator.Reset(); }
              object System.Collections.IEnumerator.Current { get { return this.Current; } }
	}

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) {
	       return new SelectEnumerable<TSource, TResult>(source, selector);
        }

	class WhereEnumerable<T> : IEnumerable<T> {
	      private IEnumerable<T> origEnum;
	      private Func<T,bool> func;
	      public WhereEnumerable(IEnumerable<T> origEnum, Func<T,bool> func) { this.origEnum = origEnum; this.func = func; }
	      public IEnumerator<T> GetEnumerator() { return new WhereEnumerator<T>(this.origEnum.GetEnumerator(), this.func); }
              System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
	}

	class WhereEnumerator<T> : IEnumerator<T> {
              private IEnumerator<T> currentEnumerator;
	      private Func<T,bool> func;
	      public WhereEnumerator(IEnumerator<T> origEnum, Func<T,bool> func) { this.currentEnumerator = origEnum; this.func = func; }
	      public bool MoveNext() { 
	      	  while (true) { 
		      bool b = this.currentEnumerator.MoveNext(); 
		      if (!b || this.func(this.currentEnumerator.Current)) {
		          return b;
  		      }
		  }
	      }
	      public T Current { get { return this.currentEnumerator.Current; } }
	      public void Dispose() {}
	      public void Reset() { this.currentEnumerator.Reset(); }
              object System.Collections.IEnumerator.Current { get { return this.Current; } }
	}

	public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
	       return new WhereEnumerable<TSource>(source, predicate);
	}

        public static TSource First<TSource>(this IEnumerable<TSource> source) {
	       IEnumerator<TSource> enumerator = source.GetEnumerator();
	       if (enumerator.MoveNext()) {
	           return enumerator.Current;
	       }
	       throw new System.InvalidOperationException();
	}

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source) {
	       IEnumerator<TSource> enumerator = source.GetEnumerator();
	       if (enumerator.MoveNext()) {
	           return enumerator.Current;
	       }
	       return default(TSource);
	}

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
	       IEnumerator<TSource> enumerator = source.GetEnumerator();
	       while (enumerator.MoveNext()) {
	           if (predicate(enumerator.Current))
	               return enumerator.Current;
	       }
	       return default(TSource);
	}

        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source) {
	       IEnumerator<TSource> enumerator = source.GetEnumerator();
	       TSource elem = default(TSource);
	       if (!enumerator.MoveNext()) {
	           return elem;
	       }
	       elem = enumerator.Current;
	       if (!enumerator.MoveNext()) {
	           return elem;
	       }
	       throw new System.InvalidOperationException();
	}

        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
	       IEnumerator<TSource> enumerator = source.GetEnumerator();
	       TSource elem = default(TSource);
	       bool found = false;
	       while (enumerator.MoveNext()) {
	           if (!predicate(enumerator.Current)) continue;
		   if (found) throw new System.InvalidOperationException();
	           elem = enumerator.Current;
		   found = true;
	       }
               return elem;
	}

     }
}
