using Npgsql;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace AdvertisingCampaignApp
{
    public partial class RegistrationPage : Page
    {
        private MainWindow mainWindow;

        public RegistrationPage()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show("Введите фамилию!");
                return;
            }
            if (txtLastName.Text.Length > 50)
            {
                MessageBox.Show("Фамилия не может быть длиннее 50 символов!");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                MessageBox.Show("Введите имя!");
                return;
            }
            if (txtFirstName.Text.Length > 50)
            {
                MessageBox.Show("Имя не может быть длиннее 50 символов!");
                return;
            }

            if (!string.IsNullOrEmpty(txtPatronymic.Text) && txtPatronymic.Text.Length > 50)
            {
                MessageBox.Show("Отчество не может быть длиннее 50 символов!");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Введите номер телефона!");
                return;
            }
            if (!Regex.IsMatch(txtPhone.Text, @"^\+?[0-9]{11}$"))
            {
                MessageBox.Show("Номер телефона должен содержать 11 цифр (например, +79991234567)!");
                return;
            }

            if (!string.IsNullOrEmpty(txtCompany.Text) && txtCompany.Text.Length > 100)
            {
                MessageBox.Show("Название компании не может быть длиннее 100 символов!");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                MessageBox.Show("Введите логин!");
                return;
            }
            if (txtLogin.Text.Length > 50)
            {
                MessageBox.Show("Логин не может быть длиннее 50 символов!");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Введите пароль!");
                return;
            }
            if (txtPassword.Password.Length < 8 || txtPassword.Password.Length > 50)
            {
                MessageBox.Show("Пароль должен быть от 8 до 50 символов!");
                return;
            }
            if (!Regex.IsMatch(txtPassword.Password, @"^(?!.*\s)(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*]).+$"))
            {
                MessageBox.Show("Пароль должен содержать:\n- минимум одну заглавную букву\n- минимум одну строчную букву\n- минимум одну цифру\n- минимум один специальный символ (!@#$%^&*)\n- не должен содержать пробелы");
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(DatabaseConfig.GetConnectionString());
                conn.Open();

                var cmd = new NpgsqlCommand("pm11.register_user", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_last_name", txtLastName.Text);
                cmd.Parameters.AddWithValue("p_first_name", txtFirstName.Text);
                cmd.Parameters.AddWithValue("p_patronymic", string.IsNullOrEmpty(txtPatronymic.Text) ? (object)DBNull.Value : txtPatronymic.Text);
                cmd.Parameters.AddWithValue("p_phone_number", txtPhone.Text);
                cmd.Parameters.AddWithValue("p_company", string.IsNullOrEmpty(txtCompany.Text) ? (object)DBNull.Value : txtCompany.Text);
                cmd.Parameters.AddWithValue("p_login", txtLogin.Text);
                cmd.Parameters.AddWithValue("p_password", txtPassword.Password);

                var userIdParam = new NpgsqlParameter("p_user_id", NpgsqlTypes.NpgsqlDbType.Integer) { Direction = ParameterDirection.Output };
                var roleIdParam = new NpgsqlParameter("p_role_id", NpgsqlTypes.NpgsqlDbType.Integer) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(userIdParam);
                cmd.Parameters.Add(roleIdParam);

                cmd.ExecuteNonQuery();

                int userId = (int)userIdParam.Value;
                int roleId = (int)roleIdParam.Value;


                MessageBox.Show("Клиент успешно зарегистрирован и вошёл в систему!");
                mainWindow.SetUser(userId, "Клиент");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}\nКод ошибки: {ex.SqlState}\nСтек вызовов: {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Общая ошибка: {ex.Message}\nСтек вызовов: {ex.StackTrace}");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.NavigateTo(new LoginPage());
        }
    }
}