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
            StrWordsFile = "Words",
            ImagesNoExists = @"Образы отсутствуют. Для их добавления и распознавания необходимо создать искомые образы, нажав кнопку 'Создать образ', затем добавить искомое слово, которое так или иначе можно составить из названий искомых образов. Затем необходимо нарисовать его в поле исходного изображения. Далее нажать кнопку 'Распознать'.";

        readonly string _strRecog, _strWordsPath = Path.Combine(Application.StartupPath, $"{StrWordsFile}.txt"), _unknownSymbolName,
            _strGrpResults, _strGrpWords;
        readonly Graphics _grFront;
        readonly Bitmap _btmFront;
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f);
        Thread _waitThread, _workThread;
        readonly Stopwatch _stopwatch = new Stopwatch();
        bool _draw;
        int _currentImage, _selectedIndex = -1;

        public FrmExample()
        {
            try
            {
                InitializeComponent();
                _btmFront = new Bitmap(pbDraw.Width, pbDraw.Height);
                _grFront = Graphics.FromImage(_btmFront);
                _strRecog = btnRecognize.Text;
                _unknownSymbolName = lblSymbolName.Text;
                _strGrpResults = grpResults.Text;
                _strGrpWords = grpWords.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"{ex.Message}{Environment.NewLine}Программа будет завершена.", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        bool EnableButtons
        {
            set
            {
                InvokeFunction(() =>
                {
                    btnClear.Enabled = value;
                    btnWordAdd.Enabled = value;
                    pbDraw.Enabled = value;
                    txtWord.Enabled = value;
                    btnImageSave.Enabled = value;
                    btnImageDelete.Enabled = value;
                    tmrImagesCount.Enabled = value;
                    btnWordRemove.Enabled = value;
                });
            }
        }

        void pbDraw_MouseDown(object sender, MouseEventArgs e)
        {
            _draw = true;
            _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
        }

        bool WordExist(string word)
        {
            return lstWords.Items.Cast<string>().Any(s => string.Compare(s, word, StringComparison.OrdinalIgnoreCase) == 0);
        }

        void btnWordAdd_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                if (string.IsNullOrWhiteSpace(txtWord.Text) || WordExist(txtWord.Text))
                {
                    txtWord.Text = string.Empty;
                    return;
                }
                lstWords.Items.Insert(0, txtWord.Text);
                WordsSave();
                txtWord.Text = string.Empty;
            }, WordsLoad);
        }

        void btnWordRemove_Click(object sender, EventArgs e)
        {
            if (!btnWordRemove.Enabled)
                return;
            SafetyExecute(() =>
            {
                int index = lstWords.SelectedIndex;
                if (index < 0)
                    return;
                _selectedIndex = index;
                lstWords.Items.RemoveAt(index);
                WordsSave();
            }, WordsLoad);
        }

        void WordsSave()
        {
            SafetyExecute(() => File.WriteAllLines(_strWordsPath, lstWords.Items.Cast<string>(), Encoding.UTF8));
        }

        void WordsLoad()
        {
            SafetyExecute(() =>
            {
                lstWords.Items.Clear();
                if (!File.Exists(_strWordsPath))
                    return;
                foreach (string s in File.ReadAllLines(_strWordsPath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(s) || WordExist(s))
                        continue;
                    string str = s;
                    if (s.Length > txtWord.MaxLength)
                        str = s.Substring(0, txtWord.MaxLength);
                    lstWords.Items.Add(str);
                }
                if (_selectedIndex < 0 || lstWords.Items.Count <= 0) return;
                lstWords.SetSelected(_selectedIndex >= lstWords.Items.Count ? lstWords.Items.Count - 1 : _selectedIndex, true);
            }, () =>
            {
                _selectedIndex = -1;
                btnWordRemove.Enabled = lstWords.Items.Count > 0;
                grpWords.Text = $@"{_strGrpWords} ({lstWords.Items.Count})";
                if (lstWords.Items.Count <= 0)
                    File.Delete(_strWordsPath);
            });
        }

        /// <summary>
        /// Вызывается при отпускании клавиши мыши над полем создания исходного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void pbDraw_MouseUp(object sender, MouseEventArgs e)
        {
            _draw = false;
        }

        /// <summary>
        /// Возвращает окно просмотра образов в исходное состояние.
        /// </summary>
        void SymbolBrowseClear()
        {
            lblSymbolName.Text = _unknownSymbolName;
            pbBrowse.Image = new Bitmap(pbBrowse.Width, pbBrowse.Height);
        }

        /// <summary>
        /// Вызывается по нажатию кнопки "Следующий" в искомых образах букв.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void btnNext_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                List<ImageRect> lst = new List<ImageRect>(ImageRect.Images);
                if (lst.Count <= 0)
                {
                    SymbolBrowseClear();
                    MessageBox.Show(this, ImagesNoExists, @"Уведомление", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            });
        }

        /// <summary>
        /// Вызывается по нажатию кнопки "Предыдущий" в искомых образах букв.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void btnPrev_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                List<ImageRect> lst = new List<ImageRect>(ImageRect.Images);
                if (lst.Count <= 0)
                {
                    SymbolBrowseClear();
                    MessageBox.Show(this, ImagesNoExists, @"Уведомление", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            });
        }

        /// <summary>
        /// Вызывается по нажатию кнопки "Удалить".
        /// Удаляет выбранное изображение.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void btnImageDelete_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                List<ImageRect> lst = new List<ImageRect>(ImageRect.Images);
                if (lst.Count <= 0)
                {
                    SymbolBrowseClear();
                    return;
                }
                if (_currentImage >= lst.Count || _currentImage < 0) return;
                File.Delete(lst[_currentImage].ImagePath);
                btnPrev_Click(null, null);
            }, RefreshImagesCount);
        }

        /// <summary>
        /// Вызывается по нажатию кнопки "Создать образ".
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void btnImageSave_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                using (FrmSymbol fs = new FrmSymbol())
                {
                    if (fs.ShowDialog() != DialogResult.OK) return;
                    pbBrowse.Image = fs.LastImage.Bitm;
                    lblSymbolName.Text = fs.LastImage.SymbolName;
                }
            }, RefreshImagesCount);
        }

        /// <summary>
        /// Выполняет подсчёт количества изображений для поиска.
        /// Обновляет состояния кнопок, связанных с изображениями.
        /// </summary>
        void RefreshImagesCount()
        {
            InvokeFunction(() =>
            {
                long count = ImageRect.Images.LongCount();
                txtImagesCount.Text = count.ToString();
                if (count <= 0)
                {
                    SymbolBrowseClear();
                    btnImageDelete.Enabled = false;
                    btnNext.Enabled = false;
                    btnPrev.Enabled = false;
                    return;
                }
                if (!btnImageDelete.Enabled || !btnNext.Enabled || !btnPrev.Enabled)
                    btnNext_Click(null, null);
                btnImageDelete.Enabled = true;
                btnNext.Enabled = true;
                btnPrev.Enabled = true;
            });
        }

        /// <summary>
        /// Запускает или останавливает таймер, выполняющий замер затраченного времени.
        /// </summary>
        /// <param name="enable">Значение true включает таймер, false - выключает.</param>
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
                }, () => InvokeFunction(() => btnRecognize.Text = _strRecog));
            })
            {
                IsBackground = true,
                Name = nameof(WaitableTimer)
            }).Start();
        }

        /// <summary>
        /// Возвращает значение true в случае, если пользователь нарисовал что-либо в окне создания исходного изображения.
        /// В противном случае возвращает значение false.
        /// </summary>
        bool IsPainting
        {
            get
            {
                int white = Color.White.ToArgb();
                for (int y = 0; y < _btmFront.Height; y++)
                    for (int x = 0; x < _btmFront.Width; x++)
                        if (_btmFront.GetPixel(x, y).ToArgb() != white)
                            return true;
                return false;
            }
        }

        /// <summary>
        /// Вызывается по нажатию кнопки "Распознать".
        /// Распознаёт изображение и выводит результат на форму.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
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
                            MessageInOtherThread(@"Образы отсутствуют. Нарисуйте какой-нибудь образ, затем сохраните его.");
                            return;
                        }
                        if (lstWords.Items.Count <= 0)
                        {
                            MessageInOtherThread(@"Слова отсутствуют. Добавьте какое-нибудь слово, которое можно составить из одного или нескольких образов.");
                            return;
                        }
                        if (!IsPainting)
                        {
                            MessageInOtherThread(@"Необходимо нарисовать какой-нибудь рисунок на рабочей поверхности.");
                            return;
                        }
                        ConcurrentBag<string> results = new Processor(_btmFront, "Main").
                            GetEqual((from ir in images select new Processor(ir.ImageMap, ir.SymbolString)).ToArray()).FindRelation(lstWords.Items);
                        InvokeFunction(() => lstResults.Items.Clear());
                        if ((results?.Count ?? 0) <= 0)
                        {
                            MessageInOtherThread(@"Распознанные образы отсутствуют. Отсутствуют слова или образы.");
                            return;
                        }
                        InvokeFunction(() =>
                        {
                            int count = results?.Count ?? 0;
                            grpResults.Text = $@"{_strGrpResults} ({count})";
                            if (results == null) return;
                            foreach (string s in results)
                                lstResults.Items.Add(s);
                        });
                    }, () => WaitableTimer(false));
                })
                {
                    IsBackground = true,
                    Name = "Recognizer"
                }).Start();
            });
        }

        /// <summary>
        /// Осуществляет выход из программы по нажатию клавиши Escape.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_KeyUp(object sender, KeyEventArgs e)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Application.Exit();
                    break;
            }
        }

        /// <summary>
        /// Осуществляет ввод искомого слова по нажатии клавиши Enter.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void txtWord_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                btnWordAdd_Click(null, null);
        }

        /// <summary>
        /// Претотвращает сигналы недопустимого ввода в текстовое поле ввода искомого слова.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void txtWord_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((Keys)e.KeyChar == Keys.Enter || (Keys)e.KeyChar == Keys.Tab || (Keys)e.KeyChar == Keys.Pause ||
                (Keys)e.KeyChar == Keys.XButton1 || e.KeyChar == 15)
                e.Handled = true;
        }

        /// <summary>
        /// Производит удаление слова по нажатию клавиши Delete.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void lstWords_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                btnWordRemove_Click(null, null);
        }

        /// <summary>
        /// Отменяет отрисовку изображения для распознавания в случае ухода указателя мыши с поля рисования.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void pbDraw_MouseLeave(object sender, EventArgs e)
        {
            _draw = false;
        }

        /// <summary>
        /// Обновляет количество изображений для поиска.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void tmrImagesCount_Tick(object sender, EventArgs e)
        {
            RefreshImagesCount();
        }

        /// <summary>
        /// Отвечает за отрисовку рисунка, создаваемого пользователем.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void pbDraw_MouseMove(object sender, MouseEventArgs e)
        {
            SafetyExecute(() =>
            {
                if (_draw)
                    _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
            }, () => pbDraw.Refresh());
        }

        /// <summary>
        /// Вызывается во время первого отображения формы.
        /// Производит инициализацию.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_Shown(object sender, EventArgs e)
        {
            pbDraw.Image = _btmFront;
            btnClear_Click(null, null);
            btnNext_Click(null, null);
            RefreshImagesCount();
            WordsLoad();
        }

        /// <summary>
        /// Очищает поле рисования исходного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void btnClear_Click(object sender, EventArgs e)
        {
            SafetyExecute(() => _grFront.Clear(Color.White), () => pbDraw.Refresh());
        }

        /// <summary>
        /// Выполняет функцию с помощью метода Invoke.
        /// </summary>
        /// <param name="funcAction">Функция, которую необходимо выполнить.</param>
        /// <param name="catchAction">Функция, которая должна быть выполнена в блоке catch.</param>
        void InvokeFunction(Action funcAction, Action catchAction = null)
        {
            if (funcAction == null)
                return;
            try
            {
                Action act = delegate
                {
                    try
                    {
                        funcAction();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            catchAction?.Invoke();
                        }
                        catch (Exception ex1)
                        {
                            MessageBox.Show(this, ex1.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                    }
                };
                if (InvokeRequired)
                    Invoke(act);
                else
                    act();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Представляет обёртку для выполнения функций с применением блоков try-catch, а также выдачей сообщений обо всех ошибках.
        /// </summary>
        /// <param name="funcAction">Функция, которая должна быть выполнена.</param>
        /// <param name="finallyAction">Функция, которая должна быть выполнена в блоке finally.</param>
        /// <param name="catchAction">Функция, которая должна быть выполнена в блоке catch.</param>
        void SafetyExecute(Action funcAction, Action finallyAction = null, Action catchAction = null)
        {
            try
            {
                funcAction?.Invoke();
            }
            catch (Exception ex)
            {
                try
                {
                    InvokeFunction(() => MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation));
                    catchAction?.Invoke();
                }
                catch
                {
                    InvokeFunction(() => MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation));
                }
            }
            finally
            {
                try
                {
                    finallyAction?.Invoke();
                }
                catch (Exception ex)
                {
                    InvokeFunction(() => MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation));
                }
            }
        }

        /// <summary>
        /// Отображает сообщение с указанным текстом в другом потоке.
        /// </summary>
        /// <param name="message">Текст отображаемого сообщения.</param>
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
            });
        }
    }
}