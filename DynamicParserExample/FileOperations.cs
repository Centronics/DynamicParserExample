using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DynamicProcessor;

namespace DynamicParserExample
{
    public struct ImageRect
    {
        public Bitmap Bitm;
        public string Tag;

        public string SymbolString
        {
            get
            {
                if (!IsSymbol)
                    return null;
                string str = Tag.Substring(1);
                if (str.Length < 2)
                    throw new Exception($"{nameof(SymbolString)}: Имя файла задано неверно ({Tag}).");
                return str;
            }
        }

        public string SymbolName => new string(Tag[1], 1);

        bool IsSymbol
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Tag) || Tag.Length < 3)
                    return false;
                char ch = char.ToLower(Tag[0]);
                return ch == 'm' || ch == 'b';
            }
        }

        public SignValue[,] ImageMap
        {
            get
            {
                SignValue[,] mas = new SignValue[Bitm.Width, Bitm.Height];
                for (int y = 0; y < Bitm.Height; y++)
                    for (int x = 0; x < Bitm.Width; x++)
                    {
                        Color col = Bitm.GetPixel(x, y);
                        if (col.ToArgb() == 0)
                            col = Color.White;
                        mas[x, y] = new SignValue(col);
                    }
                return mas;
            }
        }
    }

    public sealed class FileOperations
    {
        const string ExtImg = "bmp";
        static readonly string SearchPath = Application.StartupPath;

        public static IEnumerable<string> BitmapFiles => Directory.GetFiles(SearchPath, $"*.{ExtImg}");

        public static IEnumerable<ImageRect> Images => BitmapFiles.Select(fname =>
        {
            using (FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read))
            {
                return new ImageRect
                {
                    Bitm = new Bitmap(fs),
                    Tag = Path.GetFileNameWithoutExtension(fname)
                };
            }
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