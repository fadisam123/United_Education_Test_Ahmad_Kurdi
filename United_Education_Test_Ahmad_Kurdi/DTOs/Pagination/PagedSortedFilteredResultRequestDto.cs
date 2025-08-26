using Microsoft.AspNetCore.Mvc;

namespace United_Education_Test_Ahmad_Kurdi.DTOs.Pagination
{
    public class PagedSortedFilteredResultRequestDto
    {
        [FromQuery]
        public string? Filter { get; set; } = null;
        [FromQuery]
        public string? SortColumn { get; set; } = null;
        [FromQuery]
        public string SortOrder { get; set; } = "desc";
        [FromQuery]
        public int Page { get; set; } = 1;
        [FromQuery]
        public int? PageSize { get; set; } = null;
    }
}
