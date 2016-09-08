//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Live Search Data Mining - DaaS
//
// Author: v-wisun
//

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Specialized;

namespace CrawlNewsComments
{
    /// <summary>
    /// Crawl news and comments.
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                MainWithParameters(args);
            }
            else
            {
                CrawlAllSite();
            }
        }

        static void MainWithParameters(string[] args)
        {
            if (args[0].Trim().ToLower().Equals("-help"))
            {
                Console.WriteLine(" -help \t /show help information");
                Console.WriteLine(" -site \t /craw assigned site.");
                Console.WriteLine("\t for example:");
                Console.WriteLine("\t execute command \"-site toutiao\" to crawl toutiao site;");
                Console.WriteLine("\t execute command \"-site tencent\" to crawl tencent site;");
                Console.WriteLine("\t execute command \"-site all\" to crawl all valid sites;");
            }
            else if (args[0].Trim().ToLower().Equals("-site"))
            {
                switch (args[1].Trim().ToLower())
                {
                    case "tencent":
                        Crawler_QQ();
                        break;
                    case "toutiao":
                        Crawler_Toutiao();
                        break;
                    case "all":
                        CrawlAllSite();
                        break;
                    default:
                        Console.WriteLine("error parameter,valid parameter are tencent,toutiao,all");
                        break;
                }
            }
            else
            {
                Console.WriteLine("error parameter,please execute command \"-help\" to know how to use it");
            }
        }

        static void CrawlAllSite()
        {
            Crawler_QQ();
            Crawler_Toutiao();
        }
        static void Crawler_Toutiao()
        {
            CrawlSite(new Crawler_Toutiao());
        }

        static void Crawler_QQ()
        {
            CrawlSite(new Crawler_QQ());
        }

        static void CrawlSite(ICrawler Crawler)
        {
            Crawler.Crawl();
        }
    }
}
