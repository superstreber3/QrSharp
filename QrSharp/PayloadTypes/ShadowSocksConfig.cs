using System.Text;

namespace QrSharp.PayloadTypes;

public static partial class PayloadGenerator
{
    public class ShadowSocksConfig : QrSharp.PayloadGenerator.Payload
    {
        public enum Method
        {
            // AEAD
            Chacha20IetfPoly1305,
            Aes128Gcm,
            Aes192Gcm,
            Aes256Gcm,

            // AEAD, not standard
            XChacha20IetfPoly1305,

            // Stream cipher
            Aes128Cfb,
            Aes192Cfb,
            Aes256Cfb,
            Aes128Ctr,
            Aes192Ctr,
            Aes256Ctr,
            Camellia128Cfb,
            Camellia192Cfb,
            Camellia256Cfb,
            Chacha20Ietf,

            // alias of Aes256Cfb
            Aes256Cb,

            // Stream cipher, not standard
            Aes128Ofb,
            Aes192Ofb,
            Aes256Ofb,
            Aes128Cfb1,
            Aes192Cfb1,
            Aes256Cfb1,
            Aes128Cfb8,
            Aes192Cfb8,
            Aes256Cfb8,

            // Stream cipher, deprecated
            Chacha20,
            BfCfb,
            Rc4Md5,
            Salsa20,

            // Not standard and not in acitve use
            DesCfb,
            IdeaCfb,
            Rc2Cfb,
            Cast5Cfb,
            Salsa20Ctr,
            Rc4,
            SeedCfb,
            Table
        }

        private readonly Dictionary<string, string> _encryptionTexts = new()
        {
            { "Chacha20IetfPoly1305", "chacha20-ietf-poly1305" },
            { "Aes128Gcm", "aes-128-gcm" },
            { "Aes192Gcm", "aes-192-gcm" },
            { "Aes256Gcm", "aes-256-gcm" },

            { "XChacha20IetfPoly1305", "xchacha20-ietf-poly1305" },

            { "Aes128Cfb", "aes-128-cfb" },
            { "Aes192Cfb", "aes-192-cfb" },
            { "Aes256Cfb", "aes-256-cfb" },
            { "Aes128Ctr", "aes-128-ctr" },
            { "Aes192Ctr", "aes-192-ctr" },
            { "Aes256Ctr", "aes-256-ctr" },
            { "Camellia128Cfb", "camellia-128-cfb" },
            { "Camellia192Cfb", "camellia-192-cfb" },
            { "Camellia256Cfb", "camellia-256-cfb" },
            { "Chacha20Ietf", "chacha20-ietf" },

            { "Aes256Cb", "aes-256-cfb" },

            { "Aes128Ofb", "aes-128-ofb" },
            { "Aes192Ofb", "aes-192-ofb" },
            { "Aes256Ofb", "aes-256-ofb" },
            { "Aes128Cfb1", "aes-128-cfb1" },
            { "Aes192Cfb1", "aes-192-cfb1" },
            { "Aes256Cfb1", "aes-256-cfb1" },
            { "Aes128Cfb8", "aes-128-cfb8" },
            { "Aes192Cfb8", "aes-192-cfb8" },
            { "Aes256Cfb8", "aes-256-cfb8" },

            { "Chacha20", "chacha20" },
            { "BfCfb", "bf-cfb" },
            { "Rc4Md5", "rc4-md5" },
            { "Salsa20", "salsa20" },

            { "DesCfb", "des-cfb" },
            { "IdeaCfb", "idea-cfb" },
            { "Rc2Cfb", "rc2-cfb" },
            { "Cast5Cfb", "cast5-cfb" },
            { "Salsa20Ctr", "salsa20-ctr" },
            { "Rc4", "rc4" },
            { "SeedCfb", "seed-cfb" },
            { "Table", "table" }
        };

        private readonly string _hostname, _password, _methodStr;
        private readonly Method _method;
        private readonly string? _parameter;
        private readonly int _port;
        private readonly string? _tag;

        private readonly Dictionary<string, string> _urlEncodeTable = new()
        {
            [" "] = "+",
            ["\0"] = "%00",
            ["\t"] = "%09",
            ["\n"] = "%0a",
            ["\r"] = "%0d",
            ["\""] = "%22",
            ["#"] = "%23",
            ["$"] = "%24",
            ["%"] = "%25",
            ["&"] = "%26",
            ["'"] = "%27",
            ["+"] = "%2b",
            [","] = "%2c",
            ["/"] = "%2f",
            [":"] = "%3a",
            [";"] = "%3b",
            ["<"] = "%3c",
            ["="] = "%3d",
            [">"] = "%3e",
            ["?"] = "%3f",
            ["@"] = "%40",
            ["["] = "%5b",
            ["\\"] = "%5c",
            ["]"] = "%5d",
            ["^"] = "%5e",
            ["`"] = "%60",
            ["{"] = "%7b",
            ["|"] = "%7c",
            ["}"] = "%7d",
            ["~"] = "%7e"
        };

        /// <summary>
        ///     Generates a ShadowSocks proxy config payload.
        /// </summary>
        /// <param name="hostname">Hostname of the ShadowSocks proxy</param>
        /// <param name="port">Port of the ShadowSocks proxy</param>
        /// <param name="password">Password of the SS proxy</param>
        /// <param name="method">Encryption type</param>
        /// <param name="tag">Optional tag line</param>
        public ShadowSocksConfig(string hostname, int port, string password, Method method, string? tag = null) :
            this(hostname, port, password, method, null, tag)
        {
        }

        public ShadowSocksConfig(string hostname, int port, string password, Method method, string plugin,
            string pluginOption, string? tag = null) :
            this(hostname, port, password, method, new Dictionary<string, string>
            {
                ["plugin"] = plugin + (
                    string.IsNullOrEmpty(pluginOption)
                        ? ""
                        : $";{pluginOption}"
                )
            }, tag)
        {
        }

        public ShadowSocksConfig(string hostname, int port, string password, Method method,
            Dictionary<string, string>? parameters, string? tag = null)
        {
            _hostname = Uri.CheckHostName(hostname) == UriHostNameType.IPv6
                ? $"[{hostname}]"
                : hostname;
            if (port is < 1 or > 65535)
            {
                throw new ShadowSocksConfigException("Value of 'port' must be within 0 and 65535.");
            }

            _port = port;
            _password = password;
            _method = method;
            _methodStr = _encryptionTexts[method.ToString()];
            _tag = tag;

            if (parameters is not null)
            {
                _parameter =
                    string.Join("&",
                        parameters.Select(
                            kv => $"{UrlEncode(kv.Key)}={UrlEncode(kv.Value)}"
                        ).ToArray());
            }
        }

        private string UrlEncode(string i)
        {
            return _urlEncodeTable.Aggregate(i, (current, kv) => current.Replace(kv.Key, kv.Value));
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_parameter))
            {
                var connectionString = $"{_methodStr}:{_password}@{_hostname}:{_port}";
                var connectionStringEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(connectionString));
                return $"ss://{connectionStringEncoded}{(!string.IsNullOrEmpty(_tag) ? $"#{_tag}" : string.Empty)}";
            }

            var authString = $"{_methodStr}:{_password}";
            var authStringEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
            return
                $"ss://{authStringEncoded}@{_hostname}:{_port}/?{_parameter}{(!string.IsNullOrEmpty(_tag) ? $"#{_tag}" : string.Empty)}";
        }

        public class ShadowSocksConfigException : Exception
        {
            public ShadowSocksConfigException()
            {
            }

            public ShadowSocksConfigException(string message)
                : base(message)
            {
            }

            public ShadowSocksConfigException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}