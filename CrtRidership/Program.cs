using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;


namespace CrtRidership
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Record> records = new List<Record>();
            StreamReader reader;
            try
            {
                reader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "CrtRidership.txt");
            }
            catch (FileNotFoundException)
            {
                FileStream tempStream = File.Create(AppDomain.CurrentDomain.BaseDirectory + "CrtRidership.txt");
                tempStream.Close();
                reader = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "CrtRidership.txt");
            }
            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "DailyRidership");
            string line = reader.ReadLine();
            while (line != null)
            {
                string[] fields = line.Split(',');
                string[] date = fields[0].Split('-');
                records.Add(new Record(date[0], date[1], date[2], fields[1].Trim(), fields[2].Trim()));
                line = reader.ReadLine();
            }
            reader.Close();
            records.Sort();

            WebRequest request = WebRequest.Create("https://weibo.com/p/1008084d2241ffc593ee3a4b33237f1bf2d845?feed_sort=white&feed_filter=white");
            request.Method = "GET";
            request.Timeout = 30000;

            WebHeaderCollection headers = new WebHeaderCollection
            {
                //headers.Add("Accept-Encoding", "gzip, deflate, br");
                // TODO: Uncompress
                { "Accept-Language", "en-US, zh-CN" },
                {
                    "Cookie",
                    "login_sid_t=6c0b479d74c392dda0ec318dfbb47a71; cross_origin_proto=SSL; YF-Ugrow-G0=8751d9166f7676afdce9885c6d31cd61; " +
                "YF-V5-G0=d45b2deaf680307fa1ec077ca90627d1; WBStorage=5548c0baa42e6f3d|undefined; _s_tentry=-; Apache=1816167882893.1203.1526386093628; " +
                "SINAGLOBAL=1816167882893.1203.1526386093628; ULV=1526386094585:1:1:1:1816167882893.1203.1526386093628:; " +
                "SUB=_2AkMtpkAwf8NxqwJRmPEUyWvrbIt2yQvEieKb-rHrJRMxHRl-yT83qn0TtRB6BiZu38UVejCB58mPq6h2Mi27OLpL3yl4; " +
                "SUBP=0033WrSXqPxfM72-Ws9jqgMF55529P9D9Whvh8IiXApTc39X0GNYWOkr; YF-Page-G0=ab26db581320127b3a3450a0429cde69"
                },
                { "DNT", "1" },
                { "Upgrade-Insecure-Requests", "1" },
            };
            request.Headers = headers;
            (request as HttpWebRequest).Accept = "text/html, application/xhtml+xml, application/xml; q=0.9, */*; q=0.8";
            (request as HttpWebRequest).KeepAlive = true;
            (request as HttpWebRequest).Host = "weibo.com";
            (request as HttpWebRequest).UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.140 Safari/537.36 Edge/17.17134";

            (request as HttpWebRequest).CookieContainer = new CookieContainer();

            Cookie[] cookies = new Cookie[]
            {
                new Cookie("_s_tentry", "-"),
                new Cookie("Apache", "1816167882893.1203.1526386093628"),
                new Cookie("cross_origin_proto", "SSL"),
                new Cookie("login_sid_t", "6c0b479d74c392dda0ec318dfbb47a71"),
                new Cookie("SINAGLOBAL", "1816167882893.1203.1526386093628"),
                new Cookie("SUB", "_2AkMtpkAwf8NxqwJRmPEUyWvrbIt2yQvEieKb-rHrJRMxHRl-yT83qn0TtRB6BiZu38UVejCB58mPq6h2Mi27OLpL3yl4"),
                new Cookie("SUBP", "0033WrSXqPxfM72-Ws9jqgMF55529P9D9Whvh8IiXApTc39X0GNYWOkr"),
                new Cookie("ULV", "1526386094585:1:1:1:1816167882893.1203.1526386093628:"),
                new Cookie("WBStorage", "5548c0baa42e6f3d|undefined"),
                new Cookie("YF-Page-G0", "ab26db581320127b3a3450a0429cde69"),
                new Cookie("YF-Ugrow-G0", "8751d9166f7676afdce9885c6d31cd61"),
                new Cookie("YF-V5-G0", "d45b2deaf680307fa1ec077ca90627d1"),
            };
            for (int i = 0; i < cookies.Length; i++)
            {
                cookies[i].Domain = "weibo.com";
                (request as HttpWebRequest).CookieContainer.Add(cookies[i]);
            }
            HttpWebResponse response;
            try
            {
                response = request.GetResponse() as HttpWebResponse;
            }
            catch (WebException)
            {
                return;
            }

            string responseText = new StreamReader(response.GetResponseStream()).ReadToEnd();

            responseText = responseText.Split(new[] { "发布时间排序" }, StringSplitOptions.None)[1];
            MatchCollection matchesYear = Regex.Matches(responseText, "title=\\\\\"\\d{4}");
            MatchCollection matchesBody = Regex.Matches(responseText, "#昨日客运量#\\<\\\\/a\\>(\\s*|(&nbsp;)*)\\d+月\\d+日，重庆轨道交通线网客运量\\d*.?\\d*万乘次。");
            MatchCollection matchesImg = Regex.Matches(responseText, "wx\\d?.sinaimg.cn%2F\\S*%2F\\S*.jpg");

            for (int i = 0; i < matchesYear.Count; i++)
            {
                string year = Regex.Match(matchesYear[i].Value, "\\d{4}").Value;
                MatchCollection bodyValues = Regex.Matches(matchesBody[i].Value, "\\d+");
                string month = bodyValues[0].Value;
                string day = bodyValues[1].Value;
                string rider = bodyValues[2].Value + "." + bodyValues[3].Value;
                string img = matchesImg[i].Value.Replace("%2F", "/");

                Record tempRec = new Record(year, month, day, rider, img);

                bool exist = false;
                foreach (Record r in records)
                {
                    if (r.CompareTo(tempRec) == 0) { exist = true; break; }
                }
                if (!exist)
                {
                    Console.WriteLine("New record: " + tempRec);
                    records.Add(tempRec);
                    Console.WriteLine("Downloading details...");
                    DownloadImage(tempRec.Image, $"{tempRec.Date.Year}-{tempRec.Date.Month}-{tempRec.Date.Day}");
                }
            }

            records.Sort();

            StreamWriter writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "CrtRidership.txt", false);

            foreach (Record r in records)
            {
                writer.WriteLine(r);
            }

            writer.Close();

            Console.WriteLine("Task complete. All data saved to the directory of this app.");
        }

        static public void DownloadImage(string uri, string date)
        {
            string[] param = uri.Split('/');
            FileStream imgStream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + $"DailyRidership\\{date}.jpg", FileMode.Create, FileAccess.Write);
            WebRequest request = WebRequest.Create($"http://{uri}");
            request.Method = "GET";
            request.Timeout = 30000;

            WebHeaderCollection headers = new WebHeaderCollection
            {
                { "Accept-Language", "en-US, zh-CN" },
                {
                    "Cookie",
                    "login_sid_t=6c0b479d74c392dda0ec318dfbb47a71; cross_origin_proto=SSL; YF-Ugrow-G0=8751d9166f7676afdce9885c6d31cd61; " +
                "YF-V5-G0=d45b2deaf680307fa1ec077ca90627d1; WBStorage=5548c0baa42e6f3d|undefined; _s_tentry=-; Apache=1816167882893.1203.1526386093628; " +
                "SINAGLOBAL=1816167882893.1203.1526386093628; ULV=1526386094585:1:1:1:1816167882893.1203.1526386093628:; " +
                "SUB=_2AkMtpkAwf8NxqwJRmPEUyWvrbIt2yQvEieKb-rHrJRMxHRl-yT83qn0TtRB6BiZu38UVejCB58mPq6h2Mi27OLpL3yl4; " +
                "SUBP=0033WrSXqPxfM72-Ws9jqgMF55529P9D9Whvh8IiXApTc39X0GNYWOkr; YF-Page-G0=ab26db581320127b3a3450a0429cde69"
                },
                { "DNT", "1" },
                { "Upgrade-Insecure-Requests", "1" },
            };
            request.Headers = headers;
            (request as HttpWebRequest).Accept = "*/*";
            (request as HttpWebRequest).KeepAlive = true;
            (request as HttpWebRequest).Host = param[0];
            (request as HttpWebRequest).UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.140 Safari/537.36 Edge/17.17134";
            (request as HttpWebRequest).Referer = "diagnostics://4/";

            (request as HttpWebRequest).CookieContainer = new CookieContainer();

            HttpWebResponse response;
            try
            {
                response = request.GetResponse() as HttpWebResponse;
            }
            catch (WebException)
            {
                return;
            }
            response.GetResponseStream().CopyTo(imgStream);
            imgStream.Close();
        }

        public class Record : IComparable<Record>
        {
            public DateTime Date { get; }
            public double Ridership { get; }
            public string Image { get; }

            public Record(string year, string month, string day, string ridership, string image)
            {
                Date = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));
                Ridership = double.Parse(ridership);
                Image = image;
            }

            public override string ToString()
            {
                return $"{Date.Year}-{Date.Month}-{Date.Day}, {Ridership}, {Image}";
            }

            public int CompareTo(Record target)
            {
                return -Date.CompareTo(target.Date);
            }
            int IComparable<Record>.CompareTo(Record target)
            {
                return -Date.CompareTo(target.Date);
            }
        }
        public class RecordRiderComparer : IComparer<Record>
        {
            int IComparer<Record>.Compare(Record x, Record y)
            {
                if (x.Ridership == y.Ridership) { return 0; }
                return x.Ridership > y.Ridership ? 1 : -1;
            }
        }
    }
}

