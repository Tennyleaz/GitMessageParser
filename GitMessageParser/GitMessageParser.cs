using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Globalization;

namespace GitMessageParser
{
    public class GitMessageParser
    {
        private string _gitPath;

        public GitMessageParser(string gitPath)
        {
            // check if path exist
            if (string.IsNullOrEmpty(gitPath))
                throw new ArgumentNullException("gitPath");
            else if (!Directory.Exists(gitPath))
                throw new FileNotFoundException("gitPath does not exist!");

            _gitPath = gitPath;

            // add a / to end of path
            if (!_gitPath.EndsWith(@"\"))
                _gitPath += @"\";
        }

        /// <summary>
        /// 尋找 .git 資料夾內 HEAD 檔案，讀取 head 分支檔案位置
        /// </summary>
        /// <param name="headPath"></param>
        /// <returns></returns>
        public bool ReadHeadPath(out string headPath)
        {
            headPath = string.Empty;
            string headFile = _gitPath + "HEAD";
            if (File.Exists(headFile))         
            {
                try
                {
                    string token = @"ref:";
                    string headText = File.ReadAllText(headFile);
                    int refIndex = headText.IndexOf(token, StringComparison.CurrentCultureIgnoreCase);
                    if (refIndex >= 0)
                    {
                        headText = headText.Substring(refIndex + token.Length);
                        headText = headText.TrimStart(' ');
                        headText = headText.Replace('/', '\\');
                        headText = headText.Replace("\n", string.Empty);
                        headPath = _gitPath + headText;
                        if (File.Exists(headPath))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);                    
                }
            }
            return false;
        }

        /// <summary>
        /// 尋找 .git 資料夾內 head 分支最新的 commit hash
        /// </summary>
        /// <param name="headFile"></param>
        /// <param name="headHash"></param>
        /// <returns></returns>
        public bool ReadHeadHash(string headFile, out string headHash)
        {
            headHash = string.Empty;
            if (File.Exists(headFile))
            {
                try
                {
                    headHash = File.ReadAllText(headFile);
                    headHash = headHash.Replace("\n", string.Empty);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return false;
        }

        /// <summary>
        /// 用 git cat-file 指令解壓資料夾內存的該筆 commit log
        /// </summary>
        /// <param name="headHash"></param>
        /// <param name="commitMessage"></param>
        /// <returns></returns>
        public bool ExtractGitMessage(string headHash, out string commitMessage)
        {
            commitMessage = null;
            if (IsGitInstalled())
            {
                // find in .git/object/<first 2 chars>/<remaining hash>
                if (!string.IsNullOrEmpty(headHash))
                {
                    Process proc = new Process();
                    proc.StartInfo.FileName = @"git.exe";
                    proc.StartInfo.WorkingDirectory = _gitPath;
                    proc.StartInfo.Arguments = @"cat-file -p " + headHash;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.Start();
                    commitMessage = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 用 where.exe 尋找 git
        /// </summary>
        /// <returns></returns>
        public static bool IsGitInstalled()
        {
            string cmd = "git";
            Process proc = new Process();
            proc.StartInfo.FileName = @"where.exe";
            proc.StartInfo.Arguments = cmd;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            if (output != null && output.Contains("git.exe"))
                return true;
            else
                return false;
        }

        /// <summary>
        /// 到目前 .git 資料夾內讀取所有的 logs\refs\heads\develop 檔案
        /// </summary>
        /// <returns></returns>
        public List<GitLog> ReadLogs()
        {
            // find file <develop> in .git\logs\refs\heads\
            List<GitLog> objectList = new List<GitLog>();
            string fileName = _gitPath + @"logs\refs\heads\develop";
            if (File.Exists(fileName))
            {
                // Read the file and display it line by line.
                string line;
                StreamReader file = new StreamReader(fileName);
                while ((line = file.ReadLine()) != null)
                {
                    //Console.WriteLine(line);                    
                    // parse git log line
                    string[] lines = line.Split(' ');
                    if (lines.Length >= 5)
                    {
                        GitLog gitObj = new GitLog();
                        gitObj.parent = lines[0];
                        gitObj.hash = lines[1];
                        gitObj.Author = lines[2];
                        gitObj.AuthorEmail = lines[3];
                        int timeStamp;
                        if (int.TryParse(lines[4], out timeStamp))
                        {
                            gitObj.TimeStamp = timeStamp;
                        }
                        objectList.Add(gitObj);
                    }
                }
            }
            return objectList;
        }

        // git log --since="2018/10/12" --author="tenny" --date=iso-local --pretty=format:"%H★%an★%at★%s★%b"
        /// <summary>
        /// 直接用 git log 指令讀取 commit log
        /// </summary>
        /// <param name="dateSince"></param>
        /// <param name="author"></param>
        /// <returns></returns>
        public List<GitLog> ReadLogsDirect(DateTime dateSince, string author)
        {
            return ReadLogsDirect(dateSince.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture), author);
        }

        // git log --since="2018/10/12" --author="tenny" --date=iso-local --pretty=format:"%H★%an★%at★%s★%b"
        /// <summary>
        /// 直接用 git log 指令讀取 commit log
        /// </summary>
        /// <param name="dateArgument"></param>
        /// <param name="author"></param>
        /// <returns></returns>
        public List<GitLog> ReadLogsDirect(string dateArgument, string author)
        {
            if (string.IsNullOrEmpty(author))
                throw new ArgumentNullException("author");

            string args = "log --since=\""
                + dateArgument
                + "\" --author=\""
                + author
                + "\" --date=iso-local --pretty=format:\"%H★%an★%at★%B⛔\"";

            Process proc = new Process();
            proc.StartInfo.FileName = @"git.exe";
            proc.StartInfo.WorkingDirectory = _gitPath;
            proc.StartInfo.Arguments = args;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Start();
            string commitMessages = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            return ParseGitMessage(commitMessages);
        }

        /// <summary>
        /// 將純文字 git commit message 變成 class
        /// </summary>
        /// <param name="commitMessages"></param>
        /// <returns></returns>
        private List<GitLog> ParseGitMessage(string commitMessages)
        {
            List<GitLog> logs = new List<GitLog>();

            if (!string.IsNullOrEmpty(commitMessages))
            {
                string[] lines = commitMessages.Split('⛔');
                for (int i=0; i<lines.Length; i++)
                {
                    if (string.IsNullOrEmpty(lines[i]))
                        continue;
                    string[] arguments = lines[i].Split('★');
                    if (arguments.Length >= 4)
                    {
                        GitLog gitObj = new GitLog();
                        gitObj.hash = arguments[0];
                        gitObj.Author = arguments[1];
                        gitObj.CommitMessage = arguments[3];
                        int timeStamp;
                        if (int.TryParse(arguments[2], out timeStamp))
                        {
                            gitObj.TimeStamp = timeStamp;
                        }
                        logs.Add(gitObj);
                    }
                }
            }

            return logs;
        }

        /// <summary>
        /// 讀取 commit 訊息變成日報適合的格式
        /// </summary>
        /// <param name="commitMessages"></param>
        /// <returns></returns>
        public static CommitReport ParseReport(GitLog log)
        {
            string commitMessages = log.CommitMessage;
            // Version 1.0.0.0
            //- ADD：增加設定檔
            //處理要項：
            //1.新增 Common 資料夾，將 json &filter 檔案加入索引。
            //開發者：Tenny
            //PM：
            //模組：scanner manager v1.0.0.0
            //狀態：進行中

            string token1 = "Version";
            string token2 = "-";
            string token3 = "處理要項:";
            string token4 = "開發者";
            string ver, header, body;
            string[] exclueded = { "ADD：", "CHG：", "DEL：" };

            int index1 = commitMessages.IndexOf(token1, StringComparison.CurrentCultureIgnoreCase);
            int index2 = commitMessages.IndexOf(token2, StringComparison.CurrentCultureIgnoreCase);
            int index3 = CultureInfo.CurrentCulture.CompareInfo.IndexOf(commitMessages, token3, CompareOptions.IgnoreWidth);
            int index4 = commitMessages.IndexOf(token4, StringComparison.CurrentCultureIgnoreCase);
            if (index1 >= 0 && index2 > index1 && index3 > index2 && index4 > index3)
            {
                ver = commitMessages.Substring(index1 + token1.Length, index2 - index1 - token1.Length);
                header = commitMessages.Substring(index2 + token2.Length, index3 - index2 - token2.Length);
                header = header.Replace(exclueded[0], "");
                header = header.Replace(exclueded[1], "");
                header = header.Replace(exclueded[2], "");
                header = header.Replace("\n", "");
                ver = ver.Replace("\n", "");
                ver = ver.Replace(" ", "");
                body = commitMessages.Substring(index3 + token3.Length, index4 - index3 - token3.Length);
                List<string> bodyLines = body.Split('\n').ToList();
                for (int i = 0; i < bodyLines.Count; i++)
                    bodyLines[i] = bodyLines[i].Replace("\n", "");                
                bodyLines.RemoveAll(s => string.IsNullOrWhiteSpace(s));
                bodyLines.RemoveAll(s => s.Contains("如題。"));

                CommitReport cr = new CommitReport();
                cr.Version = ver;
                cr.Header = header;
                cr.Body = bodyLines;
                cr.TimeStamp = log.TimeStamp;          
                return cr;
            }
            return null;
        }
    }
}
