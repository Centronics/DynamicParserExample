using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using Region = DynamicParser.Region;

namespace DynamicParserExample
{
    public struct ImageRect
    {
        public Bitmap Bitm;
        public Rectangle Rect;
        public string Tag;

        public int MidX => Rect.X + Rect.Right / 2;

        public int MidY => Rect.Y + Rect.Bottom / 2;

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

    public partial class ImageChange : Form
    {
        const string ExtImg = "bmp", ExtSet = "xml";
        static readonly string SearchPath = Application.ExecutablePath;
        string _filePath, _filePathSettings;

        public ImageChange()
        {
            InitializeComponent();
        }

        static IEnumerable<ImageRect> Images
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
                    Rectangle r = (Rectangle)new XmlSerializer(typeof(Rectangle)).Deserialize(new FileStream(fn, FileMode.Open, FileAccess.Read));
                    ImageRect ir = new ImageRect
                    {
                        Bitm = new Bitmap(fname),
                        Rect = r,
                        Tag = Path.GetFileNameWithoutExtension(fname)
                    };
                    yield return ir;
                }
            }
        }

        public static List<ImageRect> ImagesNoConflict
        {
            get
            {
                List<ImageRect> lst = new List<ImageRect>();
                Region reg = new Region(FrmExample.PbWidth, FrmExample.PbHeight);
                foreach (ImageRect ir in Images)
                {
                    if (reg.IsConflict(ir.Rect))
                        continue;
                    reg.Add(ir.Rect);
                    lst.Add(ir);
                }
                return lst;
            }
        }

        void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                _filePath = string.Empty;
                if (txtName.Text.Length > 1)
                {
                    MessageBox.Show(this, @"Название символа не может быть более одного знака.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show(this, @"Название символа не может быть пустым.");
                    return;
                }
                NewFileName(txtName.Text[0]);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
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
            if (!Directory.Exists(SearchPath))
                throw new ArgumentException($"{nameof(NewFileName)}: Путь для хранения файлов не существует: {SearchPath}.");
            foreach (string fn in Directory.GetFiles(SearchPath, $"*.{ext}"))
            {
                if (string.IsNullOrWhiteSpace(fn))
                    continue;
                yield return fn;
            }
        }

        void NewFileName(char name)
        {
            long fileNumber = 0;
            foreach (string fname in GetFiles(ExtImg))
            {
                string fn = Path.GetFileNameWithoutExtension(fname);
                if (string.IsNullOrWhiteSpace(fn) || fn.Length < 2) continue;
                fn = fn.Substring(2);
                long r = 0;
                if (string.IsNullOrWhiteSpace(fn) || fn.Length < 2)
                    continue;
                if (fn[1] != name)
                    continue;
                long? number = GetNumber(fn);
                if (number != null)
                    r = number.Value;
                if (r < 0 || fileNumber >= r) continue;
                fileNumber = r;
            }
            char symbol = char.IsUpper(name) ? 'b' : 'm';
            string path = $@"{SearchPath}\{symbol}{name}{fileNumber + 1}.";
            _filePath = $"{path}{ExtImg}";
            _filePathSettings = $"{path}{ExtSet}";
        }

        public void Save(Rectangle pt, Bitmap btm)
        {
            if (btm == null)
                throw new ArgumentNullException($@"{nameof(Save)}: Выделенная область не указана.");
            btm.Save(_filePath);
            using (FileStream fs = new FileStream(_filePathSettings, FileMode.Open, FileAccess.Read))
                new XmlSerializer(typeof(Rectangle)).Serialize(fs, pt);
        }
    }
}