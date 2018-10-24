﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitMessageParser
{
    public class GitLog
    {
        public string hash;
        public string parent;
        public string Author;
        public string AuthorEmail;
        public string CommitMessage;
        public int timeStamp;
    }

    public class CommitReport
    {
        public string Version;
        public string Header;
        public string Body;
    }
}