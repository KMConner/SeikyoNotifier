using System;
using System.IO;
using System.Net;
using System.Text;
using static System.Console;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace SeikyoNotifier
{

    public class SeikyoController
    {
        private string user;
        private string pass;
        private const string TopUrl = "https://mp.seikyou.jp/mypage/Static.init.do";
        private const string SignInUrl = "https://mp.seikyou.jp/mypage/Auth.login.do";
        private const string MenuChangeUrl = "https://mp.seikyou.jp/mypage/Menu.change.do?pageNm=ALL_HISTORY";

        private static readonly Regex dateRegex = new Regex(@"(?<month>\d{1,2})/(?<day>\d{1,2})",
             RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly TimeSpan timeOffset = TimeSpan.FromHours(9);

        private CookieContainer cookie;

        public SeikyoController(string userName, string password)
        {
            user = userName;
            pass = password;
            cookie = new CookieContainer();
        }

        public int GetBalance()
        {
            GetTopPage();
            SignIn();
            GetBalanceData();
            return 334;

        }

        private void GetTopPage()
        {
            WriteLine("Retriving top page...");
            var request = (HttpWebRequest)WebRequest.Create(TopUrl);
            request.CookieContainer = cookie;
            var respose = (HttpWebResponse)request.GetResponse();
            WriteLine($"Retrived top page. Status code : {(int)respose.StatusCode}");
        }

        private void SignIn()
        {
            WriteLine("Sign in...");

            var request = (HttpWebRequest)WebRequest.Create(SignInUrl);
            request.CookieContainer = cookie;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            string content = $"loginId={user}&password={pass}";
            byte[] data = Encoding.UTF8.GetBytes(content);

            request.ContentLength = data.Length;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }

            var respose = (HttpWebResponse)request.GetResponse();
            WriteLine($"Signed in. Status code : {(int)respose.StatusCode}, Redirected to {respose.ResponseUri}");
        }

        private (int balance, DateTimeOffset lastTransaction) GetBalanceData()
        {
            var request = (HttpWebRequest)WebRequest.Create(MenuChangeUrl);
            request.CookieContainer = cookie;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            string requestText = "id=ALL_HISTORY&messageInitDirectionKbn=&messageInfoId";
            byte[] requestData = Encoding.UTF8.GetBytes(requestText);

            request.ContentLength = requestData.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(requestData, 0, requestData.Length);
                stream.Flush();
            }

            var response = (HttpWebResponse)request.GetResponse();
            string responseContent;
            using (var reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("Shift_JIS")))
            {
                responseContent = reader.ReadToEnd();
            }
            return ParseAllPurchase(responseContent);
        }

        private (int balance, DateTimeOffset lastTransaction) ParseAllPurchase(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);
            HtmlNode node = document.DocumentNode.SelectSingleNode(
                "/html/body/center/div[1]/div[3]/form[2]/table[1]/tr[2]/td[1]");

            string lastTransitionText = node.InnerText.Trim();
            Match dateMatch = dateRegex.Match(lastTransitionText);

            if (!dateMatch.Success
                || !int.TryParse(dateMatch.Groups["month"].Value, out var month)
                || !int.TryParse(dateMatch.Groups["day"].Value, out var day))
            {
                throw new Exception("Could not Load last transition date.");
            }

            DateTimeOffset now = DateTimeOffset.UtcNow + timeOffset;
            int year = now.Month > month ? now.Year - 1 : now.Year;
            var lastTransaction = new DateTimeOffset(year, month, day, 0, 0, 0, timeOffset);

            WriteLine($"Last transaction date: {lastTransaction}");

            HtmlNode node2 = document.DocumentNode.SelectSingleNode(
                "/html/body/center/div[1]/div[2]/div/div[1]/table/tr[2]/td/span");
            WriteLine($"Balance: {node2.InnerText}");

            if (!int.TryParse(node2.InnerText.Trim(), out var balance))
            {
                throw new Exception("Could not load balance");
            }

            return (balance, lastTransaction);
        }
    }
}
