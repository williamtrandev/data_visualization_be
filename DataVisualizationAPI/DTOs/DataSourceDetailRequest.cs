namespace DataVisualizationAPI.DTOs
{
    public class DataSourceDetailRequest
    {
        private int _page = 1;
        private int _pageSize = 10;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 10 : value > 100 ? 100 : value;
        }

        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public List<FilterParameter>? Filters { get; set; }
    }
} 