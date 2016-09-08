using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawlNewsComments
{
    public class MyConfigHandler : IConfigurationSectionHandler
    {
        public MyConfigHandler()
        {
        }

        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            NameValueCollection configs;
            NameValueSectionHandler baseHandler = new NameValueSectionHandler();
            configs = (NameValueCollection)baseHandler.Create(parent, configContext, section);

            return configs;
        }

        private static string GetConfig(string siteName, string nodeName)
        {
            string ParentNode = string.Format("CrawlConfig/{0}", siteName);
            return ((NameValueCollection)ConfigurationSettings.GetConfig(ParentNode))[nodeName];
        }
        public static WebSetting GetWebSetting(string siteName)
        {
            WebSetting w = new WebSetting();

            w.SiteName = GetConfig(siteName, "SiteName");
            w.CrawlOrNot = GetConfig(siteName, "CrawlOrNot");

            string CrawNewsCountStr = GetConfig(siteName, "CrawNewsCount");
            w.CrawNewsCount = string.IsNullOrEmpty(CrawNewsCountStr) ? 0 : Convert.ToInt32(CrawNewsCountStr);

            string CrawCommentsCountStr = GetConfig(siteName, "CrawCommentsCount");
            w.CrawCommentsCount = string.IsNullOrEmpty(CrawCommentsCountStr) ? 0 : Convert.ToInt32(CrawCommentsCountStr);
            w.FileNameHeadPart = GetConfig(siteName, "FileNameHeadPart");
            w.FileExtension = GetConfig(siteName, "FileExtension");

            return w;
        }
    }
}
