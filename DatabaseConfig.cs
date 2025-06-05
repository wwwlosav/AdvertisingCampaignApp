using System.Configuration;

namespace AdvertisingCampaignApp
{
    public static class DatabaseConfig
    {
        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["AdvertisingDbConnection"].ConnectionString;
        }
    }
}