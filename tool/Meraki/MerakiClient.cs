using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lup.TwilioSwitch.Meraki
{
    // See https://developer.cisco.com/meraki/api-v1/
    // Modify tags: https://developer.cisco.com/meraki/api-v1/#!modify-network-sm-devices-tags
    // Checkin: https://developer.cisco.com/meraki/api-v1/#!checkin-network-sm-devices
    public class MerakiClient : IDisposable
    {
        private const String BaseUri = "https://api.meraki.com/api/v1/";
        private const String AcceptTypeHttpHeader = "Accept-Type";
        private const String MerakiApiKeyHttpHeader = "X-Cisco-Meraki-API-Key";
        private const Int32 MaxPageSize = 5000;

        private readonly HttpClient HttpClient;

        public Boolean IsDisposed { get; private set; }

        public MerakiClient(String apiKey)
        {
            if (null == apiKey)
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            HttpClient = new HttpClient(new HttpClientHandler())
            {
                BaseAddress = new Uri(BaseUri)
            };
            HttpClient.DefaultRequestHeaders.Add(MerakiApiKeyHttpHeader, apiKey);
            HttpClient.DefaultRequestHeaders.Add(AcceptTypeHttpHeader, "application/json");
        }
/*
        public async Task<ICollection<Organisation>> ListOrganisations()
        {
            return await RequestCollection<Organisation>("organizations");
        }
        
        public async Task<ICollection<Network>> ListNetworks()
        {
            return await RequestCollection<Network>("networks");
        }*/

        public ICollection<TOutput> RequestCollection<TOutput>(String requestUri)
        {
            return Request<ICollection<TOutput>>(HttpMethod.Get, $"{requestUri}?perPage={MaxPageSize.ToString()}"); // TODO: implement paging
            ;
            /*
            ICollection<TOutput> set;
            var output = new List<TOutput>();
            var skip = 0;
            do
            {
                set = await Request<ICollection<TOutput>>(HttpMethod.Get, $"{requestUri}&skip={skip}&top={MaxPageSize}");

                skip += MaxPageSize;
                output.AddRange(set);
            } while (set.Count == MaxPageSize);

            return output;*/
        }

        private TOutput Request<TOutput>(HttpMethod method, String requestUri)
        {
            var responseText = Request(method, requestUri);
            if (null == responseText)
            {
                return default(TOutput);
            }
            else
            {
                try
                {
                    return JsonSerializer.Deserialize<TOutput>(responseText);
                }
                catch (JsonException exception)
                {
                    throw new ApiException($"Could not deserialize the response body. '{responseText}'", exception);
                }
            }
        }

        private void Request<TInput>(HttpMethod method, String requestUri, TInput requestBody)
        {
            var a = JsonSerializer.Serialize(requestBody);

            Request(method, requestUri, a);
        }

        private String Request(HttpMethod method, String requestUri, String requestBody = null)
        {
            using (var request = new HttpRequestMessage(method, requestUri))
            {
                // Attach request body, if present
                if (null != requestBody)
                {
                    request.Content = new StringContent(requestBody);
                    request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
                }

                using (var response = HttpClient.Send(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    /*
                    // Retrieve headers
                    var headers = System.Linq.Enumerable.ToDictionary(response.Headers, h_ => h_.Key, h_ => h_.Value);
                    if (response?.Content?.Headers != null)
                    {
                        foreach (var item_ in response.Content.Headers)
                        {
                            headers[item_.Key] = item_.Value;
                        }
                    }*/

                    // Retrieve raw response
                    String responseText;
                    using (var stream = response.Content.ReadAsStream())
                    using (var reader = new StreamReader(stream))
                    {
                        responseText = reader.ReadToEnd();
                    }
                    
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            return responseText;
                        case HttpStatusCode.NoContent:
                            return null;
                        case HttpStatusCode.Unauthorized:
                            throw new AuthenticationException($"Authentication failed. {responseText}");
                        default:
                            throw new ApiException($"The HTTP status code of the response was not expected ({response.StatusCode}: {responseText}.");
                    }
                }
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            HttpClient?.Dispose();
            IsDisposed = true;
        }
    }
}