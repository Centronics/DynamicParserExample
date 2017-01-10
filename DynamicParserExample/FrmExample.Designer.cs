namespace DynamicParserExample
{
    partial class FrmExample
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.pbDraw = new System.Windows.Forms.PictureBox();
            this.btnRecognize = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.lstResults = new System.Windows.Forms.ListBox();
            this.lstWords = new System.Windows.Forms.ListBox();
            this.btnWordAdd = new System.Windows.Forms.Button();
            this.btnWordRemove = new System.Windows.Forms.Button();
            this.txtWord = new System.Windows.Forms.TextBox();
            this.btnSaveImage = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnPrev = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.pbBrowse = new System.Windows.Forms.PictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // pbDraw
            // 
            this.pbDraw.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbDraw.Cursor = System.Windows.Forms.Cursors.Cross;
            this.pbDraw.Location = new System.Drawing.Point(12, 41);
            this.pbDraw.Name = "pbDraw";
            this.pbDraw.Size = new System.Drawing.Size(43, 50);
            this.pbDraw.TabIndex = 0;
            this.pbDraw.TabStop = false;
            this.pbDraw.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pbDraw_MouseDown);
            this.pbDraw.MouseLeave += new System.EventHandler(this.pbDraw_MouseLeave);
            this.pbDraw.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pbDraw_MouseMove);
            this.pbDraw.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pbDraw_MouseUp);
            // 
            // btnRecognize
            // 
            this.btnRecognize.Location = new System.Drawing.Point(12, 12);
            this.btnRecognize.Name = "btnRecognize";
            this.btnRecognize.Size = new System.Drawing.Size(161, 23);
            this.btnRecognize.TabIndex = 1;
            this.btnRecognize.Text = "Распознать";
            this.btnRecognize.UseVisualStyleBackColor = true;
            this.btnRecognize.Click += new System.EventHandler(this.btnRecognize_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(179, 12);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(70, 23);
            this.btnClear.TabIndex = 2;
            this.btnClear.Text = "Очистить";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // lstResults
            // 
            this.lstResults.FormattingEnabled = true;
            this.lstResults.Location = new System.Drawing.Point(9, 19);
            this.lstResults.Name = "lstResults";
            this.lstResults.Size = new System.Drawing.Size(221, 43);
            this.lstResults.TabIndex = 4;
            // 
            // lstWords
            // 
            this.lstWords.FormattingEnabled = true;
            this.lstWords.Location = new System.Drawing.Point(255, 67);
            this.lstWords.Name = "lstWords";
            this.lstWords.Size = new System.Drawing.Size(237, 160);
            this.lstWords.TabIndex = 6;
            // 
            // btnWordAdd
            // 
            this.btnWordAdd.Location = new System.Drawing.Point(255, 12);
            this.btnWordAdd.Name = "btnWordAdd";
            this.btnWordAdd.Size = new System.Drawing.Size(114, 23);
            this.btnWordAdd.TabIndex = 8;
            this.btnWordAdd.Text = "Добавить слово";
            this.btnWordAdd.UseVisualStyleBackColor = true;
            this.btnWordAdd.Click += new System.EventHandler(this.btnWordAdd_Click);
            // 
            // btnWordRemove
            // 
            this.btnWordRemove.Location = new System.Drawing.Point(375, 12);
            this.btnWordRemove.Name = "btnWordRemove";
            this.btnWordRemove.Size = new System.Drawing.Size(117, 23);
            this.btnWordRemove.TabIndex = 9;
            this.btnWordRemove.Text = "Удалить слово";
            this.btnWordRemove.UseVisualStyleBackColor = true;
            this.btnWordRemove.Click += new System.EventHandler(this.btnWordRemove_Click);
            // 
            // txtWord
            // 
            this.txtWord.Location = new System.Drawing.Point(255, 41);
            this.txtWord.MaxLength = 38;
            this.txtWord.Name = "txtWord";
            this.txtWord.Size = new System.Drawing.Size(237, 20);
            this.txtWord.TabIndex = 10;
            // 
            // btnSaveImage
            // 
            this.btnSaveImage.Location = new System.Drawing.Point(167, 233);
            this.btnSaveImage.Name = "btnSaveImage";
            this.btnSaveImage.Size = new System.Drawing.Size(82, 69);
            this.btnSaveImage.TabIndex = 11;
            this.btnSaveImage.Text = "Создать образ";
            this.btnSaveImage.UseVisualStyleBackColor = true;
            this.btnSaveImage.Click += new System.EventHandler(this.btnSaveImage_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnPrev);
            this.groupBox1.Controls.Add(this.btnNext);
            this.groupBox1.Controls.Add(this.pbBrowse);
            this.groupBox1.Location = new System.Drawing.Point(12, 233);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(149, 75);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Существующие образы";
            // 
            // btnPrev
            // 
            this.btnPrev.Location = new System.Drawing.Point(55, 46);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Size = new System.Drawing.Size(88, 23);
            this.btnPrev.TabIndex = 2;
            this.btnPrev.Text = "Предыдущий";
            this.btnPrev.UseVisualStyleBackColor = true;
            this.btnPrev.Click += new System.EventHandler(this.btnPrev_Click);
            // 
            // btnNext
            // 
            this.btnNext.Location = new System.Drawing.Point(55, 19);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(88, 23);
            this.btnNext.TabIndex = 1;
            this.btnNext.Text = "Следующий";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // pbBrowse
            // 
            this.pbBrowse.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbBrowse.Location = new System.Drawing.Point(6, 19);
            this.pbBrowse.Name = "pbBrowse";
            this.pbBrowse.Size = new System.Drawing.Size(43, 50);
            this.pbBrowse.TabIndex = 0;
            this.pbBrowse.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lstResults);
            this.groupBox2.Location = new System.Drawing.Point(255, 233);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(236, 75);
            this.groupBox2.TabIndex = 13;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Результат";
            // 
            // FrmExample
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(503, 308);
            this.Controls.Add(this.btnSaveImage);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.txtWord);
            this.Controls.Add(this.btnWordRemove);
            this.Controls.Add(this.btnWordAdd);
            this.Controls.Add(this.lstWords);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnRecognize);
            this.Controls.Add(this.pbDraw);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmExample";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Пример";
            this.Shown += new System.EventHandler(this.FrmExample_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pbDraw)).EndInit();
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowse)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pbDraw;
        private System.Windows.Forms.Button btnRecognize;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.ListBox lstResults;
        private System.Windows.Forms.ListBox lstWords;
        private System.Windows.Forms.Button btnWordAdd;
        private System.Windows.Forms.Button btnWordRemove;
        private System.Windows.Forms.TextBox txtWord;
        private System.Windows.Forms.Button btnSaveImage;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.PictureBox pbBrowse;
        private System.Windows.Forms.Button btnPrev;
        private System.Windows.Forms.Button btnNext;
    }
}

