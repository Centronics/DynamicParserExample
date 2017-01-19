using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicParserExample
{
    public partial class FrmSymbol : Form
    {
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f);
        readonly Graphics _grFront;
        readonly Bitmap _btmFront;
        readonly Stopwatch _sw = new Stopwatch();
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
                    MessageBox.Show(this, @"Необходимо вписать название символа. Оно не может быть более одного знака и состоять из невидимых символов.");
                    _sw.Restart();
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

        void btnClear_Click(object sender, EventArgs e)
        {
            try
            {
                _grFront.Clear(Color.White);
            }
            finally
            {
                pbBox.Refresh();
            }
        }

        void FrmSymbol_Shown(object sender, EventArgs e)
        {
            btnClear_Click(null, null);
            _sw.Restart();
        }

        void FrmSymbol_KeyUp(object sender, KeyEventArgs e)
        {
            if (_sw.ElapsedMilliseconds < 1000)
                return;
            _sw.Stop();
            // ReSharper disable once SwitchStatementMissingSomeCases
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

        void txtSymbol_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((Keys)e.KeyChar == Keys.Enter || (Keys)e.KeyChar == Keys.Tab || (Keys)e.KeyChar == Keys.Escape ||
                (Keys)e.KeyChar == Keys.Pause || (Keys)e.KeyChar == Keys.XButton1 || e.KeyChar == 15)
                e.Handled = true;
        }

        void FrmSymbol_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((Keys)e.KeyChar == Keys.Enter || (Keys)e.KeyChar == Keys.Tab || (Keys)e.KeyChar == Keys.Escape ||
                (Keys)e.KeyChar == Keys.Pause || (Keys)e.KeyChar == Keys.XButton1 || e.KeyChar == 15)
                e.Handled = true;
        }
    }
}