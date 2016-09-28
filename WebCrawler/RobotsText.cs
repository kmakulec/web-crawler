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
    static class RobotsText
    {
        static public bool GetRobotsMetaData(Uri address)
        {
            try
            {
                var req = WebRequest.Create(address.AbsoluteUri);
                var webResponse = req.GetResponse();
                var webStream = webResponse.GetResponseStream();
                StreamReader webReader = new StreamReader(webStream);

                while (!webReader.EndOfStream)
                {
                    var data = webReader.ReadLine().ToLower();
                    var metaRobots = Regex.Match(data, "<meta\\sname=\\\"robots\\\"\\scontent=\\\"([a-z,]*)\\\">");
                    if (metaRobots.Success)
                    {
                        var metaRobotsContent = metaRobots.Groups[1].Value;
                        if (metaRobotsContent.Contains("nofollow") || metaRobotsContent.Contains("noindex"))
                            return false;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        static public List<string> GetRobotsFile(string address)
        {
            List<string> disallowPlaces = new List<string>();
            var userFilter = "user-agent:";
            var allowFilter = "allow:";
            var disallowFilter = "disallow:";

            Console.WriteLine("http://" + address + "/robots.txt");
            try
            {
                var req = WebRequest.Create("http://" + address + "/robots.txt");
                var webStream = req.GetResponse().GetResponseStream();
                StreamReader webReader = new StreamReader(webStream);
                while (!webReader.EndOfStream)
                {
                    var data = webReader.ReadLine().ToLower();
                    if (data.StartsWith(userFilter))
                    {
                        var user = data.Substring(userFilter.Length).Trim();
                        if (user == "*")
                            while (!string.IsNullOrEmpty(data))
                            {
                                data = webReader.ReadLine();
                                if (!string.IsNullOrEmpty(data))
                                {
                                    data = data.ToLower();
                                }
                                else
                                {
                                    break;
                                }
                                if (data.StartsWith(disallowFilter))
                                {
                                    if (data.EndsWith("*"))
                                        data = data.TrimEnd('*');
                                    var newAddress = data.Substring(disallowFilter.Length).Trim();
                                    if (!newAddress.StartsWith(address))
                                        newAddress = address + newAddress;
                                    disallowPlaces.Add(newAddress);
                                }
                                else
                                {
                                    if (data.StartsWith(allowFilter))
                                        continue;
                                    else
                                        break;
                                }
                            }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Norobots txt file");
            }
            return disallowPlaces;
        }
    
    }
}
