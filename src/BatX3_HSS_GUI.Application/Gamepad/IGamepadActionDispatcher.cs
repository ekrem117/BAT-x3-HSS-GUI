using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatX3_HSS_GUI.Application.Gamepad
{
    public interface IGamepadActionDispatcher
    {
        Task HandlePressedAsync(string actionId, CancellationToken cancellationToken = default);

        Task HandleReleasedAsync(string actionId, CancellationToken cancellationToken = default);
    }
}