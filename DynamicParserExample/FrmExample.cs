﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DynamicParser;
using Processor = DynamicParser.Processor;
using Region = DynamicParser.Region;

namespace DynamicParserExample
{
    public partial class FrmExample : Form
    {
        const string StrRecognize = "Отмена",
            StrRecognize1 = "Отмена.",
            StrRecognize2 = "Отмена..",
            StrRecognize3 = "Отмена...",
            StrError = "Ошибка",
            StrWordsFile = "Words";

        public static int PbWidth { get; private set; }
        public static int PbHeight { get; private set; }

        readonly string _strRecog, _strWordsPath = Path.Combine(Application.StartupPath, StrWordsFile + ".txt");
        readonly Graphics _backGrFront, _frontGrFront;
        readonly Bitmap _backBtm, _frontBtm;
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f), _redPen = new Pen(Color.Red, 2.0f);
        bool _draw, _education;
        Point _eduPoint;
        Rectangle? _currentRectangle;
        Thread _waitThread, _workThread;

        public FrmExample()
        {
            InitializeComponent();
            _backBtm = new Bitmap(pbDraw.Width, pbDraw.Height);
            _frontBtm = new Bitmap(pbDraw.Width, pbDraw.Height);
            _backGrFront = Graphics.FromImage(_backBtm);
            _frontGrFront = Graphics.FromImage(_frontBtm);
            _strRecog = btnRecognize.Text;
            PbWidth = pbDraw.Width;
            PbHeight = pbDraw.Height;
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

        void btnWordAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtWord.Text))
                    return;
                lstWords.Items.Insert(0, txtWord.Text);
                WordsSave();
                txtWord.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                WordsLoad();
            }
        }

        void btnWordRemove_Click(object sender, EventArgs e)
        {
            try
            {
                int index = lstWords.SelectedIndex;
                if (index < 0)
                    return;
                lstWords.Items.RemoveAt(index);
                WordsSave();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                WordsLoad();
            }
        }

        void WordsSave()
        {
            try
            {
                File.WriteAllLines(_strWordsPath, lstWords.Items.Cast<string>(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        void WordsLoad()
        {
            try
            {
                lstWords.Items.Clear();
                if (!File.Exists(_strWordsPath))
                    return;
                foreach (string s in File.ReadAllLines(_strWordsPath))
                {
                    string str = s;
                    if (s.Length > txtWord.MaxLength)
                        str = s.Substring(0, txtWord.MaxLength);
                    lstWords.Items.Add(str);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        void Recognizing()
        {
            try
            {
                if (_workThread != null)
                {
                    try
                    {
                        Waiting();
                        _workThread.Abort();
                    }
                    catch
                    {
                        //ignored
                    }
                    finally
                    {
                        _workThread = null;
                    }
                    return;
                }
                (_workThread = new Thread((ThreadStart)delegate
               {
                   try
                   {
                       try
                       {
                           List<ImageRect> reg = ImageChange.ImagesNoConflict;
                           if (reg.Count <= 0)
                               return;
                           Processor processor = new Processor(_backBtm, "Main");
                           SearchResults sr = SearchResults.Combine(processor.GetEqual(
                               new List<ProcessorContainer>((from ir in reg select new Processor(ir.Bitm, ir.SymbolString)).
                                   Select(proc => new ProcessorContainer(new[] { proc })))));
                           Region region = processor.CurrentRegion;
                           Attacher attacher = processor.CurrentAttacher;
                           foreach (ImageRect ir in reg)
                           {
                               if (region.IsConflict(ir.Rect))
                                   continue;
                               region.Add(ir.Rect);
                               attacher.Add(ir.MidX, ir.MidY);
                           }
                           sr.FindRegion(region);
                           attacher.SetMask(region);
                           StringBuilder sb = new StringBuilder();
                           foreach (Attach.Proc pr in attacher.Attaches.Select(att => att.Unique))
                           {
                               sb.Append(pr.Procs);//Дописать процедуру подбора различных букв.
                           }
                       }
                       catch (Exception ex)
                       {
                           Invoke((Action)delegate
                          {
                              MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK,
                                  MessageBoxIcon.Exclamation);
                          });
                           Waiting();
                           btnRecognize.Text = StrError;
                       }
                       finally
                       {
                           pbDraw.Refresh();
                       }
                   }
                   catch
                   {
                       //ignored
                   }
               })
                {
                    IsBackground = true,
                    Name = nameof(Recognizing)
                }).Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        void btnRecognize_Click(object sender, EventArgs e)
        {
            try
            {
                Recognizing();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
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
            WordsLoad();
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

        void Waiting()
        {
            if (_waitThread != null)
            {
                try
                {
                    _waitThread.Abort();
                }
                catch
                {
                    //ignored
                }
                finally
                {
                    _waitThread = null;
                }
                return;
            }
            (_waitThread = new Thread((ThreadStart)delegate
           {
               try
               {
                   for (int k = 0; k < 4; k++)
                       switch (k)
                       {
                           case 0:
                               Invoke((Action)delegate
                              {
                                  btnRecognize.Text = StrRecognize;
                              });
                               Thread.Sleep(100);
                               break;
                           case 1:
                               Invoke((Action)delegate
                              {
                                  btnRecognize.Text = StrRecognize1;
                              });
                               Thread.Sleep(100);
                               break;
                           case 2:
                               Invoke((Action)delegate
                              {
                                  btnRecognize.Text = StrRecognize2;
                              });
                               Thread.Sleep(100);
                               break;
                           case 3:
                               Invoke((Action)delegate
                              {
                                  btnRecognize.Text = StrRecognize3;
                              });
                               k = -1;
                               Thread.Sleep(100);
                               break;
                           default:
                               k = 0;
                               break;
                       }
               }
               catch (ThreadAbortException)
               {
                   Invoke((Action)delegate
                   {
                       btnRecognize.Text = StrRecognize;
                   });
               }
               catch (Exception ex)
               {
                   Invoke((Action)delegate
                   {
                       btnRecognize.Text = StrError;
                       MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                   });
               }
               finally
               {
                   Invoke((Action)delegate
                   {
                       btnRecognize.Text = _strRecog;
                   });
               }
           })
            {
                IsBackground = true,
                Name = nameof(Waiting)
            }).Start();
        }
    }
}