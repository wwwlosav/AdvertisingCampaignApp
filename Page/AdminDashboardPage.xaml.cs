using Npgsql;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using OfficeOpenXml;
using System.IO;
using LiveCharts;
using LiveCharts.Wpf;

namespace AdvertisingCampaignApp
{
    public partial class AdminDashboardPage : Page
    {
        private int UserId;

        public AdminDashboardPage(int userId)
        {
            InitializeComponent();
            UserId = userId;
            LoadUsers();
            LoadTariffs();
        }

        private void LoadUsers()
        {
            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM pm11.get_users()", conn);
                using var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgUsers.ItemsSource = dt.DefaultView;
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}");
            }
        }

        private void LoadTariffs()
        {
            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM pm11.get_tariffs()", conn);
                using var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgTariffs.ItemsSource = dt.DefaultView;
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка загрузки тарифов: {ex.Message}");
            }
        }

        private void EditRole_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите пользователя!");
                return;
            }

            int id = Convert.ToInt32(row["Идентификатор"]);
            string currentRole = row["Роль"].ToString();
            string newRole = Microsoft.VisualBasic.Interaction.InputBox("Введите новую роль (Администратор, Сотрудник, Клиент):", "Изменение роли", currentRole);

            if (string.IsNullOrWhiteSpace(newRole))
            {
                MessageBox.Show("Роль не может быть пустой!");
                return;
            }

            if (newRole != currentRole)
            {
                if (!new[] { "Администратор", "Сотрудник", "Клиент" }.Contains(newRole))
                {
                    MessageBox.Show("Роль должна быть: Администратор, Сотрудник или Клиент!");
                    return;
                }

                try
                {
                    using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                    conn.Open();
                    var cmd = new NpgsqlCommand("SELECT pm11.update_user_role(@userId, @newRole)", conn);
                    cmd.Parameters.AddWithValue("userId", id);
                    cmd.Parameters.AddWithValue("newRole", newRole);
                    cmd.ExecuteNonQuery();
                    LoadUsers();
                    MessageBox.Show("Роль успешно изменена!");
                }
                catch (NpgsqlException ex)
                {
                    MessageBox.Show($"Ошибка изменения роли: {ex.Message}");
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите пользователя!");
                return;
            }

            int id = Convert.ToInt32(row["Идентификатор"]);
            string userName = $"{row["Фамилия"]} {row["Имя"]}";
            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя {userName}?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT pm11.delete_user(@userId)", conn);
                cmd.Parameters.AddWithValue("userId", id);
                cmd.ExecuteNonQuery();
                LoadUsers();
                MessageBox.Show("Пользователь удалён!");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}");
            }
        }

        private void AddType_Click(object sender, RoutedEventArgs e)
        {
            string newType = Microsoft.VisualBasic.Interaction.InputBox("Введите новый тип рекламы:", "Добавление типа");
            if (string.IsNullOrWhiteSpace(newType))
            {
                MessageBox.Show("Тип рекламы не может быть пустым!");
                return;
            }

            if (newType.Length > 50)
            {
                MessageBox.Show("Тип рекламы не может быть длиннее 50 символов!");
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT pm11.add_ad_type(@typeName)", conn);
                cmd.Parameters.AddWithValue("typeName", newType);
                cmd.ExecuteNonQuery();
                LoadTariffs();
                MessageBox.Show("Тип добавлен!");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка добавления типа: {ex.Message}");
            }
        }

        private void DeleteType_Click(object sender, RoutedEventArgs e)
        {
            if (dgTariffs.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите тип для удаления!");
                return;
            }

            int id = Convert.ToInt32(row["Идентификатор"]);
            string confirm = Microsoft.VisualBasic.Interaction.InputBox("Вы уверены, что хотите удалить тип рекламы?\nВведите 'Да' для подтверждения:", "Подтверждение удаления", "");
            if (confirm.ToLower() != "да")
            {
                MessageBox.Show("Удаление отменено.");
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT pm11.delete_ad_type(@id)", conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.ExecuteNonQuery();
                LoadTariffs();
                MessageBox.Show("Тип удалён!");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка удаления типа: {ex.Message}");
            }
        }

        private void UpdatePrice_Click(object sender, RoutedEventArgs e)
        {
            if (dgTariffs.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите тариф!");
                return;
            }

            int id = Convert.ToInt32(row["Идентификатор"]);
            string newPriceInput = Microsoft.VisualBasic.Interaction.InputBox("Введите новую цену:", "Изменение цены");
            if (!decimal.TryParse(newPriceInput, out decimal newPrice) || newPrice < 0)
            {
                MessageBox.Show("Введите корректное положительное значение цены!");
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT pm11.update_ad_type_price(@id, @price)", conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("price", newPrice);
                cmd.ExecuteNonQuery();
                LoadTariffs();
                MessageBox.Show("Цена обновлена!");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка обновления цены: {ex.Message}");
            }
        }

        private void ShowAnalytics_Click(object sender, RoutedEventArgs e)
        {
            if (dateStart.SelectedDate == null || dateEnd.SelectedDate == null)
            {
                MessageBox.Show("Выберите начальную и конечную даты!");
                return;
            }

            if (dateStart.SelectedDate > dateEnd.SelectedDate)
            {
                MessageBox.Show("Начальная дата не может быть позже конечной!");
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM pm11.get_ad_type_analytics(@start, @end)", conn);
                cmd.Parameters.AddWithValue("start", dateStart.SelectedDate.Value);
                cmd.Parameters.AddWithValue("end", dateEnd.SelectedDate.Value);
                using var reader = cmd.ExecuteReader();

                var seriesCollection = new SeriesCollection();
                while (reader.Read())
                {
                    var typeName = reader.GetString(0);
                    var orderCount = reader.GetInt64(1);
                    seriesCollection.Add(new PieSeries
                    {
                        Title = typeName,
                        Values = new ChartValues<int> { (int)orderCount },
                        DataLabels = true
                    });
                }

                if (seriesCollection.Count == 0)
                {
                    MessageBox.Show("Нет данных для отображения за выбранный период.");
                    pieChart.Series = new SeriesCollection();
                    return;
                }

                pieChart.Series = seriesCollection;
                pieChart.LegendLocation = LegendLocation.Right;
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка загрузки аналитики: {ex.Message}");
            }
        }

        private void CalculateRevenue_Click(object sender, RoutedEventArgs e)
        {
            if (dateStart.SelectedDate == null || dateEnd.SelectedDate == null)
            {
                MessageBox.Show("Выберите начальную и конечную даты!");
                return;
            }

            if (dateStart.SelectedDate > dateEnd.SelectedDate)
            {
                MessageBox.Show("Начальная дата не может быть позже конечной!");
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("CALL pm11.calculate_revenue(@start, @end, @total_revenue)", conn);
                cmd.Parameters.AddWithValue("start", dateStart.SelectedDate.Value);
                cmd.Parameters.AddWithValue("end", dateEnd.SelectedDate.Value);
                cmd.Parameters.Add(new NpgsqlParameter("total_revenue", NpgsqlTypes.NpgsqlDbType.Numeric) { Direction = ParameterDirection.InputOutput, Value = 0.00m });

                cmd.ExecuteNonQuery();

                decimal totalRevenue = Convert.ToDecimal(cmd.Parameters["total_revenue"].Value);
                MessageBox.Show($"Общий доход за период с {dateStart.SelectedDate.Value:dd.MM.yyyy} по {dateEnd.SelectedDate.Value:dd.MM.yyyy}: {totalRevenue:F2} руб.");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка расчёта дохода: {ex.Message}");
            }
        }

        private void FilterUsers(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string selectedRole = (filterRole.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (selectedRole != null && !new[] { "Все роли", "Администратор", "Сотрудник", "Клиент" }.Contains(selectedRole))
                {
                    MessageBox.Show("Выберите корректную роль!");
                    return;
                }

                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM pm11.get_users(@role)", conn);
                cmd.Parameters.AddWithValue("role", selectedRole ?? "Все роли");
                using var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgUsers.ItemsSource = dt.DefaultView;

                string searchText = searchUsers.Text?.ToLower() ?? string.Empty;
                if (!string.IsNullOrEmpty(searchText))
                {
                    dt.DefaultView.RowFilter = $"Логин LIKE '%{searchText}%' OR Роль LIKE '%{searchText}%'";
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка фильтрации пользователей: {ex.Message}");
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Вы вышли из аккаунта!");
            this.NavigationService.Navigate(new LoginPage());
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

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (dateStart.SelectedDate == null || dateEnd.SelectedDate == null)
            {
                MessageBox.Show("Выберите начальную и конечную даты!");
                return;
            }

            if (dateStart.SelectedDate > dateEnd.SelectedDate)
            {
                MessageBox.Show("Начальная дата не может быть позже конечной!");
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand("SELECT * FROM pm11.get_ad_analytics(@start, @end)", conn);
                cmd.Parameters.Add(new NpgsqlParameter("start", NpgsqlTypes.NpgsqlDbType.Date) { Value = dateStart.SelectedDate.Value.Date });
                cmd.Parameters.Add(new NpgsqlParameter("end", NpgsqlTypes.NpgsqlDbType.Date) { Value = dateEnd.SelectedDate.Value.Date });

                using var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта за выбранный период.");
                    return;
                }

                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(downloadsPath))
                    downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"ОтчётПоПродажам_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
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
                var worksheet = package.Workbook.Worksheets.Add("Отчёт по продажам");

                worksheet.Cells[1, 1].Value = "Отчёт по продажам";
                worksheet.Cells[1, 1, 1, dt.Columns.Count].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;

                worksheet.Cells[2, 1].Value = $"Период: {dateStart.SelectedDate.Value:dd.MM.yyyy} - {dateEnd.SelectedDate.Value:dd.MM.yyyy}";
                worksheet.Cells[2, 1, 2, dt.Columns.Count].Merge = true;
                worksheet.Cells[2, 1].Style.Font.Italic = true;

                string userFullName = GetCurrentUserFullName(UserId);
                string executionDateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                worksheet.Cells[3, 1].Value = $"Выполнил: {userFullName}, {executionDateTime}";
                worksheet.Cells[3, 1, 3, dt.Columns.Count].Merge = true;
                worksheet.Cells[3, 1].Style.Font.Italic = true;

                worksheet.Cells[5, 1].LoadFromDataTable(dt, true);
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                File.WriteAllBytes(saveFileDialog.FileName, package.GetAsByteArray());
                MessageBox.Show($"Отчёт экспортирован: {saveFileDialog.FileName}");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}");
            }
        }

        private void searchUsers_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterUsers(null, null);
        }

        private void searchTariffs_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTariffs.Text?.ToLower() ?? string.Empty;
            if (searchText.Length > 50)
            {
                MessageBox.Show("Поисковый запрос не может быть длиннее 50 символов!");
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM pm11.get_tariffs(@search)", conn);
                cmd.Parameters.AddWithValue("search", searchText);
                using var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgTariffs.ItemsSource = dt.DefaultView;
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка поиска тарифов: {ex.Message}");
            }
        }
    }
}