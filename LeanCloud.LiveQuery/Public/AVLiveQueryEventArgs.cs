using System;
using System.Collections.Generic;
using LeanCloud;
namespace LeanCloud.LiveQuery
{
    public class AVLiveQueryEventArgs<T> : EventArgs
        where T : AVObject
    {
        public AVLiveQueryEventArgs()
        {

        }

        public string Scope { get; set; }

        public T Payload { get; set; }
    }
}
