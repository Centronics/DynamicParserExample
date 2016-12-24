using System.Windows.Forms;

namespace DynamicParserExample
{
    public partial class Percent : Form
    {
        public Percent()
        {
            InitializeComponent();
        }

        public double? EqualPercent { get; private set; }

        void Percent_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                double res;
                if (!double.TryParse(txtPercent.Text, out res))
                {
                    EqualPercent = null;
                    MessageBox.Show(this, @"Введённое число не является числом.");
                    return;
                }
                if (res < 50.0)
                {
                    MessageBox.Show(this, @"Введённое число не может быть менее 50.");
                    EqualPercent = null;
                    return;
                }
                if (res > 100.0)
                {
                    MessageBox.Show(this, @"Введённое число не может быть более 100.");
                    EqualPercent = null;
                    return;
                }
                EqualPercent = res / 100.0;
                DialogResult = DialogResult.OK;
                return;
            }
            if (e.KeyCode != Keys.Escape) return;
            EqualPercent = null;
            Dispose();
        }
    }
}