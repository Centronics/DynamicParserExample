using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DynamicParser;

namespace DynamicParserExample
{
    public partial class FrmExample : Form
    {
        readonly Graphics _backGrFront, _frontGrFront;
        readonly Bitmap _backBtm, _frontBtm;
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f), _redPen = new Pen(Color.Red, 2.0f);
        bool _draw, _education;
        Point _eduPoint;
        Rectangle? _currentRectangle;

        public FrmExample()
        {
            InitializeComponent();
            _backBtm = new Bitmap(pbDraw.Width, pbDraw.Height);
            _frontBtm = new Bitmap(pbDraw.Width, pbDraw.Height);
            _backGrFront = Graphics.FromImage(_backBtm);
            _frontGrFront = Graphics.FromImage(_frontBtm);
        }

        void pbDraw_MouseDown(object sender, MouseEventArgs e)
        {
            _draw = true;
            _eduPoint = e.Location;
        }

        void pbDraw_MouseUp(object sender, MouseEventArgs e)
        {
            _draw = false;
            SaveImage();
        }

        void SaveImage()
        {
            if (_currentRectangle == null)
                return;
            using (ImageChange ic = new ImageChange())
            {
                if (ic.ShowDialog() != DialogResult.OK)
                    return;
                Bitmap btm = new Bitmap(_currentRectangle.Value.Width, _currentRectangle.Value.Height);
                for (int y = _currentRectangle.Value.Y, y1 = 0; y < _currentRectangle.Value.Bottom; y++, y1++)
                    for (int x = _currentRectangle.Value.X, x1 = 0; x < _currentRectangle.Value.Right; x++, x1++)
                        btm.SetPixel(x1, y1, _backBtm.GetPixel(x, y));
                btm.Save(ic.FilePath);
            }
        }

        void btnRecognize_Click(object sender, EventArgs e)
        {
            Processor processor = new Processor(_backBtm, "Main");
            List<Bitmap> lst = new List<Bitmap>(ImageChange.Images);

        }

        void pbDraw_MouseLeave(object sender, EventArgs e)
        {
            _currentRectangle = null;
            _draw = false;
        }

        void pbDraw_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (!_draw) return;
                if (!_education)
                {
                    _backGrFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
                    return;
                }
                _frontGrFront.Clear(Color.Transparent);
                int bx = _eduPoint.X, by = _eduPoint.Y;
                if (e.X < _eduPoint.X)
                    bx = e.X;
                if (e.Y < _eduPoint.Y)
                    by = e.Y;
                int lx = Math.Abs(_eduPoint.X - e.X), ly = Math.Abs(_eduPoint.Y - e.Y);
                _currentRectangle = new Rectangle(bx, by, lx, ly);
                _frontGrFront.DrawRectangle(_redPen, _currentRectangle.Value);
            }
            finally
            {
                pbDraw.Refresh();
            }
        }

        void FrmExample_Shown(object sender, EventArgs e)
        {
            pbDraw.BackgroundImage = _backBtm;
            pbDraw.Image = _frontBtm;
            btnClear_Click(null, null);
        }

        void btnClear_Click(object sender, EventArgs e)
        {
            _backGrFront.Clear(Color.White);
            pbDraw.Refresh();
        }

        void btnEducation_Click(object sender, EventArgs e)
        {
            try
            {
                _frontGrFront.Clear(Color.Transparent);
                _education = !_education;
                btnRecognize.Enabled = !_education;
            }
            finally
            {
                pbDraw.Refresh();
            }
        }
    }
}