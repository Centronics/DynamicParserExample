﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using DynamicProcessor;

namespace DynamicParserExample
{
    /// <summary>
    /// Предназначен для работы с образами искомых букв.
    /// </summary>
    public class ImageRect
    {
        /// <summary>
        /// Расширение изображения образа искомой буквы.
        /// </summary>
        public const string ExtImg = "bmp";

        /// <summary>
        /// Путь, по которому осуществляется поиск искомых образов букв.
        /// </summary>
        public static string SearchPath { get; } = Application.StartupPath;

        /// <summary>
        /// Получает изображения букв, поиск которых следует осуществить.
        /// </summary>
        static IEnumerable<string> BitmapFiles => Directory.GetFiles(SearchPath, $"*.{ExtImg}");

        /// <summary>
        /// Содержит текущее изображение.
        /// </summary>
        public Bitmap Bitm { get; }

        /// <summary>
        /// Полный путь к текущему образу.
        /// </summary>
        public string ImagePath { get; }

        /// <summary>
        /// Определяет значение поля <see cref="DynamicParser.Processor.Tag"/>.
        /// </summary>
        public string SymbolString { get; }

        /// <summary>
        /// Символ текущей буквы в виде строки.
        /// </summary>
        public string SymbolName { get; }

        /// <summary>
        /// Символ текущей буквы.
        /// </summary>
        public char Symbol { get; }

        /// <summary>
        /// Номер текущего образа.
        /// </summary>
        public ulong Number { get; }

        /// <summary>
        /// Получает значение, является ли данный файл образом, предназначенным для распознавания.
        /// Значение true означает, что данный файл является образом для распознавания, false - нет.
        /// </summary>
        public bool IsSymbol { get; }

        /// <summary>
        /// Инициализирует экземпляр образа буквы для распознавания.
        /// </summary>
        /// <param name="btm">Изображение буквы.</param>
        /// <param name="tag">Название буквы.</param>
        /// <param name="imagePath">Полный путь к изображению буквы.</param>
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
            ImagePath = imagePath;
            IsSymbol = true;
        }

        /// <summary>
        /// Сохраняет указанный образ буквы с указанным названием.
        /// </summary>
        /// <param name="name">Название буквы.</param>
        /// <param name="btm">Изображение буквы.</param>
        /// <returns>Возвращает экмемпляр текущего класса образа буквы.</returns>
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

        /// <summary>
        /// Получает текущее изображение в виде набора знаков объектов карты.
        /// </summary>
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

        /// <summary>
        /// Получает все имеющиеся на данный момент образы букв для распознавания.
        /// </summary>
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

        /// <summary>
        /// Выполняет разбор имени файла с образом буквы, выделяя номер буквы.
        /// </summary>
        /// <param name="number">Возвращает номер текущей буквы.</param>
        /// <param name="tag">Имя файла без расширения.</param>
        /// <returns>Возвращает значение true в случае, если разбор имени файла прошёл успешно, в противном случае - false.</returns>
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

        /// <summary>
        /// Генерирует имя нового образа, увеличивая его номер.
        /// </summary>
        /// <param name="name">Имя образа, на основании которого требуется сгенерировать новое имя.</param>
        /// <returns>Возвращает строку полного пути к файлу нового образа.</returns>
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