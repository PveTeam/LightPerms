using System.Collections.Generic;
using System.Linq;
using java.util;

namespace LuckPerms.Torch.Extensions;

public static class EnumerableExtensions
{
    public static Collection ToCollection<T>(this IEnumerable<T> enumerable)
    {
        var collection = enumerable is IReadOnlyCollection<T> readOnlyCollection ? new ArrayList(readOnlyCollection.Count) : new ArrayList(((IReadOnlyCollection<T>)(enumerable = enumerable.ToArray())).Count);
        
        foreach (var t in enumerable)
        {
            collection.add(t);
        }
        
        return collection;
    }
}