using Domain.Entities.Base;
using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Domain.Entities
{
    public class Product : BaseEntity
    {
        [ForeignKey("Category")]
        public Guid CategoryId { get; set; }
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NameUnsign = RemoveDiacritics(value);
            }
        }
        public string Code { get; set; }
        public string NameUnsign { get; private set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public bool isDisable { get; set; }
        public DeliverySize? DeliverySize { get; set; }
        public virtual Category Category { get; set; }
        [JsonIgnore]
        public IList<OrderDetail> OrderDetails { get; set; }
        public IList<ProductImage> ProductImages { get; set; }
        private string RemoveDiacritics(string text)
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

            return result.ToString();
        }
    }
}
