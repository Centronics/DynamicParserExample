using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DynamicParserExample
{
    public struct ImageRect
    {
        public Bitmap Bitm;
        public string Tag;

        public string SymbolString => Symbol.HasValue ? new string(Symbol.Value, 1) : null;

        public bool HasValue => Symbol.HasValue;

        char? Symbol
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(Tag) || Tag.Length < 2)
                        return null;
                    if (char.ToLower(Tag[0]) == 'b')
                        return char.ToUpper(Tag[1]);
                    return char.ToLower(Tag[0]) == 'm' ? char.ToLower(Tag[1]) : (char?)null;
                }
                catch
                {
                    return null;
                }
            }
        }
    }

    public sealed class FileOperations
    {
        const string ExtImg = "bmp", ExtSet = "xml";
        static readonly string SearchPath = Application.ExecutablePath;

        public static IEnumerable<ImageRect> Images
        {
            get
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (string fname in GetFiles(ExtImg))
                {
                    if (string.IsNullOrWhiteSpace(fname))
                        continue;
                    string fn = Path.ChangeExtension(fname, ExtSet);
                    if (!File.Exists(fn))
                        continue;
                    ImageRect ir = new ImageRect
                    {
                        Bitm = new Bitmap(fname),
                        Tag = Path.GetFileNameWithoutExtension(fname)
                    };
                    yield return ir;
                }
            }
        }

        static long? GetNumber(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return null;
            if (str.Length < 2)
                return null;
            StringBuilder sb = new StringBuilder();
            for (int k = str.Length - 1; k >= 0; k--)
            {
                char ch = str[k];
                if (char.IsDigit(ch))
                    sb.Append(ch);
                else
                    break;
            }
            long number;
            long.TryParse(sb.ToString(), out number);
            return number;
        }

        public static IEnumerable<string> GetFiles(string ext)
        {
            return Directory.GetFiles(SearchPath, $"*.{ext}");
        }

        static string NewFileName(char name)
        {
            long fileNumber = 0;
            foreach (string fname in GetFiles(ExtImg))
            {
                string fn = Path.GetFileNameWithoutExtension(fname);
                if (string.IsNullOrWhiteSpace(fn) || fn.Length < 2) continue;
                fn = fn.Substring(2);
                if (string.IsNullOrWhiteSpace(fn) || fn.Length < 2)
                    continue;
                if (fn[1] != name)
                    continue;
                long? number = GetNumber(fn);
                if (number == null)
                    continue;
                long r = number.Value;
                if (r < 0 || fileNumber >= r) continue;
                fileNumber = r;
            }
            char symbol = char.IsUpper(name) ? 'b' : 'm';
            string path = $@"{SearchPath}\{symbol}{name}{fileNumber + 1}";
            return $"{path}.{ExtImg}";
        }

        public static void Save(char name, Bitmap btm)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(Save)}: Выделенная область не указана.");
            btm.Save(NewFileName(name));
        }
    }

}