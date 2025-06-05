using Npgsql;
using OfficeOpenXml;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.VisualBasic;
using NpgsqlTypes;

namespace AdvertisingCampaignApp
{
    public partial class EmployeeDashboardPage : Page
    {
        private int UserId;

        public EmployeeDashboardPage(int userId)
        {
            InitializeComponent();
            UserId = userId;
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM pm11.get_employee_orders()", conn);
                using var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Нет заказов для отображения.");
                    dgOrders.ItemsSource = null;
                    return;
                }

                if (dt.Columns.Contains("Дата заказа"))
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["Дата заказа"] != DBNull.Value)
                        {
                            DateTime dateValue = Convert.ToDateTime(row["Дата заказа"]);
                            row["Дата заказа"] = dateValue.ToString("dd.MM.yyyy");
                        }
                        else
                        {
                            row["Дата заказа"] = "Нет даты";
                        }
                    }
                }

                dgOrders.ItemsSource = dt.DefaultView;
                System.Diagnostics.Debug.WriteLine($"Загружено строк: {dt.Rows.Count}");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}\nКод ошибки: {ex.ErrorCode}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Общая ошибка: {ex.Message}\nСтек вызовов: {ex.StackTrace}");
            }
        }

        private void UpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrders.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите заказ!");
                return;
            }

            if (cmbStatus.SelectedItem is not ComboBoxItem statusItem)
            {
                MessageBox.Show("Выберите новый статус!");
                return;
            }

            string newStatus = statusItem.Content.ToString();
            if (!new[] { "Создан", "В процессе", "Завершён", "Отменён" }.Contains(newStatus))
            {
                MessageBox.Show("Выберите корректный статус!");
                return;
            }

            try
            {
                int id = Convert.ToInt32(row["Номер заказа"]);
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT pm11.update_order_status(@id, @status::pm11.order_status)", conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("status", newStatus); // Npgsql автоматически обработает приведение благодаря ::pm11.order_status
                cmd.ExecuteNonQuery();
                LoadOrders();
                MessageBox.Show("Статус заказа успешно обновлён!");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка обновления статуса: {ex.Message}");
            }
        }

        private void AddComment_Click(object sender, RoutedEventArgs e)
        {
            if (dgOrders.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите заказ!");
                return;
            }

            try
            {
                int id = Convert.ToInt32(row["Номер заказа"]);
                string currentComment = row["Комментарий"] != DBNull.Value ? row["Комментарий"].ToString() : string.Empty;
                string comment = Interaction.InputBox("Введите комментарий к заказу:", "Добавить комментарий", currentComment);

                if (string.IsNullOrWhiteSpace(comment))
                {
                    MessageBox.Show("Комментарий не может быть пустым!");
                    return;
                }

                if (comment.Length > 500)
                {
                    MessageBox.Show("Комментарий не может быть длиннее 500 символов!");
                    return;
                }

                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT pm11.add_order_comment(@id, @comment)", conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("comment", comment);
                cmd.ExecuteNonQuery();
                LoadOrders();
                MessageBox.Show("Комментарий добавлен!");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка добавления комментария: {ex.Message}");
            }
        }

        private string GetCurrentUserFullName(int userId)
        {
            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT pm11.get_user_full_name(@userId)", conn);
                cmd.Parameters.AddWithValue("userId", userId);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "Неизвестный пользователь";
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка получения данных пользователя: {ex.Message}");
                return "Неизвестный пользователь";
            }
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (dateStart.SelectedDate == null || dateEnd.SelectedDate == null)
            {
                MessageBox.Show("Выберите начальную и конечные даты!");
                return;
            }

            if (dateStart.SelectedDate > dateEnd.SelectedDate)
            {
                MessageBox.Show("Начальная дата не может быть позже конечной!");
                return;
            }

            if (cmbReportStatus.SelectedItem is not ComboBoxItem statusItem)
            {
                MessageBox.Show("Выберите статус!");
                return;
            }

            string selectedStatus = statusItem.Content.ToString();
            if (!new[] { "Создан", "В процессе", "Завершён", "Отменён" }.Contains(selectedStatus))
            {
                MessageBox.Show("Выберите корректный статус!");
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM pm11.get_orders_by_status(@status::pm11.order_status, @start, @end)", conn);
                cmd.Parameters.AddWithValue("status", selectedStatus);
                cmd.Parameters.Add(new NpgsqlParameter("start", NpgsqlDbType.Date) { Value = dateStart.SelectedDate.Value.Date }); // Явно указываем тип date
                cmd.Parameters.Add(new NpgsqlParameter("end", NpgsqlDbType.Date) { Value = dateEnd.SelectedDate.Value.Date }); // Явно указываем тип date

                using var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта за выбранный период.");
                    return;
                }

                // Диагностика: выводим данные для проверки
                foreach (DataRow row in dt.Rows)
                {
                    System.Diagnostics.Debug.WriteLine($"Номер заказа: {row["Номер заказа"]}, Дата заказа: {row["Дата заказа"]}");
                }

                // Путь к папке "Загрузки"
                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(downloadsPath))
                    downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"ОтчётПоЗаказам_{DateTime.Now:yyyyMMdd}.xlsx",
                    DefaultExt = ".xlsx",
                    Filter = "Файлы Excel (*.xlsx)|*.xlsx",
                    InitialDirectory = downloadsPath
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    MessageBox.Show("Экспорт отменён.");
                    return;
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Отчёт по заказам");

                // Заголовок отчёта
                worksheet.Cells[1, 1].Value = "Отчёт по заказам";
                worksheet.Cells[1, 1, 1, dt.Columns.Count].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;

                // Период
                worksheet.Cells[2, 1].Value = $"Период: {dateStart.SelectedDate.Value:dd.MM.yyyy} - {dateEnd.SelectedDate.Value:dd.MM.yyyy}";
                worksheet.Cells[2, 1, 2, dt.Columns.Count].Merge = true;
                worksheet.Cells[2, 1].Style.Font.Italic = true;

                // Кто сделал отчёт и дата
                string userFullName = GetCurrentUserFullName(UserId);
                string reportDate = DateTime.Now.ToString("dd.MM.yyyy");
                worksheet.Cells[3, 1].Value = $"Составил: {userFullName}, Дата: {reportDate}";
                worksheet.Cells[3, 1, 3, dt.Columns.Count].Merge = true;
                worksheet.Cells[3, 1].Style.Font.Italic = true;

                // Загружаем данные
                worksheet.Cells[5, 1].LoadFromDataTable(dt, true);

                // Форматируем столбец "Дата заказа" (столбец 3)
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var cell = worksheet.Cells[i + 6, 3];
                    var dateValue = dt.Rows[i]["Дата заказа"];
                    if (dateValue != DBNull.Value && dateValue != null)
                    {
                        cell.Style.Numberformat.Format = "dd.MM.yyyy";
                        cell.Value = Convert.ToDateTime(dateValue);
                    }
                    else
                    {
                        cell.Value = "Нет даты";
                    }
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                File.WriteAllBytes(saveFileDialog.FileName, package.GetAsByteArray());
                MessageBox.Show($"Отчёт экспортирован: {saveFileDialog.FileName}");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка генерации отчёта: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Вы вышли из аккаунта!");
            this.NavigationService.Navigate(new LoginPage());
        }
    }
}