using System;
using UnityEngine;

namespace Glider.Core.SerializableData
{
    [System.Serializable]
    public class LocalizedMessage
    {
        [System.Serializable]
        public class Message
        {
            public string language;
            public string value;
        }

        public LocalizedMessageKey lmk;
        public Message[] messages;
        public string[] bundles;

        public string Find(SystemLanguage language)
        {
            foreach (var msg in messages)
            {
                if (String.Equals(msg.language, language.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    return msg.value;
                }
            }

            return null;
        }
    }
}