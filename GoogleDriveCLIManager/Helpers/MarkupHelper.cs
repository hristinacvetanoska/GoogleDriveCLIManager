namespace GoogleDriveCLIManager.Helpers
{
    public static class MarkupHelper
    {
        public static string Escape(string text)
            => text.Replace("[", "[[").Replace("]", "]]");
    }
}