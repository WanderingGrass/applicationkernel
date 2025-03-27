// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Todd.Applicationkernel.Core.Abstractions
{
    public static class Utils
    {
        /// <summary>
        /// Returns a human-readable text string that describes an IEnumerable collection of objects.
        /// </summary>
        /// <typeparam name="T">The type of the list elements.</typeparam>
        /// <param name="collection">The IEnumerable to describe.</param>
        /// <param name="toString">Converts the element to a string. If none specified, <see cref="object.ToString"/> will be used.</param>
        /// <param name="separator">The separator to use.</param>
        /// <param name="putInBrackets">Puts elements within brackets</param>
        /// <returns>A string assembled by wrapping the string descriptions of the individual
        /// elements with square brackets and separating them with commas.</returns>
        public static string EnumerableToString<T>(IEnumerable<T>? collection, Func<T, string>? toString = null,
                                                        string separator = ", ", bool putInBrackets = true)
        {
            if (collection == null)
                return putInBrackets ? "[]" : "null";

            if (collection is ICollection<T> { Count: 0 })
                return putInBrackets ? "[]" : "";

            var enumerator = collection.GetEnumerator();
            if (!enumerator.MoveNext())
                return putInBrackets ? "[]" : "";

            var firstValue = enumerator.Current;
            if (!enumerator.MoveNext())
            {
                return putInBrackets
                    ? toString != null ? $"[{toString(firstValue)}]" : firstValue == null ? "[null]" : $"[{firstValue}]"
                    : toString != null ? toString(firstValue) : firstValue == null ? "null" : (firstValue.ToString() ?? "");
            }

            var sb = new StringBuilder();
            if (putInBrackets) sb.Append('[');

            if (toString != null) sb.Append(toString(firstValue));
            else if (firstValue == null) sb.Append("null");
            else sb.Append($"{firstValue}");

            do
            {
                sb.Append(separator);

                var value = enumerator.Current;
                if (toString != null) sb.Append(toString(value));
                else if (value == null) sb.Append("null");
                else sb.Append($"{value}");
            } while (enumerator.MoveNext());

            if (putInBrackets) sb.Append(']');
            return sb.ToString();
        }
    }
}
