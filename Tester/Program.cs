using GitMessageParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            string testPath = @"C:\Workspace\PDFView\PDF View 2\.git";
            GitMessageParser.GitMessageParser parser = new GitMessageParser.GitMessageParser(testPath);

            if (!GitMessageParser.GitMessageParser.IsGitInstalled())
            {
                Console.WriteLine("git not installed");
            }

            string headPath;
            if (parser.ReadHeadPath(out headPath))
            {
                Console.WriteLine(headPath);
                string headHash;
                if (parser.ReadHeadHash(headPath, out headHash))
                {
                    Console.WriteLine(headHash);

                    List<GitLog> objectList = parser.ReadLogs();

                    string messageLines;
                    if (parser.ExtractGitMessage(headHash, out messageLines))
                    {
                        Console.WriteLine(messageLines);
                        List<GitLog> objectList2 = parser.ReadLogsDirect(DateTime.Today.AddDays(-1), "tenny");
                        foreach (var log in objectList2)
                        {
                            var cr = GitMessageParser.GitMessageParser.ParseReport(log);
                        }
                    }                    
                }
            }
            Console.ReadLine();
        }
    }
}
