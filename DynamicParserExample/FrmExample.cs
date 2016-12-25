﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DynamicParser;
using Processor = DynamicParser.Processor;
using Region = DynamicParser.Region;

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
            try
            {
                _draw = false;
                SaveImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
                ic.Save(_currentRectangle.Value, btm);
            }
        }

        void WriteImage(ImageRect ir)
        {
            Bitmap btm = ir.Bitm;
            for (int y = ir.Rect.Y, y1 = 0; y < ir.Rect.Bottom; y++, y1++)
                for (int x = ir.Rect.X, x1 = 0; x < ir.Rect.Right; x++, x1++)
                    _backBtm.SetPixel(x, y, btm.GetPixel(x1, y1));
        }

        void btnRecognize_Click(object sender, EventArgs e)
        {
            try
            {
                Processor processor = new Processor(_backBtm, "Main");
                List<ImageRect> lst = new List<ImageRect>(ImageChange.Images);
                if (lst.Count <= 0)
                    return;
                Processor first = new Processor(lst[0].Bitm, lst[0].Tag);
                Processor[] procs = new Processor[lst.Count - 1];
                for (int k = 1, n = 0; k < lst.Count; k++, n++)
                    procs[n] = new Processor(lst[k].Bitm, lst[k].Tag);
                SearchResults sr = processor.GetEqual(first, procs);
                Region region = first.CurrentRegion;
                foreach (ImageRect ir in lst)
                    region.Add(ir.Rect);
                sr.FindRegion(region);
                Attacher attacher = first.CurrentAttacher;
                foreach (ImageRect ir in lst)
                    attacher.Add(ir.MidX, ir.MidY);
                attacher.SetMask(region);
                //МОЖНО СДЕЛАТЬ ПОИСК СЛОВА
                List<Attach.Proc> atts = attacher.Attaches.Select(att => att.Unique).ToList();
                foreach (Attach.Proc fproc in atts)
                    WriteImage(lst.First(rect => rect.Tag == ((fproc.Procs?.Count() ?? 0) > 0 ? (fproc.Procs?.ElementAt(0).Tag ?? string.Empty) : string.Empty)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
            finally
            {
                pbDraw.Refresh();
            }
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