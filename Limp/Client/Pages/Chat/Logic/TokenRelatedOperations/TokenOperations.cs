using Limp.Client.HubInteraction.Handlers.Helpers;
using Limp.Client.Services.JWTReader;
using Microsoft.JSInterop;

namespace Limp.Client.Pages.Chat.Logic.TokenRelatedOperations
{
    public static class TokenOperations
    {
        public static async Task<string> ResolveMyUsername(IJSRuntime jSRuntime)
        {
            string? accessToken = await JWTHelper.GetAccessTokenAsync(jSRuntime);
            //Reading username from access-token
            string myUsername = TokenReader.GetUsernameFromAccessToken(accessToken);

            return myUsername;
        }
    }
}
