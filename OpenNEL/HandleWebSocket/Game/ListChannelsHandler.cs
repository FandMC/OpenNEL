using OpenNEL.network;
using OpenNEL.type; 
using System.Text.Json;
using OpenNEL.Entities.Web.NEL;
using OpenNEL.Manager;

namespace OpenNEL.HandleWebSocket.Game;

internal class ListChannelsHandler : IWsHandler
{
    public string Type => "list_channels";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        List<EntityQueryGameSessions> list = (from interceptor in GameManager.Instance.GetQueryInterceptors()
            select new EntityQueryGameSessions
            {
                Id = "interceptor-" + interceptor.Id,
                ServerName = interceptor.Server,
                Guid = interceptor.Name.ToString(),
                CharacterName = interceptor.Role,
                ServerVersion = interceptor.Version,
                StatusText = "Running",
                ProgressValue = 0,
                Type = "Interceptor",
                LocalAddress = interceptor.LocalAddress
            }).ToList();

        return new { type = "channels", list };
    }
}
