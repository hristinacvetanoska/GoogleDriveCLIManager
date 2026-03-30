namespace GoogleDriveCLIManager.Helpers
{
    /// <summary>
    /// Provides utility methods for safe rendering of text in Spectre.Console.
    /// </summary>
    public static class MarkupHelper
    {
        /// <summary>
        /// Escapes special Spectre.Console markup characters in a string.
        /// Prevents bracket characters from being interpreted as markup tags.
        /// </summary>
        /// <param name="text">The raw text to escape.</param>
        /// <returns>
        /// A string safe for use in Spectre.Console markup rendering.
        /// Example: "[Report] Final" becomes "[[Report]] Final"
        /// </returns>
        public static string Escape(string text)
            => text.Replace("[", "[[").Replace("]", "]]");
    }
}