using Etch.OrchardCore.ContentPermissions.Models;
using Etch.OrchardCore.ContentPermissions.ViewModels;
using Microsoft.AspNetCore.Http;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Security.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Users.Services;
using OrchardCore.Users;
using Microsoft.AspNetCore.Identity;
using System.Threading;

namespace Etch.OrchardCore.ContentPermissions.Drivers
{
    public class ContentPermissionsDisplay : ContentPartDisplayDriver<ContentPermissionsPart>
    {
        #region Dependencies

        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRoleService _roleService;
        private readonly IUserService _userService;
        private readonly IUserRoleStore<IUser> _userRoleStore;

        #endregion

        #region Constructor

        public ContentPermissionsDisplay(IContentDefinitionManager contentDefinitionManager, IHttpContextAccessor httpContextAccessor, IRoleService roleService, IUserService userService, IUserRoleStore<IUser> userRoleStore)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _httpContextAccessor = httpContextAccessor;
            _roleService = roleService;
            _userService = userService;
            _userRoleStore = userRoleStore;
        }

        #endregion

        #region Overrides

        public override async Task<IDisplayResult> DisplayAsync(ContentPermissionsPart part, BuildPartDisplayContext context)
        {
            var user = await _userService.GetUserAsync(_httpContextAccessor.HttpContext.User.Identity.Name);

            if (context.DisplayType != "Detail" || !part.Enabled || await CanAccess(user, part.Roles))
            {
                return null;
            }

            var settings = GetSettings(part);
            var redirectUrl = settings.HasRedirectUrl ? settings.RedirectUrl : "/Error/403";

            if (!redirectUrl.StartsWith("/"))
            {
                redirectUrl = $"/{redirectUrl}";
            }

            _httpContextAccessor.HttpContext.Response.StatusCode = 403;
            _httpContextAccessor.HttpContext.Response.Redirect($"{_httpContextAccessor.HttpContext.Request.PathBase}{redirectUrl}", false);
            return null;
        }

        public override async Task<IDisplayResult> EditAsync(ContentPermissionsPart part, BuildPartEditorContext context)
        {
            var roles = await _roleService.GetRoleNamesAsync();


            return Initialize<ContentPermissionsPartEditViewModel>("ContentPermissionsPart_Edit", model =>
            {
                model.ContentPermissionsPart = part;
                model.Enabled = part.Enabled;
                model.PossibleRoles = roles.ToArray();
                model.Roles = part.Roles;
            })
            .Location("Parts#Security:10");
        }

        public override async Task<IDisplayResult> UpdateAsync(ContentPermissionsPart model, IUpdateModel updater, UpdatePartEditorContext context)
        {
            await updater.TryUpdateModelAsync(model, Prefix, m => m.Enabled, m => m.Roles);

            if (!model.Enabled)
            {
                model.Roles = Array.Empty<string>();
            }

            return Edit(model, context);
        }

        #endregion

        #region Helpers

        private async Task<bool> CanAccess(IUser user, string[] roles)
        {
            // Anonymous and Authenticated both don't get attached to a users account but are inferred
            // so we need to check those manually before we compare against the user's roles

            if (roles.Any(x => x.Equals("Anonymous", StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            if (user == null)
            {
                return false;
            }
            
            if (roles.Any(x => x.Equals("Authenticated", StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            foreach (var role in roles)
            {
                if(await _userRoleStore.IsInRoleAsync(user, role, CancellationToken.None))
                {
                    return true;
                }
            }

            return false;
        }

        private ContentPermissionsPartSettings GetSettings(ContentPermissionsPart part)
        {
            var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(part.ContentItem.ContentType);
            var contentTypePartDefinition = contentTypeDefinition.Parts.FirstOrDefault(x => string.Equals(x.PartDefinition.Name, nameof(ContentPermissionsPart)));
            return contentTypePartDefinition.GetSettings<ContentPermissionsPartSettings>();
        }

        #endregion
    }
}
