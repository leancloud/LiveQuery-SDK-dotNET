using System;
using System.Collections.Generic;
using LeanCloud;
using System.Collections;

namespace LeanCloud.LiveQuery
{
    public class AVLiveQueryEventArgs<T> : EventArgs
        where T : AVObject
    {
        public AVLiveQueryEventArgs()
        {

        }

        public string Scope { get; set; }

        public IEnumerable<string> Keys { get; set; }

        public T Payload { get; set; }
    }
}
