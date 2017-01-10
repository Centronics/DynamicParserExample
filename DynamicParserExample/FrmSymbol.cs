using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicParserExample
{
    public partial class FrmSymbol : Form
    {
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f);
        readonly Graphics _grFront;
        readonly Bitmap _btmFront;
        bool _draw;

        public FrmSymbol()
        {
            InitializeComponent();
            _btmFront = new Bitmap(pbBox.Width, pbBox.Height);
            _grFront = Graphics.FromImage(_btmFront);
            pbBox.Image = _btmFront;
        }

        void pbBox_MouseUp(object sender, MouseEventArgs e)
        {
            _draw = false;
        }

        void pbBox_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                _draw = true;
                _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
            }
            finally
            {
                pbBox.Refresh();
            }
        }

        void pbBox_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_draw)
                    _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
            }
            finally
            {
                pbBox.Refresh();
            }
        }

        void pbBox_MouseLeave(object sender, EventArgs e)
        {
            _draw = false;
        }

        void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtSymbol.Text))
                {
                    MessageBox.Show(this, @"Необходимо вписать название символа.");
                    return;
                }
                FileOperations.Save(txtSymbol.Text[0], _btmFront);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка");
            }
        }

        void FrmSymbol_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    btnOK_Click(null, null);
                    break;
                case Keys.Escape:
                    DialogResult = DialogResult.Cancel;
                    break;
            }
        }
    }
}