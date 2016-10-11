using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Serialization;
using Timer = System.Timers.Timer;
using System.Threading;
using QuickGraph.Algorithms.Observers;
using QuickGraph.Algorithms.ShortestPath;

namespace WebCrawler
{
    class Crawler
    {
        public static Uri _rootAddress;
        static int _maxThread;
        static int _threadCount = 0;
        static int _404WebCount = 0;
        static int _networkDiameter = 0;
        static double _networkAveragePathLength = 0;
        static double _clusteringCeofficient = 0;
        public Stopwatch _execTime = new Stopwatch();
        static Queue<KeyValuePair<Uri, Uri>> _toVisitAdresses = new Queue<KeyValuePair<Uri,Uri>>();
        static List<Uri> _visitedAddressList = new List<Uri>();
        static List<string> _robotsBannedAdressList = new List<string>(); 
        public static BidirectionalGraph<string, Edge<string>> _graph = new BidirectionalGraph<string, Edge<string>>();

        protected Timer _timeoutTimer;

        static List<Regex> _Documents = new List<Regex>()
        {
            new Regex("/documents/"),
            new Regex("\\.pdf.*"),
            new Regex("\\.doc.*"),
            new Regex("\\.docx.*"),
            new Regex("\\.ico.*"),
            new Regex("\\.jpg.*"),
            new Regex("\\.png.*"),
            new Regex("\\.zip.*"),
            new Regex("\\.rar.*"),
            new Regex("\\.avi.*"),
            new Regex("\\.gzip.*"),
            new Regex("\\.gz.*"),
            new Regex("\\.tar.*"),
            new Regex("\\.tgz.*"),
            new Regex("\\.7z.*"),
            new Regex("\\.gif"),
        };

        public Crawler(string address, int threadLmit = 10)
        {
            _rootAddress = new Uri(address);
            _maxThread = threadLmit;
            //_robotsBannedAdressList = RobotsText.GetRobotsFile(_rootAddress.Host);
        }

        public void Run()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            _visitedAddressList.Add(_rootAddress);
            _graph.AddVertex(_rootAddress.AbsoluteUri);

            var data = GetDataFromAdress(_rootAddress, _rootAddress);
            if (data == "")
            {
                return;
            }

            SaveToFile(_rootAddress, data);

            List<Uri> newAddressUriList = GetAllLinksFromData(data, _rootAddress);

            foreach (var newAddressUri in newAddressUriList)
            {
                if (!_toVisitAdresses.Any(x => x.Key.Equals(newAddressUri)))
                    _toVisitAdresses.Enqueue(new KeyValuePair<Uri, Uri>(newAddressUri, _rootAddress));
                _graph.AddVerticesAndEdge(new Edge<string>(_rootAddress.AbsoluteUri, newAddressUri.AbsoluteUri));
            }

            var threads = new List<Thread>();
            for (int i = 0; i < _maxThread; i++)
            {
                var thread = new Thread(() => CrawlThread());
                thread.Start();
                threads.Add(thread);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
            stopwatch.Stop();
            _execTime = stopwatch;
        }

       

        private static void CrawlThread()
        {
            bool endThread = false;
            while (endThread != true)
            {
                KeyValuePair<Uri, Uri> addressKeyValuePair = new KeyValuePair<Uri, Uri>();
                lock (_toVisitAdresses)
                {
                    lock (_visitedAddressList)
                    {
                        if (_toVisitAdresses.Count == 0)
                        {
                            endThread = true;
                            continue;
                        }
                        addressKeyValuePair = _toVisitAdresses.Dequeue();
                        //Console.WriteLine("Thread {0} take web: {1} in queue stay: {2}", Thread.CurrentThread.ManagedThreadId, addressKeyValuePair.Key, _toVisitAdresses.Count);

                        _visitedAddressList.Add(addressKeyValuePair.Key);
                    }

                }
                
                

                var data = GetDataFromAdress(addressKeyValuePair.Key, addressKeyValuePair.Value);
                if (data == "")
                {
                    lock (_graph)
                    {
                        _graph.RemoveEdge(new Edge<string>(addressKeyValuePair.Value.AbsoluteUri,
                        addressKeyValuePair.Key.AbsoluteUri));
                        _graph.RemoveVertex(addressKeyValuePair.Key.AbsoluteUri);
                    }
                    continue;
                }

                SaveToFile(addressKeyValuePair.Key, data);

                List<Uri> newAddressUriList = GetAllLinksFromData(data, addressKeyValuePair.Key);

                foreach (var newAddressUri in newAddressUriList)
                {
                    lock (_visitedAddressList)
                    {
                        bool alreadyExists = _visitedAddressList.Any(x => x.AbsoluteUri.Equals(newAddressUri.AbsoluteUri));
                        if (alreadyExists)
                        {
                            lock (_graph)
                            {
                                if (!_graph.ContainsVertex(newAddressUri.AbsoluteUri))
                                {
                                    _graph.AddVertex(newAddressUri.AbsoluteUri);
                                }
                                if (!_graph.ContainsEdge(addressKeyValuePair.Key.AbsoluteUri,
                                newAddressUri.AbsoluteUri))
                                    _graph.AddEdge(new Edge<string>(addressKeyValuePair.Key.AbsoluteUri,
                                newAddressUri.AbsoluteUri));
                            }
                        }
                        else
                        {
                            lock (_toVisitAdresses)
                            {
                                if (!_toVisitAdresses.Any(x => x.Key.Equals(newAddressUri)))
                                    _toVisitAdresses.Enqueue(new KeyValuePair<Uri, Uri>(newAddressUri, addressKeyValuePair.Key));
                                lock (_graph)
                                {
                                    if (!_graph.ContainsEdge(addressKeyValuePair.Key.AbsoluteUri,
                                newAddressUri.AbsoluteUri))
                                        _graph.AddVerticesAndEdge(new Edge<string>(addressKeyValuePair.Key.AbsoluteUri,
                                newAddressUri.AbsoluteUri));
                                }
                            }
                        }
                    }
                    
                }
            }
        }


        private static string GetDataFromAdress(Uri address, Uri source)
        {
            var request = (HttpWebRequest)WebRequest.Create(address.AbsoluteUri);
            request.KeepAlive = false;
            request.ProtocolVersion = HttpVersion.Version10;
            request.ServicePoint.ConnectionLimit = 1;
            request.UseDefaultCredentials = true;

            try
            {
                var response = request.GetResponse();
                var stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string data = reader.ReadToEnd();

                var metaRobots = Regex.Match(data, "<meta\\sname=\\\"robots\\\"\\scontent=\\\"([a-z,]*)\\\">");
                if (metaRobots.Success)
                {
                    var metaRobotsContent = metaRobots.Groups[1].Value;
                    if (metaRobotsContent.Contains("nofollow") || metaRobotsContent.Contains("noindex"))
                        return "";
                }

                return data;
            }
            catch (WebException errorException)
            {
                ErrorMessage("Can't open " + address + "\n from: " + source.AbsoluteUri);// + ": " + errorException);
                    _404WebCount++;
                return "";
            }

        }

        private static List<Uri> GetAllLinksFromData(string data, Uri adress)
        {
            List<Uri> newAdressList = new List<Uri>();
            var match = Regex.Match(data, "<a href=\\\"([^mailto#][-A-Za-z0-9,.:;?@_~/&]*)\\\"");

            while (match.Success)
            {
                var matchValue = match.Groups[1].Value;

                if (!matchValue.StartsWith("http"))
                    matchValue = FixPath(matchValue, adress.AbsoluteUri);

                try
                {
                    Uri matchValueUri = new Uri(matchValue);
                    if (isNotBanned(matchValueUri.AbsoluteUri) && matchValueUri.Host == _rootAddress.Host)
                        newAdressList.Add(matchValueUri);
                }
                catch (Exception ex)
                {
                    ErrorMessage("match: " + matchValue + " FROM ADDRESS: " + adress.AbsoluteUri);
                    ErrorMessage("Can't parse match: " + ex);
                }
                
                match = match.NextMatch();
            }

            return newAdressList;

        }

        private static bool isNotBanned(string address)
        {
            foreach (var banned in _robotsBannedAdressList)
                if (address.Contains(banned))
                    return false;
            foreach (var banned in _Documents)
                if (banned.IsMatch(address.ToLower()))
                    return false;

            return true;
        }

        public static string FixPath(string link, string originatingUrl)
        {
            string formattedLink = String.Empty;

            if (link.IndexOf("../") > -1)
            {
                formattedLink = ResolveRelativePaths(link, originatingUrl);
            }
            else if (link.StartsWith("/"))
            {
                formattedLink = _rootAddress.AbsoluteUri.Substring(0, _rootAddress.AbsoluteUri.LastIndexOf("/")) + link;
            }
            else if ((originatingUrl.IndexOf(_rootAddress.AbsolutePath) > -1
                     && link.IndexOf(_rootAddress.AbsolutePath) == -1) || link.IndexOf("./") > -1)
            {
                formattedLink = originatingUrl.Substring(0, originatingUrl.LastIndexOf("/") + 1) + link;
            }
            else if (link.IndexOf(_rootAddress.AbsolutePath) == -1)
            {
                formattedLink = _rootAddress.AbsolutePath + link;
            }
            else
            {
                formattedLink = _rootAddress.AbsoluteUri.Substring(0, _rootAddress.AbsoluteUri.LastIndexOf("/")) + link;
                //formattedLink = originatingUrl.Substring(0, originatingUrl.LastIndexOf("/")) + link;
            }

            return formattedLink;
        }

        private static string ResolveRelativePaths(string relativeUrl, string originatingUrl)
        {
            string resolvedUrl = String.Empty;

            string[] relativeUrlArray = relativeUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string[] originatingUrlElements = originatingUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int indexOfFirstNonRelativePathElement = 0;
            for (int i = 0; i <= relativeUrlArray.Length - 1; i++)
            {
                if (relativeUrlArray[i] != "..")
                {
                    indexOfFirstNonRelativePathElement = i;
                    break;
                }
            }

            int countOfOriginatingUrlElementsToUse = 0;
            if (originatingUrl.Substring(originatingUrl.Length-1) == "/")
            {
                countOfOriginatingUrlElementsToUse = originatingUrlElements.Length - indexOfFirstNonRelativePathElement;
            }
            else
            {
                countOfOriginatingUrlElementsToUse = originatingUrlElements.Length - indexOfFirstNonRelativePathElement - 1;
            }
            
            for (int i = 0; i <= countOfOriginatingUrlElementsToUse - 1; i++)
            {
                if (originatingUrlElements[i] == "http:" || originatingUrlElements[i] == "https:")
                    resolvedUrl += originatingUrlElements[i] + "//";
                else
                    resolvedUrl += originatingUrlElements[i] + "/";
            }

            for (int i = 0; i <= relativeUrlArray.Length - 1; i++)
            {
                if (i >= indexOfFirstNonRelativePathElement)
                {
                    resolvedUrl += relativeUrlArray[i];

                    if (i < relativeUrlArray.Length - 1)
                        resolvedUrl += "/";
                }
            }

            return resolvedUrl;
        }


        // SAVE
        private static void SaveToFile(Uri address, string data)
        {
            try
            {
                (new FileInfo("C:\\Users\\Admin\\Desktop\\crawl-files\\" + address.AbsolutePath)).Directory.Create();

                string index = null;

                if (address.AbsolutePath.Trim().EndsWith(@"/"))
                    index = "index";

                StreamWriter file = null;
                if (address.AbsolutePath.Trim().EndsWith(".html"))
                    file = new StreamWriter("C:\\Users\\Admin\\Desktop\\crawl-files\\" + address.AbsolutePath + index);
                else
                    file = new StreamWriter("C:\\Users\\Admin\\Desktop\\crawl-files\\" + address.AbsolutePath + index + ".html");

                file.WriteLine(data);

                file.Close();
            }
            catch (Exception exception)
            {
                ErrorMessage(exception.ToString());
            }
            

        }


        public void SaveGraph()
        {

            using (var xwriter = XmlWriter.Create("C:\\Users\\Admin\\Desktop\\dump1.graphml"))
                _graph.SerializeToGraphML<string, Edge<string>, BidirectionalGraph<string, Edge<string>>>(xwriter);

        }

        public void SaveGraphAnalysys()
        {
            Console.WriteLine("STARTING Analysis graph\n");

            string log = null;

            log += "Website graph: " + _rootAddress;
            log += "\n";
            log += "Crawl time: " + _execTime.ElapsedMilliseconds + "[ms]";
            log += "\n";
            log += "\n\n";

            log += "404Error: " + _404WebCount;
            log += "\n";
            log += "Vertices: " + _graph.Vertices.Count();
            log += "\n";
            log += "Edges: " + _graph.Edges.Count();
            log += "\n\n";


            //In Edge schedule
            Dictionary<int,double> inEdgeList = new Dictionary<int,double>();
            int allInEdge = 0;
            foreach (var vertex in _graph.Vertices)
            {
                int inDegreeVertex = _graph.InDegree(vertex);
                if (inEdgeList.ContainsKey(inDegreeVertex))
                {
                    inEdgeList[inDegreeVertex]++;
                    allInEdge++;
                }
                else
                {
                    inEdgeList.Add(inDegreeVertex,1);
                    allInEdge++;
                }
            }

            log += "In Edge schedule: in file inEdgeCSV.csv\n";

            Console.WriteLine("In Edge schedule - done\n");

            var inEdgeCSV = new StringBuilder();
            
            foreach (var d in inEdgeList.OrderBy(key => key.Key))
            {
                inEdgeCSV.AppendLine(d.Key + "," + d.Value / allInEdge + "," + d.Value);
            }

            File.WriteAllText("C:\\Users\\Admin\\Desktop\\inEdgeCSV.csv", inEdgeCSV.ToString());

            log += "All inEdges: " + allInEdge;

            log += "\n\n";

            //Out Edge schedule
            Dictionary<int, double> outEdgeList = new Dictionary<int, double>();
            int allOutEdge = 0;
            foreach (var vertex in _graph.Vertices)
            {
                int outDegreeVertex = _graph.OutDegree(vertex);
                if (outEdgeList.ContainsKey(outDegreeVertex))
                {
                    outEdgeList[outDegreeVertex]++;
                    allOutEdge++;
                }
                else
                {
                    outEdgeList.Add(outDegreeVertex, 1);
                    allOutEdge++;
                }
            }

            log += "Out Edge schedule: in file outEdgeCSV.csv\n";

            Console.WriteLine("Out Edge schedule - done\n");

            var outEdgeCSV = new StringBuilder();

            foreach (var d in outEdgeList.OrderBy(key => key.Key))
            {
                //log += d.Key + ": " + d.Value/allOutEdge + "\n";
                outEdgeCSV.AppendLine(d.Key + "," + d.Value / allOutEdge + "," + d.Value);
            }

            File.WriteAllText("C:\\Users\\Admin\\Desktop\\outEdgeCSV.csv", outEdgeCSV.ToString());

            log += "All outEdges: " + allOutEdge;

            log += "\n\n\n";

            
            foreach (var vertex in _graph.Vertices)
            {
                Func<Edge<string>, double> edgeCost = e => 1;

                // We want to use Dijkstra on this graph
                DijkstraShortestPathAlgorithm<string, Edge<string>> dijkstra = new DijkstraShortestPathAlgorithm<string, Edge<string>>(_graph, edgeCost);

                //// attach a distance observer to give us the shortest path distances
                VertexDistanceRecorderObserver<string, Edge<string>> distObserver = new VertexDistanceRecorderObserver<string, Edge<string>>(edgeCost);

                using (distObserver.Attach(dijkstra))
                {
                        //// Run the algorithm with A set to be the source

                        dijkstra.Compute(vertex);
                        foreach (KeyValuePair<string, double> kvp in distObserver.Distances)
                        {
                            if (_networkDiameter < kvp.Value)
                                _networkDiameter = (int)kvp.Value;
                            _networkAveragePathLength += kvp.Value;
                            //log += "Distance from " + vertex + " to node" + kvp.Key + " is " + kvp.Value + "\n";
                            //Console.WriteLine("Distance from {2} to node {0} is {1}", kvp.Key, kvp.Value, vertex);
                        }

                }
            }

            log += "Network Diameter: " + _networkDiameter;
            log += "\n\n";
            Console.WriteLine("Network Diameter - done\n");


            _networkAveragePathLength = _networkAveragePathLength/(_graph.VertexCount*(_graph.VertexCount-1));

            log += "Average path length: " + _networkAveragePathLength;
            log += "\n\n";
            Console.WriteLine("Average path length - done\n");

            foreach (var vertex in _graph.Vertices)
            {
                int connectVertexCount = 0;
                List<string> connectedVertexList = new List<string>();
                var outEdges = _graph.OutEdges(vertex);
                foreach (var outEdge in outEdges)
                {
                    if(!connectedVertexList.Contains(outEdge.Target))
                        connectedVertexList.Add(outEdge.Target);
                }
                var inEdges = _graph.InEdges(vertex);
                foreach (var inEdge in inEdges)
                {
                    if (!connectedVertexList.Contains(inEdge.Source))
                        connectedVertexList.Add(inEdge.Source);
                }

                foreach (var connectVertex in connectedVertexList)
                    foreach (var connectVertex2 in connectedVertexList)
                    {
                        if (_graph.ContainsEdge(connectVertex, connectVertex2))
                            connectVertexCount++;
                    }
                        

                if(connectVertexCount > 1)
                    _clusteringCeofficient += (double)connectVertexCount / (connectedVertexList.Count * (connectedVertexList.Count - 1));
            }

            log += "Average clustering coefficient: " + _clusteringCeofficient/_graph.VertexCount;
            log += "\n\n";

            Console.WriteLine("Average clustering coefficient - done\n");

            double graphDensity = (double)(2*_graph.EdgeCount)/(_graph.VertexCount*(_graph.VertexCount - 1));
            log += "Graph density: " + graphDensity;
            log += "\n\n";

            Console.WriteLine("Density - done\n");


            StreamWriter file = new StreamWriter("C:\\Users\\Admin\\Desktop\\graph_log.txt");

            file.WriteLine(log);
        
            file.Close();

            Console.WriteLine("Save file - done\n");
        }



        private static void ErrorMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + message);
            Console.ResetColor();
        }

        // SHOW
        public string ShowAllAddresses()
        {
            string allAddresses = null;
            int i = 0;

            foreach (var uri in _visitedAddressList)
            {
                allAddresses += "\n" + i + ": " + uri.AbsoluteUri;
                i++;
            }
            return allAddresses;
        }

        public string ShowAllGraphVertex()
        {
            string b = null;
            b += _graph.VertexCount + "\n";
            /*int i = 0;
            foreach (var vertex in _graph.Vertices)
            {
                b += i + ": " + vertex + "\n";
                i++;
            }*/
            return b;

        }

        public string ShowAllGraphEdges()
        {
            string b = null;
            b += _graph.Edges.Count() + "\n";
            /*int i = 0;
            foreach (var edge in _graph.Edges)
            {
                b += i + ": " + edge + "\n"; 
                i++;
            }*/
            return b;

        }

        public int Count404()
        {
            return _404WebCount;
        }

        public void ReadGraphFromFile(string source)
        {

            using (var xreader = XmlReader.Create(source))
            {
                _graph.DeserializeFromGraphML(xreader, id => id, (source2, target, id) => new Edge<string>(source2, target));
            }
            
        }

        public double PageRank(double alpha = 0.85)
        {
            Dictionary<string, double> pageRanks = new Dictionary<string, double>();
            double averagePG = 0;
            double averagePG_temp = -1;
            int iteration = 0;
            int iterationEqual = 0;
            int n = _graph.VertexCount;

            //initiation value
            foreach (var vertex in _graph.Vertices)
            {
                pageRanks.Add(vertex, 0/n);
            }


            bool stopIteration = false;

            while (!stopIteration)
            {
                if (averagePG == averagePG_temp)
                {
                    Console.WriteLine("AVERAGE IS THE SAME LIKE AVERAGE_TEMP");
                    if (iterationEqual == 10)
                        stopIteration = true;
                    iterationEqual++;
                }
                else
                    averagePG_temp = averagePG;
                averagePG = 0;
                iteration++;

                if (iteration == 1)
                    stopIteration = true;

                foreach (var vertex in _graph.Vertices)
                {
                    double rankSum = 0;
                    foreach (var inEdge in _graph.InEdges(vertex))
                    {
                        if (_graph.InDegree(inEdge.Source) != 0)
                            rankSum += pageRanks[inEdge.Source]/_graph.InDegree(inEdge.Source);
                        //Console.WriteLine("{0}/{1}", pageRanks[inEdge.Source], _graph.InDegree(inEdge.Source));
                    }
                    pageRanks[vertex] = (1 - alpha) + alpha*rankSum;
                    averagePG += pageRanks[vertex];
                }
                averagePG = averagePG / n;
                Console.WriteLine("{2}: After iteration {0} average is: {1}", iteration, averagePG, alpha);
            }

            return averagePG;
        }
    }
}
