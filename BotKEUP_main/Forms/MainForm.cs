using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;

namespace BotKEUP_main
{
    public partial class MainForm : Form
    {
        /* ПРИ ДОБАВЛЕНИИ:
         * 1) КНОПОК В МЕНЮ СЛЕВА И ТАБЛИЦ В КАЖДОЙ ВКЛАДКЕ: НАЗВАНИЕ ТАБЛИЦЫ В MYSQL, А ТАКЖЕ ДОБАВЛЯТЬ ИХ В СЛОВАРЬ buttonsAndPanels. 
         * ДЛЯ ТАБЛИЦ ТАКЖЕ ПРОПИСАТЬ В СЛОВАРЕ VoidsMain.columnheadertexts ОТОБРАЖЕНИЕ КОЛОНОК ТАБЛИЦЫ НА РУССКОМ {"Название колонки в БД","Название колонки на русском"}
         * 2) COMBOBOX ДЛЯ ФИЛЬТРАЦИИ: НАЗВАНИЕ СТОЛБЦА, С КОТОРОГО ПОСЛЕ БУДУТ БРАТЬСЯ ОРИГИНАЛЬНЫЕ ЗНАЧЕНИЯ ДЛЯ БЫСТРОЙ ФИЛЬТРАЦИИ.
         */

        string lastcellvalue; // Для записи первого значения редактируемой ячейки в DGV
        Guna2ComboBox currentcombobox; // Для записи необходимого combobox, чтобы скопировать от туда значение
        Guna2TextBox currenttextbox; // Для записи необходимого textbox, чтобы скопировать от туда значение
        Guna2GradientPanel currentpanel; // Для записи текущей вкладки
        Dictionary<Guna2TileButton, Guna2GradientPanel> buttonsAndPanels; // Словарь для привязывания кнопок переключения и вкладок
        Dictionary<int, DataGridViewRow> addingRowsDict = new Dictionary<int, DataGridViewRow>(); // Словарь для записи временных строк, которые после можно будет внести в таблицу
        public MainForm()
        {
            InitializeComponent();
            buttonsAndPanels = new Dictionary<Guna2TileButton, Guna2GradientPanel>() {
                { guna2TileButtonГруппы, guna2GradientPanelГруппы},
                { guna2TileButtonЗаявки, guna2GradientPanelЗаявки},
                { guna2TileButtonПреподаватели, guna2GradientPanelПреподаватели },
                { guna2TileButtonСтуденты, guna2GradientPanelСтуденты },
                { guna2TileButtonСпециальности, guna2GradientPanelСпециальности }
            };
        }

        // Загрузка формы
        private void MainForm_Load(object sender, EventArgs e)
        {
            currentpanel = guna2GradientPanelЗаявки; // Присвоение текущей начальной вкладки

            // Перебор каждого элемента каждой вкладки для привязки данных БД к таблицам
            foreach (Control ctrl1 in guna2GradientPanel3.Controls)
            {
                if (ctrl1 is Guna2GradientPanel)
                    foreach (Control ctrl2 in (ctrl1 as Guna2GradientPanel).Controls)
                    {
                        if (ctrl2 is Guna2DataGridView)
                        {
                            // Очистка столбцов каждого DGV
                            (ctrl2 as Guna2DataGridView).Columns.Clear();
                            try
                            {
                                // Присвоение начального DataSourse для таблиц
                                (ctrl2 as Guna2DataGridView).DataSource = VoidsMain.SelectRequestAsDataTable($"SELECT * FROM {(ctrl2 as Guna2DataGridView).Tag}");
                            }
                            catch
                            {
                                VoidsMain.MessageBoxCustomShow("Ошибка запроса", "Невозможно привязать данные БД к таблице " + (ctrl2 as Guna2DataGridView).Tag.ToString() + "!");
                                return;
                            }

                            // Ограничение на изменение FOREIGN KEY
                            (ctrl2 as Guna2DataGridView).Columns[0].ReadOnly = true;

                            // Установка формата для столбцов с типом данных datetime
                            foreach (DataGridViewColumn col in (ctrl2 as Guna2DataGridView).Columns)
                                if (col.ValueType == typeof(DateTime))
                                {
                                    col.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
                                    col.ReadOnly = true;
                                }
                        }
                        // Присвоение каждому Checkbox значений
                        if (ctrl2 is Guna2ComboBox)
                        {
                            VoidsMain.CheckBoxLoad(ctrl2 as Guna2ComboBox);
                        }
                    }
            }

            // Указание начального количества заявок в кружочке
            guna2NotificationPaint1.Text = VoidsMain.SelectRequestAsDataTable($"SELECT * FROM {guna2DataGridViewЗаявки.Tag}").Rows.Count.ToString();
        }

        // Переключение вкладок
        private void guna2TileButton2_MouseClick(object sender, MouseEventArgs e)
        {
            // Проверка на активность кнопки и вкладки
            if ((sender as Guna2TileButton).FillColor != Color.Bisque)
            {
                if (addingRowsDict.Count > 0 && VoidsMain.MessageBoxCustomShow("Предупреждение", "В данной вкладке имеются непримененные изменения. Вы уверены, что хотите их потерять?", true) == DialogResult.Cancel) return;
                // Процедуры изменения кнопки и вкладки
                VoidsMain.ChangePanel(buttonsAndPanels, sender);
                VoidsMain.ChangeButton(buttonsAndPanels, sender);

                currentpanel = buttonsAndPanels[sender as Guna2TileButton]; // Запись текущей вкладки в переменную
                addingRowsDict.Clear(); // Очистка всех временных столбцов
                применитьИзмененияToolStripMenuItem.Enabled = addingRowsDict.Count > 0; // Деактивация кнопки "Применить изменения"
                UpdateDGVFromDB(); // Обновление данных в таблице
            }
        }

        // Фильтрация/Обновление данных при изменении текста в textbox
        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {
            Guna2DataGridView dgv = currentpanel.Controls.OfType<Guna2DataGridView>().First();
            
            Guna2TextBox txb = null;
            Guna2ComboBox combobox = currentpanel.Controls.OfType<Guna2ComboBox>().FirstOrDefault();

            // Определяем, откуда пришел вызов
            if (sender is Guna2TextBox)
            {
                txb = sender as Guna2TextBox;
            }
            else if (sender is Guna2ComboBox)
            {
                combobox = sender as Guna2ComboBox;
                txb = currentpanel.Controls.OfType<Guna2TextBox>().FirstOrDefault();
            }

            if (dgv.DataSource is DataTable table)
            {
                string filterExpression = string.Empty;

                // Фильтрация по текстовому полю
                if (!string.IsNullOrEmpty(txb.Text))
                {
                    foreach (DataColumn col in table.Columns)
                    {
                        
                        filterExpression += $"CONVERT([{col.ColumnName}], 'System.String') LIKE '%{txb.Text}%' OR ";
                    }
                    filterExpression = filterExpression.TrimEnd(" OR ".ToCharArray());
                }

                // Фильтрация по ComboBox
                if (combobox != null && combobox.SelectedItem.ToString() != "Все")
                {
                    string comboboxTagTemp = VoidsMain.columnheadertexts[combobox.Tag.ToString()];
                    string comboFilter = $"[{comboboxTagTemp}] = '{combobox.SelectedItem}'";
                    filterExpression = string.IsNullOrEmpty(filterExpression) ? comboFilter : $"{comboFilter} AND ({filterExpression})";
                }

                // Применение фильтрации
                (table.DefaultView).RowFilter = filterExpression;
            }
        }

        // Обновление DGV из БД
        private void UpdateDGVFromDB()
        {
            Guna2DataGridView dgv = currentpanel.Controls.OfType<Guna2DataGridView>().First();

            if (dgv.Tag != null)
            {
                dgv.DataSource = VoidsMain.SelectRequestAsDataTable($"SELECT * FROM {dgv.Tag}");
            }
            guna2NotificationPaint1.Text = VoidsMain.SelectRequestAsDataTable($"SELECT * FROM {guna2DataGridViewЗаявки.Tag}").Rows.Count.ToString();
        }

        // Очистка фильтров
        private void guna2TileButton1_Click(object sender, EventArgs e)
        {
            Guna2TextBox txb = currentpanel.Controls.OfType<Guna2TextBox>().First();
            txb.Clear();
        }

        // Операция "Копировать" в CMS для combobox
        private void скопироватьНазваниеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentcombobox.SelectedItem != null)
                Clipboard.SetText(currentcombobox.SelectedItem.ToString());
        }

        // Запись текущего combobox при открытии CMS
        private void guna2ContextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            currentcombobox = (sender as Guna2ContextMenuStrip).SourceControl as Guna2ComboBox;
        }

        // Запись текущего textbox при открытии CMS
        private void guna2ContextMenuStrip2_Opening(object sender, CancelEventArgs e)
        {
            currenttextbox = (sender as Guna2ContextMenuStrip).SourceControl.Parent as Guna2TextBox;
        }

        // Операция "Вырезать" в CMS для textbox
        private void вырезатьtoolStripMenuItem_Click(object sender, EventArgs e)
        {
            currenttextbox.Cut();
        }

        // Операция "Копировать" в CMS для textbox
        private void копироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currenttextbox.Text != string.Empty)
                if (currenttextbox.SelectedText != string.Empty)
                    Clipboard.SetText(currenttextbox.SelectedText);
            else
                    Clipboard.SetText(currenttextbox.Text);
        }

        // Операция "Вставить" в CMS для textbox
        private void вставитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currenttextbox.Paste();
        }

        // Операция "Удалить" в CMS для textbox
        private void удалитьToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            currenttextbox.Text = string.Empty;
        }

        // Операция "Выделить" в CMS для textbox
        private void выделитьВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currenttextbox.SelectAll();
        }

        // Изменение элемента, на котором находится курсор, дабы за него можно было потянуть и перетащить форму
        private void guna2GradientPanel1_MouseMove(object sender, MouseEventArgs e)
        {
            guna2DragControl1.TargetControl = sender as Control;
        }

        // Изменение шрифта на текущей таблице
        private void шрифтToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Guna2DataGridView dgv = currentpanel.Controls.OfType<Guna2DataGridView>().First();
            
            fontDialog1.Font = dgv.RowTemplate.DefaultCellStyle.Font;
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                // Сохраняем DataTable перед изменением
                DataTable dt = dgv.DataSource as DataTable;
                dgv.DataSource = null; // Очищаем источник данных

                // Применяем новый шрифт
                dgv.RowTemplate.DefaultCellStyle.Font = fontDialog1.Font;

                // Восстанавливаем DataTable
                dgv.DataSource = dt;
            }
        }

        // Открытие CMS принажатии ПКМ на DGV во вкладке "Заявки"
        private void guna2DataGridViewЗаявки_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0) // Проверяем, что это правая кнопка и строка
            {
                if ((sender as Guna2DataGridView).SelectedRows.Count < 3) (sender as Guna2DataGridView).ClearSelection();
                (sender as Guna2DataGridView).Rows[e.RowIndex].Selected = true; // Выделяем строку
                guna2ContextMenuStrip3.Show(Cursor.Position); // Показываем контекстное меню в месте курсора
            }
        }

        // Открытие CMS принажатии ПКМ на DGV в остальных вкладках
        private void guna2DataGridViewГруппы_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0) // Проверяем, что это правая кнопка и строка
            {
                if ((sender as Guna2DataGridView).SelectedRows.Count < 3) (sender as Guna2DataGridView).ClearSelection();
                (sender as Guna2DataGridView).Rows[e.RowIndex].Selected = true; // Выделяем строку
                guna2ContextMenuStrip4.Show(Cursor.Position); // Показываем контекстное меню в месте курсора
            }
        }

        // Одобрение заявки пользователя
        private void одобритьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Guna2DataGridView dgv = currentpanel.Controls.OfType<Guna2DataGridView>().First();

            foreach (DataGridViewRow dgvr in dgv.SelectedRows)
            {
                switch (dgvr.Cells[5].Value.ToString()) // Проверка заявки на требуемую должность
                {
                    case "Преподаватель":
                        try
                        {
                            VoidsMain.InsDelUpdRequest($@"INSERT INTO teachers VALUES ({dgvr.Cells[0].Value}, '{dgvr.Cells[1].Value}', '{dgvr.Cells[2].Value}', '{dgvr.Cells[3].Value}')");
                        }
                        catch
                        {
                            VoidsMain.MessageBoxCustomShow("Ошибка запроса", "Невозможно вставить данные, проверьте их наличие или формат!");
                            return;
                        }
                        break;
                    case "Староста":
                        try
                        {
                            VoidsMain.InsDelUpdRequest($"UPDATE students SET IsLeader = 1 WHERE chat_id = {dgvr.Cells[0].Value}");
                        }
                        catch
                        {
                            VoidsMain.MessageBoxCustomShow("Ошибка запроса", "Невозможно обновить данные, проверьте их наличие или формат!");
                            return;
                        }
                        break;
                }

                // Удаление заявки в случае, если заявка одобрилась
                try
                {
                    VoidsMain.InsDelUpdRequest($"DELETE FROM applications WHERE chat_id = {dgvr.Cells[0].Value}");
                    UpdateDGVFromDB();
                    
                }
                catch
                {
                    VoidsMain.MessageBoxCustomShow("Ошибка запроса", "Невозможно удалить данные, проверьте их наличие или целостность!");
                    return;
                }
            }
        }

        // Отклонение заявки пользователя
        private void отклонитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Guna2DataGridView dgv = currentpanel.Controls.OfType<Guna2DataGridView>().First();
          
            foreach (DataGridViewRow dgvr in dgv.SelectedRows)
            {
                try
                {
                    VoidsMain.InsDelUpdRequest($"DELETE FROM applications WHERE chat_id = {dgvr.Cells[0].Value}"); 
                    UpdateDGVFromDB();
                }
                catch
                {
                    VoidsMain.MessageBoxCustomShow("Ошибка запроса", "Невозможно отклонить заявки, проверьте их наличие!");
                    return;
                }
            }
        }

        // Изменение значения в ячейке DGV
        private void guna2DataGridViewЗаявки_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // Проверка индексации, чтобы не было ошибок
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                Guna2DataGridView dgv = sender as Guna2DataGridView;

                // Проверка на изменение данных во временных строчках, которые не нужно применять, пока администратор не нажал на "Применить изменения"
                if (addingRowsDict.Values.Contains(dgv.CurrentRow) == true)
                {
                    return;
                }

                string primarykey = dgv.Columns[0].HeaderCell.Value.ToString();
                string changingcell = dgv.Columns[e.ColumnIndex].HeaderCell.Value.ToString();

                if (VoidsMain.columnheadertexts.Values.Contains(primarykey))
                    primarykey = VoidsMain.columnheadertexts.FirstOrDefault(x => x.Value == primarykey).Key;
                if (VoidsMain.columnheadertexts.Values.Contains(changingcell))
                    changingcell = VoidsMain.columnheadertexts.FirstOrDefault(x => x.Value == changingcell).Key;

                string querrytext = string.Empty;
                switch(dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) // Проверка на соответствие типов данных, приведение их к читабельному варианту для БД
                {
                    case true:
                        querrytext = "1";
                        break;
                    case false:
                        querrytext = "0";
                        break;
                    default: 
                        querrytext = "\'" + dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() + "\'";
                        break;
                }

                // Применение изменений в БД
                try
                {
                    VoidsMain.InsDelUpdRequest($@"UPDATE {dgv.Tag} SET {changingcell} = {querrytext} WHERE {primarykey} = '{dgv.Rows[e.RowIndex].Cells[0].Value}'");
                }
                catch
                { 
                    Guna2MessageDialog asd = new Guna2MessageDialog();
                    VoidsMain.MessageBoxCustomShow("Ошибка запроса", "Невозможно выполнить запрос на изменение. Возможно введенное сообщение не соответствует формату!");
                    dgv.CurrentCell.Value = lastcellvalue; // Возвращение изначального значения клетки
                    return;
                }
            }
        }

        // Добавление строки в таблицу БД
        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            Guna2DataGridView dgv = currentpanel.Controls.OfType<Guna2DataGridView>().First(); // Получаем текущий DataGridView
           
            DataTable table = (DataTable)dgv.DataSource; // Приводим источник данных DataGridView к DataTable
            DataRow row = table.NewRow(); // Создаем новую строку

            for (int i = 0; i < table.Columns.Count; i++) // Проходим по всем столбцам таблицы
            {
                switch (table.Columns[i].DataType) // Проверяем тип данных столбца
                {
                    case Type t when t == typeof(string):
                        {
                            string baseValue = "Значение";
                            string newValue = baseValue + dgv.Rows.Count; // Формируем начальное значение

                            // Проверяем, существует ли уже такое значение в этом столбце
                            while (table.AsEnumerable().Any(r => r[i].ToString() == newValue))
                            {
                                newValue = baseValue + (dgv.Rows.Count + 1); // Увеличиваем счетчик
                            }

                            row[i] = newValue;
                            break;
                        }

                    case Type t when t == typeof(Int64):
                        {
                            int newValue = dgv.Rows.Count;

                            // Проверяем уникальность
                            while (table.AsEnumerable().Any(r => r[i].ToString() == newValue.ToString()))
                            {
                                newValue = dgv.Rows.Count + 1;
                            }

                            row[i] = newValue;
                            break;
                        }

                    case Type t when t == typeof(SByte):
                        {
                            byte newValue = (byte)dgv.Rows.Count;
                            byte counter = 1;

                            while (table.AsEnumerable().Any(r => r[i].ToString() == newValue.ToString()))
                            {
                                newValue = (byte)(dgv.Rows.Count + counter);
                                counter++;
                            }

                            row[i] = newValue;
                            break;
                        }

                    case Type t when t == typeof(DateTime):
                        row[i] = DateTime.MinValue; // Ставим минимальное значение даты (1753-01-01 для SQL Server)
                        break;
                }
            }

            table.Rows.Add(row); // Добавляем строку в DataTable

            int rowIndex = dgv.Rows.Count - 1; // Определяем индекс добавленной строки
            addingRowsDict[rowIndex] = dgv.Rows[rowIndex]; // Добавляем строку в словарь

            dgv.Columns[0].ReadOnly = false; // Делаем первый столбец редактируемым

            if (addingRowsDict.Count > 0) // Если есть добавленные строки
                применитьИзмененияToolStripMenuItem.Enabled = true; // Активируем кнопку "Применить изменения"
        }

        // Удаление строки из таблицы БД
        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            Guna2DataGridView dgv = currentpanel.Controls.OfType<Guna2DataGridView>().First(); // Получаем текущий DataGridView
            DataTable table = (DataTable)dgv.DataSource; // Приводим источник данных DataGridView к DataTable
            string primarykey = dgv.Columns[0].HeaderText;

            if (VoidsMain.columnheadertexts.Values.Contains(primarykey))
                primarykey = VoidsMain.columnheadertexts.FirstOrDefault(x => x.Value == primarykey).Key;

            // Получаем список индексов выделенных строк (сортируем по убыванию, чтобы индексация не сбивалась)
            List<int> selectedIndexes = dgv.SelectedRows.Cast<DataGridViewRow>().Select(row => row.Index).OrderByDescending(index => index).ToList();

            foreach (int rowIndex in selectedIndexes)
            {
                if (rowIndex >= 0)
                {
                    if (!addingRowsDict.ContainsKey(rowIndex)) // Если строка уже в БД, нужно удалить ее через запрос
                    {
                        try
                        {
                            VoidsMain.InsDelUpdRequest($@"DELETE FROM {dgv.Tag} WHERE {primarykey} = '{dgv.Rows[rowIndex].Cells[0].Value}'");
                        }
                        catch
                        {
                            VoidsMain.MessageBoxCustomShow("Ошибка запроса", "Невозможно удалить данные, проверьте их наличие или целостность!");
                            return;
                        }
                    }
                    else
                    {
                        addingRowsDict.Remove(rowIndex);
                    }

                    table.Rows.RemoveAt(rowIndex); // Удаление строки из DataTable
                }
            }

            // Пересоздаем словарь с правильными индексами
            Dictionary<int, DataGridViewRow> updatedDict = new Dictionary<int, DataGridViewRow>();
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (addingRowsDict.Values.Contains(dgv.Rows[i])) // Сопоставляение строки по объектам
                {
                    updatedDict[i] = dgv.Rows[i]; // Присваивание новых индексов
                }
            }
            addingRowsDict = updatedDict; // Обновляем словарь

            применитьИзмененияToolStripMenuItem.Enabled = addingRowsDict.Count > 0; // Включаем кнопку "Применить изменения", если есть добавленные строки
        }

        // Применение изменений в таблице, занесение их в БД
        private void применитьИзмененияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Guna2DataGridView dgv = currentpanel.Controls.OfType<Guna2DataGridView>().First(); // Получаем текущий DataGridView на активной панели
            

            foreach (var kvp in addingRowsDict) // Перебираем все строки, добавленные пользователем
            {
                DataGridViewRow dgvr = kvp.Value; // Получаем строку из словаря
                string temp = string.Join(", ", dgvr.Cells.Cast<DataGridViewCell>() // Формируем строку значений для SQL-запроса
                .Select(cell => $"'{cell.Value?.ToString()}'")); // Оборачиваем каждое значение в кавычки
                try
                {
                    // Выполняем SQL-запрос на добавление новой строки в базу данных
                    VoidsMain.InsDelUpdRequest($"INSERT INTO {dgv.Tag} VALUES ({temp})");
                }
                catch
                {
                    // Выводим сообщение об ошибке, если запрос не удался
                    VoidsMain.MessageBoxCustomShow("Ошибка запроса", "Невозможно выполнить запрос. Возможно несоответствие первичных и внешних ключей!");
                    return; // Прерываем выполнение метода
                }
            }

            // Очищаем словарь после успешного сохранения
            addingRowsDict.Clear();

            dgv.Columns[0].ReadOnly = true; // Блокируем редактирование первого столбца
            (sender as ToolStripMenuItem).Enabled = false; // Отключаем кнопку "Применить изменения"
        }

        // Запись начального значения ячейки при двойном щелчке на нее
        private void guna2DataGridViewГруппы_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            lastcellvalue = (sender as Guna2DataGridView).CurrentCell.Value.ToString();
        }

        // Обновление вкладки
        private void обновитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Если на вкладке есть несохраненные изменения
            if (addingRowsDict.Count > 0 && VoidsMain.MessageBoxCustomShow("Предупреждение", "В данной вкладке имеются непримененные изменения. Вы уверены, что хотите их потерять?", true) == DialogResult.Cancel) return;
            UpdateDGVFromDB();
        }
    }
}
