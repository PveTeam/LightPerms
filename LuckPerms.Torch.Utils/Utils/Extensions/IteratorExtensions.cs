using System.Collections;
using java.util;
using java.util.stream;

namespace LuckPerms.Torch.Utils.Extensions;

public static class IteratorExtensions
{
    public static StreamEnumerable<T> AsEnumerable<T>(this BaseStream stream) => new(stream);
    
    public static IteratorEnumerator<object> GetEnumerator(this BaseStream stream) => new(stream.iterator());

    public struct StreamEnumerable<T>(BaseStream stream) : IEnumerable<T>
    {
        public IteratorEnumerator<T> GetEnumerator() => new(stream.iterator());
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

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

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}