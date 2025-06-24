using Npgsql;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using NpgsqlTypes;

namespace AdvertisingCampaignApp
{
    public partial class ClientDashboardPage : Page
    {
        private int UserId;

        public ClientDashboardPage(int userId)
        {
            InitializeComponent();
            UserId = userId;
            LoadAdTypes();
            LoadOrders();
        }

        private void LoadAdTypes()
        {
            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM pm11.get_ad_types()", conn);
                using var reader = cmd.ExecuteReader();
                var adTypes = new List<dynamic>();
                while (reader.Read())
                {
                    adTypes.Add(new { id = reader.GetInt32(0), type_name = reader.GetString(1), price = reader.GetDecimal(2) });
                }
                cmbAdType.ItemsSource = adTypes;
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка загрузки типов рекламы: {ex.Message}");
            }
        }

        private void LoadOrders()
        {
            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM pm11.get_client_orders(@userId)", conn);
                cmd.Parameters.AddWithValue("userId", UserId);
                using var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dgOrders.ItemsSource = dt.DefaultView;
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}");
            }
        }

        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            var selectedAdType = cmbAdType.SelectedItem as dynamic;
            if (selectedAdType == null)
            {
                MessageBox.Show("Выберите тип рекламы!");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtQuantity.Text) || !int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное положительное количество!");
                return;
            }

            try
            {
                decimal cost = selectedAdType.price * quantity;
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT pm11.create_order_and_ad(@userId, @orderDate, @cost, @quantity, @adTypeId, @completionDate)", conn);
                cmd.Parameters.AddWithValue("userId", UserId);
                cmd.Parameters.Add(new NpgsqlParameter("orderDate", NpgsqlDbType.Date) { Value = DateTime.Now.Date }); // Явно указываем тип date
                cmd.Parameters.AddWithValue("cost", cost);
                cmd.Parameters.AddWithValue("quantity", quantity);
                cmd.Parameters.AddWithValue("adTypeId", selectedAdType.id);
                cmd.Parameters.Add(new NpgsqlParameter("completionDate", NpgsqlDbType.Date) { Value = DateTime.Now.AddDays(10).Date }); // Явно указываем тип date
                int newOrderId = (int)cmd.ExecuteScalar();

                MessageBox.Show($"Заказ успешно создан! Номер заказа: {newOrderId}");
                LoadOrders();
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка создания заказа: {ex.Message}\nКод ошибки: {ex.SqlState}\nСтек вызовов: {ex.StackTrace}");
            }
        }

        private void cmbAdType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCostPreview();
        }

        private void txtQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                txtCostPreview.Text = "Введите корректное количество!";
                return;
            }

            UpdateCostPreview();
        }

        private void UpdateCostPreview()
        {
            var selectedAdType = cmbAdType.SelectedItem as dynamic;
            int quantity = 0;
            int parsedQuantity = 0;

            if (selectedAdType != null && int.TryParse(txtQuantity.Text, out parsedQuantity) && parsedQuantity > 0)
            {
                quantity = parsedQuantity;
            }

            decimal cost = selectedAdType?.price * quantity ?? 0;
            txtCostPreview.Text = $"Стоимость: {cost:F2} руб.";
        }

        private void ShowOrderDetails_Click(object sender, RoutedEventArgs e)
        {
            string sqlQuery = "SELECT * FROM pm11.get_order_details(@orderId)";
            if (dgOrders.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите заказ для просмотра подробностей!");
                return;
            }

            try
            {
                int orderId = Convert.ToInt32(row["Номер заказа"]);
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand(sqlQuery, conn);
                cmd.Parameters.AddWithValue("orderId", orderId);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string comment = reader["Комментарий"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Комментарий")) : "Нет комментария";
                    string details = $"Номер заказа: {reader.GetInt32(reader.GetOrdinal("Номер заказа"))}\n" +
                                     $"Дата заказа: {reader.GetDateTime(reader.GetOrdinal("Дата заказа")).ToString("dd.MM.yyyy")}\n" +
                                     $"Дата завершения: {reader.GetDateTime(reader.GetOrdinal("Дата завершения")).ToString("dd.MM.yyyy")}\n" +
                                     $"Тип рекламы: {reader.GetString(reader.GetOrdinal("Тип рекламы"))}\n" +
                                     $"Стоимость: {reader.GetDecimal(reader.GetOrdinal("Стоимость"))} руб.\n" +
                                     $"Количество: {reader.GetInt32(reader.GetOrdinal("Количество"))}\n" +
                                     $"Статус: {reader.GetString(reader.GetOrdinal("Статус"))}\n" +
                                     $"Комментарий: {comment}";
                    MessageBox.Show(details, "Подробности заказа");
                }
                else
                {
                    MessageBox.Show("Данные о заказе не найдены!");
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка получения данных: {ex.Message}\nКод ошибки: {ex.ErrorCode}\nSQL: {sqlQuery}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Общая ошибка: {ex.Message}\nСтек вызовов: {ex.StackTrace}");
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Вы вышли из аккаунта!");
            this.NavigationService.Navigate(new LoginPage());
        }
    }
}