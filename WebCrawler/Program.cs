using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            string web = "http://pwsz.elblag.pl";

            Crawler crawl = new Crawler(web);

            Console.WriteLine("Starting crawl: " + web);
            crawl.Run();
            crawl.SaveGraph();
            //crawl.SaveGraphAnalysys();
            Console.WriteLine("\n");
            //Console.WriteLine("ALL VISITED ADDRESSES:");
            //Console.WriteLine(crawl.ShowAllAddresses());

            Console.WriteLine("\n");
            Console.WriteLine("Time [ms]:");
            Console.WriteLine(crawl._execTime.ElapsedMilliseconds);

            Console.WriteLine("\n");
            Console.WriteLine("ALL VERTEX:");
            Console.WriteLine(crawl.ShowAllGraphVertex());

            Console.WriteLine("\n");
            Console.WriteLine("ALL EDGES:");
            Console.WriteLine(crawl.ShowAllGraphEdges());

            Console.WriteLine("\n");
            Console.WriteLine("404 errors:");
            Console.WriteLine(crawl.Count404());

            Console.WriteLine("\n");
            crawl.SaveGraphAnalysys();


            Console.WriteLine("\n\n\n");
            Console.WriteLine("DONE");
            Console.Read();


        }
    }
}
