using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;

namespace PathfinderJson
{
    public static class UpdateChecker
    {


        public static async Task<UpdateData> CheckForUpdatesAsync()
        {
            HttpClient wc = new HttpClient();
            // Github's API only responds if there's a user agent present. I'm currently using a Chrome user agent (the actual user agent doesn't really matter, just the fact that it's present)
            wc.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36 PathfinderJson/" + App.AppVersion.ToString());
            var t = await wc.GetFromJsonAsync<GitHubReleaseData?>("https://api.github.com/repos/JaykeBird/PathfinderJson/releases/latest");

            //string s = await wc.DownloadStringTaskAsync("https://api.github.com/repos/JaykeBird/PathfinderJson/releases/latest");
            //var t = JsonConvert.DeserializeObject<GitHubReleaseData>(s);

            // no longer need the WebClient
            wc.Dispose();

            if (t != null)
            {

                if (App.AppVersion >= new Version(t.tag_name))
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
            else
            {
                return new UpdateData();
            }
        }

    }

    public class UpdateData
    {
        public bool HasUpdate { get; set; } = false;

        public string TagName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Url { get; set; } = "https://github.com/JaykeBird/PathfinderJson/releases";
        public DateTime PublishTime { get; set; } = DateTime.Today;
        public GitHubReleaseData.Asset[]? Assets { get; set; }
        public string Body { get; set; } = "";
    }
}
