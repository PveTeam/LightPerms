using java.io;
using JavaIOException = java.io.IOException;
using SystemIOException = System.IO.IOException;

namespace LuckPerms.Torch.Utils.Extensions;

public static class StreamExtensions
{
    public static InputStream GetInputStream(this Stream stream)
    {
        if (!stream.CanRead)
            throw new ArgumentException("Stream should be readable", nameof(stream));

        return new WrapperInputStream(stream);
    }

    private sealed class WrapperInputStream(Stream stream) : InputStream
    {
        public override int read()
        {
            try
            {
                return stream.ReadByte();
            }
            catch (SystemIOException e)
            {
                throw new JavaIOException(e.Message, e);
            }
        }

        public override int read(byte[] b, int off, int len)
        {
            try
            {
                return stream.Read(b, off, len);
            }
            catch (SystemIOException e)
            {
                throw new JavaIOException(e.Message, e);
            }
        }

        public override long skip(long n)
        {
            if (!stream.CanSeek) return base.skip(n);
            
            try
            {
                return stream.Seek(n, SeekOrigin.Current);
            }
            catch (SystemIOException e)
            {
                throw new JavaIOException(e.Message, e);
            }
        }

        public override int available()
        {
            try
            {
                return (int)(stream.Length - stream.Position);
            }
            catch (SystemIOException e)
            {
                throw new JavaIOException(e.Message, e);
            }
        }

        public override void reset()
        {
            if (!stream.CanSeek)
            {
                base.reset();
                return;
            }
            
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            catch (SystemIOException e)
            {
                throw new JavaIOException(e.Message, e);
            }
        }

        public override void close()
        {
            stream.Dispose();
        }
    }
}