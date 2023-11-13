namespace heh;

// https://github.com/ServiceStack/ServiceStack.Text/blob/master/src/ServiceStack.Text/StringExtensions.cs
public static class StringExtensions
{
    public static bool Glob(this string value, string pattern)
    {
        int pos;
        for (pos = 0; pattern.Length != pos; pos++)
        {
            switch (pattern[pos])
            {
                case '?':
                    break;

                case '*':
                    for (var i = value.Length; i >= pos; i--)
                    {
                        if (Glob(value.Substring(i), pattern.Substring(pos + 1)))
                            return true;
                    }
                    return false;

                default:
                    if (value.Length == pos || char.ToUpper(pattern[pos]) != char.ToUpper(value[pos]))
                    {
                        return false;
                    }
                    break;
            }
        }

        return value.Length == pos;
    }
}
