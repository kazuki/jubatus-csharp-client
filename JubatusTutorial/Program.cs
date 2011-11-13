using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Jubatus.Client;

namespace Jubatus.Tutorial
{
    class Program : IDisposable
    {
        public static void Main (string[] args)
        {
            // default options
            EndPoint[] servers = new EndPoint[] {new IPEndPoint(IPAddress.Loopback, 9199)};
            string name = "test";
            string algo = "PA";

            // parse arguments
            if (args.Length % 2 != 0) {
                ShowUsage ();
                return;
            }
            for (int i = 0; i < args.Length; i += 2) {
                switch (args[i]) {
                    case "-s":
                    case "--server_list":
                        servers = ParseIPEndPoints (args[i + 1]);
                        break;
                    case "-n":
                    case "--name":
                        name = args[i + 1];
                        break;
                    case "-a":
                    case "--algo":
                        algo = args[i + 1];
                        break;
                    default:
                        ShowUsage();
                        return;
                }
            }

            using (Program prog = new Program (servers, name, algo)) {
                prog.Run ();
            }
        }

        public Program (EndPoint[] servers, string name, string algo)
        {
            this.RPC = new TinyMsgPackRPC(8, 128, 1024, TimeSpan.FromSeconds(2));
            this.ServerChooser = new RoundRobinChooser(servers);
            this.Servers = servers;
            this.Config = new Config {
                method = algo,
                converter = new ConverterConfig {
                    string_filter_types = new Dictionary<string,Dictionary<string,string>> {
                        {"detag", new Dictionary<string, string> {
                            {"method", "regexp"},
                            {"pattern", "<[^>]*>"},
                            {"replace", ""}}}
                    },
                    string_filter_rules = new List<FilterRule> {
                        new FilterRule {key = "message", type = "detag", suffix = "-detagged"}
                    },
                    num_filter_types = new Dictionary<string,Dictionary<string,string>> (),
                    num_filter_rules = new List<FilterRule> (),
                    string_types = new Dictionary<string,Dictionary<string,string>> (),
                    string_rules = new List<StringRule> {
                        new StringRule {key = "message-detagged", type = "space", sample_weight = "bin", global_weight = "bin"}
                    },
                    num_types = new Dictionary<string,Dictionary<string,string>> (),
                    num_rules = new List<NumRule> ()
                }
            };
            this.Classifier = new Classifier(RPC, ServerChooser, name);
        }

        public void Run ()
        {
            Classifier.SetConfig (Config);

            using (StreamReader reader = new StreamReader ("train.dat")) {
                string line;
                KeyValuePair<string, Datum>[] data = new KeyValuePair<string,Datum>[1];
                while ((line = reader.ReadLine ()) != null) {
                    string[] items = line.Split (new char[] {','}, 2);
                    string text;
                    using (StreamReader freader = new StreamReader (items[1])) {
                        text = freader.ReadToEnd ();
                    }
                    data[0] = new KeyValuePair<string,Datum> (items[0], new Datum {
                        string_values = new List<KeyValuePair<string,string>> () {
                            new KeyValuePair<string, string> ("message", text)
                        }
                    });
                    Classifier.Train (data);
                    Console.WriteLine (line);
                }
            }

            Classifier.Save ("tutorial");
            Classifier.Load ("tutorial");

            int total = 0, ok = 0;
            using (StreamReader reader = new StreamReader ("test.dat")) {
                string line;
                while ((line = reader.ReadLine ()) != null) {
                    string[] items = line.Split (new char[] {','}, 2);
                    string text;
                    using (StreamReader freader = new StreamReader (items[1])) {
                        text = freader.ReadToEnd ();
                    }
                    Datum data = new Datum {
                        string_values = new List<KeyValuePair<string,string>> () {
                            new KeyValuePair<string, string> ("message", text)
                        }
                    };
                    EstimateResults[] results = Classifier.Classify (data);
                    EstimateResults mostLikely = EstimateResults.ChooseMostLikely (results);
                    bool isOK = items[0].CompareTo (mostLikely.Label) == 0;
                    ++total;
                    if (isOK) ++ok;
                    Console.WriteLine ("{2:p2} {1} {0}", line, isOK ? "OK" : "NG", ok / (double)total);
                }
            }
        }

        public void Dispose ()
        {
            RPC.Dispose ();
        }

        public Config Config { get; private set; }
        public IRPC RPC { get; private set; }
        public EndPoint[] Servers { get; private set; }
        public IServerChooser ServerChooser { get; private set; }
        public Classifier Classifier { get; private set; }

        static EndPoint[] ParseIPEndPoints (string text)
        {
            string[] endpoints = text.Split(',');
            EndPoint[] ret = new EndPoint[endpoints.Length];
            for (int i = 0; i < endpoints.Length; i ++) {
                string[] items = endpoints[i].Split(':');
                ushort port;
                IPAddress adrs;
                if (items.Length > 2 || !ushort.TryParse(items[1], out port) || !IPAddress.TryParse(items[0], out adrs))
                    throw new FormatException ("Parse   Error: " + text);
                ret[i] = new IPEndPoint (adrs, (int)port);
            }
            return ret;
        }

        static void ShowUsage ()
        {
            Console.WriteLine ("Usage: tutorial.py [options]\r\n\r\n" +
                                "Options:\r\n" +
                                "  -s SERVER_LIST, --server_list=SERVER_LIST\r\n" +
                                "         (ex: 192.168.1.1:9199,192.168.1.2:9199)\r\n" +
                                "  -n NAME, --name=NAME\r\n" +
                                "  -a ALGO, --algo=ALGO");
        }
    }
}
