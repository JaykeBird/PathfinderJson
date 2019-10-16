using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;

namespace PathfinderJson
{
    public static class UpdateChecker
    {

        public static Version VERSION = new Version("0.9.1");

        public static async Task<UpdateData> CheckForUpdatesAsync()
        {
            WebClient wc = new WebClient();
            wc.Encoding = new UTF8Encoding();
            wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko"); // IE11 user agent
            string s = await wc.DownloadStringTaskAsync("https://api.github.com/repos/JaykeBird/PathfinderJson/releases/latest");

            var t = JsonConvert.DeserializeObject<GitHubReleaseData>(s);

            // no longer need the WebClient
            wc.Dispose();

            if (VERSION >= new Version(t.tag_name))
            {
                return new UpdateData();
            }
            else
            {
                return new UpdateData
                {
                    HasUpdate = true,
                    Assets = t.assets,
                    PublishTime = t.published_at,
                    Name = t.name,
                    TagName = t.tag_name,
                    Url = t.html_url,
                    Body = t.body
                };
            }
        }

    }

    public class UpdateData
    {
        public bool HasUpdate { get; set; } = false;

        public string TagName { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public DateTime PublishTime { get; set; }
        public GitHubReleaseData.Asset[] Assets { get; set; }
        public string Body { get; set; }
    }
}
