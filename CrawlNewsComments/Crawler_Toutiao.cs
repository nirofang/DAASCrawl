using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace CrawlNewsComments
{
    public class Crawler_Toutiao : Crawler
    {
        private const string SiteUrl = "http://toutiao.com/";

        /// <summary>
        /// this is the url of Toutiao's mews
        /// para "max_behot_time" is the time when the news be hot,and the time format relies on Toutiao's server 
        /// param "count" is the count of News to crawl
        /// </summary>
        private const string SiteNewsUrl = "http://toutiao.com/api/article/recent/?max_behot_time={0}&count={1}";

        /// <summary>
        /// this is the url of Toutiao's comments
        /// param "count" is the count of comments for one news to crawl
        /// </summary>
        private const string CommentsUrl = "http://toutiao.com/group/{0}/comments/?count={1}";
        private const string siteName = "toutiao";

        ///crawl setting of Toutiao.Such as crawl this site or not,how many news or comments to crawl,
        ///the head part of the saved file name,the extension of the saved file.
        private WebSetting webSetting;

        public Crawler_Toutiao()
        {
            webSetting = MyConfigHandler.GetWebSetting(siteName);
        }

        public override string GetSiteName()
        {
            return siteName;
        }

        public override string GetFileName()
        {
            return Path.Combine(InitInfo.FileSaveFolder, webSetting.FileNameHeadPart + "." + DateTime.Now.ToString("yyyyMMddHHmm") + webSetting.FileExtension);
        }

        public override bool GetCrawlFlag()
        {
            return webSetting.CrawlOrNot == "1";
        }

        public override IList<NewsItem> GetNewsList()
        {
            List<NewsItem> newsList = new List<NewsItem>();

            IList<JToken> jsonComments = GetJsonNewsList();

            List<News_Toutiao> newslist = new List<News_Toutiao>();
            int crowIndex = 1;

            foreach (JToken token in jsonComments)
            {
                News_Toutiao temp = JsonConvert.DeserializeObject<News_Toutiao>(token.ToString());

                //if Article_Type's value is 0,then the news is Toutiao's internal news,crawl it
                //if Article_Type's value is 1,then the news is Toutiao's external news,drop it
                if (temp.Article_Type == 0)
                {
                    newslist.Add(temp);
                }
            }
            // Distinct news by news's url
            newslist = newslist.GroupBy(n => n.BaseUrl).Select(g => g.First()).ToList();

            Console.WriteLine("Ready to Crawl {0} site,and the news count is {1}", siteName, newslist.Count);

            foreach (News_Toutiao m in newslist)
            {
                Console.WriteLine("Crawling url: {0}  No.{1}", m.BaseUrl, crowIndex++);
                int count = m.CommentCount > webSetting.CrawCommentsCount ? webSetting.CrawCommentsCount : m.CommentCount;
                if (count == 0)
                {
                    // if the count of newscomments is 0,drop this news and continue.
                    continue;
                }
                string commentUrl = count == 0 ? string.Empty : string.Format(CommentsUrl, m.NewsID, count);

                NewsItem item = new NewsItem();
                item.Title = m.Title;
                item.BaseUrl = m.BaseUrl;
                item.Keywords = m.Keywords;
                item.Summary = m.Summary;
                item.CommentUrl = commentUrl;
                item.CommentCount = m.CommentCount;
                item.Comments = GetComments(item) as List<Comment>;
                if (item.Comments.Count == 0)
                {
                    // if crawler doesn't get comments,drop this news and continue.
                    continue;
                }

                // get news's firstpara and bodytext.
                string[] newsContent = GetNewsBodyText(item.BaseUrl);
                item.FirstPara = newsContent[0];
                item.BodyText = newsContent[1];

                newsList.Add(item);
            }
            return newsList.GroupBy(n => n.BaseUrl).Select(g => g.First()).ToList() as IList<NewsItem>;  //Distinct news
        }

        public override IList<Comment> GetComments(NewsItem state)
        {
            List<Comment> comments = new List<Comment>();

            HtmlDocument doc = ArticleParserHelper.GetHtmlDoc(state.CommentUrl);

            if (null != doc)
            {
                HtmlNode rootNode = doc.DocumentNode;
                string xpathhdNews = "//div[@class='comment-content']";

                HtmlNodeCollection newshdCollection = rootNode.SelectNodes(xpathhdNews);
                if (null == newshdCollection)
                {
                    return comments;
                }

                foreach (HtmlNode wraperNode in newshdCollection)
                {
                    Comment c = new Comment();
                    HtmlNode c_content = wraperNode.SelectSingleNode("./div[@class='content']");
                    c.Cotent = c_content.InnerText.Trim();
                    HtmlNode c_vote = wraperNode.SelectSingleNode("./div[@class='comment_actions clearfix']/span[@class='action']/a[@class='comment_digg ']");
                    c.Vote = Convert.ToInt32(c_vote.InnerText.Trim());

                    comments.Add(c);
                }
            }

            return comments;
        }

        public List<JToken> GetJsonNewsList()
        {
            string max_behot_time = string.Empty;
            string uri = string.Empty;
            string jsonResult = string.Empty;
            int onceGet = InitInfo.ToutiaoOnceGetMaxCount;
            List<JToken> jsonNewsList = new List<JToken>();
            for (int i = 0; i < webSetting.CrawNewsCount / onceGet; i++)
            {
                uri = string.Format(SiteNewsUrl, max_behot_time, onceGet);
                jsonResult = ArticleParserHelper.GetHtmlStr(uri);
                JObject obj = JObject.Parse(jsonResult);

                jsonNewsList.AddRange(obj["data"].Children().ToList());
                max_behot_time = obj["next"]["max_behot_time"].ToString();
            }
            if (webSetting.CrawNewsCount % onceGet > 0)
            {
                uri = string.Format(SiteNewsUrl, max_behot_time, webSetting.CrawNewsCount % onceGet);
                jsonResult = ArticleParserHelper.GetHtmlStr(uri);
                JObject obj = JObject.Parse(jsonResult);
                jsonNewsList.AddRange(obj["data"].Children().ToList());
            }

            return jsonNewsList;
        }

        private string[] GetNewsBodyText(string url)
        {
            string firstpara = string.Empty;
            string content = string.Empty;
            HtmlDocument doc = ArticleParserHelper.GetHtmlDoc(url);
            if (null != doc)
            {
                HtmlNode rootNode = doc.DocumentNode;
                string xpathhdNews = "//div[@class='article-content']//p";

                HtmlNodeCollection newshdCollection = rootNode.SelectNodes(xpathhdNews);
                if (null == newshdCollection)
                {
                    return new string[] { firstpara, content };
                }

                foreach (HtmlNode wraperNode in newshdCollection)
                {
                    if (!string.IsNullOrEmpty(wraperNode.InnerText.Trim()))
                    {
                        firstpara = wraperNode.InnerText.Trim();
                        break;
                    }
                }

                foreach (HtmlNode wraperNode in newshdCollection)
                {
                    content += wraperNode.InnerText.Trim();
                }
            }

            return new string[] { firstpara, content };
        }

        public class News_Toutiao
        {
            [JsonProperty("id")]
            public string NewsID;

            [JsonProperty("title")]
            public string Title;

            [JsonProperty("seo_url")]
            public string BaseUrl;

            [JsonProperty("keywords")]
            public string Keywords;

            [JsonProperty("abstract")]
            public string Summary;

            [JsonProperty("display_url")]
            public string CommentUrl;

            [JsonProperty("comment_count")]
            public int CommentCount;

            [JsonProperty("article_type")]
            public int Article_Type;
        }
    }
}
