using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DynamicParserExample
{
    public struct ImageRect
    {
        public Bitmap Bitm;
        public string Tag;

        public string SymbolString => Symbol.HasValue ? new string(Symbol.Value, 1) : null;

        char? Symbol
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(Tag) || Tag.Length < 3)
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
        const string ExtImg = "bmp";
        static readonly string SearchPath = Application.StartupPath;

        public static IEnumerable<string> BitmapFiles => Directory.GetFiles(SearchPath, $"*.{ExtImg}");

        public static IEnumerable<ImageRect> Images => BitmapFiles.Select(fname => new ImageRect
        {
            Bitm = new Bitmap(fname),
            Tag = Path.GetFileNameWithoutExtension(fname)
        });

        static long? GetNumber(string str)
        {
            long number;
            long.TryParse(str, out number);
            return number;
        }

        static string NewFileName(char name)
        {
            long fileNumber = 0;
            foreach (string fname in BitmapFiles)
            {
                string fn = Path.GetFileNameWithoutExtension(fname);
                if (string.IsNullOrWhiteSpace(fn) || fn.Length < 3) continue;
                if (fn[1] != name)
                    continue;
                fn = fn.Substring(2);
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