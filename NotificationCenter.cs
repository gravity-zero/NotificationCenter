using System;
using System.Collections.Generic;
using Jellyfin.Plugin.NotificationCenter.Configuration;
using Jellyfin.Plugin.NotificationCenter.Data;
using Jellyfin.Plugin.NotificationCenter.EventHandlers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NotificationCenter;

public class NotificationCenterPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly MediaAddedHandler _mediaAddedHandler;
    private readonly ScriptInjector _scriptInjector;

    public NotificationCenterPlugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        ILoggerFactory loggerFactory)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
        _loggerFactory = loggerFactory;

        Repository = new NotificationRepository(
            applicationPaths,
            loggerFactory.CreateLogger<NotificationRepository>());

        _mediaAddedHandler = new MediaAddedHandler(
            _libraryManager,
            _userManager,
            _userDataManager,
            Repository,
            _loggerFactory);

        // Inject client script on startup
        _scriptInjector = new ScriptInjector(loggerFactory.CreateLogger<ScriptInjector>());
        _scriptInjector.InjectScript();
    }

    public override string Name => "NotificationCenter";
    public override Guid Id => Guid.Parse("eb5d7894-8eef-4b36-aa6f-5d124e828ce1");

    public static NotificationCenterPlugin? Instance { get; private set; }
    
    public NotificationRepository Repository { get; }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            }
        };
    }

}
