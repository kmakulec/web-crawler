using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            string web = "http://";

            Console.WriteLine("Set website to crawl:\n");
            Console.Write(web);
            web += Console.ReadLine();

            Crawler crawl = new Crawler(web);
            Console.WriteLine("Starting crawl: " + web);
            crawl.Run();
            //crawl.SaveGraph();
            //crawl.SaveGraphAnalysys();
            Console.WriteLine("\n");
            Console.WriteLine("ALL VISITED ADDRESSES:");
            Console.WriteLine(crawl.ShowAllAddresses());

            /*crawl.ReadGraphFromFile("C:\\Users\\Admin\\Desktop\\dump.graphml");
            Console.WriteLine(crawl.ShowAllGraphVertex());

            double[] pgValue = new[] {0.0, 0.05, 0.15, 0.25, 0.35, 0.45, 0.55, 0.65, 0.75, 0.85, 0.95, 1.0};

            double[] result = new double[pgValue.Length];

            int i = 0;
            foreach (var val in pgValue)
            {
                result[i]= crawl.PageRank(val);
                i++;
            }
*/
            /*double sum = 0;
            foreach (var res in result)
            {
                sum += res;
            }*/

/*            for (i = 0; i < result.Length; i++)
            {
                //result[i] = result[i]/sum;
                Console.WriteLine("{0}: {1}", pgValue[i], result[i]);
            }*/
            

            /*Console.WriteLine("\n");
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
            */

            Console.WriteLine("\n\n\n");
            Console.WriteLine("DONE");
            Console.Read();

        }
    }
}
