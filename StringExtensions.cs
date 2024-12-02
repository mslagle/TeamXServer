public static class StringExtensions
{
    /// <summary>
    /// Computes a stable hash code for the given string.
    /// </summary>
    /// <param name="str">The input string.</param>
    /// <returns>A stable hash code.</returns>
    public static int GetStableHashCode(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return 0;

        unchecked
        {
            int hash = 23;
            foreach (char c in str)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }
}