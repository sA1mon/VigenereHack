namespace VigenereHack
{
    using System;
    using System.Linq;
    using System.Text;

    public static class Utils
    {
        private static readonly char[] Alphabet = { 'а', 'б', 'в', 'г', 'д', 'е', 'ё', 'ж', 'з', 'и', 'й',
            'к', 'л', 'м', 'н', 'о', 'п', 'р', 'с', 'т', 'у', 'ф', 'х', 'ц', 'ч', 'ш',
            'щ', 'ъ', 'ы', 'ь', 'э', 'ю', 'я', ' ', ',', '.'};

        private static readonly string[] Books = { "war.txt", "anna.txt" };

        public static char[] GetAlphabet()
        {
            return Alphabet;
        }

        public static string[] GetBooksFileArray()
        {
            return Books;
        }

        public static int GetCharAlphabetIndex(char ch)
        {
            return Alphabet.ToList().FindIndex(x => x.Equals(ch));
        }

        public static char ShiftChar(char ch, int shift)
        {
            var alphaList = Alphabet.ToList();
            var index = alphaList.FindIndex(x => x == ch);

            return alphaList.ElementAt((index + shift) % alphaList.Count);
        }

        public static string GetSeparator(char separatorChar, int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be greater than 0.");

            var separator = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                separator.Append(separatorChar);
            }

            return separator.ToString();
        }
    }
}