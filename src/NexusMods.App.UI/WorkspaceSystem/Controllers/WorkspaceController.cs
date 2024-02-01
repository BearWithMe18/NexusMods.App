using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Windows;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

internal sealed class WorkspaceController : ReactiveObject, IWorkspaceController
{
    private readonly IWorkspaceWindow _window;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkspaceController> _logger;

    public WindowId WindowId => _window.WindowId;

    private readonly SourceCache<WorkspaceViewModel, WorkspaceId> _workspaces = new(x => x.Id);
    private readonly ReadOnlyObservableCollection<IWorkspaceViewModel> _allWorkspaces;
    public ReadOnlyObservableCollection<IWorkspaceViewModel> AllWorkspaces => _allWorkspaces;

    [Reactive] public IWorkspaceViewModel ActiveWorkspace { get; private set; } = null!;

    public WorkspaceController(IWorkspaceWindow window, IServiceProvider serviceProvider)
    {
        _window = window;

        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<WorkspaceController>>();

        _workspaces
            .Connect()
            .Transform(x => (IWorkspaceViewModel)x)
            .Bind(out _allWorkspaces)
            .Subscribe();
    }

    public IWorkspaceViewModel CreateWorkspace(Optional<IWorkspaceContext> context, Optional<PageData> pageData)
    {
        var vm = new WorkspaceViewModel(
            workspaceController: this,
            factoryController: _serviceProvider.GetRequiredService<PageFactoryController>(),
            unregisterFunc: UnregisterWorkspace
        )
        {
            Context = context.HasValue ? context.Value : EmptyContext.Instance
        };

        _workspaces.AddOrUpdate(vm);

        var addPanelBehavior = pageData.HasValue
            ? new AddPanelBehavior(new AddPanelBehavior.WithCustomTab(pageData.Value))
            : new AddPanelBehavior(new AddPanelBehavior.WithDefaultTab());

        vm.AddPanel(
            WorkspaceGridState.From(new[]
            {
                new PanelGridState(PanelId.DefaultValue, MathUtils.One)
            }, isHorizontal: vm.IsHorizontal),
            addPanelBehavior
        );

        return vm;
    }

    private void UnregisterWorkspace(WorkspaceViewModel workspaceViewModel)
    {
        _workspaces.Remove(workspaceViewModel.Id);

        if (ReferenceEquals(ActiveWorkspace, workspaceViewModel))
        {
            ChangeActiveWorkspace(AllWorkspaces.First().Id);
        }
    }

    private bool TryGetWorkspace(WorkspaceId workspaceId, [NotNullWhen(true)] out WorkspaceViewModel? workspaceViewModel)
    {
        if (!_workspaces.Lookup(workspaceId).TryGet(out workspaceViewModel))
        {
            _logger.LogError("Failed to retrieve the Workspace View Model with the ID {WorkspaceID} referenced by the WeakReference", workspaceId);
            return false;
        }

        return true;
    }

    private bool TryGetPanel(IWorkspaceViewModel workspaceViewModel, PanelId panelId, [NotNullWhen(true)] out IPanelViewModel? panelViewModel)
    {
        panelViewModel = workspaceViewModel.Panels.FirstOrDefault(panel => panel.Id == panelId);
        if (panelViewModel is null)
        {
            _logger.LogError("Failed to find Panel with ID {PanelID} in Workspace with ID {WorkspaceID}", panelId, workspaceViewModel.Id);
            return false;
        }

        return true;
    }

    private bool TryGetTab(IPanelViewModel panelViewModel, PanelTabId tabId, [NotNullWhen(true)] out IPanelTabViewModel? tabViewModel)
    {
        tabViewModel = panelViewModel.Tabs.FirstOrDefault(tab => tab.Id == tabId);
        if (tabViewModel is null)
        {
            _logger.LogError("Failed to find Tab with ID {TabID} in Panel with ID {PanelID} in Workspace with ID {WorkspaceID}", tabId, panelViewModel.Id, panelViewModel.WorkspaceId);
            return false;
        }

        return true;
    }

    public void ChangeActiveWorkspace(WorkspaceId workspaceId)
    {
        foreach (var workspace in AllWorkspaces)
        {
            if (workspace.Id == workspaceId)
            {
                workspace.IsActive = true;
                ActiveWorkspace = workspace;
            }
            else
            {
                workspace.IsActive = false;
            }
        }
    }

    public void AddPanel(WorkspaceId workspaceId, WorkspaceGridState newWorkspaceState, AddPanelBehavior behavior)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        workspaceViewModel.AddPanel(newWorkspaceState, behavior);
    }

    public void OpenPage(WorkspaceId workspaceId, PageData pageData, OpenPageBehavior behavior)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        workspaceViewModel.OpenPage(pageData, behavior);
    }

    public void SwapPanels(WorkspaceId workspaceId, PanelId firstPanelId, PanelId secondPanelId)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        workspaceViewModel.SwapPanels(firstPanelId, secondPanelId);
    }

    public void ClosePanel(WorkspaceId workspaceId, PanelId panelToClose)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        workspaceViewModel.ClosePanel(panelToClose);
    }

    public void SetTabTitle(string title, WorkspaceId workspaceId, PanelId panelId, PanelTabId tabId)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        if (!TryGetPanel(workspaceViewModel, panelId, out var panelViewModel)) return;
        if (!TryGetTab(panelViewModel, tabId, out var tabViewModel)) return;

        tabViewModel.Header.Title = title;
    }

    public void SetIcon(IImage? icon, WorkspaceId workspaceId, PanelId panelId, PanelTabId tabId)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        if (!TryGetPanel(workspaceViewModel, panelId, out var panelViewModel)) return;
        if (!TryGetTab(panelViewModel, tabId, out var tabViewModel)) return;

        tabViewModel.Header.Icon = icon;
    }
}
