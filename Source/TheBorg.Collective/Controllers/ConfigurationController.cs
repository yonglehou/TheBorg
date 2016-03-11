// The MIT License(MIT)
// 
// Copyright(c) 2016 Rasmus Mikkelsen
// https://github.com/rasmus/TheBorg
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System.Web.Http;
using Serilog;
using TheBorg.Collective.Extensions;
using TheBorg.Common;
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.Collective.Controllers
{
    [RoutePrefix("api/plugin-configuration")]
    public class ConfigurationController : ApiController
    {
        private readonly ILogger _logger;
        private readonly IConfigurationReader _configurationReader;

        public ConfigurationController(
            ILogger logger,
            IConfigurationReader configurationReader)
        {
            _logger = logger;
            _configurationReader = configurationReader;
        }

        [Route("{key}")]
        [HttpGet]
        public IHttpActionResult Get(string key)
        {
            var pluginId = User.GetPluginId();
            var configKey = ConfigKey(pluginId, key);
            string value;
            if (!_configurationReader.TryReadString(configKey, out value))
            {
                _logger.Debug($"Could not find configuration key '{configKey}'");
                return NotFound();
            }

            _logger.Verbose($"Read configuration key '{configKey}'");
            return Json(value);
        }

        private static string ConfigKey(PluginId pluginId, string key)
        {
            return $"{pluginId.Value}:{key}";
        }
    }
}