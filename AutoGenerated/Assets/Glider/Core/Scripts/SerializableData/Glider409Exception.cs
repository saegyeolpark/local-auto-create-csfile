namespace Glider.Core.SerializableData
{
    /// <summary>
    /// Glider WAS에서 statusCode: 409일 경우 
    /// </summary>
    [System.Serializable]
    public class Glider409Exception
    {
        public Exception409 error;

        [System.Serializable]
        public class Exception409
        {
            public int code;
            public LocalizedMessage localizedMessage;
            public string systemMessage;
            public int traceId;
        }
    }
}