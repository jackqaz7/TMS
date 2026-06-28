using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CoreAPI.Services
{
    [Authorize]
    public class NotificationsHub : Hub
    {
    }
}
