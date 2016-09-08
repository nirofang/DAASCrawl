//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Live Search Data Mining - DaaS
//
// Author: v-wisun
//

using System.Collections.Generic;

namespace CrawlNewsComments
{
    public interface ICrawler
    {
        IList<NewsItem> GetNewsList();
        IList<Comment> GetComments(NewsItem state);
        void Crawl();
        //string GetFileName();
        //string GetSiteName();
    }

    public class NewsItem
    {
        public string Title;
        public string BaseUrl;
        public string Keywords;
        public string Summary;
        public string FirstPara;
        public string BodyText;
        public string CommentUrl;
        public int CommentCount;
        public List<Comment> Comments;
    }

    public class Comment
    {
        public string Cotent;
        public int Vote;
    }
}
