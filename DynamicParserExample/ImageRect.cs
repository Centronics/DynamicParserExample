using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using DynamicProcessor;

namespace DynamicParserExample
{
    public class ImageRect
    {
        public const string ExtImg = "bmp";
        public static string SearchPath { get; } = Application.StartupPath;
        static IEnumerable<string> BitmapFiles => Directory.GetFiles(SearchPath, $"*.{ExtImg}");

        public Bitmap Bitm { get; }
        public string Tag { get; }
        public string ImagePath { get; }
        public string SymbolString { get; }
        public string SymbolName { get; }
        public char Symbol { get; }
        public ulong Number { get; }
        public bool IsSymbol { get; }

        public ImageRect(Bitmap btm, string tag, string imagePath)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(ImageRect)}: {nameof(btm)} = null.");
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentNullException(nameof(imagePath), $@"{nameof(ImageRect)}: {nameof(imagePath)} = null.");
            ulong? number;
            if (!NameParser(out number, tag) || number == null)
                return;
            SymbolString = tag.Substring(1);
            Symbol = char.ToUpper(tag[1]);
            SymbolName = new string(Symbol, 1);
            Number = number.Value;
            Bitm = btm;
            Tag = tag;
            ImagePath = imagePath;
            IsSymbol = true;
        }

        public static ImageRect Save(char name, Bitmap btm)
        {
            if (btm == null)
                throw new ArgumentNullException(nameof(btm), $@"{nameof(Save)}: Сохраняемое изображение не указано.");
            string path = NewFileName(name);
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                btm.Save(fs, ImageFormat.Bmp);
            ImageRect ir = new ImageRect(btm, Path.GetFileNameWithoutExtension(path), path);
            if (!ir.IsSymbol)
                throw new Exception($"{nameof(Save)}: Неизвестная ошибка при сохранении изображения.");
            return ir;
        }

        public SignValue[,] ImageMap
        {
            get
            {
                if (!IsSymbol)
                    return null;
                SignValue[,] mas = new SignValue[Bitm.Width, Bitm.Height];
                for (int y = 0; y < Bitm.Height; y++)
                    for (int x = 0; x < Bitm.Width; x++)
                        mas[x, y] = new SignValue(Bitm.GetPixel(x, y));
                return mas;
            }
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

        static bool NameParser(out ulong? number, string tag)
        {
            number = null;
            if (string.IsNullOrWhiteSpace(tag) || tag.Length < 3)
                return false;
            char ch = char.ToUpper(tag[0]);
            if (ch != 'M' && ch != 'B')
                return false;
            ulong ul;
            if (!ulong.TryParse(tag.Substring(2), out ul)) return false;
            number = ul;
            return true;
        }

        static string NewFileName(char name)
        {
            ImageRect imageRect = null;
            {
                char nm = char.ToUpper(name);
                ulong max = 0;
                foreach (ImageRect ir in Images)
                {
                    if (ir.Symbol != nm)
                        continue;
                    if (ir.Number < max)
                        continue;
                    max = ir.Number;
                    imageRect = ir;
                }
            }
            char prefix = char.IsUpper(name) ? 'b' : 'm';
            return imageRect != null ? $@"{SearchPath}\{prefix}{name}{unchecked(imageRect.Number + 1)}.{ExtImg}" : $@"{SearchPath}\{prefix}{name}0.{ExtImg}";
        }
    }
}