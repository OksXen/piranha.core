using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Microsoft.Extensions.Configuration;
using Piranha;
using Piranha.Extend;
using Piranha.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LuceneField = Lucene.Net.Documents.Field;
using LuceneStore = Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;

namespace PiranhaLucene.Search.Services
{
    public class LuceneSearchService : ISearch, ILuceneSearchService
    {        
        private const Version APP_LUCENE_VERSION = Version.LUCENE_30;
        private readonly StandardAnalyzer _analyzer;
        private readonly LuceneStore.Directory _directory;

        public LuceneSearchService(IConfiguration configuration)
        {
            var baseDirectory = configuration.GetSection("Lucene:BasePathDirectory").Value;
            var currentDirectory = Directory.GetCurrentDirectory();
            var indexPath = Path.Combine(currentDirectory, baseDirectory);

            if (!Directory.Exists(indexPath))
            {
                Directory.CreateDirectory(indexPath);
            }
         
            _directory = LuceneStore.FSDirectory.Open(indexPath);

            // Create an analyzer to process the text
            _analyzer = new StandardAnalyzer(APP_LUCENE_VERSION);
        }

        public async Task DeletePageAsync(PageBase page)
        {
            await Task.Run(() =>
            {                               
                // Create an index writer                
                using var writer = new IndexWriter(_directory, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                var term = new Term("contentId", page.Id.ToString());
                writer.DeleteDocuments(term);
                writer.Optimize();
                writer.Flush(triggerMerge: false, flushDocStores: false, flushDeletes: true);
                writer.Dispose();
            });
        }

        public async Task DeletePostAsync(PostBase post)
        {
            await Task.Run(() =>
            {
                using var writer = new IndexWriter(_directory, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                Term term = new Term("contentId", post.Id.ToString());
                writer.DeleteDocuments(term);
                writer.Optimize();
                writer.Flush(triggerMerge: false, flushDocStores: false, flushDeletes: true);
                writer.Dispose();
            });
        }

        public async Task SavePageAsync(PageBase page)
        {
            await Task.Run(() =>
            {                                
                using var writer = new IndexWriter(_directory, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                var doc = CreateLuceneDocument(page);
                writer.AddDocument(doc);
                writer.Optimize();
                writer.Flush(triggerMerge: false, flushDocStores: false, flushDeletes: true);
                writer.Dispose();
            });
        }

        public async Task SavePostAsync(PostBase post)
        {
            await Task.Run(() =>
            {
                using var writer = new IndexWriter(_directory, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                var doc = CreateLuceneDocument(post);
                writer.AddDocument(doc);
                writer.Optimize();
                writer.Flush(triggerMerge: false, flushDocStores: false, flushDeletes: true);
                writer.Dispose();
            });
        }

        public async Task<List<Guid>> SearchByKey(string searchKey)
        {
            var guids = new List<Guid>();
            await Task.Run(() =>
            {
                // Create an index writer                
                using var writer = new IndexWriter(_directory, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

                using var reader = writer.GetReader();
                using var searcher = new IndexSearcher(reader);

                //Use QueryParser for "ANALYZED" fields
              //  var queryParser = new QueryParser(APP_LUCENE_VERSION, "title", _analyzer);

                var fields = new List<string>()
                {
                    "title",
                    "slug",
                    "body",
                    "tags",
                    "category"
                };
               
                var multiFieldParser = new MultiFieldQueryParser(APP_LUCENE_VERSION, fields.ToArray(), _analyzer);
                Query multiFieldQuery = multiFieldParser.Parse(searchKey);
                BooleanQuery innerExpr = new BooleanQuery();
                Term term = new Term("contentType", "post");
                TermQuery termQuery = new TermQuery(term);

                innerExpr.Add(multiFieldQuery, Occur.SHOULD);
                innerExpr.Add(termQuery, Occur.MUST);

                var result = searcher.Search(multiFieldQuery, 100);
                foreach (var item in result.ScoreDocs)
                {
                    var guid = reader.Document(item.Doc).GetField("contentId").StringValue;
                    guids.Add(new Guid(guid));
                }

                searcher.Dispose();
                reader.Dispose();
                writer.Dispose();
            });
            return guids;
        }

        public async Task<List<Guid>> SearchByTag(string tagKey)
        {
            var guids = new List<Guid>();
            await Task.Run(() =>
            {
                // Create an index writer                
                using var writer = new IndexWriter(_directory, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

                using var reader = writer.GetReader();
                using var searcher = new IndexSearcher(reader);

                //Use QueryParser for "ANALYZED" fields
                //  var queryParser = new QueryParser(APP_LUCENE_VERSION, "title", _analyzer);

                var fields = new List<string>()
                {                    
                    "tags"
                };

                var multiFieldParser = new MultiFieldQueryParser(APP_LUCENE_VERSION, fields.ToArray(), _analyzer);
                Query multiFieldQuery = multiFieldParser.Parse(tagKey);
                BooleanQuery innerExpr = new BooleanQuery();
                Term term = new Term("contentType", "post");
                TermQuery termQuery = new TermQuery(term);

                innerExpr.Add(multiFieldQuery, Occur.SHOULD);
                innerExpr.Add(termQuery, Occur.MUST);

                var result = searcher.Search(multiFieldQuery, 100);
                foreach (var item in result.ScoreDocs)
                {
                    var guid = reader.Document(item.Doc).GetField("contentId").StringValue;
                    guids.Add(new Guid(guid));
                }

                searcher.Dispose();
                reader.Dispose();
                writer.Dispose();
            });
            return guids;
        }

        Document CreateLuceneDocument(PageBase page)
        {
            var cleaned = GetCleanedBodyFromBlocks(page.Blocks);
            var doc = new Document();
            doc.Add(CreateField("contentId", page.Id.ToString()));
            doc.Add(CreateField("contentType", "page"));
            doc.Add(CreateField("slug", page.Slug));
            doc.Add(CreateField("title", page.Title));
            doc.Add(CreateField("body", cleaned));
            doc.Add(CreateField("published", page.Published.ToString()));
            return doc;
        }

        LuceneField CreateField(string fieldName, string fieldValue)
        {
            var field = new LuceneField(fieldName, string.IsNullOrEmpty(fieldValue) ? "" : fieldValue, LuceneField.Store.YES, LuceneField.Index.ANALYZED);
            return field;
        }

        string GetCleanedBodyFromBlocks(IList<Block> blocks)
        {
            var body = new StringBuilder();
            foreach (var block in blocks)
            {
                if (block is ISearchable searchableBlock)
                {
                    body.AppendLine(searchableBlock.GetIndexedContent());
                }
            }

            var cleanHtml = new Regex("<[^>]*(>|$)");
            var cleanSpaces = new Regex("[\\s\\r\\n]+");

            var cleaned = cleanSpaces.Replace(cleanHtml.Replace(body.ToString(), " "), " ").Trim();

            return cleaned;
        }

        Document CreateLuceneDocument(PostBase post)
        {
            var cleaned = GetCleanedBodyFromBlocks(post.Blocks);            
            var doc = new Document();            
            doc.Add(CreateField("contentId", post.Id.ToString()));
            doc.Add(CreateField("contentType", "post"));
            doc.Add(CreateField("slug", post.Slug));
            doc.Add(CreateField("title", post.Title));
            doc.Add(CreateField("body", cleaned));
            doc.Add(CreateField("tags", string.Join(" ", post.Tags.Select(t => t.Title).ToList())));
            doc.Add(CreateField("category", post.Category.Title));
            doc.Add(CreateField("published", post.Published.ToString()));
            return doc;
        }

        public async Task RebuildIndex(List<PageBase> pages)
        {
            await Task.Run(() =>
            {
                using var writer = new IndexWriter(_directory, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                var term = new Term("contentType", "page");
                writer.DeleteDocuments(term);
                writer.Optimize();
                writer.Flush(triggerMerge: false, flushDocStores: false, flushDeletes: true);
                writer.Dispose();
            });

            foreach (var p in pages)
            {
                await SavePageAsync(p);
            }
        }



        public async Task RebuildIndex(List<PostBase> posts)
        {
            await Task.Run(() =>
            {
                using var writer = new IndexWriter(_directory, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                var term = new Term("contentType", "post");
                writer.DeleteDocuments(term);
                writer.Optimize();
                writer.Flush(triggerMerge: false, flushDocStores: false, flushDeletes: true);
                writer.Dispose();
            });

            foreach (var p in posts)
            {
                await SavePostAsync(p);
            }
        }

    }
}
