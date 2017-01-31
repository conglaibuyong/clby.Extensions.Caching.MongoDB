using System;
using System.Diagnostics;

namespace clby.Extensions.Misc
{
    [DebuggerStepThrough]
    public static class Ensure
    {
        public static T IsNotNull<T>(T value, string paramName) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName, "Value cannot be null.");
            }
            return value;
        }
        public static T IsGreaterThanOrEqualTo<T>(T value, T comparand, string paramName) where T : IComparable<T>
        {
            if (value.CompareTo(comparand) < 0)
            {
                var message = string.Format("Value is not greater than or equal to {1}: {0}.", value, comparand);
                throw new ArgumentOutOfRangeException(paramName, message);
            }
            return value;
        }
    }
}
