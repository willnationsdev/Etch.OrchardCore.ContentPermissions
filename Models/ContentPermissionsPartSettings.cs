namespace Etch.OrchardCore.ContentPermissions.Models
{
    public class ContentPermissionsPartSettings
    {
        public string RedirectUrl { get; set; }

        public bool BlockAdminEditAccess { get; set; }
        public string AdminRedirectUrl { get; set; }

        public bool HasRedirectUrl
        {
            get { return !string.IsNullOrWhiteSpace(RedirectUrl); }
        }

        public bool HasAdminRedirectUrl
        {
            get { return !string.IsNullOrWhiteSpace(AdminRedirectUrl); }
        }
    }
}
