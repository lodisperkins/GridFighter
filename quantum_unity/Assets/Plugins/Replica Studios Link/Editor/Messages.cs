using System;

namespace Packages.Replica.Bridge.Editor
{
    [Serializable]
    public class Message
    {
        public string type;

        public bool IsCreateAsset()
        {
            return type == "create_asset";
        }

        public bool IsAttribution()
        {
            return type == "attribution";
        }

        public bool IsHandshake()
        {
            return type == "handshake";
        }
    }

    [Serializable]
    public class CreateAssetMessage
    {
        public string type = "create_asset";
        public string url;
        public string filename;
        public string text;
    }

    [Serializable]
    public class HandshakeMessage
    {
        public string type = "handshake";
        public string platform = "unity";
        public string platformVersion;
        public string projectName;
        public string pluginName;
        public string pluginVersion;
        public string referralSource;
        public string uid;

    }

    [Serializable]
    public class AttributionMessage
    {
        public string type = "attribution";
        public string uid;
        public string location;
    }
}