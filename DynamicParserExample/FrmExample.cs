using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DynamicParser;
using Processor = DynamicParser.Processor;

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
        readonly Bitmap _btmFront;
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f);
        Thread _waitThread, _workThread;
        readonly Stopwatch _stopwatch = new Stopwatch();
        bool _draw;
        int _currentImage;

        public FrmExample()
        {
            InitializeComponent();
            _btmFront = new Bitmap(pbDraw.Width, pbDraw.Height);
            _grFront = Graphics.FromImage(_btmFront);
            _strRecog = btnRecognize.Text;
            btnNext_Click(null, null);
        }

        bool EnableButtons
        {
            set
            {
                InvokeFunction(() =>
                {
                    btnClear.Enabled = value;
                    btnWordAdd.Enabled = value;
                    btnWordRemove.Enabled = value;
                    pbDraw.Enabled = value;
                    txtWord.Enabled = value;
                    btnSaveImage.Enabled = value;
                    btnDeleteImage.Enabled = value;
                });
            }
        }

        void pbDraw_MouseDown(object sender, MouseEventArgs e)
        {
            _draw = true;
            _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
        }

        void btnWordAdd_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                if (string.IsNullOrWhiteSpace(txtWord.Text))
                    return;
                lstWords.Items.Insert(0, txtWord.Text);
                WordsSave();
                txtWord.Text = string.Empty;
            }, WordsLoad, null, true);
        }

        void btnWordRemove_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                int index = lstWords.SelectedIndex;
                if (index < 0)
                    return;
                lstWords.Items.RemoveAt(index);
                WordsSave();
            }, WordsLoad, null, true);
        }

        void WordsSave()
        {
            SafetyExecute(() => File.WriteAllLines(_strWordsPath, lstWords.Items.Cast<string>(), Encoding.UTF8), null, null, true);
        }

        void WordsLoad()
        {
            SafetyExecute(() =>
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
            }, null, null, true);
        }

        void pbDraw_MouseUp(object sender, MouseEventArgs e)
        {
            _draw = false;
        }

        void btnNext_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                List<ImageRect> lst = new List<ImageRect>(FileOperations.Images);
                if (lst.Count <= 0)
                {
                    pbBrowse.Image = new Bitmap(pbBrowse.Width, pbBrowse.Height);
                    return;
                }
                if (_currentImage >= lst.Count - 1)
                    _currentImage = 0;
                else
                    _currentImage++;
                if (_currentImage >= lst.Count || _currentImage < 0) return;
                ImageRect ir = lst[_currentImage];
                pbBrowse.Image = ir.Bitm;
                lblSymbolName.Text = ir.SymbolName;
            }, null, null, true);
        }

        void btnPrev_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                List<ImageRect> lst = new List<ImageRect>(FileOperations.Images);
                if (lst.Count <= 0)
                {
                    pbBrowse.Image = new Bitmap(pbBrowse.Width, pbBrowse.Height);
                    return;
                }
                if (_currentImage <= 0)
                    _currentImage = lst.Count - 1;
                else
                    _currentImage--;
                if (_currentImage >= lst.Count || _currentImage < 0) return;
                ImageRect ir = lst[_currentImage];
                pbBrowse.Image = ir.Bitm;
                lblSymbolName.Text = ir.SymbolName;
            }, null, null, true);
        }

        void btnDeleteImage_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                List<ImageRect> lst = new List<ImageRect>(FileOperations.Images);
                if (lst.Count <= 0)
                {
                    pbBrowse.Image = new Bitmap(pbBrowse.Width, pbBrowse.Height);
                    return;
                }
                if (_currentImage >= lst.Count || _currentImage < 0) return;
                File.Delete(lst[_currentImage].ImagePath);
                btnPrev_Click(null, null);
            }, null, null, true);
        }

        void btnSaveImage_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                using (FrmSymbol fs = new FrmSymbol())
                    fs.ShowDialog();
            }, null, null, true);
        }

        void tmrThread_Tick(object sender, EventArgs e)
        {
            if (_workThread?.IsAlive != true)
            {
                SafetyExecute(() => _waitThread?.Abort(), () =>
                {
                    _waitThread = null;
                    EnableButtons = true;
                    _workThread = null;
                    tmrThread.Enabled = false;
                });
                return;
            }
            if (_workThread?.IsAlive == true && _waitThread?.IsAlive != true)
                (_waitThread = new Thread((ThreadStart)delegate
                {
                    SafetyExecute(() =>
                    {
                        EnableButtons = false;
                        _stopwatch.Restart();
                        try
                        {
                            for (int k = 0; k < 4; k++)
                                switch (k)
                                {
                                    case 0:
                                        InvokeFunction(() =>
                                        {
                                            btnRecognize.Text = StrRecognize;
                                            lblElapsedTime.Text =
                                                $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{_stopwatch.Elapsed.Seconds:00}";
                                        });
                                        Thread.Sleep(100);
                                        break;
                                    case 1:
                                        InvokeFunction(() =>
                                        {
                                            btnRecognize.Text = StrRecognize1;
                                            lblElapsedTime.Text =
                                                $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{_stopwatch.Elapsed.Seconds:00}";
                                        });
                                        Thread.Sleep(100);
                                        break;
                                    case 2:
                                        InvokeFunction(() =>
                                        {
                                            btnRecognize.Text = StrRecognize2;
                                            lblElapsedTime.Text =
                                                $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{_stopwatch.Elapsed.Seconds:00}";
                                        });
                                        Thread.Sleep(100);
                                        break;
                                    case 3:
                                        InvokeFunction(() =>
                                        {
                                            btnRecognize.Text = StrRecognize3;
                                            lblElapsedTime.Text =
                                                $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{_stopwatch.Elapsed.Seconds:00}";
                                        });
                                        k = -1;
                                        Thread.Sleep(100);
                                        break;
                                    default:
                                        k = -1;
                                        break;
                                }
                        }
                        finally
                        {
                            _stopwatch.Stop();
                        }
                    }, () => InvokeFunction(() => btnRecognize.Text = _strRecog, null, true), null, true);
                })
                {
                    IsBackground = true,
                    Name = "WaitableTimer"
                }).Start();
        }

        void btnRecognize_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                if (_workThread?.IsAlive == true)
                {
                    SafetyExecute(() => _workThread.Abort(), () => _workThread = null);
                    return;
                }
                tmrThread.Enabled = true;
                (_workThread = new Thread((ThreadStart)delegate
                {
                    SafetyExecute(() =>
                    {
                        List<ImageRect> images = new List<ImageRect>(FileOperations.Images);
                        if (images.Count <= 0)
                        {
                            new Thread(() => InvokeFunction(() => MessageBox.Show(this,
                                  @"Никаких образов не найдено. Нарисуйте какой-нибудь образ, затем сохраните его.")))
                            {
                                IsBackground = true,
                                Name = "ImageNotFound"
                            }.Start();
                            return;
                        }
                        Processor processor = new Processor(_btmFront, "Main");
                        SearchResults sr = processor.GetEqual((from ir in images select new Processor(ir.ImageMap, ir.SymbolString)).ToArray());
                        string[] results =
                        (from string word in lstWords.Items
                         where !string.IsNullOrWhiteSpace(word)
                         where sr.FindRelation(word)
                         select word).ToArray();
                        InvokeFunction(() =>
                        {
                            lstResults.Items.Clear();
                            foreach (string s in results)
                                lstResults.Items.Add(s);
                        }, null, true);
                    }, () => InvokeFunction(() => pbDraw.Refresh(), null, true), null, true);
                })
                {
                    IsBackground = true,
                    Name = "Recognizer"
                }).Start();
            }, null, null, true);
        }

        void FrmExample_KeyUp(object sender, KeyEventArgs e)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (e.KeyCode)
            {
                case Keys.R:
                    btnRecognize_Click(null, null);
                    break;
                case Keys.Escape:
                    Application.Exit();
                    break;
            }
        }

        void txtWord_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                btnWordAdd_Click(null, null);
        }

        void lstWords_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                btnWordRemove_Click(null, null);
        }

        void pbDraw_MouseLeave(object sender, EventArgs e)
        {
            _draw = false;
        }

        void pbDraw_MouseMove(object sender, MouseEventArgs e)
        {
            SafetyExecute(() =>
            {
                if (_draw)
                    _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
            }, () => pbDraw.Refresh());
        }

        void FrmExample_Shown(object sender, EventArgs e)
        {
            pbDraw.Image = _btmFront;
            btnClear_Click(null, null);
            WordsLoad();
        }

        void btnClear_Click(object sender, EventArgs e)
        {
            SafetyExecute(() => _grFront.Clear(Color.White), () => pbDraw.Refresh());
        }

        void InvokeFunction(Action funcAction, Action catchAction = null, bool invokeErrorMessage = false)
        {
            if (funcAction == null)
                return;
            try
            {
                Invoke((Action)delegate
               {
                   try
                   {
                       funcAction();
                   }
                   catch (ThreadAbortException) { }
                   catch (Exception ex)
                   {
                       try
                       {
                           if (invokeErrorMessage)
                               MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                           catchAction?.Invoke();
                       }
                       catch (ThreadAbortException) { }
                       catch (Exception ex1)
                       {
                           if (invokeErrorMessage)
                               MessageBox.Show(this, ex1.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                       }
                   }
               });
            }
            catch
            {
                //ignored
            }
        }

        void SafetyExecute(Action funcAction, Action finallyAction = null, Action catchAction = null, bool invokeErrorMessage = false)
        {
            try
            {
                funcAction?.Invoke();
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                try
                {
                    catchAction?.Invoke();
                }
                catch (ThreadAbortException) { }
                catch
                {
                    if (invokeErrorMessage)
                        InvokeFunction(() => MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation));
                }
            }
            finally
            {
                try
                {
                    finallyAction?.Invoke();
                }
                catch (ThreadAbortException) { }
                catch (Exception ex)
                {
                    if (invokeErrorMessage)
                        InvokeFunction(() => MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation));
                }
            }
        }
    }
}