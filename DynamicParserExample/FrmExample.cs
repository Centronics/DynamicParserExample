﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DynamicParser;

namespace DynamicParserExample
{
    /// <summary>
    ///     Класс основной формы приложения.
    /// </summary>
    public partial class FrmExample : Form
    {
        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrRecognize = "Ждите   ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrRecognize1 = "Ждите.  ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrRecognize2 = "Ждите.. ";

        /// <summary>
        ///     Надпись на кнопке "Распознать".
        /// </summary>
        const string StrRecognize3 = "Ждите...";

        /// <summary>
        ///     Имя файла с искомыми словами.
        /// </summary>
        const string StrWordsFile = "Words";

        /// <summary>
        ///     Текст ошибки в случае, если отсутствуют образы для поиска (распознавания).
        /// </summary>
        const string ImagesNoExists =
            @"Образы отсутствуют. Для их добавления и распознавания необходимо создать искомые образы, нажав кнопку 'Создать образ', затем добавить искомое слово, которое так или иначе можно составить из названий искомых образов. Затем необходимо нарисовать его в поле исходного изображения. Далее нажать кнопку 'Распознать'.";

        /// <summary>
        ///     Задаёт цвет и ширину для рисования в окне создания распознаваемого изображения.
        /// </summary>
        readonly Pen _blackPen = new Pen(Color.Black, 2.0f);

        /// <summary>
        ///     Таймер для измерения времени, затраченного на распознавание.
        /// </summary>
        readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        ///     Хранит значение свойства <see cref="GroupBox.Text" /> объекта <see cref="grpResults" />.
        /// </summary>
        readonly string _strGrpResults;

        /// <summary>
        ///     Хранит значение свойства <see cref="GroupBox.Text" /> объекта <see cref="grpWords" />.
        /// </summary>
        readonly string _strGrpWords;

        /// <summary>
        ///     Текст кнопки "Распознать". Сохраняет исходное значение свойства <see cref="Button.Text" /> кнопки
        ///     <see cref="btnRecognize" />.
        /// </summary>
        readonly string _strRecog;

        /// <summary>
        ///     Строка пути сохранения/загрузки файла, содержащего искомые слова.
        /// </summary>
        readonly string _strWordsPath = Path.Combine(Application.StartupPath, $"{StrWordsFile}.txt");

        /// <summary>
        ///     Содержит изначальное значение поля "Название" искомого образа буквы.
        /// </summary>
        readonly string _unknownSymbolName;

        /// <summary>
        ///     Изображение, которое выводится в окне создания распознаваемого изображения.
        /// </summary>
        Bitmap _btmFront;

        /// <summary>
        ///     Индекс образа для распознавания, рассматриваемый в данный момент.
        /// </summary>
        int _currentImage;

        /// <summary>
        ///     Определяет, разрешён вывод создаваемой пользователем линии на экран или нет.
        ///     Значение true - вывод разрешён, в противном случае - false.
        /// </summary>
        bool _draw;

        /// <summary>
        ///     Поверхность рисования в окне создания распознаваемого изображения.
        /// </summary>
        Graphics _grFront;

        /// <summary>
        ///     Отражает индекс выделенного в данный момент искомого слова.
        /// </summary>
        int _selectedIndex = -1;

        /// <summary>
        ///     Поток, отвечающий за выполнение процедуры распознавания.
        /// </summary>
        Thread _workThread;

        /// <summary>
        ///     Ширина образа для распознавания.
        /// </summary>
        public static int ImageWidth { get; private set; }

        /// <summary>
        ///     Высота образа для распознавания.
        /// </summary>
        public static int ImageHeight { get; private set; }

        /// <summary>
        ///     Конструктор основной формы приложения.
        /// </summary>
        public FrmExample()
        {
            try
            {
                InitializeComponent();
                Initialize();
                _strRecog = btnRecognize.Text;
                _unknownSymbolName = lblSymbolName.Text;
                _strGrpResults = grpResults.Text;
                _strGrpWords = grpWords.Text;
                ImageWidth = pbBrowse.Width;
                ImageHeight = pbBrowse.Height;
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"{ex.Message}{Environment.NewLine}Программа будет завершена.", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        ///     Отключает или включает доступность кнопок на время выполнения операции.
        /// </summary>
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
                    btnSaveImage.Enabled = value;
                    btnLoadImage.Enabled = value;
                });
            }
        }

        /// <summary>
        ///     Возвращает значение true в случае, если пользователь нарисовал что-либо в окне создания исходного изображения.
        ///     В противном случае возвращает значение false.
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
        ///     Предназначена для инициализации структур, отвечающих за вывод создаваемого изображения на экран.
        /// </summary>
        /// <param name="btmPath">Путь к файлу исходного изображения.</param>
        void Initialize(string btmPath = null)
        {
            if (string.IsNullOrEmpty(btmPath))
            {
                _btmFront = new Bitmap(pbDraw.Width, pbDraw.Height);
            }
            else
            {
                Bitmap btm;
                using (FileStream fs = new FileStream(btmPath, FileMode.Open, FileAccess.Read))
                    btm = new Bitmap(fs);
                ImageFormat iformat = btm.RawFormat;
                if (!iformat.Equals(ImageFormat.Bmp))
                {
                    MessageBox.Show(this,
                        $@"Загружаемое изображение не подходит по формату: {iformat}; необходимо: {ImageFormat.Bmp}",
                        @"Ошибка");
                    btm.Dispose();
                    return;
                }
                if (btm.Width != pbDraw.Width)
                {
                    MessageBox.Show(this,
                        $@"Загружаемое изображение не подходит по ширине: {btm.Width}; необходимо: {pbDraw.Width}",
                        @"Ошибка");
                    btm.Dispose();
                    return;
                }
                if (btm.Height != pbDraw.Height)
                {
                    MessageBox.Show(this,
                        $@"Загружаемое изображение не подходит по высоте: {btm.Height}; необходимо: {pbDraw.Height}",
                        @"Ошибка");
                    btm.Dispose();
                    return;
                }
                _btmFront = btm;
            }
            _grFront?.Dispose();
            _grFront = Graphics.FromImage(_btmFront);
            pbDraw.Image = _btmFront;
        }

        /// <summary>
        ///     Вызывается, когда пользователь начинает рисовать исходное изображение.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void pbDraw_MouseDown(object sender, MouseEventArgs e)
        {
            _draw = true;
            _grFront.DrawRectangle(_blackPen, new Rectangle(e.X, e.Y, 1, 1));
        }

        /// <summary>
        ///     Получает значение, означающее, существует заданное слово в коллекции или нет.
        ///     В случае, если оно существует, возвращается значение true, в противном случае - false.
        /// </summary>
        /// <param name="word">Проверяемое слово.</param>
        /// <returns>В случае, если указанное слово существует, возвращается значение true, в противном случае - false.</returns>
        bool WordExist(string word)
        {
            return
                lstWords.Items.Cast<string>().Any(s => string.Compare(s, word, StringComparison.OrdinalIgnoreCase) == 0);
        }

        /// <summary>
        ///     Добавляет указанное искомое слово.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
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

        /// <summary>
        ///     Удаляет выделенное искомое слово.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
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

        /// <summary>
        ///     Сохраняет искомые слова в файл, имя которого содержится в константе <see cref="StrWordsFile" /> с расширением txt.
        ///     Кодировка: UTF-8.
        /// </summary>
        void WordsSave()
        {
            SafetyExecute(() => File.WriteAllLines(_strWordsPath, lstWords.Items.Cast<string>(), Encoding.UTF8));
        }

        /// <summary>
        ///     Загружает искомые слова из файла, имя которого содержится в константе <see cref="StrWordsFile" /> с расширением txt.
        ///     Кодировка: UTF-8.
        /// </summary>
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
                lstWords.SetSelected(
                    _selectedIndex >= lstWords.Items.Count ? lstWords.Items.Count - 1 : _selectedIndex, true);
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
        ///     Вызывается при отпускании клавиши мыши над полем создания исходного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void pbDraw_MouseUp(object sender, MouseEventArgs e)
        {
            _draw = false;
        }

        /// <summary>
        ///     Возвращает окно просмотра образов в исходное состояние.
        /// </summary>
        void SymbolBrowseClear()
        {
            lblSymbolName.Text = _unknownSymbolName;
            pbBrowse.Image = new Bitmap(pbBrowse.Width, pbBrowse.Height);
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Следующий" в искомых образах букв.
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
                    MessageBox.Show(this, ImagesNoExists, @"Уведомление", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
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
            }, () => tmrImagesCount.Enabled = true);
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Предыдущий" в искомых образах букв.
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
                    MessageBox.Show(this, ImagesNoExists, @"Уведомление", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
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
            }, () => tmrImagesCount.Enabled = true);
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Удалить".
        ///     Удаляет выбранное изображение.
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
            }, () =>
            {
                RefreshImagesCount();
                tmrImagesCount.Enabled = true;
            });
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Создать образ".
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
            }, () =>
            {
                RefreshImagesCount();
                tmrImagesCount.Enabled = true;
            });
        }

        /// <summary>
        ///     Выполняет подсчёт количества изображений для поиска.
        ///     Обновляет состояния кнопок, связанных с изображениями.
        /// </summary>
        void RefreshImagesCount()
        {
            InvokeFunction(() =>
            {
                try
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
                }
                catch
                {
                    tmrImagesCount.Enabled = false;
                    throw;
                }
            });
        }

        /// <summary>
        ///     Запускает или останавливает таймер, выполняющий замер времени, затраченного на распознавание.
        /// </summary>
        void WaitableTimer()
        {
            new Thread((ThreadStart) delegate
            {
                SafetyExecute(() =>
                {
                    _stopwatch.Restart();
                    try
                    {
                        #region Switcher

                        for (int k = 0; k < 4; k++)
                        {
                            switch (k)
                            {
                                case 0:
                                    InvokeFunction(() =>
                                    {
                                        btnRecognize.Text = StrRecognize;
                                        lblElapsedTime.Text =
                                            $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{_stopwatch
                                                .Elapsed.Seconds:00}";
                                    });
                                    Thread.Sleep(100);
                                    break;
                                case 1:
                                    InvokeFunction(() =>
                                    {
                                        btnRecognize.Text = StrRecognize1;
                                        lblElapsedTime.Text =
                                            $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{_stopwatch
                                                .Elapsed.Seconds:00}";
                                    });
                                    Thread.Sleep(100);
                                    break;
                                case 2:
                                    InvokeFunction(() =>
                                    {
                                        btnRecognize.Text = StrRecognize2;
                                        lblElapsedTime.Text =
                                            $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{_stopwatch
                                                .Elapsed.Seconds:00}";
                                    });
                                    Thread.Sleep(100);
                                    break;
                                case 3:
                                    InvokeFunction(() =>
                                    {
                                        btnRecognize.Text = StrRecognize3;
                                        lblElapsedTime.Text =
                                            $@"{_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{_stopwatch
                                                .Elapsed.Seconds:00}";
                                    });
                                    k = -1;
                                    Thread.Sleep(100);
                                    break;
                                default:
                                    k = -1;
                                    break;
                            }
                            if (_workThread?.IsAlive != true)
                                return;
                        }

                        #endregion
                    }
                    finally
                    {
                        _stopwatch.Stop();
                        EnableButtons = true;
                    }
                }, () => InvokeFunction(() => btnRecognize.Text = _strRecog));
            })
            {
                IsBackground = true,
                Name = nameof(WaitableTimer)
            }.Start();
        }

        /// <summary>
        ///     Вызывается по нажатию кнопки "Распознать".
        ///     Распознаёт изображение и выводит результат на форму.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void btnRecognize_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                if (_workThread?.IsAlive == true)
                    return;
                EnableButtons = false;
                (_workThread = new Thread((ThreadStart) delegate
                {
                    SafetyExecute(() =>
                    {
                        WaitableTimer();
                        List<ImageRect> images = new List<ImageRect>(ImageRect.Images);
                        if (images.Count <= 0)
                        {
                            MessageInOtherThread(
                                @"Образы отсутствуют. Нарисуйте какой-нибудь образ, затем сохраните его.");
                            return;
                        }
                        if (lstWords.Items.Count <= 0)
                        {
                            MessageInOtherThread(
                                @"Слова отсутствуют. Добавьте какое-нибудь слово, которое можно составить из одного или нескольких образов.");
                            return;
                        }
                        if (!IsPainting)
                        {
                            MessageInOtherThread(@"Необходимо нарисовать какой-нибудь рисунок на рабочей поверхности.");
                            return;
                        }
                        ConcurrentBag<string> results = new Processor(_btmFront, "Main").
                            GetEqual((from ir in images select new Processor(ir.ImageMap, ir.SymbolString)).ToArray())
                            .FindRelation(lstWords.Items);
                        InvokeFunction(() => lstResults.Items.Clear());
                        if ((results?.Count ?? 0) <= 0)
                        {
                            InvokeFunction(() => grpResults.Text = $@"{_strGrpResults} (0)");
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
                    });
                })
                {
                    IsBackground = true,
                    Name = "Recognizer"
                }).Start();
            });
        }

        /// <summary>
        ///     Осуществляет выход из программы по нажатию клавиши Escape.
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
        ///     Осуществляет ввод искомого слова по нажатии клавиши Enter.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void txtWord_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                btnWordAdd_Click(null, null);
        }

        /// <summary>
        ///     Претотвращает сигналы недопустимого ввода в текстовое поле ввода искомого слова.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void txtWord_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((Keys) e.KeyChar == Keys.Enter || (Keys) e.KeyChar == Keys.Tab || (Keys) e.KeyChar == Keys.Pause ||
                (Keys) e.KeyChar == Keys.XButton1 || e.KeyChar == 15)
                e.Handled = true;
        }

        /// <summary>
        ///     Производит удаление слова по нажатию клавиши Delete.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void lstWords_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                btnWordRemove_Click(null, null);
        }

        /// <summary>
        ///     Отменяет отрисовку изображения для распознавания в случае ухода указателя мыши с поля рисования.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void pbDraw_MouseLeave(object sender, EventArgs e)
        {
            _draw = false;
        }

        /// <summary>
        ///     Обновляет количество изображений для поиска.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void tmrImagesCount_Tick(object sender, EventArgs e)
        {
            RefreshImagesCount();
        }

        /// <summary>
        ///     Отвечает за отрисовку рисунка, создаваемого пользователем.
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
        ///     Вызывается во время первого отображения формы.
        ///     Производит инициализацию.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void FrmExample_Shown(object sender, EventArgs e)
        {
            btnClear_Click(null, null);
            btnNext_Click(null, null);
            RefreshImagesCount();
            WordsLoad();
        }

        /// <summary>
        ///     Очищает поле рисования исходного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void btnClear_Click(object sender, EventArgs e)
        {
            SafetyExecute(()=> _grFront.Clear(Color.White), () => pbDraw.Refresh());
        }

        /// <summary>
        ///     Обрабатывает событие нажатие кнопки сохранения созданного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void btnSaveImage_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                if (dlgSaveImage.ShowDialog(this) != DialogResult.OK) return;
                using (FileStream fs = new FileStream(dlgSaveImage.FileName, FileMode.Create, FileAccess.Write))
                    _btmFront.Save(fs, ImageFormat.Bmp);
            });
        }

        /// <summary>
        ///     Обрабатывает событие нажатие кнопки загрузки созданного изображения.
        /// </summary>
        /// <param name="sender">Вызывающий объект.</param>
        /// <param name="e">Данные о событии.</param>
        void btnLoadImage_Click(object sender, EventArgs e)
        {
            SafetyExecute(() =>
            {
                if (dlgOpenImage.ShowDialog(this) != DialogResult.OK) return;
                Initialize(dlgOpenImage.FileName);
            });
        }

        /// <summary>
        ///     Выполняет метод с помощью метода Invoke.
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
                            MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                            catchAction?.Invoke();
                        }
                        catch (Exception ex1)
                        {
                            MessageBox.Show(this, ex1.Message, @"Ошибка", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
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
        ///     Представляет обёртку для выполнения функций с применением блоков try-catch, а также выдачей сообщений обо всех
        ///     ошибках.
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
                    InvokeFunction(
                        () =>
                            MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation));
                    catchAction?.Invoke();
                }
                catch
                {
                    InvokeFunction(
                        () =>
                            MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation));
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
                    InvokeFunction(
                        () =>
                            MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation));
                }
            }
        }

        /// <summary>
        ///     Отображает сообщение с указанным текстом в другом потоке.
        /// </summary>
        /// <param name="message">Текст отображаемого сообщения.</param>
        void MessageInOtherThread(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            SafetyExecute(() =>
            {
                new Thread(
                    (ThreadStart)
                    delegate
                    {
                        InvokeFunction(
                            () =>
                                MessageBox.Show(this, message, @"Ошибка", MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation));
                    })
                {
                    IsBackground = true,
                    Name = @"Message"
                }.Start();
            });
        }
    }
}