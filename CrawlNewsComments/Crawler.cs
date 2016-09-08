//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Live Search Data Mining - DaaS
//
// Author: v-wisun
//

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace CrawlNewsComments
{
    public abstract class Crawler : ICrawler
    {
        public abstract IList<NewsItem> GetNewsList();
        public abstract IList<Comment> GetComments(NewsItem state);
        public abstract string GetFileName();
        public abstract bool GetCrawlFlag();
        public abstract string GetSiteName();

        /// <summary>
        /// Crawl news and comments,save file to local file
        /// </summary>
        public void Crawl()
        {
            string content = string.Empty;
            if (!GetCrawlFlag())
            {
                return;
            }

            bool isCrawled = false;
            int tryCount = 0;

            while (!isCrawled && tryCount++ < InitInfo.RetryCount)
            {
                InitInfo.LogMessage(string.Format("Starting to crawl {0} site...", GetSiteName()));
                try
                {
                    IList<NewsItem> news = GetNewsList();
                    if (news != null && news.Count > 0)
                    {
                        switch (Path.GetExtension(GetFileName()).ToLower())
                        {
                            case ".json":
                                content = CrawlAsJson(news);
                                break;
                            case ".xml":
                                content = CrawlAsXml(news);
                                break;
                            default:
                                content = CrawlAsXml(news);
                                break;
                        }

                        if (!string.IsNullOrEmpty(content))
                        {
                            isCrawled = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    InitInfo.LogMessage("Failed to crawl. Exceptoin: " + ex.GetBaseException().ToString());
                }

                if (!isCrawled)
                {
                    InitInfo.LogMessage("Failed to crawl. Retry after 10 seconds...");
                    System.Threading.Thread.Sleep(10 * 1000);
                    continue;
                }
                else
                {
                    string fileName = GetFileName();
                    InitInfo.LogMessage("Save file to " + fileName);
                    ArticleParserHelper.SaveToLocalFile(fileName, content);
                    ArticleParserHelper.CopyFileAndUpdateLatestFile(fileName);
                }
            }
            if (!isCrawled)
            {
                InitInfo.LogMessage("Error occurs when crawl news and comments!");
            }
        }

        /// <summary>
        /// Crawl and write news & comments in json format.
        /// </summary>
        /// <param name="news">news list</param>
        /// <returns>Xml string.</returns>
        private string CrawlAsXml(IList<NewsItem> news)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (XmlTextWriter writer = new XmlTextWriter(sw))
            {
                writer.Formatting = System.Xml.Formatting.Indented;

                if (0 == news.Count)
                {
                    return string.Empty;
                }

                writer.WriteStartElement("root");
                Dictionary<string, string> elements = new Dictionary<string, string>();

                foreach (var newsItem in news)
                {
                    #region item
                    writer.WriteStartElement("item");

                    elements.Add("title", newsItem.Title);
                    elements.Add("baseurl", newsItem.BaseUrl);
                    elements.Add("keywords", newsItem.Keywords);
                    elements.Add("summary", newsItem.Summary);
                    elements.Add("firstpara", newsItem.FirstPara);
                    elements.Add("bodytext", newsItem.BodyText);
                    WriteSequentialElements(writer, elements);

                    #region Comments
                    IList<Comment> comments = newsItem.Comments;
                    if (null != comments && comments.Count > 0)
                    {
                        foreach (var comment in comments)
                        {
                            writer.WriteStartElement("comment");

                            elements.Add("text", comment.Cotent);
                            elements.Add("vote", comment.Vote.ToString());
                            WriteSequentialElements(writer, elements);
                            elements.Clear();

                            writer.WriteEndElement();  // </comment>
                        }
                    }
                    #endregion  // end of Comments

                    writer.WriteEndElement();  // </item>
                    #endregion  // end of item
                }

                writer.WriteEndElement();  // </root>
            }

            return sw.GetStringBuilder().ToString();
        }

        /// <summary>
        /// Crawl and write news & comments in json format.
        /// </summary>
        /// <param name="news">news list</param>
        /// <returns>Json string.</returns>
        private string CrawlAsJson(IList<NewsItem> news)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                if (0 == news.Count)
                {
                    return string.Empty;
                }

                writer.WriteStartObject();
                writer.WritePropertyName("root");
                writer.WriteStartObject();
                writer.WritePropertyName("item");
                writer.WriteStartArray();
                Dictionary<string, string> properties = new Dictionary<string, string>();
                foreach (var newsItem in news)
                {
                    writer.WriteStartObject();

                    properties.Add("title", newsItem.Title);
                    properties.Add("baseurl", newsItem.BaseUrl);
                    properties.Add("keywords", newsItem.Keywords);
                    properties.Add("summary", newsItem.Summary);
                    properties.Add("firstpara", newsItem.FirstPara);
                    properties.Add("bodytext", newsItem.BodyText);
                    WriteSequentialProperties(writer, properties);
                    properties.Clear();

                    #region Comments
                    IList<Comment> comments = newsItem.Comments;// GetComments(newsItem, commentCount);
                    writer.WritePropertyName("comment");
                    writer.WriteStartArray();
                    if (null != comments && comments.Count > 0)
                    {
                        foreach (var comment in comments)
                        {
                            writer.WriteStartObject();

                            properties.Add("text", comment.Cotent);
                            properties.Add("vote", comment.Vote.ToString());
                            WriteSequentialProperties(writer, properties);

                            writer.WriteEnd();
                        }
                    }
                    writer.WriteEndArray();
                    #endregion  // end of Comments

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEnd();
                writer.WriteEndObject();
            }

            return sw.GetStringBuilder().ToString();
        }

        /// <summary>
        /// Write xml elements in sequential.
        /// </summary>
        /// <param name="writer">Xml text writer.</param>
        /// <param name="elements">Dictionary of element key and value. Clear after write complete.</param>
        private void WriteSequentialElements(XmlTextWriter writer, Dictionary<string, string> elements)
        {
            foreach (var element in elements)
            {
                writer.WriteStartElement(element.Key);
                writer.WriteString(element.Value);
                writer.WriteEndElement();
            }
            elements.Clear();
        }

        /// <summary>
        /// Write json properties in sequential.
        /// </summary>
        /// <param name="writer">Json writer.</param>
        /// <param name="properties">Dictionary of property key and value. Clear after write complete.</param>
        private void WriteSequentialProperties(JsonWriter writer, Dictionary<string, string> properties)
        {
            foreach (var property in properties)
            {
                writer.WritePropertyName(property.Key);
                writer.WriteValue(property.Value);
            }
            properties.Clear();
        }
    }
}
