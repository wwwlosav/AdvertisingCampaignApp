using AdvertisingCampaignApp;
using System.Windows;
using System.Windows.Controls;

namespace AdvertisingCampaignApp
{
    public partial class MainWindow : Window
    {
        private int UserId;
        private string Role;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                NavigateTo(new LoginPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке начальной страницы: {ex.Message}");
            }
        }

        public void NavigateTo(Page page)
        {
            mainFrame.Content = page;
        }

        public void SetUser(int userId, string role)
        {
            UserId = userId;
            Role = role;
            LoadRolePage();
        }

        private void LoadRolePage()
        {
            try
            {
                switch (Role)
                {
                    case "Администратор":
                        NavigateTo(new AdminDashboardPage(UserId));
                        break;
                    case "Сотрудник":
                        NavigateTo(new EmployeeDashboardPage(UserId));
                        break;
                    case "Клиент":
                        NavigateTo(new ClientDashboardPage(UserId));
                        break;
                    default:
                        MessageBox.Show("Неизвестная роль пользователя!");
                        NavigateTo(new LoginPage());
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке страницы роли: {ex.Message}");
            }
        }
    }
}