using System.Text.Json.Serialization;

namespace Domain.Entities.Base
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = new Guid();

        public DateTime CreationDate { get; set; }
        [JsonIgnore]
        public Guid? CreatedBy { get; set; }
        [JsonIgnore]
        public DateTime? ModificationDate { get; set; }
        [JsonIgnore]
        public Guid? ModificationBy { get; set; }
        [JsonIgnore]
        public DateTime? DeletionDate { get; set; }
        [JsonIgnore]
        public Guid? DeleteBy { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
