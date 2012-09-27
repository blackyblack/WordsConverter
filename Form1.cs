using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WordsConverter
{
    public partial class Form1 : Form
    {
        private SQLiteConnection sql_con;
        private SQLiteCommand sql_cmd;

        private const int LIMIT_TABLE_COUNT = 5000;

        public Form1()
        {
            InitializeComponent();
        }

        private void Connect(String name)
        {
            sql_con = new SQLiteConnection("Data Source=" + name +
                ";Version=3;UseUTF16Encoding=False;New=False;Compress=True;");

            sql_con.Open();
        }

        private void Disconnect()
        {
            sql_con.Close();
        }

        private void DropTable(String name)
        {
            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "DROP TABLE IF EXISTS [" + name + "]";
                sql_cmd.ExecuteNonQuery();
            }

            DropIndex(name + "_word_idx");
            DropIndex(name + "_freq_idx");
        }

        private void DropUserTable(String name)
        {
            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "DROP TABLE IF EXISTS [" + name + "]";
                sql_cmd.ExecuteNonQuery();
            }

            DropIndex(name + "_word_idx");
            DropIndex(name + "_freq_idx");
            DropIndex(name + "_locale_idx");
        }

        private void DropIndex(String name)
        {
            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "DROP INDEX IF EXISTS [" + name + "]";
                sql_cmd.ExecuteNonQuery();
            }
        }

        private void DropTables()
        {
            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "DROP TABLE IF EXISTS android_metadata";
                sql_cmd.ExecuteNonQuery();
            }

            DropTable("en_strings");
            DropTable("ru_strings");
            DropUserTable("user_strings");
        }

        //create system data
        private void CreateInit()
        {
            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "CREATE TABLE IF NOT EXISTS android_metadata (locale TEXT)";
                sql_cmd.ExecuteNonQuery();
            }

            ClearTable("android_metadata");

            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "INSERT INTO android_metadata (locale) VALUES ('en_US')";
                sql_cmd.ExecuteNonQuery();
            }
        }

        //create word and freq index for each table
        private void CreateIndexStrings(String name)
        {
            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS [" + name + "_word_idx]" +
                    " ON " + name +
                    "([WORD] COLLATE BINARY ASC)";
                sql_cmd.ExecuteNonQuery();
            }

            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "CREATE INDEX IF NOT EXISTS [" + name + "_freq_idx]" +
                    " ON " + name +
                    "([FREQ] DESC)";
                sql_cmd.ExecuteNonQuery();
            }
        }

        //create table with words for specific language
        private void CreateTableStrings(String name)
        {
            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "CREATE TABLE IF NOT EXISTS [" + name + "] " +
                    "([_id] INTEGER NOT NULL PRIMARY KEY, " +
                    "[WORD] TEXT NOT NULL COLLATE BINARY, " +
                    "[FREQ] INTEGER NOT NULL)";
                sql_cmd.ExecuteNonQuery();
            }

            CreateIndexStrings(name);
        }

        //create word and freq index for user words
        private void CreateIndexUserStrings(String name)
        {
            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS [" + name + "_word_idx]" +
                    " ON " + name +
                    "([WORD] COLLATE BINARY ASC)";
                sql_cmd.ExecuteNonQuery();
            }

            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "CREATE INDEX IF NOT EXISTS [" + name + "_freq_idx]" +
                    " ON " + name +
                    "([FREQ] DESC)";
                sql_cmd.ExecuteNonQuery();
            }

            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "CREATE INDEX IF NOT EXISTS [" + name + "_locale_idx]" +
                    " ON " + name +
                    "([LOCALE] DESC)";
                sql_cmd.ExecuteNonQuery();
            }
        }

        //create table for user words
        private void CreateTableUserStrings(String name)
        {
            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "CREATE TABLE IF NOT EXISTS [" + name + "] " +
                    "([_id] INTEGER NOT NULL PRIMARY KEY, " +
                    "[LOCALE] TEXT NOT NULL DEFAULT 'en_US' COLLATE BINARY, " +
                    "[WORD] TEXT NOT NULL COLLATE BINARY, " +
                    "[FREQ] INTEGER NOT NULL)";
                sql_cmd.ExecuteNonQuery();
            }

            CreateIndexUserStrings(name);
        }

        private void ClearTable(String name)
        {
            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "DELETE FROM [" + name + "]";
                sql_cmd.ExecuteNonQuery();
            }
        }

        private void StringsDbInsert(String name, String word, int freq)
        {
            using (sql_cmd = sql_con.CreateCommand())
            {
                sql_cmd.CommandText = "INSERT INTO [" + name + "] " +
                    "([WORD], [FREQ]) VALUES " +
                    "(@wordValue, @freqValue)";

                SQLiteParameter wordValue = new SQLiteParameter("@wordValue");
                wordValue.Value = word;
                SQLiteParameter freqValue = new SQLiteParameter("@freqValue");
                freqValue.Value = freq;
                sql_cmd.Parameters.Add(wordValue);
                sql_cmd.Parameters.Add(freqValue);

                sql_cmd.ExecuteNonQuery();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                button1.Enabled = false;

                Connect("dict.db");

                button1.Enabled = true;
                label1.Text = "База подключена";
            }
            catch(Exception)
            {
                label1.Text = "База не подключена";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CreateInit();
            CreateTableStrings("en_strings");
            CreateTableStrings("ru_strings");
            
            CreateTableUserStrings("user_strings");

            MessageBox.Show("База создана");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                button3.Enabled = false;

                Disconnect();

                button3.Enabled = true;
                label1.Text = "База не подключена";
            }
            catch (Exception)
            {
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconnect();
            label1.Text = "База не подключена";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();

            if (res != DialogResult.OK)
            {
                return;
            }

            String wordsFile = openFileDialog1.FileName;

            var lineCount = File.ReadLines(wordsFile).Count();
            int limitCount = 0;

            if (lineCount > LIMIT_TABLE_COUNT)
                lineCount = LIMIT_TABLE_COUNT;

            try
            {
                ClearTable("en_strings");

                progressBar1.Value = 0;
                progressBar1.Maximum = lineCount;

                using (StreamReader sr = new StreamReader(wordsFile))
                {
                    String line;

                    // Read and display lines from the file until the end of 
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        limitCount++;
                        if (limitCount > lineCount)
                            break;

                        var items = line.Split(' ');
                        String word = items[0];
                        int freq = int.Parse(items[1]);

                        StringsDbInsert("en_strings", word, freq);

                        progressBar1.PerformStep();
                        Application.DoEvents();
                    }
                }

                MessageBox.Show("Словарь en_strings загружен");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки en_strings:" + ex.Message);
            }

            progressBar1.Value = 0;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            DropTables();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();

            if (res != DialogResult.OK)
            {
                return;
            }

            String wordsFile = openFileDialog1.FileName;

            var lineCount = File.ReadLines(wordsFile).Count();
            int limitCount = 0;

            if (lineCount > LIMIT_TABLE_COUNT)
                lineCount = LIMIT_TABLE_COUNT;

            try
            {
                ClearTable("ru_strings");

                progressBar1.Value = 0;
                progressBar1.Maximum = lineCount;

                using (StreamReader sr = new StreamReader(wordsFile, Encoding.GetEncoding("windows-1251")))
                {
                    String line;

                    // Read and display lines from the file until the end of 
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        limitCount++;
                        if (limitCount > lineCount)
                            break;

                        var items = line.Split(' ');
                        String word = items[2];
                        double freq = double.Parse(items[1], CultureInfo.InvariantCulture);

                        int intFreq = (int)(freq * 100);

                        StringsDbInsert("ru_strings", word, intFreq);

                        progressBar1.PerformStep();
                        Application.DoEvents();
                    }
                }

                MessageBox.Show("Словарь ru_strings загружен");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки ru_strings:" + ex.Message);
            }

            progressBar1.Value = 0;
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            DropTable("user_strings");
        }
    }
}
