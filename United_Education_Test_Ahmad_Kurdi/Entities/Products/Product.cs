using United_Education_Test_Ahmad_Kurdi.Domain.Categories;

namespace United_Education_Test_Ahmad_Kurdi.Domain.Models
{
    public class Product : BaseEntity
    {
        #region Properties
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public float Price { get; set; }
        #endregion

        #region Navigation Properties
        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; } = null!; // has name and id (Category.Name, Category.Id)
        #endregion
    }
}
