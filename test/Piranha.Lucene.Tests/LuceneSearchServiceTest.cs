using BalitaOnline.Core.Models;
using Microsoft.Extensions.Configuration;
using PiranhaLucene.Search.Services;
using System;
using System.IO;
using Xunit;

namespace Piranha.Lucene.Tests
{
    public class LuceneSearchServiceTest
    {
        [Fact]
        public async void SearchPageByTitle_Test()
        {
            var currentDir = Directory.GetCurrentDirectory();

            IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(currentDir)
            .AddJsonFile("appsettings.json")
            .Build();

            var luceneService = new LuceneSearchService(configuration);

            var title = "title of the page";
            var searchKey1 = "page";
            var searchKey2 = "title";
            var page = CreateStandardPage(title);
            var guid = page.Id;

            await luceneService.SavePageAsync(page);
            var result1 = await luceneService.SearchByKey(searchKey1);
            var result2 = await luceneService.SearchByKey(searchKey2);

            var found1 = result1.Exists(g => g == guid);
            var found2 = result2.Exists(g => g == guid);
            Assert.True(found1);
            Assert.True(found2);
        }

        [Fact]
        public async void SearchPageBySlug_Test()
        {
            var currentDir = Directory.GetCurrentDirectory();

            IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(currentDir)
            .AddJsonFile("appsettings.json")
            .Build();

            var luceneService = new LuceneSearchService(configuration);

            var title = "title of the page";            
            var page = CreateStandardPage(title);
            page.Slug = "slugname";
            var guid = page.Id;

            await luceneService.SavePageAsync(page);
            var result1 = await luceneService.SearchByKey(page.Slug);
            

            var found1 = result1.Exists(g => g == guid);
            
            Assert.True(found1);
            
        }

        [Fact]
        public async void SearchPostByCategory_Test()
        {
            var currentDir = Directory.GetCurrentDirectory();

            IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(currentDir)
            .AddJsonFile("appsettings.json")
            .Build();

            var luceneService = new LuceneSearchService(configuration);

            var title = "title of the post";
            var post = CreateNewsArticlePost(title);
            post.Category = new Models.Taxonomy() { Title = "category name" };
            var guid = post.Id;

            await luceneService.SavePostAsync(post);
            var result1 = await luceneService.SearchByKey(post.Category.Title);


            var found1 = result1.Exists(g => g == guid);

            Assert.True(found1);

        }

        static StandardPage CreateStandardPage(string title)
        {
            var page = new StandardPage()
            {
                Title = title,
                Id = Guid.NewGuid()
            };

            return page;
        }


        static NewsArticlePost CreateNewsArticlePost(string title)
        {
            var post = new NewsArticlePost()
            {
                Title = title,
                Id = Guid.NewGuid()
            };

            return post;
        }
    }
}
