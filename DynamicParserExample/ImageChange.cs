using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DynamicParserExample
{
    public partial class ImageChange : Form
    {
        const string Ext = "bmp";
        static readonly string SearchPath = Application.ExecutablePath;

        public ImageChange()
        {
            InitializeComponent();
        }

        public string FilePath { get; private set; }

        public static IEnumerable<Bitmap> Images => Files.Select(fn => new Bitmap(fn));

        void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                FilePath = string.Empty;
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
                FilePath = NewFileName(txtName.Text[0]);
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

        public static IEnumerable<string> Files
        {
            get
            {
                if (!Directory.Exists(SearchPath))
                    throw new ArgumentException($"{nameof(NewFileName)}: Путь для хранения файлов не существует: {SearchPath}.");
                foreach (string fn in Directory.GetFiles(SearchPath, $"*.{Ext}"))
                {
                    if (string.IsNullOrWhiteSpace(fn))
                        continue;
                    yield return fn;
                }
            }
        }

        public static string NewFileName(char name)
        {
            long fileNumber = 0;
            foreach (string fname in Files)
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
            return $@"{SearchPath}\{symbol}{name}{fileNumber + 1}.{Ext}";
        }
    }
}