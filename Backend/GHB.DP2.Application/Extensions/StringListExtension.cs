namespace GHB.DP2.Application.Extensions
{
    public static class StringListExtension
    {
        public static string JoinWithLastPrefix(
            this IEnumerable<string> words,
            string? lastPrefix,
            string splitWord = " ")
        {
            var wordListSource = words.ToList();

            if (!wordListSource.Any())
            {
                return string.Empty;
            }

            var wordList = wordListSource
                           .Where(w => !string.IsNullOrWhiteSpace(w))
                           .ToList();

            return wordList.Count switch
            {
                1 => wordList[0],
                2 => $"{wordList[0]} {lastPrefix}{wordList[1]}",
                _ => string.Join(splitWord, wordList[..^1]) + $"{splitWord} {lastPrefix}{wordList[^1]}",
            };
        }
    }
}