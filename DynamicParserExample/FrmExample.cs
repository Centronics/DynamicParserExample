using System;
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
        const string StrRecognize = "Отмена   ",
            StrRecognize1 = "Отмена.  ",
            StrRecognize2 = "Отмена.. ",
            StrRecognize3 = "Отмена...",
            StrWordsFile = "Words";

        readonly string _strRecog, _strWordsPath = Path.Combine(Application.StartupPath, $"{StrWordsFile}.txt");
        readonly Graphics _grFront;
        readonly Bitmap _frontBtm;
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f);
        Thread _waitThread, _workThread;
        bool _draw;
        int _currentImage;

        public FrmExample()
        {
            InitializeComponent();
            _frontBtm = new Bitmap(pbDraw.Width, pbDraw.Height);
            _grFront = Graphics.FromImage(_frontBtm);
            _strRecog = btnRecognize.Text;
            btnNext_Click(null, null);
        }

        void pbDraw_MouseDown(object sender, MouseEventArgs e)
        {
            _draw = true;
            _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
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
                    if (string.IsNullOrWhiteSpace(s))
                        continue;
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

        Bitmap GetBitmap(Rectangle rect)
        {
            Bitmap btm = new Bitmap(rect.Width, rect.Height);
            if (rect.Right > _frontBtm.Width)
                throw new ArgumentException($@"{nameof(GetBitmap)}: Координата оси X выходит за пределы.", nameof(rect));
            if (rect.Bottom > _frontBtm.Height)
                throw new ArgumentException($@"{nameof(GetBitmap)}: Координата оси Y выходит за пределы.", nameof(rect));
            for (int y1 = 0, y = rect.Y; y < rect.Bottom; y1++, y++)
                for (int x1 = 0, x = rect.X; x < rect.Right; x1++, x++)
                    btm.SetPixel(x1, y1, _frontBtm.GetPixel(x, y));
            return btm;
        }

        void pbDraw_MouseUp(object sender, MouseEventArgs e)
        {
            _draw = false;
        }

        void btnNext_Click(object sender, EventArgs e)
        {
            try
            {
                List<ImageRect> lst = new List<ImageRect>(FileOperations.Images);
                if (lst.Count <= 0)
                    return;
                if (_currentImage >= lst.Count - 1)
                    _currentImage = 0;
                else
                    _currentImage++;
                if (_currentImage < lst.Count && _currentImage >= 0)
                    pbBrowse.Image = lst[_currentImage].Bitm;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        void btnPrev_Click(object sender, EventArgs e)
        {
            try
            {
                List<ImageRect> lst = new List<ImageRect>(FileOperations.Images);
                if (lst.Count <= 0)
                    return;
                if (_currentImage <= 0)
                    _currentImage = lst.Count - 1;
                else
                    _currentImage--;
                if (_currentImage < lst.Count && _currentImage >= 0)
                    pbBrowse.Image = lst[_currentImage].Bitm;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        void btnSaveImage_Click(object sender, EventArgs e)
        {
            try
            {
                using (FrmSymbol fs = new FrmSymbol())
                    fs.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        static WordSearcher GetWords(IEnumerable<Registered> lstRegs)//МОЖНО СДЕЛАТЬ КОНСТРУКТОР
        {
            if (lstRegs == null)
                throw new ArgumentNullException(nameof(lstRegs), $@"{nameof(GetWords)}: Список зарегистрированных объектов не указан (null).");
            return new WordSearcher(new List<string[]>(
                    from registered in lstRegs where registered != null select (from proc in registered.Register from pr in proc.Procs select pr.Tag).ToArray()));
        }

        void btnRecognize_Click(object sender, EventArgs e)
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
                        Waiting();
                        List<ImageRect> images = new List<ImageRect>(FileOperations.Images);
                        if (images.Count <= 0)
                        {
                            MessageBox.Show(this, @"Никаких образов не найдено. Нарисуйте какой-нибудь образ, затем сохраните его.");
                            return;
                        }
                        Processor processor = new Processor(_frontBtm, "Main");
                        SearchResults sr = processor.GetEqual((from ir in images select new Processor(ir.Bitm, ir.SymbolString)).ToArray());
                        Region region = sr.AllMaps;
                        WordSearcher ws = GetWords(region.Elements);
                        foreach (Registered registered in region.Elements)
                        {
                            Bitmap btm = GetBitmap(registered.Region);
                            foreach (Reg reg in registered.Register)
                                foreach (Processor pr in reg.Procs)
                                {
                                    string tag = pr.Tag;
                                    if (tag.Length != 1)
                                        continue;
                                    FileOperations.Save(tag[0], btm);
                                }
                        }
                        List<string> results = (from string s in lstWords.Items where ws.IsEqual(s) select s).ToList();
                        Invoke((Action)delegate
                        {
                            try
                            {
                                lstResults.Items.Clear();
                                foreach (string s in results)
                                    lstResults.Items.Add(s);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            }
                        });
                    }
                    catch (ThreadAbortException) { }
                    catch (Exception ex)
                    {
                        Invoke((Action)delegate
                        {
                            MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        });
                        Waiting();
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
                Name = "Recognizer"
            }).Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        void pbDraw_MouseLeave(object sender, EventArgs e)
        {
            _draw = false;
        }

        void pbDraw_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_draw)
                    _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
            }
            finally
            {
                pbDraw.Refresh();
            }
        }

        void FrmExample_Shown(object sender, EventArgs e)
        {
            pbDraw.Image = _frontBtm;
            btnClear_Click(null, null);
            WordsLoad();
        }

        void btnClear_Click(object sender, EventArgs e)
        {
            try
            {
                _grFront.Clear(Color.White);
            }
            catch
            {
                //ignored
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
                               Thread.Sleep(300);
                               break;
                           case 1:
                               Invoke((Action)delegate
                              {
                                  btnRecognize.Text = StrRecognize1;
                              });
                               Thread.Sleep(300);
                               break;
                           case 2:
                               Invoke((Action)delegate
                              {
                                  btnRecognize.Text = StrRecognize2;
                              });
                               Thread.Sleep(300);
                               break;
                           case 3:
                               Invoke((Action)delegate
                              {
                                  btnRecognize.Text = StrRecognize3;
                              });
                               k = -1;
                               Thread.Sleep(300);
                               break;
                           default:
                               k = 0;
                               break;
                       }
               }
               catch (ThreadAbortException) { }
               catch (Exception ex)
               {
                   Invoke((Action)delegate
                   {
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