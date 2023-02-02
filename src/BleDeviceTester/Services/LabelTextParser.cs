using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using IDS.Portable.Common;
using BrakingSystem = OneControl.Direct.MyRvLinkBle.MyRvLinkBleGatewayScanResult;

namespace RvLinkDeviceTester.Services
{
    public enum GatewayType
    {
        Unknown,
        WiFi,
        Bluetooth,
        Aquafi,
        SonixCamera,
        WindSensor,
        BatteryMonitor,
        AntiLockBraking,
        SwayBraking,
    }

    public enum ValueMatchType
    {
        Id,
        Password
    }

    public static class SharedGatewayParsingMetadata
    {
        /// <summary>
        /// Gateways that support text label reading (unknown gateway assumes support)
        /// </summary>
        public static readonly ReadOnlyCollection<GatewayType> TextSupportedGateways = new List<GatewayType>
        {
            GatewayType.WindSensor,
            GatewayType.Aquafi,
            GatewayType.Bluetooth,
            GatewayType.WiFi,
            GatewayType.SonixCamera,
            GatewayType.AntiLockBraking,
            GatewayType.SwayBraking,
            GatewayType.Unknown
        }.AsReadOnly();

        /// <summary>
        /// Gateways that support QR code reading (unknown gateway assumes support)
        /// </summary>
        public static readonly ReadOnlyCollection<GatewayType> QrSupportedGateways = new List<GatewayType>
        {
            GatewayType.WindSensor,
            GatewayType.Aquafi,
            GatewayType.Bluetooth,
            GatewayType.WiFi,
            GatewayType.SonixCamera,
            GatewayType.BatteryMonitor,
            GatewayType.AntiLockBraking,
            GatewayType.SwayBraking,
            GatewayType.Unknown
        }.AsReadOnly();

        /// <summary>
        /// The keys to get the match group for each regex based on the value type.
        /// </summary>
        public static readonly ReadOnlyDictionary<ValueMatchType, string> ValueMatchGroupKeys = new ReadOnlyDictionary<ValueMatchType, string>(new Dictionary<ValueMatchType, string>
        {
            { ValueMatchType.Id, "id" },
            { ValueMatchType.Password, "pass" },
        });

        /// <summary>
        /// The regexes used to match the id for each gateway type.
        /// </summary>
        public static readonly ReadOnlyDictionary<GatewayType, Regex> IdRegexDictionary = new ReadOnlyDictionary<GatewayType, Regex>(new Dictionary<GatewayType, Regex>
        {
            {GatewayType.Aquafi, new Regex("AquaFi.(?'id'[a-zA-Z0-9|]{6})", RegexOptions.IgnoreCase)},
            {GatewayType.Bluetooth, new Regex(@"LCIRemote|Remote(?'id'[a-zA-Z0-9|]{9})", RegexOptions.IgnoreCase | RegexOptions.Singleline)},
            {GatewayType.WiFi, new Regex("RV.(?'id'[a-zA-Z0-9|]{16})", RegexOptions.IgnoreCase)},
            {GatewayType.SonixCamera, new Regex("(?'id'INSIGHT_[a-z0-9]{6})", RegexOptions.IgnoreCase | RegexOptions.Singleline)},
            {GatewayType.WindSensor, new Regex("DT=2F&MAC=(?'id'[0-9A-Fa-f]{12})", RegexOptions.IgnoreCase )},
            {GatewayType.BatteryMonitor, new Regex("MAC=(?'id'[0-9A-Fa-f]{12})", RegexOptions.IgnoreCase )},
            {GatewayType.AntiLockBraking, new Regex("(?'id'"+BrakingSystem.AntiLockBrakingNamePrefix+"_[a-z0-9]{11})", RegexOptions.IgnoreCase | RegexOptions.Singleline)},
            {GatewayType.SwayBraking, new Regex("(?'id'"+BrakingSystem.SwayBrakingNamePrefix+"_[a-z0-9]{10})|"+"(?'id'"+BrakingSystem.SwayBrakingNamePrefix+"[0-9]{5})", RegexOptions.IgnoreCase | RegexOptions.Singleline)}
        });

        /// <summary>
        /// The regexes used to match the password for each gateway type.
        /// </summary>
        public static readonly ReadOnlyDictionary<GatewayType, Regex> PasswordRegexDictionary = new ReadOnlyDictionary<GatewayType, Regex>(new Dictionary<GatewayType, Regex>
        {
            {GatewayType.Aquafi, new Regex("PASSWORD.{0,3}(?'pass'[a-zA-Z0-9|]{8})", RegexOptions.IgnoreCase)},
            {GatewayType.Bluetooth, new Regex("(?:PASSWORD|PW).{0,3}(?'pass'[0-9|]{6})", RegexOptions.IgnoreCase | RegexOptions.Singleline)},
            {GatewayType.WiFi, new Regex("PASSWORD.{0,3}(?'pass'[a-zA-Z0-9|]{8})", RegexOptions.IgnoreCase)},
            {GatewayType.AntiLockBraking, new Regex("pw=(?'pass'[a-zA-Z0-9|]{6})", RegexOptions.IgnoreCase)}
        });

        /// <summary>
        /// Common character replacements for OCR mistakes
        /// </summary>
        public static readonly ReadOnlyDictionary<char, char> ReplacementDictionary = new ReadOnlyDictionary<char, char>(new Dictionary<char, char>
        {
            {'|', '1'},
            {' ', '_'}
        });

        /// <summary>
        /// The dictionary to use for each value type (id, password, version) to match text with regex
        /// based on the gateway type.
        /// </summary>
        public static readonly ReadOnlyDictionary<ValueMatchType, ReadOnlyDictionary<GatewayType, Regex>> ValueMatchDictionariesDictionary = new ReadOnlyDictionary<ValueMatchType, ReadOnlyDictionary<GatewayType, Regex>>(
            new Dictionary<ValueMatchType, ReadOnlyDictionary<GatewayType, Regex>>{
                { ValueMatchType.Id, IdRegexDictionary },
                { ValueMatchType.Password, PasswordRegexDictionary }
            });
    }

    public class ParsedLabelTextResult
    {
        public static readonly ParsedLabelTextResult Empty = new ParsedLabelTextResult(GatewayType.Unknown, String.Empty, String.Empty);

        public ParsedLabelTextResult(GatewayType gatewayType, string id, string password = "")
        {
            GatewayType = gatewayType;
            Id = id;
            Password = password;
            IsValid = _CalculateIsValid();
        }

        private bool _CalculateIsValid()
        {
            if (GatewayType == GatewayType.Unknown)
            {
                // if we don't know what kind of gateway this is, let's just call it valid if it
                // has a password and an id (this is likely from a QR code)
                return !string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Password);
            }

            // valid no matter what if the gateway doesn't have a regex to match with. otherwise valid if the value isn't empty
            // If there is a key for the selected gateway, then the ID/password must be present.
            bool isIdValid = !SharedGatewayParsingMetadata.IdRegexDictionary.ContainsKey(GatewayType) || !String.IsNullOrWhiteSpace(Id);
            bool isPasswordValid = !SharedGatewayParsingMetadata.PasswordRegexDictionary.ContainsKey(GatewayType) || !String.IsNullOrWhiteSpace(Password);

            return isIdValid && isPasswordValid;
        }

        public bool IsValid { get; }
        public GatewayType GatewayType { get; }
        public string Id { get; }
        public string Password { get; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType() || !(obj is ParsedLabelTextResult other)) return false;
            return Id.Equals(other.Id) && Password.Equals(other.Password);
        }

        public override int GetHashCode()
        {
            return Tuple.Create(Id, Password).GetHashCode();
        }

        public override string ToString()
        {
#if DEBUG
            return $"Id: {Id}, Password: {Password}, Gateway Type: {GatewayType}, Valid?: {IsValid}";
#else
            return $"Id: {Id}, Gateway Type: {GatewayType}, Valid?: {IsValid}";
#endif
        }
    }

    public class LabelTextParser
    {
        private const string LogTag = nameof(LabelTextParser);
        private const string QrIdKey = "devid";
        private const string QrPasswordKey = "pw";

        public static string OldWiFiGatewayQrText = "qr*1y";

        public bool IsQrOnly { get; set; }

        /// <summary>
        /// The current gateway type that is being scanned based
        /// on successful scan results.
        /// </summary>
        private GatewayType _suspectedGatewayType = GatewayType.Unknown;

        /// <summary>
        /// The functions used to sanitize the match text for each value type.
        /// </summary>
        private Dictionary<ValueMatchType, Func<string, GatewayType, string>> SanitizationMethodsDictionary => new Dictionary<ValueMatchType, Func<string, GatewayType, string>>
        {
            { ValueMatchType.Id, SanitizeId },
            { ValueMatchType.Password, SanitizePassword },
        };

        /// <summary>
        /// Parses text from the image and from the QR code and determines which to use.
        /// </summary>
        /// <param name="imageText"></param>
        /// <param name="qrText"></param>
        /// <returns>Empty string if neither text is valid, whichever text is valid if one is valid and the other isn't, the QR text if both texts are valid but not equal, or the parsed text if both texts are valid and equal.</returns>
        public ParsedLabelTextResult ParseLabelText(string imageText, string qrText)
        {
            if (string.IsNullOrWhiteSpace(imageText) && string.IsNullOrWhiteSpace(qrText))
            {
                return ParsedLabelTextResult.Empty;
            }

            var ocrParsedLabelTextResult = ParsedLabelTextResult.Empty;
            if (!IsQrOnly)
            {
                ocrParsedLabelTextResult = SharedGatewayParsingMetadata.TextSupportedGateways.Contains(_suspectedGatewayType) ? ParseImageText(imageText) : ParsedLabelTextResult.Empty;
            }

            ParsedLabelTextResult qrParsedLabelTextResult;
            if (qrText?.EndsWith(OldWiFiGatewayQrText, StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                qrParsedLabelTextResult = ocrParsedLabelTextResult.IsValid ? ocrParsedLabelTextResult : new ParsedLabelTextResult(GatewayType.WiFi, OldWiFiGatewayQrText, "_");
            }
            else
            {
                qrParsedLabelTextResult = SharedGatewayParsingMetadata.QrSupportedGateways.Contains(_suspectedGatewayType) ? ParseQrText(qrText) : ParsedLabelTextResult.Empty;
            }

            if (ocrParsedLabelTextResult.Equals(ParsedLabelTextResult.Empty) && qrParsedLabelTextResult.Equals(ParsedLabelTextResult.Empty))
            {
                return ParsedLabelTextResult.Empty;
            }

            if (qrParsedLabelTextResult.Equals(ParsedLabelTextResult.Empty))
            {
                return ocrParsedLabelTextResult;
            }

            return qrParsedLabelTextResult;
        }

        /// <summary>
        /// Takes in raw image text and converts it to a set of id, password,
        /// and version.
        /// </summary>
        /// <param name="imageText">The raw text scanned from an image.</param>
        /// <returns>A parsed text result</returns>
        private ParsedLabelTextResult ParseImageText(string imageText)
        {
            if (string.IsNullOrWhiteSpace(imageText)) return ParsedLabelTextResult.Empty;

            // if we match every *supported* value (some or all of id, password, version),
            // then we consider this the gateway we were looking for.
            bool allSupportedMatchesMatchedForSomeGatewayType = true;
            // a map of the value types to their sanitized text values.
            var sanitizedMatchText = new Dictionary<ValueMatchType, string>();

            try
            {
                foreach (GatewayType gatewayType in Enum.GetValues(typeof(GatewayType)))
                {
                    if (gatewayType == GatewayType.Unknown) { continue; }
                    // we assume we've matched everything for each gateway until a match fails.
                    allSupportedMatchesMatchedForSomeGatewayType = true;
                    // loop through all of id, password, version and match values from text
                    foreach (ValueMatchType valueMatchType in Enum.GetValues(typeof(ValueMatchType)))
                    {
                        // if this value is not supported by this gateway type, set result to empty and check
                        // next value.
                        ReadOnlyDictionary<GatewayType, Regex> matchDictionary = SharedGatewayParsingMetadata.ValueMatchDictionariesDictionary[valueMatchType];
                        bool gatewaySupportsThisValueMatchType = matchDictionary.ContainsKey(gatewayType);
                        if (!gatewaySupportsThisValueMatchType)
                        {
                            sanitizedMatchText[valueMatchType] = String.Empty;
                            continue;
                        }
                        // if we matched the value successfully for this gateway,
                        // then get the group key, pull out the value, and sanitize the text.
                        Match valueMatch = matchDictionary[gatewayType].Match(imageText);
                        if (valueMatch.Success)
                        {
                            string matchTextKey = SharedGatewayParsingMetadata.ValueMatchGroupKeys[valueMatchType];
                            Func<string, GatewayType, string> sanitizeFunc = SanitizationMethodsDictionary[valueMatchType];
                            sanitizedMatchText[valueMatchType] = sanitizeFunc(valueMatch.Groups[matchTextKey].Value, gatewayType);
                        }
                        else
                        {
                            // if we didn't match the supported value, then set this to false
                            // and bail out of the value matching loop
                            allSupportedMatchesMatchedForSomeGatewayType = false;
                            break;
                        }
                    }

                    // everything matched from raw text, so this is probably our
                    // gateway type
                    if (!allSupportedMatchesMatchedForSomeGatewayType) continue;
                    _suspectedGatewayType = gatewayType;
                    TaggedLog.Debug(LogTag, $"Suspected gateway type is '{_suspectedGatewayType}'");
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            // we did not match all supported values for any gateway
            // so bail with an empty response
            if (!allSupportedMatchesMatchedForSomeGatewayType)
            {
                return ParsedLabelTextResult.Empty;
            }

            // pull out the values (some or all may be matched, some may be empty)
            string id = sanitizedMatchText[ValueMatchType.Id];
            string password = sanitizedMatchText[ValueMatchType.Password];

            var parsedLabelTextResult = new ParsedLabelTextResult(_suspectedGatewayType, id, password);

#if DEBUG
            TaggedLog.Debug(LogTag, $"Parsed '{parsedLabelTextResult}' from image text '{imageText}'");
#endif
            return parsedLabelTextResult;
        }

        private ParsedLabelTextResult ParseQrText(string qrText)
        {
            if (string.IsNullOrWhiteSpace(qrText)) return ParsedLabelTextResult.Empty;

            // Some QR codes may not be URIs. If so, attempt to parse as normal text.
            if (!qrText.Trim().StartsWith("http")) { return ParseImageText(qrText); }

            var queryParams = HttpUtility.ParseQueryString(new Uri(qrText).Query);
            // Some QR codes may not container query parameters. If so, attempt to parse as normal text. 
            if (!queryParams.AllKeys.Contains(QrIdKey) || !queryParams.AllKeys.Contains(QrPasswordKey))
            {
                return ParseImageText(qrText);
            }

            string id = queryParams[QrIdKey];
            string password = queryParams[QrPasswordKey];

            GatewayType matchedGatewayType = GatewayType.Unknown;

            // check to see if the id matches any of our regex. if so let's assume that's the gateway type
            foreach (GatewayType gatewayType in Enum.GetValues(typeof(GatewayType)))
            {
                if (gatewayType == GatewayType.Unknown) { continue; }

                bool gatewaySupportsThisIdMatchType = SharedGatewayParsingMetadata.IdRegexDictionary.ContainsKey(gatewayType);
                if (!gatewaySupportsThisIdMatchType) { continue; }

                // if we matched the value successfully for this gateway,
                // then get the group key, pull out the value, and sanitize the text.
                Match idMatch = SharedGatewayParsingMetadata.IdRegexDictionary[gatewayType].Match(id);
                if (!idMatch.Success) continue;
                matchedGatewayType = gatewayType;
                break;
            }

            var parsedLabelTextResult = new ParsedLabelTextResult(matchedGatewayType, id, password);

            TaggedLog.Debug(LogTag, $"Parsed '{parsedLabelTextResult}' from QR text: '{qrText}'");

            return parsedLabelTextResult;
        }

        private static string SanitizeId(string idText, GatewayType gatewayType)
        {
            var idWithReplacedCharacters = FixIncorrectCharacters(idText);
            switch (gatewayType)
            {
                case GatewayType.Bluetooth:
                    return "LCIRemote" + idWithReplacedCharacters;
                case GatewayType.WiFi:
                    return "MyRV_" + idWithReplacedCharacters.ToUpper();
                case GatewayType.Aquafi:
                    return "LCI_AquaFi_" + idWithReplacedCharacters;
                case GatewayType.SonixCamera:
                case GatewayType.WindSensor:
                case GatewayType.BatteryMonitor:
                case GatewayType.AntiLockBraking:
                case GatewayType.SwayBraking:
                    return idText;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gatewayType), gatewayType, null);
            }
        }

        private string SanitizePassword(string passwordText, GatewayType gatewayType = GatewayType.Unknown) // NOSONAR (csharpsquid:S1172)
        // This unused parameter is here to keep the sanitization method signatures the same for storage in a collection
        {
            return FixIncorrectCharacters(passwordText).ToUpper();
        }

        private string SanitizeVersion(string versionText, GatewayType gatewayType = GatewayType.Unknown) // NOSONAR (csharpsquid:S1172)
        // This unused parameter is here to keep the sanitization method signatures the same for storage in a collection
        {
            return versionText;
        }

        public static string FixIncorrectCharacters(string text)
        {
            return SharedGatewayParsingMetadata.ReplacementDictionary.Aggregate(text, (accumulator, pair) => accumulator.Replace(pair.Key, pair.Value));
        }
    }
}