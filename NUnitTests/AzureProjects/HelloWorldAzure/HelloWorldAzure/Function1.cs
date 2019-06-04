using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AzureStubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace HelloWorld
{
    // only used to get the assembly containing this classs from the nunitests
    public class ReferenceToHelloWorldDll {}

    public class Named
    {
        public string Name;
    }
    public static class Function1
    {
        /*[FunctionName("Function1")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string name = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;

            if (name == null)
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                name = data?.name;
            }

            return name == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);
        }*/

        [FunctionName("Function1")]
        public static HttpResponseMessage Run_NoBugs([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string name = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;

            if (name == null)
                name = req.Content.ReadContentStub();

            HttpResponseMessage result = name == null
                ? CreateHttpResponseMessage(req, HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : CreateHttpResponseMessage(req, HttpStatusCode.OK, "Hello " + name);

            Contract.Assert(result.StatusCode == HttpStatusCode.OK || result.StatusCode == HttpStatusCode.BadRequest);
            Contract.Assert((name == null || result.StatusCode == HttpStatusCode.OK));
            //Contract.Assert(false);
            return result;
        }

        [FunctionName("Function2")]
        public static HttpResponseMessage Run_Bugged([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string name = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;

            if (name == null)
                name = req.Content.ReadContentStub();

            HttpResponseMessage result = name == null
                ? CreateHttpResponseMessage(req, HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : CreateHttpResponseMessage(req, HttpStatusCode.OK, "Hello " + name);

            //Contract.Assert(result.StatusCode == HttpStatusCode.OK || result.StatusCode == HttpStatusCode.BadRequest);
            Contract.Assert(!(name == null || result.StatusCode == HttpStatusCode.OK));
            //Contract.Assert(false);
            return result;
        }


        // var Status_Field : [Ref]int;
        //procedure boogie_si_record_int(x: int);
        //procedure boogie_si_record_Ref(x: Ref);

        private static HttpResponseMessage CreateHttpResponseMessage(HttpRequestMessage req, HttpStatusCode state, string msg)
        {
            var r = req.CreateResponse(state, msg);
            Contract.Assume(r.StatusCode == state);
            return r;
        }
    }
}
