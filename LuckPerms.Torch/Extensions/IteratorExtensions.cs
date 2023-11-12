using System;
using System.Collections;
using System.Collections.Generic;
using java.util;

namespace LuckPerms.Torch.Extensions;

public static class IteratorExtensions
{
    public static IteratorEnumerator<object> GetEnumerator(this Iterator iterator) => new(iterator);
    
    public static IteratorEnumerable<T> AsEnumerable<T>(this Iterator iterator) => new(iterator);

    public struct IteratorEnumerator<T>(Iterator iterator) : IEnumerator<T>
    {
        public bool MoveNext()
        {
            if (!iterator.hasNext()) return false;
            
            Current = (T)iterator.next();
            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        object? IEnumerator.Current => Current;

        public T Current { get; private set; }

        public void Dispose()
        {
        }
    }
    
    public struct IteratorEnumerable<T>(Iterator iterator) : IEnumerable<T>
    {
        public IteratorEnumerator<T> GetEnumerator() => new(iterator);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => null!;
    }
}