using System;

namespace ModestTree
{
    public static class MiscExtensions
    {
        // We'd prefer to use the name Format here but that conflicts with
        // the existing string.Format method
        public static string Fmt(this string s, params object[] args)
        {
            // Do in-place change to avoid the memory alloc
            // This should be fine because the params is always used instead of directly
            // passing an array
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                args[i] = arg switch
                {
                    // This is much more understandable than just the empty string
                    null => "NULL",
                    // This often reads much better sometimes
                    Type type => type.PrettyName(),
                    _ => args[i]
                };
            }

            return string.Format(s, args);
        }
    }
}