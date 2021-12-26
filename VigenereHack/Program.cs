namespace VigenereHack
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    internal class Program
    {
        private static void Main()
        {
            var alphabet = Utils.GetAlphabet();
            var books = Utils.GetBooksFileArray();

            var rnd = new Random();

            Console.WriteLine("Hack process was started... Wait a minute...");
            var monogramSample = GetMonogramSample(books, alphabet) as Dictionary<char, double>;
            var bigramSample = GetBigramSample(books, alphabet) as Dictionary<string, double>;

            var encryptedText = File.ReadAllText("text.txt");

            var probableKeyLengths = KasiskiMethod(encryptedText, 3);
            var bestText = encryptedText;
            var bestKeyLength = 0;
            var bestRatio = double.MaxValue;

            foreach (var length in probableKeyLengths.OrderByDescending(x => x))
            {
                if (length > 1)
                {
                    var decrypted = Decrypt(encryptedText, alphabet, length, monogramSample, bigramSample, rnd, out var ratio);
                    if (ratio < bestRatio)
                    {
                        bestText = decrypted;
                        bestKeyLength = length;
                        bestRatio = ratio;
                    }
                }
            }

            var separator = Utils.GetSeparator('=', 44);
            Console.WriteLine($"{separator}\nDone!\nResult:");
            var bestKey = GetKey(encryptedText, bestText, bestKeyLength, alphabet);
            Console.WriteLine($"Key length: {bestKeyLength}\nKey: {bestKey}\nRatio: {bestRatio:F5}\n{separator}");
            Console.WriteLine($"{bestText}\n{separator}");
        }

        private static string Decrypt(
            string input, 
            IReadOnlyList<char> chars, 
            int length, 
            IReadOnlyDictionary<char, double> monogramSample,
            IReadOnlyDictionary<string, double> bigramSample, 
            Random rnd, 
            out double ratio)
        {
            var key = new List<int>();
            var sameShiftChars = new List<char>();
            for (var i = 0; i < length; i++)
            {
                sameShiftChars.Clear();
                for (var j = 0; j < Math.Ceiling((double)input.Length / length); j++)
                {
                    if (j * length + i < input.Length)
                        sameShiftChars.Add(input[j * length + i]);
                }

                key.Add(BestShift(sameShiftChars, monogramSample));
            }

            var result = UseKey(input, key);
            ratio = GetRatio(GetBigramText(result, chars), bigramSample);
            var currentKey = new List<int>(key);
            var anyActions = true;

            while (anyActions)
            {
                anyActions = false;
                for (var j = 1; j < chars.Count; j++)
                {
                    for (var k = 0; k < currentKey.Count; k++)
                    {
                        currentKey = new List<int>(key);
                        currentKey[k] = (currentKey[k] + j) % chars.Count;
                        var currentText = UseKey(input, currentKey);
                        var currentRatio = GetRatio(GetBigramText(currentText, chars), bigramSample);

                        if (currentRatio < ratio)
                        {
                            result = currentText;
                            ratio = currentRatio;
                            key = currentKey;

                            anyActions = true;
                        }
                    }
                }

            }
            RandomSwap(key, rnd, chars);

            return result;
        }

        

        private static string UseKey(string input, IReadOnlyList<int> key)
        {
            var result = new StringBuilder();
            var length = key.Count;
            for (var i = 0; i < input.Length; i++)
            {
                result.Append(Utils.ShiftChar(input[i], key[i % length]));
            }

            return result.ToString();
        }

        private static int BestShift(IReadOnlyCollection<char> sameShiftChars, IReadOnlyDictionary<char, double> monogramSample)
        {
            var result = 0;
            var bestRatio = double.MaxValue;

            for (var shift = 0; shift < monogramSample.Count; shift++)
            {
                var currentDict = new Dictionary<char, double>();
                foreach (var ch in sameShiftChars)
                {
                    var shifted = Utils.ShiftChar(ch, shift);
                    if (currentDict.ContainsKey(shifted))
                    {
                        currentDict[shifted]++;
                    }
                    else
                    {
                        currentDict.Add(shifted, 1.0 / sameShiftChars.Count);
                    }
                }

                var ratio = GetMonogramRatio(currentDict, monogramSample);
                if (ratio < bestRatio)
                {
                    bestRatio = ratio;
                    result = shift;
                }
            }

            return result;
        }

        private static double GetMonogramRatio(IDictionary<char, double> currentDict, IReadOnlyDictionary<char, double> monogramSample)
        {
            return currentDict.Keys.Sum(key => Math.Abs(currentDict[key] - monogramSample[key]));
        }
        
        private static IEnumerable<int> KasiskiMethod(string input, int startSubstringLength)
        {
            var length = startSubstringLength;
            var lengthsBetweenMatched = new Dictionary<int, int>();

            while (length < input.Length / 3)
            {
                for (var i = 0; i < input.Length - length; i++)
                {
                    var stringToFind = input.Substring(i, length);
                    var stringsMatched = 1;
                    var positions = new List<int> {i};

                    for (var j = i + length; j < input.Length - length; j++)
                    {
                        if (input[j] == stringToFind[0])
                        {
                            var isMatch = true;
                            for (var k = 0; k < length && isMatch; k++)
                            {
                                if (stringToFind[k] != input[j + k])
                                {
                                    isMatch = false;
                                }
                            }

                            if (isMatch)
                            {
                                stringsMatched++;
                                positions.Add(j);
                            }
                        }
                    }

                    if (stringsMatched > 2)
                    {
                        for (var l = 1; l < positions.Count; l++)
                        {
                            var len = positions[l] - positions[l - 1];
                            if (lengthsBetweenMatched.ContainsKey(len))
                            {
                                lengthsBetweenMatched[len]++;
                            }
                            else
                            {
                                lengthsBetweenMatched.Add(len, 1);
                            }
                        }
                    }
                }

                length++;
            }

            var sum = lengthsBetweenMatched.Values.Sum();
            var avgValue = sum / (double)lengthsBetweenMatched.Count;
            var lengthsToGetFactors = lengthsBetweenMatched
                .Where(pair => pair.Value > avgValue)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            var allFactors = new SortedDictionary<int, int>();
            foreach (var factor in lengthsToGetFactors.SelectMany(pair => GetFactors(pair.Key)))
            {
                if (allFactors.ContainsKey(factor))
                {
                    allFactors[factor]++;
                }
                else
                {
                    allFactors.Add(factor, 1);
                }
            }

            sum = allFactors.Sum(factor => factor.Value);
            avgValue = sum / (double)allFactors.Count;

            var result = 
                (
                    from factor in allFactors 
                    where factor.Value > avgValue * 2 
                    select factor.Key
                )
                .ToList();

            if (result.Count == 0)
            {
                result.AddRange(
                    from factor in allFactors 
                    where factor.Value > avgValue 
                    select factor.Key
                );
            }

            return result;
        }

        private static IEnumerable<int> GetFactors(int n)
        {
            var result = new List<int>();
            for (var i = 1; i <= n; i++)
            {
                if (n % i == 0)
                {
                    result.Add(i);
                }
            }
            return result;
        }

        private static string GetKey(string input, string bestText, int bestKeyLength, IReadOnlyCollection<char> chars)
        {
            var result = new StringBuilder();
            var inputPart = input.Substring(0, bestKeyLength);
            var textPart = bestText.Substring(0, bestKeyLength);

            for (var i = 0; i < bestKeyLength; i++)
            {
                var inputPartChar = Utils.GetCharAlphabetIndex(inputPart[i]);
                var textPartChar = Utils.GetCharAlphabetIndex(textPart[i]);

                result.Append(chars.ElementAt((inputPartChar - textPartChar + chars.Count) % chars.Count));
            }

            return result.ToString();
        }

        private static void RandomSwap(IReadOnlyCollection<int> key, Random rnd, IReadOnlyCollection<char> chars)
        {
            var result = new List<int>(key);
            var index = rnd.Next() % key.Count;
            result[index] = (result[index] + rnd.Next()) % chars.Count;
        }

        private static IDictionary<char, double> GetMonogramSample(IEnumerable<string> textNames, char[] chars)
        {
            var letterCount = 0;
            var sumDict = chars.ToDictionary(ch => ch, ch => 0);

            foreach (var name in textNames)
            {
                var text = File.ReadAllText(name);
                var currentTextDict = GetMonogramDictionary(text, chars);
                foreach (var ch in chars)
                {
                    sumDict[ch] += currentTextDict[ch];
                    letterCount += currentTextDict[ch];
                }
            }

            return sumDict
                .OrderByDescending(x => x.Value)
                .ToDictionary(pair => pair.Key, pair => pair.Value / (double) letterCount);
        }

        private static IDictionary<char, int> GetMonogramDictionary(string text, IEnumerable<char> chars)
        {
            var result = chars.ToDictionary(ch => ch, ch => 0);
            foreach (var lowChar in text
                .Select(char.ToLower)
                .Where(lowChar => result.ContainsKey(lowChar)))
            {
                result[lowChar]++;
            }

            return result;
        }

        private static Dictionary<string, int> GetBigramDictionary(string text, IReadOnlyList<char> chars)
        {
            var matrix = new int[chars.Count, chars.Count];
            for (var i = 0; i < text.Length - 1; i++)
            {
                var lowChar1 = char.ToLower(text[i]);
                var lowChar2 = char.ToLower(text[i + 1]);
                if (chars.Contains(lowChar1) && chars.Contains(lowChar2))
                {
                    matrix[Utils.GetCharAlphabetIndex(lowChar1), Utils.GetCharAlphabetIndex(lowChar2)]++;
                }
            }
            var result = new Dictionary<string, int>();

            for (var i = 0; i < chars.Count; i++)
            {
                for (var j = 0; j < chars.Count; j++)
                {
                    result.Add(string.Concat(chars[i], chars[j]), matrix[i, j]);
                }
            }
            return result;
        }

        private static IDictionary<string, double> GetBigramText(string text, IReadOnlyList<char> chars)
        {
            var dict = GetBigramDictionary(text, chars);
            var letterCount = dict.Values.Sum();

            return dict
                .OrderByDescending(x => x.Value)
                .ToDictionary(pair => pair.Key, pair => pair.Value / (double) letterCount);
        }

        private static IDictionary<string, double> GetBigramSample(IEnumerable<string> textNames, IReadOnlyList<char> chars)
        {
            var sumDict = new Dictionary<string, int>();
            var letterCount = 0;
            foreach (var ch1 in chars)
            {
                foreach (var ch2 in chars)
                    sumDict.Add(string.Concat(ch1, ch2), 0);
            }

            foreach (var name in textNames)
            {
                using (var sr = new StreamReader(name))
                {
                    var text = sr.ReadToEnd();
                    var currentTextDict = GetBigramDictionary(text, chars);
                    foreach (var key in currentTextDict.Keys)
                    {
                        sumDict[key] += currentTextDict[key];
                        letterCount += currentTextDict[key];
                    }
                }
            }

            return sumDict
                .OrderByDescending(x => x.Value)
                .ToDictionary(pair => pair.Key, pair => pair.Value / (double) letterCount);
        }

        private static double GetRatio(IDictionary<string, double> input, IReadOnlyDictionary<string, double> book)
        {
            return input.Keys.Sum(key => Math.Abs(input[key] - book[key]));
        }
    }
}