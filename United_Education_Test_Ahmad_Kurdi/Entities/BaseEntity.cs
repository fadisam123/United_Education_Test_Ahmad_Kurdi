namespace United_Education_Test_Ahmad_Kurdi.Domain
{
    public class BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; } = null;
    }
}
