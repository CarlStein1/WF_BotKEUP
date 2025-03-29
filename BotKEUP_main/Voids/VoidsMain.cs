using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using BotKEUP_main.Forms;

namespace BotKEUP_main
{
    internal class VoidsMain
    {
        // Строка подключения к базе данных MySQL
        static string connectionstring = "server=localhost;port=3306;database=keup;user=root;";

        // Словарь для записей названия колонок, для перевода с английского на русский
        public static Dictionary<string, string> columnheadertexts = new Dictionary<string, string>() {
                { "Surname", "Фамилия"},
                { "Name", "Имя" },
                { "Patronymic", "Отчество" },
                { "ApplicationDate", "ДатаЗаявки" },
                { "Position", "Должность" },
                { "GroupName", "НазваниеГруппы" },
                { "Specialization", "Специализация" },
                { "MaxCourse", "МаксимальныйКурс"},
                { "Code", "Код"},
                { "IsLeader", "Староста"}
            };

    // Метод переключения панелей
    public static void ChangePanel(Dictionary<Guna2TileButton, Guna2GradientPanel> dict, object sender)
        {
            foreach (Guna2GradientPanel panels in dict.Values)
            {
                // Скрываем все панели, кроме той, которая связана с нажатой кнопкой
                panels.Visible = panels == dict[sender as Guna2TileButton];
            }
        }

        // Метод изменения внешнего вида кнопок
        public static void ChangeButton(Dictionary<Guna2TileButton, Guna2GradientPanel> dict, object sender)
        {
            foreach (Guna2TileButton buttons in dict.Keys)
            {
                // Устанавливаем стандартный стиль для всех кнопок
                buttons.FillColor = Color.FloralWhite;
                buttons.ForeColor = Color.DimGray;
                buttons.HoverState.FillColor = Color.FromArgb(255, 239, 218);
            }
            // Меняем стиль активной кнопки
            (sender as Guna2TileButton).FillColor = Color.Bisque;
            (sender as Guna2TileButton).ForeColor = Color.Black;
            (sender as Guna2TileButton).HoverState.FillColor = Color.Bisque;
        }

        // Выполнение SELECT-запроса и возврат результата в виде DataTable
        public static DataTable SelectRequestAsDataTable(string request)
        {
            DataTable dataTable = new DataTable();
            using (MySqlConnection connection = new MySqlConnection(connectionstring))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(request, connection))
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                    reader.Close();
                }
            }
                foreach (DataColumn dc in dataTable.Columns)
                {
                    if (columnheadertexts.Keys.Contains(dc.ColumnName))
                        dc.ColumnName = columnheadertexts[dc.ColumnName];
                }
            return dataTable;
        }

        // Выполнение SELECT-запроса и возврат результата в виде массива строк
        public static string[,] SelectRequestAsStringArray(string query)
        {
            List<string[]> result = new List<string[]>();
            int columnCount = 0;
            using (MySqlConnection connection = new MySqlConnection(connectionstring))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    columnCount = reader.FieldCount;
                    while (reader.Read())
                    {
                        string[] rowValues = new string[columnCount];
                        for (int i = 0; i < columnCount; i++)
                        {
                            rowValues[i] = reader[i].ToString();
                        }
                        result.Add(rowValues);
                    }
                    reader.Close();
                }
            }
            // Преобразование списка в двумерный массив
            string[,] arrayResult = new string[result.Count, columnCount];
            for (int i = 0; i < result.Count; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    arrayResult[i, j] = result[i][j];
                }
            }
            return arrayResult;
        }

        // Метод для выполнения INSERT, DELETE, UPDATE запросов
        public static void InsDelUpdRequest(string request)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionstring))
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(request, connection);
                command.ExecuteNonQuery();
            }
        }

        // Метод загрузки данных в ComboBox
        public static void CheckBoxLoad(Guna2ComboBox combobox)
        {
            string[,] items = SelectRequestAsStringArray($"SELECT DISTINCT {combobox.Tag} FROM {combobox.Parent.Controls.OfType<Guna2DataGridView>().First().Tag}");
            combobox.Items.Clear();
            combobox.Items.Add("Все");
            combobox.SelectedIndex = 0;
            for (int i = 0; i < items.GetLength(0); i++)
            {
                combobox.Items.Add(items[i, 0]);
            }
        }

        // Метод для вызова кастомного MessageBox
        public static DialogResult MessageBoxCustomShow(string title, string text, bool needconfirm = false)
        {
            MessageBoxCustom cfrmsg = new MessageBoxCustom();
            cfrmsg.title = title;
            cfrmsg.text = text;
            cfrmsg.needconfirm = needconfirm;
            cfrmsg.ShowDialog();
            return cfrmsg.DialogResult;
        }
    }
}
