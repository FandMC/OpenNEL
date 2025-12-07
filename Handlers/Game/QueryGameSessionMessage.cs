using System.Collections.Generic;
using System.Linq;
using OpenNEL.type;
using OpenNEL.Entities.Web.NEL;
using OpenNEL.Manager;

namespace OpenNEL_WinUI.Handlers.Game;

public class QueryGameSession
{
    public object Execute()
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
        return new { type = "query_game_session", items = list };
    }
}
