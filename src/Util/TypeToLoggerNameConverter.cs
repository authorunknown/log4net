using System;
using System.Text;

namespace log4net.Util
{
    /// <summary>
    /// Lookup the corresponding logger name for a given <see cref="T:System.Type"/>
    /// </summary>
    public class TypeToLoggerNameConverter
    {
        /// <summary>
        /// Gets the logger name that corresponds to the given type.  Generic type
        /// parameters are stripped.
        /// </summary>
        public static string GetLoggerName(Type type)
        {
            string fullName = type.FullName;
            int i = fullName.IndexOf('`');
            if (i > 0)
            {
                fullName = fullName.Substring(0, i);
            }
            return fullName;
        }
    }
}
