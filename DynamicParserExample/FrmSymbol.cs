using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DynamicParserExample
{
    public partial class FrmSymbol : Form
    {
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f);
        readonly Graphics _grFront;
        readonly Bitmap _btmFront;
        readonly Stopwatch _sw = new Stopwatch();
        public ImageRect LastImage { get; private set; }
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

        void pbBox_MouseLeave(object sender, EventArgs e)
        {
            _draw = false;
        }

        void pbBox_MouseDown(object sender, MouseEventArgs e)
        {
            RunFunction(() =>
            {
                _draw = true;
                _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
            });
            RunFunction(() => RunFunction(() => pbBox.Refresh()));
        }

        void pbBox_MouseMove(object sender, MouseEventArgs e)
        {
            RunFunction(() =>
            {
                if (_draw)
                    _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
            });
            RunFunction(() => RunFunction(() => pbBox.Refresh()));
        }

        void btnOK_Click(object sender, EventArgs e)
        {
            RunFunction(() =>
            {
                if (string.IsNullOrWhiteSpace(txtSymbol.Text))
                {
                    MessageBox.Show(this, @"Необходимо вписать название символа. Оно не может быть более одного знака и состоять из невидимых символов.");
                    _sw.Restart();
                    return;
                }
                LastImage = ImageRect.Save(txtSymbol.Text[0], _btmFront);
                DialogResult = DialogResult.OK;
            });
        }

        void btnClear_Click(object sender, EventArgs e)
        {
            RunFunction(() => _grFront.Clear(Color.White));
            RunFunction(() => pbBox.Refresh());
        }

        void FrmSymbol_Shown(object sender, EventArgs e)
        {
            RunFunction(() =>
            {
                btnClear_Click(null, null);
                _sw.Restart();
            });
        }

        void FrmSymbol_KeyUp(object sender, KeyEventArgs e)
        {
            RunFunction(() =>
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
            });
        }

        void txtSymbol_KeyPress(object sender, KeyPressEventArgs e)
        {
            RunFunction(() =>
            {
                if ((Keys)e.KeyChar == Keys.Enter || (Keys)e.KeyChar == Keys.Tab ||
                    (Keys)e.KeyChar == Keys.Escape ||
                    (Keys)e.KeyChar == Keys.Pause || (Keys)e.KeyChar == Keys.XButton1 || e.KeyChar == 15)
                    e.Handled = true;
            });
        }

        void FrmSymbol_KeyPress(object sender, KeyPressEventArgs e)
        {
            RunFunction(() =>
            {
                if ((Keys)e.KeyChar == Keys.Enter || (Keys)e.KeyChar == Keys.Tab || (Keys)e.KeyChar == Keys.Escape ||
                    (Keys)e.KeyChar == Keys.Pause || (Keys)e.KeyChar == Keys.XButton1 || e.KeyChar == 15)
                    e.Handled = true;
            });
        }

        void RunFunction(Action act)
        {
            if (act == null)
                return;
            try
            {
                act();
            }
            catch (Exception ex)
            {
                try
                {
                    MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                catch
                {
                    //ignored
                }
            }
        }
    }
}