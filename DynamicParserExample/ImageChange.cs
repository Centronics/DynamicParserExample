using System;
using System.IO;
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

        string GetFileName(string fname)
        {
            if (string.IsNullOrWhiteSpace(fname))
                throw new ArgumentException($@"{nameof(GetFileName)}: Имя файла не может быть пустым.", nameof(fname));
            if (fname.Length > 1)
                throw new ArgumentException($@"{nameof(GetFileName)}: Имя файла не может быть длиннее одного символа.", nameof(fname));
            return fname.ToUpper() == fname ? $"b{fname}" : $"m{fname}";
        }

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
                FilePath = NewFileName(GetFileName(txtName.Text));
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        static string NewFileName(string name)
        {
            if (!Directory.Exists(SearchPath))
                throw new ArgumentException($"{nameof(NewFileName)}: Путь для хранения файлов не существует: {SearchPath}");
            string[] files = Directory.GetFiles(SearchPath, $"*.{Ext}");
            long fileNumber = 0;
            foreach (string fname in files)
            {
                if (string.IsNullOrWhiteSpace(fname))
                    continue;
                string fn = Path.GetFileNameWithoutExtension(fname);
                if (string.IsNullOrWhiteSpace(fn) || fn.Length < 2) continue;
                fn = fn.Substring(2);
                long r;
                if (string.IsNullOrWhiteSpace(fn))
                    r = 0;
                else
                    if (!long.TryParse(fn, out r)) continue;
                if (r < 0 || fileNumber >= r) continue;
                fileNumber = r;
            }
            return $@"{SearchPath}\{name}{fileNumber}.{Ext}";
        }
    }
}