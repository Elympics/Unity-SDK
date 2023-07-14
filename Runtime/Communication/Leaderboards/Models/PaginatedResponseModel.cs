using System;
using System.Collections.Generic;

namespace Elympics
{
    [Serializable]
    internal class PaginatedResponseModel<T>
    {
        public List<T> data;
        public int pageNumber;
        public int pageSize;
        public int totalPages;
        public int totalRecords;

        public string firstPage;
        public string lastPage;
        public string nextPage;
        public string previousPage;
    }
}
