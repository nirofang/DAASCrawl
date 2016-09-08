using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawlNewsComments
{
    public class InitInfo
    {
        public static readonly string FileSaveFolder = ConfigurationManager.AppSettings["FileSaveFolder"];
        public static readonly string FileCopyFolder = ConfigurationManager.AppSettings["FileCopyFolder"];
        //public static readonly string CrawlerConfig = ConfigurationManager.AppSettings["CrawlerConfig"];
        public static readonly int RetryCount = GetRetryCount();

        // The Toutiao site allows each request up to 50 pieces of news
        public static readonly int ToutiaoOnceGetMaxCount = 50;

        public static int GetRetryCount()
        {
            int count = 0;
            string retryCountStr = ConfigurationManager.AppSettings["RetryCount"];
            if (int.TryParse(retryCountStr, out count))
            {
                return count;
            }
            else
            {
                return 3;
            }
        }
        public static void LogMessage(string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToString() + "] " + message);
        }
    }
}
