using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DynamicProcessor;

namespace DynamicParserExample
{
    public struct ImageRect
    {
        public const string ExtImg = "bmp";
        public static string SearchPath { get; } = Application.StartupPath;
        static IEnumerable<string> BitmapFiles => Directory.GetFiles(SearchPath, $"*.{ExtImg}");

        public Bitmap Bitm { get; }
        public string Tag { get; }
        public string ImagePath { get; }

        public ImageRect(Bitmap btm, string tag, string imagePath)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(ImageRect)}: {nameof(btm)} = null.");
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentNullException(nameof(imagePath), $@"{nameof(ImageRect)}: {nameof(imagePath)} = null.");
            Bitm = btm;
            Tag = tag;
            ImagePath = imagePath;
        }

        public string SymbolString => IsSymbol ? Tag.Substring(1) : null;

        public string SymbolName => IsSymbol ? new string(Tag[1], 1) : null;

        public char? Symbol => IsSymbol ? char.ToUpper(Tag[1]) : (char?)null;

        public bool IsSymbol
        {
            get
            {
                ulong? number;
                return NameParser(out number) && number.HasValue;
            }
        }

        public ulong? Number
        {
            get
            {
                ulong? number;
                return NameParser(out number) ? number : null;
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

        bool NameParser(out ulong? number)
        {
            number = null;
            if (string.IsNullOrWhiteSpace(Tag) || Tag.Length < 3)
                return false;
            char ch = char.ToUpper(Tag[0]);
            if (ch != 'M' && ch != 'B')
                return false;
            ulong ul;
            if (!ulong.TryParse(Tag.Substring(2), out ul)) return false;
            number = ul;
            return true;
        }

        public static IEnumerable<ImageRect> Images
        {
            get
            {
                foreach (string fname in BitmapFiles)
                    using (FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Read))
                    {
                        ImageRect ir = new ImageRect(new Bitmap(fs), Path.GetFileNameWithoutExtension(fname), fname);
                        if (ir.IsSymbol)
                            yield return ir;
                    }
            }
        }
    }

    public sealed class FileOperations
    {
        static string NewFileName(char name)
        {
            ImageRect? imageRect = null;
            {
                char nm = char.ToUpper(name);
                ulong max = 0;
                foreach (ImageRect ir in ImageRect.Images)
                {
                    if (ir.Symbol != nm)
                        continue;
                    if (ir.Number == null)
                        continue;
                    if (ir.Number < max)
                        continue;
                    max = ir.Number.Value;
                    imageRect = ir;
                }
            }
            char prefix = char.IsUpper(name) ? 'b' : 'm';
            // ReSharper disable once MergeSequentialChecks
            if (imageRect != null && imageRect.Value.Number != null)
            {
                string path = $@"{ImageRect.SearchPath}\{prefix}{name}{imageRect.Value.Number.Value + 1}";
                return $"{path}.{ImageRect.ExtImg}";
            }
            string pathEmpty = $@"{ImageRect.SearchPath}\{prefix}{name}0";
            return $"{pathEmpty}.{ImageRect.ExtImg}";
        }

        public static void Save(char name, Bitmap btm)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(Save)}: Выделенная область не указана.");
            btm.Save(NewFileName(name));
        }
    }
}