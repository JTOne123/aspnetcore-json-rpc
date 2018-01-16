﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Community.AspNetCore.JsonRpc.Benchmarks.Framework;
using Community.AspNetCore.JsonRpc.Benchmarks.Middleware;
using Community.AspNetCore.JsonRpc.Benchmarks.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Community.AspNetCore.JsonRpc.Benchmarks.Suites
{
    [BenchmarkSuite("JsonRpcHandler")]
    public abstract class JsonRpcHandlerBenchmarks
    {
        private readonly IDictionary<string, string> _resources = new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly TestServer _serverHandler;
        private readonly TestServer _serverService;
        private readonly HttpClient _clientHandler;
        private readonly HttpClient _clientService;

        protected JsonRpcHandlerBenchmarks()
        {
            _serverHandler = new TestServer(new WebHostBuilder().Configure(_ => _.UseJsonRpcHandler<JsonRpcTestHandler>()));
            _serverService = new TestServer(new WebHostBuilder().Configure(_ => _.UseJsonRpcService<JsonRpcTestService>()));
            _clientHandler = _serverHandler.CreateClient();
            _clientService = _serverService.CreateClient();
            _clientHandler.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _clientService.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            foreach (var asset in new[] { "nam", "pos", "err", "not" })
            {
                _resources[asset] = EmbeddedResourceManager.GetString($"Assets.{asset}.json");
            }
        }

        private HttpContent CreateHttpContent(string asset)
        {
            var content = _resources[asset];
            var result = new StringContent(content, Encoding.UTF8, "application/json");

            result.Headers.ContentLength = content.Length;

            return result;
        }

        [Benchmark(Description = "han: nam")]
        public async Task HandlerWithParamsByName()
        {
            await _clientHandler.PostAsync(_serverHandler.BaseAddress, CreateHttpContent("nam")).ConfigureAwait(false);
        }

        [Benchmark(Description = "han: pos")]
        public async Task HandlerWithParamsByPosition()
        {
            await _clientHandler.PostAsync(_serverHandler.BaseAddress, CreateHttpContent("pos")).ConfigureAwait(false);
        }

        [Benchmark(Description = "han: err")]
        public async Task HandlerWithErrorResponse()
        {
            await _clientHandler.PostAsync(_serverHandler.BaseAddress, CreateHttpContent("err")).ConfigureAwait(false);
        }

        [Benchmark(Description = "han: not")]
        public async Task HandlerWithNotification()
        {
            await _clientHandler.PostAsync(_serverHandler.BaseAddress, CreateHttpContent("not")).ConfigureAwait(false);
        }

        [Benchmark(Description = "ser: nam")]
        public async Task ServiceWithParamsByName()
        {
            await _clientService.PostAsync(_serverHandler.BaseAddress, CreateHttpContent("nam")).ConfigureAwait(false);
        }

        [Benchmark(Description = "ser: pos")]
        public async Task ServiceWithParamsByPosition()
        {
            await _clientService.PostAsync(_serverHandler.BaseAddress, CreateHttpContent("pos")).ConfigureAwait(false);
        }

        [Benchmark(Description = "ser: err")]
        public async Task ServiceWithErrorResponse()
        {
            await _clientService.PostAsync(_serverHandler.BaseAddress, CreateHttpContent("err")).ConfigureAwait(false);
        }

        [Benchmark(Description = "ser: not")]
        public async Task ServiceWithNotification()
        {
            await _clientService.PostAsync(_serverHandler.BaseAddress, CreateHttpContent("not")).ConfigureAwait(false);
        }
    }
}