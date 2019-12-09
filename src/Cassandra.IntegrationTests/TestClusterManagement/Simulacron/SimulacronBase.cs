//
//      Copyright (C) DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cassandra.IntegrationTests.SimulacronAPI;
using Cassandra.Tasks;
using Newtonsoft.Json.Linq;

namespace Cassandra.IntegrationTests.TestClusterManagement.Simulacron
{
    public class SimulacronBase
    {
        public string Id { get; }

        protected SimulacronBase(string id)
        {
            Id = id;
        }

        protected static async Task<JObject> Post(string url, object body)
        {
            var bodyStr = SimulacronBase.GetJsonFromObject(body);
            var content = new StringContent(bodyStr, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                client.BaseAddress = SimulacronManager.BaseAddress;
                var response = await client.PostAsync(url, content).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    // Get the error message
                    throw new InvalidOperationException(await response.Content.ReadAsStringAsync()
                                                                      .ConfigureAwait(false));
                }
                var dataStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JObject.Parse(dataStr);
            }
        }

        private static string GetJsonFromObject(object body)
        {
            var bodyStr = string.Empty;
            if (body != null)
            {
                bodyStr = JObject.FromObject(body).ToString();
            }
            return bodyStr;
        }

        protected static async Task<JObject> Put(string url, object body)
        {
            var bodyStr = SimulacronBase.GetJsonFromObject(body);
            var content = new StringContent(bodyStr, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                client.BaseAddress = SimulacronManager.BaseAddress;
                var response = await client.PutAsync(url, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var dataStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(dataStr))
                {
                    return null;
                }
                return JObject.Parse(dataStr);
            }
        }

        protected static async Task<dynamic> Get(string url)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = SimulacronManager.BaseAddress;
                var response = await client.GetAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var dataStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JObject.Parse(dataStr);
            }
        }

        protected static async Task Delete(string url)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = SimulacronManager.BaseAddress;
                var response = await client.DeleteAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
        }

        public dynamic GetLogs()
        {
            return TaskHelper.WaitToComplete(GetLogsAsync());
        }

        public Task<dynamic> GetLogsAsync()
        {
            return Get(GetPath("log"));
        }

        public dynamic PrimeDynamic(dynamic body)
        {
            Task<dynamic> task = Post(GetPath("prime"), body);
            return TaskHelper.WaitToComplete(task);
        }

        public Task<JObject> PrimeAsync(IPrimeRequest request)
        {
            return SimulacronBase.Post(GetPath("prime"), request.Render());
        }
        
        public JObject Prime(IPrimeRequest request)
        {
            return TaskHelper.WaitToComplete(PrimeAsync(request));
        }

        protected string GetPath(string endpoint)
        {
            return "/" + endpoint + "/" + Id;
        }

        public dynamic GetConnections()
        {
            return TaskHelper.WaitToComplete(Get(GetPath("connections")));
        }

        public Task DisableConnectionListener(int attempts = 0, string type = "unbind")
        {
            return Delete(GetPath("listener") + "?after=" + attempts + "&type=" + type);
        }

        public Task<JObject> EnableConnectionListener(int attempts = 0, string type = "unbind")
        {
            return Put(GetPath("listener") + "?after=" + attempts + "&type=" + type, null);
        }

        public IList<dynamic> GetQueries(string query, string queryType = "QUERY")
        {
            return TaskHelper.WaitToComplete(GetQueriesAsync(query, queryType));
        }

        public async Task<IList<dynamic>> GetQueriesAsync(string query, string queryType = "QUERY")
        {
            var response = await GetLogsAsync().ConfigureAwait(false);
            IEnumerable<dynamic> dcInfo = response?.data_centers;
            if (dcInfo == null)
            {
                return new List<dynamic>(0);
            }
            return dcInfo
                   .Select(dc => dc.nodes)
                   .Where(nodes => nodes != null)
                   .SelectMany<dynamic, dynamic>(nodes => nodes)
                   .Where(n => n.queries != null)
                   .SelectMany<dynamic, dynamic>(n => n.queries)
                   .Where(q => (q.type == queryType || queryType == null) && (q.query == query || query == null))
                   .ToArray();
        }

        public JObject PrimeFluent(Func<IPrimeRequestFluent, IThenFluent> builder)
        {
            return TaskHelper.WaitToComplete(PrimeFluentAsync(builder));
        }
        
        public Task<JObject> PrimeFluentAsync(Func<IPrimeRequestFluent, IThenFluent> builder)
        {
            var prime = SimulacronBase.PrimeBuilder();
            return builder(prime).ApplyAsync(this);
        }

        public static IPrimeRequestFluent PrimeBuilder()
        {
            return new PrimeRequestFluent();
        }
    }
}