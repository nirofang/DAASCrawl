//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Live Search Data Mining - DaaS
//
// Author: v-wisun
//

using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CrawlNewsComments
{
    public class Crawler_QQ : Crawler
    {
        private const string siteUrl = "http://news.qq.com/";
        private const string commentHost = "http://coral.qq.com/";

        /// <summary>
        /// this is the url of Tencent's hotmomments
        /// param "reqnum" is the count of hotcomments for one news to crawl
        /// </summary>
        private const string HotCommentUrl = "http://coral.qq.com/article/{0}/hotcomment?reqnum={1}";

        /// <summary>
        /// this is the url of Tencent's comments
        /// param "reqnum" is the count of comments fro one news to crawl
        /// </summary>
        private const string CommentsUrl = "http://coral.qq.com/article/{0}/comment?reqnum={1}";
        private const string siteName = "tencent";

        ///crawl setting of Tencent.Such as crawl this site or not,how many comments for each news to crawl,
        ///the head part of the saved file name,the extension of the saved file.
        private WebSetting webSetting;

        public Crawler_QQ()
        {
            webSetting = MyConfigHandler.GetWebSetting(siteName);
        }

        public override string GetSiteName()
        {
            return siteName;
        }

        public override string GetFileName()
        {
            string fileName = Path.Combine(InitInfo.FileSaveFolder, webSetting.FileNameHeadPart + "." + DateTime.Now.ToString("yyyyMMddHHmm") + webSetting.FileExtension);
            return fileName;
        }

        public override bool GetCrawlFlag()
        {
            return webSetting.CrawlOrNot == "1";
        }

        public override IList<NewsItem> GetNewsList()
        {
            IList<NewsItem> newsList = new List<NewsItem>();
            HtmlDocument doc = ArticleParserHelper.GetHtmlDoc(siteUrl);
            if (null != doc)
            {
                HtmlNode rootNode = doc.DocumentNode;

                //Get top 1 heading list
                this.GetHeadingNewsList(rootNode, newsList);

                //Get 2nd heading list
                this.GetHeadingNewsList(rootNode, newsList, "//div[@id='headingNews']/div[@class='hdNews hasPic cf']");

                //Get top news in main page
                this.GetHotNewsList(rootNode, newsList);

                //Get hot top new in main page
                this.GetHotNewsList(rootNode, newsList, "//div[@class='Q-pList']");
            }

            return newsList.GroupBy(n => n.BaseUrl).Select(g => g.First()).ToList() as IList<NewsItem>;  //Distinct news
        }

        public override IList<Comment> GetComments(NewsItem state)
        {
            string articleId = Regex.Match(state.CommentUrl, @"[0-9]{1,}").Value;
            string requestUrl = string.Format(HotCommentUrl, articleId, webSetting.CrawCommentsCount);

            List<Comment> newsComments = GetComments(requestUrl) as List<Comment>;
            if (newsComments.Count < webSetting.CrawCommentsCount && state.CommentCount > newsComments.Count)
            {
                requestUrl = string.Format(CommentsUrl, articleId, webSetting.CrawCommentsCount - newsComments.Count);
                IEnumerable<Comment> list = GetComments(requestUrl) as IEnumerable<Comment>;
                if (list.Count() > 0)
                {
                    newsComments.AddRange(list);
                }
            }

            return newsComments.GroupBy(c => c.Cotent).Select(g => g.First()).ToList() as IList<Comment>;  //Distinct comments
        }

        private void GetHeadingNewsList(HtmlNode rootNode, IList<NewsItem> newsList, string xpathhdNews = "")
        {
            if (string.IsNullOrEmpty(xpathhdNews))
            {
                xpathhdNews = "//div[@id='headingNews']/div[@class='hdNews PicBig cf' and @id='theOne']";
            }
            HtmlNodeCollection newshdCollection = rootNode.SelectNodes(xpathhdNews);
            if (null == newshdCollection)
            {
                return;
            }

            foreach (HtmlNode wraperNode in newshdCollection)
            {
                NewsItem item = GetGeneralNewsInfo(wraperNode, "./h2/span/a", "");
                if (null != item)
                {
                    item.Comments = GetComments(item) as List<Comment>;
                    //this.GetMoreNewsInfo(item); >This line is not required
                    newsList.Add(item);
                }
            }
        }

        private void GetHotNewsList(HtmlNode rootNode, IList<NewsItem> newsList, string xpathHotNews = "")
        {
            if (string.IsNullOrEmpty(xpathHotNews))
            {
                xpathHotNews = "//div[@class='Q-tpList']/div[@class='Q-tpWrap']";
            }
            HtmlNodeCollection newsCollection = rootNode.SelectNodes(xpathHotNews);
            if (null == newsCollection)
            {
                return;
            }

            foreach (HtmlNode wraperNode in newsCollection)
            {
                NewsItem item = GetGeneralNewsInfo(wraperNode);
                if (null != item)
                {
                    item.Comments = GetComments(item) as List<Comment>;
                    this.GetMoreNewsInfo(item);
                    newsList.Add(item);
                }
            }
        }

        private NewsItem GetGeneralNewsInfo(HtmlNode wraperNode,
            string xpathNews = "./em/span/span/a",
            string xpathSummary = "./p[@class='l22']",
            string xpathCommentState = "./div[@class='btns']/a[@class='discuzBtn']")
        {
            HtmlNode newsNode = wraperNode.SelectSingleNode(xpathNews);
            string newsUrl = null == newsNode ? string.Empty : newsNode.Attributes["href"].Value;
            string newsTitle = null == newsNode ? string.Empty : newsNode.InnerText;
            string newsSummary = string.Empty;
            if (!string.IsNullOrEmpty(xpathSummary))
            {
                HtmlNode summaryNode = wraperNode.SelectSingleNode(xpathSummary);
                newsSummary = null == summaryNode ? string.Empty : summaryNode.InnerText;
            }

            NewsItem item = null;
            HtmlNode commentsStateNode = wraperNode.SelectSingleNode(xpathCommentState);
            if (null != commentsStateNode)
            {
                int commentsCount = Int32.Parse(commentsStateNode.InnerText);
                string commentsUrl = commentsStateNode.Attributes["href"].Value;
                item = new NewsItem
                {
                    BaseUrl = newsUrl,
                    Title = newsTitle,
                    Summary = newsSummary,
                    CommentCount = commentsCount,
                    CommentUrl = commentsUrl,
                    BodyText = "",
                    FirstPara = "",
                    Keywords = ""
                };
            }

            return item;
        }

        /// <summary>
        /// Crawl news keywords, first paragraph and body text.
        /// </summary>
        /// <param name="news">Adding more info to the news item.</param>
        private void GetMoreNewsInfo(NewsItem news)
        {
            if (null == news)
            {
                news = new NewsItem();
            }

            System.Threading.Thread.Sleep(1000);  //delayed request is required to unblock from server side
            Console.WriteLine("Crawling url: {0}", news.BaseUrl);
            HtmlDocument doc = ArticleParserHelper.GetHtmlDoc(news.BaseUrl);
            if (null == doc)
            {
                return;
            }

            HtmlNode rootNode = doc.DocumentNode;
            news.Keywords = GetNewsKeywords(rootNode, " ");

            string[] content = GetNewsContent(rootNode);
            news.FirstPara = content[0];
            news.BodyText = content[1];
        }

        private string GetNewsKeywords(HtmlNode rootNode, string splitter)
        {
            string keywords = string.Empty;
            HtmlNodeCollection keywordNodes = rootNode.SelectNodes("//h2[@bosszone='keyword']/span");
            if (null != keywordNodes)
            {
                foreach (var item in keywordNodes)
                {
                    keywords += (item.InnerText + splitter);
                }
            }

            return keywords.Trim();
        }

        /// <summary>
        /// Get the content and first paragraph.
        /// </summary>
        /// <param name="rootNode"></param>
        /// <returns>First paragraph at index 0 while content index 1.</returns>
        private string[] GetNewsContent(HtmlNode rootNode)
        {
            string firstpara = string.Empty;
            string content = string.Empty;

            HtmlNodeCollection paraNodes = rootNode.
                SelectNodes("//div[@id='Cnt-Main-Article-QQ']/div[@class='content']/div[@id='body_section']/p[not(@align='center')]");
            if (null == paraNodes || paraNodes.Count == 0)
            {
                paraNodes = rootNode.SelectNodes("//div[@id='Cnt-Main-Article-QQ']/p[not(@align='center')]");
            }

            if (null != paraNodes && paraNodes.Count > 0)
            {
                HtmlNode videoNode = paraNodes.FirstOrDefault(x => x.SelectSingleNode("./div[@id='invideocon']") != null);
                if (null != videoNode)
                {
                    paraNodes.Remove(videoNode);
                }
                if (paraNodes.Count > 0)
                {
                    firstpara = paraNodes[0].InnerText;
                    foreach (var item in paraNodes)
                    {
                        content += item.InnerText;
                    }
                }
            }

            return new string[] { firstpara, content };
        }

        private IList<Comment> GetComments(string requestUrl)
        {
            IList<Comment> comments = new List<Comment>();
            string html = ArticleParserHelper.GetHtmlStr(requestUrl);

            try
            {
                string result = Regex.Unescape(html);  //Translate unicode to Chinese characters
                string jsonResult = CleanJsonString(result);
                JObject obj = JObject.Parse(jsonResult);
                IList<JToken> jsonComments = obj["data"]["commentid"].Children().ToList();
                foreach (JToken token in jsonComments)
                {
                    NewsComment_QQ cmt = JsonConvert.DeserializeObject<NewsComment_QQ>(token.ToString());
                    comments.Add(new Comment()
                    {
                        Cotent = cmt.Content,
                        Vote = Int32.Parse(cmt.Up)
                    });
                }
            }
            catch (Exception)
            {
                return comments;
            }

            return comments;
        }

        private string CleanJsonString(string input)
        {
            string result;

            // 'custom' attribute breaks json structure, remove it
            result = Regex.Replace(input, @"\""custom\"":\"".*?,", "", RegexOptions.IgnoreCase);

            // Remove 'nick' attribute as well
            result = Regex.Replace(result, @"\""nick\"":\"".*?,", "", RegexOptions.IgnoreCase);

            return result;
        }

        public struct NewsComment_QQ
        {
            public string Content;
            public string Up;
        }
    }
}
