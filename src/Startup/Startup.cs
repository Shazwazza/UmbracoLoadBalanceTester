using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Caching;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Mvc;
using System.Web.Services.Description;
using Examine;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Sync;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using ActionFilterAttribute = System.Web.Http.Filters.ActionFilterAttribute;

namespace Umbraco.Web.UI.App_Code
{

    public class LoadBalancingConfigurationEventHandler : ApplicationEventHandler
    {
        /// <summary>
        /// Overridable method to execute when All resolvers have been initialized but resolution is not frozen so they can be modified in this method
        /// </summary>
        /// <param name="umbracoApplication"></param>
        /// <param name="applicationContext"></param>
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            //Replace the server messenger:

            ServerMessengerResolver.Current.SetServerMessenger(new BatchedDatabaseServerMessenger(
                applicationContext,
                UmbracoConfig.For.UmbracoSettings().DistributedCall.Enabled,
                //You can customize some options by setting the options parameters here:
                new DatabaseServerMessengerOptions
                {
                    //These callbacks will be executed if the server has not been synced
                    // (i.e. it is a new server or the lastsynced.txt file has been removed)
                    RebuildingCallbacks = new Action[]
                {
                    //rebuild the xml cache file if the server is not synced
                    () => global::umbraco.content.Instance.RefreshContentFromDatabase(),
                    //rebuild indexes if the server is not synced
                    // NOTE: This will rebuild ALL indexes including the members, if developers want to target specific 
                    // indexes then they can adjust this logic themselves.
                    () => Examine.ExamineManager.Instance.RebuildIndex()
                }
                }));

            //Replace the server registrar (this is optional but allows you to track active servers participating
            // in your load balanced environment):

            ServerRegistrarResolver.Current.SetServerRegistrar(new DatabaseServerRegistrar(
                new Lazy<ServerRegistrationService>(() => applicationContext.Services.ServerRegistrationService),
                //You can customize some options by setting the options parameters here:
                new DatabaseServerRegistrarOptions()));
        }
    }


    /// <summary>
    /// Ensures the CORS headers are there for this particular document
    /// </summary>
    public class LBTestDocController : RenderMvcController
    {

        /// <summary>
        /// Called after the action result that is returned by an action method is executed.
        /// </summary>
        /// <param name="filterContext">Information about the current request and action result</param>
        protected override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            base.OnResultExecuted(filterContext);

            filterContext.HttpContext.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
            filterContext.HttpContext.Response.Headers["Access-Control-Allow-Origin"] = "*";
        }
    }

    [CORSActionFilter]
    public class LBTestingKitController : UmbracoApiController
    {
        [System.Web.Http.HttpGet]
        public PublishedCounts GetPublishedCount()
        {
            var roots = Umbraco.ContentQuery.TypedContentAtRoot();
            return new PublishedCounts
            {
                DatabasePublished = Services.ContentService.CountPublished(),
                CachePublished = roots.DescendantsOrSelf<IPublishedContent>().Count()
            };
        }

        [System.Web.Http.HttpGet]
        public bool SearchInternalId(int id)
        {
            var criteria = ExamineManager.Instance.SearchProviderCollection["InternalSearcher"].CreateSearchCriteria().Id(id);
            var count = ExamineManager.Instance.SearchProviderCollection["InternalSearcher"].Search(criteria.Compile()).TotalItemCount;
            if (count != 1)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Item not found in index"));
            }
            return true;
        }

        [System.Web.Http.HttpGet]
        public bool SearchExternalId(int id)
        {
            var criteria = ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"].CreateSearchCriteria().Id(id);
            var count = ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"].Search(criteria.Compile()).TotalItemCount;
            if (count != 1)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Item not found in index"));
            }
            return true;
        }

        [System.Web.Http.HttpGet]
        public int HelloWorld()
        {
            return 1234;
        }

        [System.Web.Http.HttpGet]
        public int CreateContent()
        {
            var c = Services.ContentService.CreateContentWithIdentity("LB_" + Guid.NewGuid().ToString("N"), 5370, "LBTestDoc");
            var status = Services.ContentService.PublishWithStatus(c);
            if (!status)
            {
                throw new ApplicationException(status.Result.StatusType.ToString());
            }

            return c.Id;
        }

    }

    public class CORSActionFilterAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Occurs after the action method is invoked.
        /// </summary>
        /// <param name="actionExecutedContext">The action executed context.</param>
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);

            actionExecutedContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST");
            actionExecutedContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        }
    }

    public class CreateContentResult
    {
        public int ContentId { get; set; }
    }

    public class PublishedCounts
    {
        public int DatabasePublished { get; set; }
        public int CachePublished { get; set; }
    }
}