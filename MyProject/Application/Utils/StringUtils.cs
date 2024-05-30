using System.Globalization;
using System.Text;

namespace Application.Utils
{
    public static class StringUtils
    {
        public static string Hash(this string input)
        {
            // todo hash the string
            return input;
        }
        public static string RemoveDiacritics(string text)
        {
            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder result = new StringBuilder();

            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (c == 'đ' || c == 'Đ')
                {
                    result.Append('d');
                }
                else if (category != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            return result.ToString().ToUpper().Replace(" ", "");
        }
    }
}
