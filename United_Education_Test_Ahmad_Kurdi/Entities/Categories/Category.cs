using StackExchange.Redis;
using United_Education_Test_Ahmad_Kurdi.Domain.Models;

namespace United_Education_Test_Ahmad_Kurdi.Domain.Categories
{
    public class Category : BaseEntity
    {
        #region Properties
        public string Name { get; set; } = null!;
        #endregion

        #region Navigation Properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
        #endregion
    }
}
