using Npgsql;
using System.Windows;
using System.Windows.Controls;

namespace AdvertisingCampaignApp
{
    public partial class LoginPage : Page
    {
        private MainWindow mainWindow;

        public LoginPage()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                MessageBox.Show("Введите логин!");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Введите пароль!");
                return;
            }

            if (txtLogin.Text.Length > 50) // Предполагаемая максимальная длина логина
            {
                MessageBox.Show("Логин не может быть длиннее 50 символов!");
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT user_id, role_name FROM pm11.authenticate_user(@login, @password)", conn);
                cmd.Parameters.AddWithValue("login", txtLogin.Text);
                cmd.Parameters.AddWithValue("password", txtPassword.Password);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    int userId = reader.GetInt32(reader.GetOrdinal("user_id"));
                    string role = reader.GetString(reader.GetOrdinal("role_name"));
                    mainWindow.SetUser(userId, role);
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль!");
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}\nКод ошибки: {ex.ErrorCode}");
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.NavigateTo(new RegistrationPage());
        }
    }
}