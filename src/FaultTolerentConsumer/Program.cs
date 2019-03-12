using System;
using System.Threading.Tasks;

namespace FaultTolerentConsumer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var baseApiEndpoint = "https://localhost:44345/api";
            var getBasilRoute = baseApiEndpoint + "/Basil";
            var getWhoIsBasilRoute = getBasilRoute + "/whois";
            var getCallManuelBasilRoute = getBasilRoute + "/callmanuel";
            var getSybillBasilRoute = getBasilRoute + "/sybill";

            var polly = new PollyTester();

            Console.WriteLine("Welcome to Polly demonstration!");
            Console.WriteLine("The Console App is running in parallel with a Web Api Server,");
            Console.WriteLine("on each test the url is given, feel free to use Fiddler (or other) to re-test the calls without Polly interaction");


            while (true)
            {
                var option = writeMenu();

                switch (option)
                {
                    case 1:
                        immediateRetryText(getBasilRoute);                        
                        await polly.CallUnreliableServerWithImmediateRetry(getBasilRoute);
                        Console.WriteLine();
                        break;
                    case 2:
                        awaitRetryText(getBasilRoute);
                        await polly.CallUnreliableServerWithAwaitRetry(getBasilRoute);
                        Console.WriteLine();
                        break;
                    case 3:
                        unauthorizedRetryText(getWhoIsBasilRoute);
                        await polly.CallAuthenticatedServerWithRetry(getWhoIsBasilRoute);
                        Console.WriteLine();
                        break;
                    case 4:
                        retryThenFallbackText(getCallManuelBasilRoute);
                        await polly.CallForbiddedResourceAndThenFallback(getCallManuelBasilRoute);
                        Console.WriteLine();
                        break;
                    case 5:
                        handleExceptionText("http://localhost:1234/doesnotexist");
                        await polly.CallUnreachableServerWithMultiplePredicates("http://localhost:1234/doesnotexist");
                        Console.WriteLine();
                        break;
                    case 6:
                        retryWithTimeoutText(getSybillBasilRoute);
                        await polly.CallWithTimeout(getSybillBasilRoute);
                        Console.WriteLine();
                        break;
                    default:
                        break;
                }
            }

        }

        static int writeMenu()
        {            
            Console.WriteLine("1. Test Immediate Retry (Reactive Strategy)");
            Console.WriteLine("2. Test Retry-Await (Reactive Strategy)");
            Console.WriteLine("3. Test Unathorized and Retry (Reactive Strategy)");
            Console.WriteLine("4. Test Retry-Fallback (Reactive Strategy)");
            Console.WriteLine("5. Test Handling HttpException (Reactive Strategy)");
            Console.WriteLine("6. Test Timeout with Retry (Proactive Strategy)");


            return Convert.ToInt32(Console.ReadLine());
        }

        static void immediateRetryText(string route)
        {
            Console.WriteLine();
            Console.WriteLine("In this test we are about to call a server which is know to fail.");
            Console.WriteLine("The server responds correctly on every 3rd attempt and fails for the remaining.");
            Console.WriteLine("On each error, polly will immediately retry the call in hopes of a sucessfull call.");
            Console.WriteLine("Attempting to call : " + route);
            Console.WriteLine();
        }

        static void awaitRetryText(string route)
        {
            Console.WriteLine();
            Console.WriteLine("In this test we are about to call a server which is know to fail.");
            Console.WriteLine("The server responds correctly on every 3rd attempt and fails for the remaining.");
            Console.WriteLine("On each error, polly will await and then retry the call in hopes of a sucessfull call.");
            Console.WriteLine("Attempting to call : " + route);
            Console.WriteLine();
        }

        static void unauthorizedRetryText(string route)
        {
            Console.WriteLine();
            Console.WriteLine("In this test we are about to call a server which needs basic authentication thru a cookie.");
            Console.WriteLine("The server responds with a 401 if it cookie isn't set");
            Console.WriteLine("On an error, Polly will check if it is a 401 and proceed to call authentication code");
            Console.WriteLine("Attempting to call : " + route);
            Console.WriteLine();
        }

        static void retryThenFallbackText(string route)
        {
            Console.WriteLine();
            Console.WriteLine("In this test we are about to call a server which continously emit a 403-Forbidden.");            
            Console.WriteLine("On an error, Polly will attempt to retry, when it retry attempts fail, it will do a fallback, returning a chached value");
            Console.WriteLine("Attempting to call : " + route);
            Console.WriteLine();
        }

        static void handleExceptionText(string route)
        {
            Console.WriteLine();
            Console.WriteLine("In this test we are about to call and endpoint that does not exist.");
            Console.WriteLine("On an error, Polly will capture the error and react and the progagate the exception.");
            Console.WriteLine("Attempting to call : " + route);
            Console.WriteLine();
        }

        static void retryWithTimeoutText(string route)
        {
            Console.WriteLine();
            Console.WriteLine("In this test we are about to call and endpoint that takes too long to respond on occasions.");
            Console.WriteLine("Polly will wait for 1 second on each call and then cancel the request and the retry will emit a new one.");
            Console.WriteLine("Attempting to call : " + route);
            Console.WriteLine();
        }
    }
}
