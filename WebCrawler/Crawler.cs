using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebCrawler
{
    class Crawler
    {
        public static string _rootAdress;
        public static int _maxThread;
        public static List<string> _visitedAddressList = new List<string>(); 

        public Crawler(string adress, int threadLimit = 100)
        {
            _rootAdress = adress;
            _maxThread = threadLimit;
        }

        public void Run()
        {
            Crawl(_rootAdress, 0);

        }

        public string ShowAllAddesses()
        {
            return string.Join("\n", _visitedAddressList.ToArray());
        }

        private static void Crawl(string address, int parentId)
        {
            var data = GetDataFromAdress(address);
            if (data == "")
            {
                return;
            }
            
            _visitedAddressList.Add(address);
            parentId += 1;
            List<string> newAdressList = GetAllLinksFromData(data);

            foreach (string newAdress in newAdressList)
            {
                _visitedAddressList.Add(newAdress);
                Crawl(newAdress, parentId);
            }

        }

        private static List<string> GetAllLinksFromData(string data)
        {
            List<string> newAdressList = new List<string>();
            var match = Regex.Match(data, "<a href=\\\"([^mailto#][-A-Za-z0-9,.:;?@_~/&]*)\\\"");

            while (match.Success)
            {
                var matchValue = match.Groups[1].Value;
                if (!matchValue.StartsWith("http"))
                {
                    matchValue = _rootAdress + matchValue; //@Todo: repeair addresses geter to better get dynamic/static links
                }

                if (AdressHasNotBeenVisited(matchValue))
                {
                    newAdressList.Add(matchValue);
                }
                match = match.NextMatch();
            }

            return newAdressList;

        }

        private static string GetDataFromAdress(string address)
        {
            var request = WebRequest.Create(address);
            request.UseDefaultCredentials = true;


            try
            {
                var response = request.GetResponse();
                var stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (WebException errorException)
            {
                Console.WriteLine("Can't open " + address + ": " + errorException);
                return "";
            }
            
        }

        private static bool AdressHasNotBeenVisited(string address)
        {
            if (_visitedAddressList.Contains(address))
            {
                return false;
            }

            return true;
        }
    }
}
