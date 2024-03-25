using System;
using UnityEngine;

namespace Glider.Core.SerializableData
{
    public class InvalidCloudDataHashException : Exception
    {
        [System.Serializable]
        public class Data
        {
            public string message;
            public string causeClass;
            public string causeField;
            public string oldValue;
            public string newValue;
        }

        private Data data;

        public InvalidCloudDataHashException(string message, string causeClass, int causeIndex, string oldValue,
            string newValue) : base(message)
        {
            this.data = new Data()
            {
                causeClass = causeClass,
                causeField = causeIndex.ToString(),
                oldValue = oldValue,
                newValue = newValue,
                message = message
            };
        }

        public InvalidCloudDataHashException(string message, string causeClass, string oldValue, string newValue) :
            base(message)
        {
            this.data = new Data()
            {
                causeClass = causeClass,
                causeField = string.Empty,
                oldValue = oldValue,
                newValue = newValue,
                message = message
            };
        }

        public InvalidCloudDataHashException(string message, string causeClass, int causeIndex, string oldValue) :
            base(message)
        {
            this.data = new Data()
            {
                causeClass = causeClass,
                causeField = causeIndex.ToString(),
                oldValue = oldValue,
                message = message
            };
        }

        public InvalidCloudDataHashException(string message, string causeClass, string oldValue) : base(message)
        {
            this.data = new Data()
            {
                causeClass = causeClass,
                causeField = string.Empty,
                oldValue = oldValue,
                message = message
            };
        }

        public override string Message => JsonUtility.ToJson(data);
    }
}