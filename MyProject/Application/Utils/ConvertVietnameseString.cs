using System.Globalization;
using System.Text;

namespace Application.Utils
{
    public static class ConvertVietnameseString
    {
        public static string RemoveDiacritics(string text)
        {
            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder result = new StringBuilder();

            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

                // Replace "đ" and "d" with "d"
                if (c == 'đ' || c == 'Đ')
                {
                    result.Append('d');
                }
                else if (category != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}
