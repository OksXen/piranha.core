using Piranha.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PiranhaLucene.Search.Services
{
    public interface ILuceneSearchService
    {
        Task<List<Guid>> SearchByKey(string searchKey);
        Task<List<Guid>> SearchByTag(string tagKey);

        Task RebuildIndex(List<PageBase> pages);
        Task RebuildIndex(List<PostBase> posts);
    }
}
