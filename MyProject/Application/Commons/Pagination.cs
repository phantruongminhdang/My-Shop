namespace Application.Commons
{
    public class Pagination<T>
    {
        public int TotalItemsCount { get; set; }
        public int PageSize { get; set; }
        public int TotalPagesCount
        {
            get
            {
                if (PageSize != 0)
                {
                    var temp = TotalItemsCount / PageSize;
                    if (TotalItemsCount % PageSize == 0)
                    {
                        return temp;
                    }
                    return temp + 1;
                }
                else
                    return 1;
            }
        }
        public int PageIndex { get; set; }

        public bool Next => PageIndex + 1 < TotalPagesCount;
        public bool Previous => PageIndex > 0;
        public List<T> Items { get; set; } = new List<T>();
    }
}
