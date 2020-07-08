namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithFilter : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public HiddenItem LastLoggedInCheck { get; private set; }
        public DisplayItem FilterExample { get; private set; }
        public StringItem FilterString { get; private set; }

        public ConfigurationDataBasicLoginWithFilter(string FilterInstructions)
        {
            Username = new StringItem { Name = "Username" };
            Password = new StringItem { Name = "Password" };
            LastLoggedInCheck = new HiddenItem { Name = "LastLoggedInCheck" };
            FilterExample = new DisplayItem(FilterInstructions)
            {
                Name = ""
            };
            FilterString = new StringItem { Name = "Filters (optional)" };
        }


    }
}
