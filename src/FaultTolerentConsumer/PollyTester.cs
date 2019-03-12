using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FaultTolerentConsumer
{
    /*
     * - You may notice that each method creates its necessary policies to execute. 
         However this does not need to be so, policies can be shared and even injected (Policies are thread-safe)
         Furthermore such policies may be application-wide or even company-wide, they could then be exported as package.
         Also noteworthy is the fact that Polly also has it's own mechanism of sharing policies, thru the PolicyRegistry class (a concurrent dictionary) 
         and are then retrived by name: i.ex: PolicRegistry.Get<RetryPolicy<T>>(nameOfPolicy);
     */

    public class PollyTester
    {        
        /*
         * Retry policy expected idempotency, in other words if it fails the first time and succeeds at the n+100 time of a retry
         * it is expected that the value returned would be the same for every 'n' call.
         * 
         * For correctly built API this is true, except for POST calls.
         */
        public async Task CallUnreliableServerWithImmediateRetry(string endpoint)
        {
            // Polly does not alter responses, on a response matching the predicate it will check if a retry should be done and attempt another call
            // This behaviour can be verified for example using Fiddler, the console will emit a single correct value, but Fiddler will capture 4 Http calls.
            IAsyncPolicy<HttpResponseMessage> retryPolicy2Times =
                Policy
                    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .RetryAsync(2, onRetry: (response, retryCount) => {
                        Console.WriteLine("Response wasn't sucessful: " + getHttpStatusAsString(response.Result.StatusCode));
                        Console.WriteLine("Retry number: " + retryCount);
                    }); // Returns a AsyncRetryPolicy

            using (var client = new HttpClient())
            {
                var response = await retryPolicy2Times.ExecuteAsync(() => client.GetAsync(endpoint));
                Console.WriteLine("Call with status code: " + getHttpStatusAsString(response.StatusCode));

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Content of the response: " + content);
                }                                
            }
        }

        // This is a sensible option, similar to a jitter when delaying packages in 'real' world apps, 
        // retrying in the sequence may only clutter your log files or increase the already overloaded server
        public async Task CallUnreliableServerWithAwaitRetry(string endpoint)
        {
            IAsyncPolicy<HttpResponseMessage> retryPolicy2WithWait =
                Policy
                    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(2, 
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)/2), // An growing wait factor on each retry (1s, 2s, 4s)
                        onRetry: (response, retryCount) => {
                            Console.WriteLine("Time: " + DateTime.Now.TimeOfDay);
                            Console.WriteLine("Response wasn't sucessful: " + getHttpStatusAsString(response.Result.StatusCode));
                            Console.WriteLine("Retry number: " + retryCount);
                    }); // Returns a AsyncRetryPolicy

            using (var client = new HttpClient())
            {
                var response = await retryPolicy2WithWait.ExecuteAsync(() => client.GetAsync(endpoint));
                Console.WriteLine("Call with status code: " + getHttpStatusAsString(response.StatusCode));

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Content of the response: " + content);
                }
            }
        }
        
        public async Task CallAuthenticatedServerWithRetry(string endpoint)
        {
            var client = new HttpClient();
            
            IAsyncPolicy<HttpResponseMessage> retryPolicy2AndAuth =
                Policy
                    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .RetryAsync(2, onRetry: (r, retryCount) => {
                        // This is the ideal option to log failed calls or even for some backup code to trigger.
                        // In this example of triggered code when receiving a 401-Unauthorized, proceed to authentication code before the next retry
                        Console.WriteLine("Response wasn't sucessful: " + getHttpStatusAsString(r.Result.StatusCode));
                        Console.WriteLine("Retry number: " + retryCount);

                        if(r.Result.StatusCode == HttpStatusCode.Unauthorized)
                        {

                            client.DefaultRequestHeaders.Authorization
                                = new AuthenticationHeaderValue("Emitter", "Some Token");
                            
                        }
                    }); // Returns a AsyncRetryPolicy


                var response = await retryPolicy2AndAuth.ExecuteAsync(() => client.GetAsync(endpoint));
                Console.WriteLine("Call with status code: " + getHttpStatusAsString(response.StatusCode));

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Content of the response: " + content);
                }            
        }

        public async Task CallForbiddedResourceAndThenFallback(string endpoint)
        {
            var cachedResult = "Here I am señor Basil!";
            IAsyncPolicy<HttpResponseMessage> fallBackPolicy =
                Policy
                    .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Forbidden)
                    .FallbackAsync(getCachedResponse(cachedResult));          

            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await fallBackPolicy.ExecuteAsync(                 
                     () => client.GetAsync(endpoint));
            }        

        }

        public async Task CallUnreachableServerWithMultiplePredicates(string endpoint)
        {
            IAsyncPolicy<HttpResponseMessage> retryPolicy2Times =
                Policy
                    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .Or<HttpRequestException>() // If it's not a sucess code or a request exception!
                    .RetryAsync(1, onRetry: (response, retryCount) => {

                        if(response.Exception is HttpRequestException)
                        {
                            Console.WriteLine("Request was unable to complete, with a HttpRequestException");
                        }
                        else
                        {
                            Console.WriteLine("Response wasn't sucessful: " + getHttpStatusAsString(response.Result.StatusCode));
                        }
                        
                        Console.WriteLine("Retry number: " + retryCount);
                    }); 

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await retryPolicy2Times.ExecuteAsync(() => client.GetAsync(endpoint));
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception was propagated from outside the client with a type of: " + ex.GetType());
            }
        }

        public async Task CallWithTimeout(string endpoint)
        {
            var timeoutPolicy = Policy.TimeoutAsync(1); // throws TimeoutRejectedException after 1 second

            var retryPolicy =
                Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .Or<TimeoutRejectedException>()
                    .RetryAsync(3, onRetry: (response, retryCount) => {
                        if(response.Exception is TimeoutRejectedException)
                        {
                            Console.WriteLine("Polly timed out");
                        }

                        Console.WriteLine("Retry number: " + retryCount);
                    });

            using (var client = new HttpClient())
            {
                // It is worth mentioning that a fallback could be chained here, 
                // meaning 2 retries with timeouts and everything else fails a fallback value
                // NOTE: Fallback in that scenario will not be called if a sucessfull request is made.
                var response = await retryPolicy.ExecuteAsync(() => 
                                       timeoutPolicy.ExecuteAsync(
                                           token => client.GetAsync(endpoint, token), CancellationToken.None
                                       ));

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Content of the response: " + content);
                }

            }
        }

        private static string getHttpStatusAsString(HttpStatusCode code)
        {
            return $"{(int)code} - {code}";
        }

        private static HttpResponseMessage getCachedResponse(string cachedValue)
        {
            Console.WriteLine("Returning a cached value of :" + cachedValue);

            return
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(cachedValue)
            };
        }
    }
}
