using System;
using System.IO;
using java.io;
using Torch.Utils;

namespace LuckPerms.Torch.Extensions;

public static class StreamExtensions
{
    public static InputStream GetInputStream(this Stream stream)
    {
        if (!stream.CanRead)
            throw new ArgumentException("Stream should be readable", nameof(stream));

        var array = stream.ReadToEnd(); // TODO make it not allocate array for an entire stream content

        return new ByteArrayInputStream(array);
    }
}