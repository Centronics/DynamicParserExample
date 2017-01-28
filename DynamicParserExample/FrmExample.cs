using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
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
                List<ImageRect> lst = new List<ImageRect>(ImageRect.Images);
                if (lst.Count <= 0)
                {
                    pbBrowse.Image = new Bitmap(pbBrowse.Width, pbBrowse.Height);
                    MessageBox.Show(this, @"Образы отсутствуют. Для их добавления и распознавания необходимо создать искомые образы, нажав кнопку 'Создать образ', затем добавить искомое слово, которое так или иначе можно составить из названий искомых образов. Затем необходимо нарисовать его в поле исходного изображения. Далее нажать кнопку 'Распознать'.", @"Уведомление", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            }, RefreshImagesCount, null, true);
        }

        void btnPrev_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                List<ImageRect> lst = new List<ImageRect>(ImageRect.Images);
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
            }, RefreshImagesCount, null, true);
        }

        void btnDeleteImage_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                List<ImageRect> lst = new List<ImageRect>(ImageRect.Images);
                if (lst.Count <= 0)
                {
                    pbBrowse.Image = new Bitmap(pbBrowse.Width, pbBrowse.Height);
                    return;
                }
                if (_currentImage >= lst.Count || _currentImage < 0) return;
                File.Delete(lst[_currentImage].ImagePath);
                btnPrev_Click(null, null);
            }, RefreshImagesCount, null, true);
        }

        void btnSaveImage_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                using (FrmSymbol fs = new FrmSymbol())
                {
                    if (fs.ShowDialog() != DialogResult.OK) return;
                    pbBrowse.Image = fs.LastImage.Bitm;
                    lblSymbolName.Text = fs.LastImage.SymbolName;
                }
            }, RefreshImagesCount, null, true);
        }

        void RefreshImagesCount()
        {
            InvokeFunction(() => txtImagesCount.Text = ImageRect.Images.LongCount().ToString(), null, true);
        }

        void WaitableTimer(bool enable)
        {
            if (!enable && _waitThread?.IsAlive == true)
            {
                SafetyExecute(() => _waitThread?.Abort(), () =>
                {
                    _waitThread = null;
                    EnableButtons = true;
                });
                return;
            }
            if (!enable)
                return;
            (_waitThread = new Thread((ThreadStart)delegate
            {
                SafetyExecute(() =>
                {
                    _stopwatch.Restart();
                    try
                    {
                        #region Switcher
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
                        #endregion
                    }
                    finally
                    {
                        _stopwatch.Stop();
                    }
                }, () => InvokeFunction(() => btnRecognize.Text = _strRecog, null, true), null, true);
            })
            {
                IsBackground = true,
                Name = nameof(WaitableTimer)
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
                EnableButtons = false;
                (_workThread = new Thread((ThreadStart)delegate
                {
                    SafetyExecute(() =>
                    {
                        WaitableTimer(true);
                        List<ImageRect> images = new List<ImageRect>(ImageRect.Images);
                        if (images.Count <= 0)
                        {
                            MessageInOtherThread(@"Никаких образов не найдено. Нарисуйте какой-нибудь образ, затем сохраните его.");
                            return;
                        }
                        ConcurrentBag<string> results = new Processor(_btmFront, "Main").
                            GetEqual((from ir in images select new Processor(ir.ImageMap, ir.SymbolString)).ToArray()).FindRelation(lstWords.Items);
                        if ((results?.Count ?? 0) <= 0)
                        {
                            MessageInOtherThread(@"Никаких образов не распознано.");
                            return;
                        }
                        InvokeFunction(() =>
                        {
                            lstResults.Items.Clear();
                            if (results == null) return;
                            foreach (string s in results)
                                lstResults.Items.Add(s);
                        }, null, true);
                    }, () => WaitableTimer(false), null, true);
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

        void tmrImagesCount_Tick(object sender, EventArgs e)
        {
            RefreshImagesCount();
        }

        void pbDraw_MouseMove(object sender, MouseEventArgs e)
        {
            SafetyExecute(() =>
            {
                if (_draw)
                    _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
            }, () => pbDraw.Refresh(), null, true);
        }

        void FrmExample_Shown(object sender, EventArgs e)
        {
            pbDraw.Image = _btmFront;
            btnClear_Click(null, null);
            btnNext_Click(null, null);
            WordsLoad();
        }

        void btnClear_Click(object sender, EventArgs e)
        {
            SafetyExecute(() => _grFront.Clear(Color.White), () => pbDraw.Refresh(), null, true);
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
                    if (invokeErrorMessage)
                        InvokeFunction(() => MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation));
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

        void MessageInOtherThread(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            SafetyExecute(() =>
            {
                new Thread((ThreadStart)delegate
               {
                   InvokeFunction(() => MessageBox.Show(this, message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation));
               })
                {
                    IsBackground = true,
                    Name = @"Message"
                }.Start();
            }, null, null, true);
        }
    }
}