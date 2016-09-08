//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Live Search Data Mining - DaaS
//
// Author: v-wisun
//

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace CrawlNewsComments
{
    public class ArticleParserHelper
    {
        /// <summary>
        /// Get html source code from url.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string GetHtmlStr(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            string html = string.Empty;
            try
            {
                WebRequest webRequest = WebRequest.Create(url);
                webRequest.Timeout = 30 * 1000;
                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    if (((HttpWebResponse)webResponse).StatusCode == HttpStatusCode.OK)
                    {
                        Stream stream = webResponse.GetResponseStream();
                        string coder = ((HttpWebResponse)webResponse).CharacterSet;

                        StreamReader reader = new StreamReader(stream, string.IsNullOrEmpty(coder) ? Encoding.Default : Encoding.GetEncoding(coder));
                        html = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to crawl {0}, exception: {1}", url, ex.GetBaseException().Message);
                //Request may timeout sometimes, not getting a good way to handle it
                Random randomDelay = new Random();
                System.Threading.Thread.Sleep(randomDelay.Next(100, 5000));
            }

            return html;
        }

        /// <summary>
        /// Initialize the HtmlDocument through removing scripts, styles and comments.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static HtmlDocument GetHtmlDoc(string url)
        {
            return InitializeHtmlDoc(GetHtmlStr(url));
        }

        /// <summary>
        /// Remove script, style and comment code from html string.
        /// </summary>
        /// <param name="htmlString">Indicating the webpage source code.</param>
        /// <returns></returns>
        public static HtmlDocument InitializeHtmlDoc(string htmlString)
        {
            if (string.IsNullOrEmpty(htmlString))
            {
                return null;
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlString);
            doc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style" || n.Name == "#comment")
                .ToList()
                .ForEach(n => n.Remove());

            return doc;
        }

        /// <summary>
        /// save data to local file
        /// </summary>
        /// <param name="path">file save path</param>
        /// <param name="data">data of the file</param>
        public static void SaveToLocalFile(string path, string data)
        {
            string folder = Path.GetDirectoryName(path);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(data);
                    sw.Flush();
                }
            }
        }

        /// <summary>
        /// copy file and update latest file
        /// </summary>
        /// <param name="fileFullName">full name of the file</param>
        public static void CopyFileAndUpdateLatestFile(string fileFullName)
        {
            if (File.Exists(fileFullName))
            {
                string nameNoFolder = Path.GetFileNameWithoutExtension(fileFullName);
                string shortName = nameNoFolder.Substring(0, nameNoFolder.LastIndexOf('.'));

                string folder = InitInfo.FileCopyFolder;
                try
                {
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                    string savePath = Path.Combine(folder, Path.GetFileName(fileFullName));
                    string shortNameFilePath = Path.Combine(folder, shortName + Path.GetExtension(fileFullName));
                    InitInfo.LogMessage("Copy file to " + savePath);
                    File.Copy(fileFullName, savePath);
                    InitInfo.LogMessage("Update file  " + shortNameFilePath);
                    File.Copy(fileFullName, shortNameFilePath, true);
                }
                catch (Exception ex)
                {
                    InitInfo.LogMessage("Failed to CopyFile. Exceptoin: " + ex.GetBaseException().ToString());
                }
            }
        }

        /// <summary>
        /// Get HtmlDocument from local file.
        /// </summary>
        /// <param name="filePath">Indicating the path of local file.</param>
        /// <returns></returns>
        public static string LoadHtmlSnippetFromFile(string path)
        {
            try
            {
                TextReader reader = File.OpenText(@path);
                return reader.ReadToEnd();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
