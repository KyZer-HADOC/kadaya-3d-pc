namespace Kadaya
{
    public static class SpriteFontText
    {
        public static string ToSupportedGlyphs(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            char[] characters = text.ToCharArray();
            for (int i = 0; i < characters.Length; i++)
            {
                char character = characters[i];
                if (character >= ' ' && character <= '~') continue;

                characters[i] = character switch
                {
                    '\u2191' => '^', // up arrow
                    '\u2193' => 'v', // down arrow
                    '\u2190' => '<', // left arrow
                    '\u2192' => '>', // right arrow
                    '\u00b7' => '|', // middle dot
                    '\u2014' => '-', // em dash
                    '\u2013' => '-', // en dash
                    '\u2018' or '\u2019' => '\'',
                    '\u201c' or '\u201d' => '"',
                    _ => '?'
                };
            }

            return new string(characters);
        }
    }
}
