﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.IoTStreamAnalytics.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.IoTStreamAnalytics.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.IoTStreamAnalytics.Services.Http;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.IoTStreamAnalytics.Services.Helpers
{
    public interface IHttpClientWrapper
    {
        Task<T> GetAsync<T>(string uri, string description, bool acceptNotFound = false);

        Task<T> PostAsync<T>(string uri, T postObj, string description);
    }

    public class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly ILogger logger;
        private readonly IHttpClient client;

        public HttpClientWrapper(
            ILogger logger,
            IHttpClient client)
        {
            this.logger = logger;
            this.client = client;
        }

        public async Task<T> GetAsync<T>(
            string uri,
            string description,
            bool acceptNotFound = false)
        {
            var request = new HttpRequest();
            request.SetUriFromString(uri);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("User-Agent", "Telemetry Agent");

            IHttpResponse response;

            try
            {
                response = await client.GetAsync(request);
            }
            catch (Exception e)
            {
                logger.Error("Request failed", () => new { uri, e });
                throw new ExternalDependencyException($"Failed to load {description}");
            }

            if (response.StatusCode == HttpStatusCode.NotFound && acceptNotFound)
            {
                return default(T);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.Error("Request failed", () => new { uri, response.StatusCode, response.Content });
                throw new ExternalDependencyException($"Unable to load {description}");
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(response.Content);
            }
            catch (Exception e)
            {
                logger.Error($"Could not parse result from {uri}: {e.Message}", () => { });
                throw new ExternalDependencyException($"Could not parse result from {uri}");
            }
        }

        public async Task<T> PostAsync<T>(string uri, T postObj, string description)
        {
            var request = new HttpRequest();
            request.SetUriFromString(uri);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("User-Agent", "Telemetry Agent");

            //request.Options.Timeout = 5 * 1000;
            //request.Options.AllowInsecureSSLServer = true;
            request.Options.EnsureSuccess = false;

            request.SetContent<T>(postObj, Encoding.UTF8, "application/json");

            IHttpResponse response;

            try
            {
                response = await client.PostAsync(request);
            }
            catch (Exception e)
            {
                logger.Error("Request failed", () => new { uri, e });
                throw new ExternalDependencyException($"Failed to create {description}");
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.Error("Request failed", () => new { uri, response.StatusCode, response.Content });
                throw new ExternalDependencyException($"Unable to create {description}");
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(response.Content);
            }
            catch (Exception e)
            {
                logger.Error($"Could not parse result from {uri}: {e.Message}", () => { });
                throw new ExternalDependencyException($"Could not parse result from {uri}");
            }
        }
    }
}
