using System;
using System.Data;
using System.Windows.Forms;

namespace CountriesApp
{
    public partial class Form1 : Form
    {
        DataCountry Country = new DataCountry();
        Connection conn = new Connection();

        public Form1()
        {
            InitializeComponent();
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            textBox1.Text = string.Empty;
        }

        //получение данных о конкретной стране 
        private void button1_Click(object sender, EventArgs e)
        {
            
            Country = conn.GetDataAPI(textBox1.Text);

            if (Country != null)
            {
                richTextBox1.Text = "Name: " + Country.Name + "\nCountry code: " + Country.CallingCodes + "\nCapital: " + Country.Capital +
                                "\nArea: " + Country.Area + "\nPopulation: " + Country.Population + "\nRegion: " + Country.Region;

                DialogResult answer = DialogResult.No;

                answer = MessageBox.Show("Do I need to save '" + Country.Name + "' in the database?", "Add data", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (answer == DialogResult.Yes)
                    conn.InsertData();
            }
        }
       
        //кнопка обновить бд
        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.DataSource = conn.ReloadData();
            if (dataGridView1.DataSource != null) MessageBox.Show("The database was updated successfully!", "Success", MessageBoxButtons.OK);
        }

        //закрытие соединения с бд при закрытии формы
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            conn.CloseConnection();
        }

        //загрузка данных при переключении на вкладку "Database"
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.DataSource = conn.LoadData();
        }
    }
}
