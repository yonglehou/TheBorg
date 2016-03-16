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

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using TheBorg.Collective.Extensions;
using TheBorg.Collective.Services;
using TheBorg.Interface.ValueObjects;

namespace TheBorg.Collective.Controllers
{
    [RoutePrefix("api/settings")]
    public class SettingsController : ApiController
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(
            ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [Route("{key}")]
        [HttpPut]
        [HttpPost]
        public async Task<IHttpActionResult> Set(string key, CancellationToken cancellationToken)
        {
            var settingKey = SettingKey.With(key);
            var content = await Request.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(content))
            {
                return BadRequest("No content");
            }

            await _settingsService.SetAsync(
                settingKey,
                User.GetPluginId().ToSettingGroupKey(),
                content,
                cancellationToken)
                .ConfigureAwait(false);

            return Ok();
        }

        [Route("{key}")]
        [HttpGet]
        public async Task<IHttpActionResult> Get(string key, CancellationToken cancellationToken)
        {
            var settingKey = SettingKey.With(key);

            var content = await _settingsService.GetAsync(
                settingKey,
                User.GetPluginId().ToSettingGroupKey(),
                cancellationToken)
                .ConfigureAwait(false);

            return Content(HttpStatusCode.OK, content);
        }
    }
}