using BatX3_HSS_GUI.Application.Communication.Command;
using BatX3_HSS_GUI.Application.Configuration.Runtime;
using BatX3_HSS_GUI.Application.Detection;
using BatX3_HSS_GUI.Application.Video;
using BatX3_HSS_GUI.Infrastructure.Communication.Command;
using BatX3_HSS_GUI.Infrastructure.Communication.Detection;
using BatX3_HSS_GUI.Infrastructure.Communication.Video;
using BatX3_HSS_GUI.Infrastructure.Configuration.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BatX3_HSS_GUI.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)

        {
            services.AddSingleton<UdpCommandService>();
            services.AddSingleton<UdpVideoFrameSource>();
            services.AddSingleton<UdpDetectionFrameSource>();

            services.AddSingleton<ICommandService>(provider => provider.GetRequiredService<UdpCommandService>());
            services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<UdpCommandService>());
            services.AddSingleton<IVideoFrameSource>(provider => provider.GetRequiredService<UdpVideoFrameSource>());
            services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<UdpVideoFrameSource>());
            services.AddSingleton<IDetectionFrameSource>(provider => provider.GetRequiredService<UdpDetectionFrameSource>());
            services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<UdpDetectionFrameSource>());
            services.AddSingleton<IRuntimeSettingsApplyService, RuntimeSettingsApplyService>();

            return services;
        }
    }
}