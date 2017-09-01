using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace vs_oflinstall_cleaner
{
    class Cleaner
    {
        static void Main(string[] args)
        {
            Console.Title = "Visual Studio Offline Install Cleaner";
            string path = @".";
            if (args.Length > 0)
                path = args[0];
            Do(path);
            Console.Write("Press any key to exit...");
            Console.ReadKey();
        }

        static void Do(string path)
        {
            Console.WriteLine("Scanning...");
            PackageInfo[] packages = Scan(path);
            if (packages.Length > 0)
            {
                Console.WriteLine("Found {0} package(s).", packages.Length);
                packages = ProcessData(packages);
                if (packages.Length > 0)
                {
                    Console.WriteLine("Found {0} old package(s).", packages.Length);
                    Console.WriteLine("Cleaning...");
                    Clean(path, packages);
                    Console.WriteLine("Clean done.");
                }
                else
                    Console.WriteLine("Not found old package.");
            }
            else
                Console.WriteLine("Not found package.");
        }

        static PackageInfo[] Scan(string path)
        {
            string[] dirs = Directory.GetDirectories(path);
            Regex reg = new Regex(@"([^,]+),version=([^,]+)");
            List<PackageInfo> packages = new List<PackageInfo>();
            foreach (string item in dirs)
            {
                string name = Path.GetFileName(item);
                Match match = reg.Match(name);
                if (match.Success)
                {
                    GroupCollection values = match.Groups;
                    if (values.Count == 3)
                    {
                        PackageInfo package = new PackageInfo
                        {
                            Name = values[1].Value,
                            Ver = new Version(values[2].Value)
                        };
                        packages.Add(package);
                    }
                }
            }
            return packages.ToArray();
        }

        static Dictionary<string, Version[]> ParseMap(PackageInfo[] packages)
        {
            IEnumerable<IGrouping<string, PackageInfo>> groups = packages.GroupBy(i => i.Name);
            Dictionary<string, Version[]> map = new Dictionary<string, Version[]>();
            foreach (IGrouping<string, PackageInfo> item in groups)
            {
                List<Version> versions = item.Select(i => i.Ver).Distinct().ToList();
                versions.Sort();
                map.Add(item.Key, versions.ToArray());
            }
            return map;
        }

        static PackageInfo[] ProcessData(PackageInfo[] packages)
        {
            List<PackageInfo> _packages = new List<PackageInfo>();
            Dictionary<string, Version[]> map = ParseMap(packages);
            foreach (KeyValuePair<string, Version[]> item in map)
            {
                if (item.Value.Length > 1)
                {
                    Version[] versions = item.Value;
                    for (int i = 0; i < versions.Length - 1; i++)
                    {
                        PackageInfo package = new PackageInfo()
                        {
                            Name = item.Key,
                            Ver = versions[i]
                        };
                        _packages.Add(package);
                    }
                }
            }
            return _packages.ToArray();
        }

        static void Clean(string path, PackageInfo[] packages)
        {
            string[] dirs = Directory.GetDirectories(path);
            Regex reg = new Regex(@"([^,]+),version=([^,]+)");
            foreach (string item in dirs)
            {
                string name = Path.GetFileName(item);
                Match match = reg.Match(name);
                if (match.Success)
                {
                    GroupCollection values = match.Groups;
                    if (values.Count == 3)
                    {
                        bool enq = packages.Any(i => i.Name == values[1].Value &&
                                                     i.Ver == new Version(values[2].Value));
                        if (enq)
                            Directory.Delete(item, true);
                    }
                }
            }
            reg = new Regex(@"Response[^\.]*\.json");
            string[] files = Directory.GetFiles(path);
            foreach (string item in files)
            {
                if (reg.IsMatch(item))
                    File.Delete(item);
            }
            path = $@"{path}\Archive";
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    class PackageInfo
    {
        public string Name { get; set; }
        public Version Ver { get; set; }
    }
}