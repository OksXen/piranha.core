using Microsoft.Extensions.DependencyInjection;
using Piranha;
using PiranhaLucene.Search.Services;

namespace PiranhaLucene.Search
{
    public static class PiranhaLuceneSearchExtensions
    {
        /// <summary>
        /// Adds the Azure Search module.
        /// </summary>
        /// <param name="serviceBuilder">The service builder</param>
        /// <param name="serviceName">The unique name of the azure search service</param>
        /// <param name="apiKey">The admin api key</param>
        /// <returns>The services</returns>
        public static PiranhaServiceBuilder UseLuceneSearch(this PiranhaServiceBuilder serviceBuilder)
        {
            serviceBuilder.Services.AddPiranhaLuceneSearch();

            return serviceBuilder;
        }

        /// <summary>
        /// Adds the Azure Search module.
        /// </summary>
        /// <param name="services">The current service collection</param>
        /// <param name="serviceName">The unique name of the azure search service</param>
        /// <param name="apiKey">The admin api key</param>
        /// <returns>The services</returns>
        public static IServiceCollection AddPiranhaLuceneSearch(this IServiceCollection services)
        {
            // Add the identity module
            App.Modules.Register<Module>();

            // Register the search service
            services.AddSingleton<ISearch, LuceneSearchService>();
            services.AddSingleton<ILuceneSearchService, LuceneSearchService>();        

            return services;
        }
    }
}
