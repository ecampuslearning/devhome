// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Extensions;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets.Hosts;
using WinUIEx;

namespace DevHome.Dashboard.Views;
public sealed partial class CustomizeWidgetDialog : ContentDialog
{
    public Widget EditedWidget { get; set; }

    public WidgetViewModel ViewModel { get; set; }

    private readonly WidgetDefinition _widgetDefinition;
    private readonly WidgetHost _widgetHost;
    private readonly WidgetCatalog _widgetCatalog;
    private static DispatcherQueue _dispatcher;

    public CustomizeWidgetDialog(WidgetHost host, WidgetCatalog catalog, AdaptiveCardRenderer renderer, DispatcherQueue dispatcher, WidgetDefinition widgetDefinition)
    {
        ViewModel = new WidgetViewModel(null, Microsoft.Windows.Widgets.WidgetSize.Large, null, renderer, dispatcher);
        this.InitializeComponent();

        _widgetHost = host;
        _widgetCatalog = catalog;
        _widgetDefinition = widgetDefinition;
        _dispatcher = dispatcher;

        // Get the application root window so we know when it has closed.
        Application.Current.GetService<WindowEx>().Closed += OnMainWindowClosed;

        _widgetCatalog.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;

        this.Loaded += InitializeWidgetCustomization;
    }

    private async void InitializeWidgetCustomization(object sender, RoutedEventArgs e)
    {
        var size = WidgetHelpers.GetLargetstCapabilitySize(_widgetDefinition.GetWidgetCapabilities());

        // Create the widget for configuration. We will need to delete it if
        var widget = await _widgetHost.CreateWidgetAsync(_widgetDefinition.Id, size);
        Log.Logger()?.ReportInfo("CustomizeWidgetDialog", $"Created Widget {widget.Id}");

        ViewModel.Widget = widget;
    }

    private void UpdateWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Logger()?.ReportDebug("CustomizeWidgetDialog", $"Exiting dialog, updated widget");
        EditedWidget = ViewModel.Widget;
        HideDialog();
    }

    private async void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Logger()?.ReportDebug("CustomizeWidgetDialog", $"Exiting dialog, cancel button clicked");
        var widgetIdToDelete = ViewModel.Widget.Id;
        await ViewModel.Widget.DeleteAsync();
        Log.Logger()?.ReportInfo("CustomizeWidgetDialog", $"Deleted Widget {widgetIdToDelete}");

        EditedWidget = null;
        HideDialog();
    }

    private void HideDialog()
    {
        _widgetCatalog.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;
        this.Hide();
    }

    private async void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        Log.Logger()?.ReportInfo("CustomizeWidgetDialog", $"Window Closed, delete partially created widget");
        await ViewModel.Widget.DeleteAsync();
    }

    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        var deletedDefinitionId = args.DefinitionId;

        if (_widgetDefinition.Id.Equals(deletedDefinitionId, StringComparison.Ordinal))
        {
            Log.Logger()?.ReportDebug("CustomizeWidgetDialog", $"Exiting dialog, widget definition was deleted");
            EditedWidget = null;
            _widgetCatalog.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;
            _dispatcher.TryEnqueue(() =>
            {
                this.Hide();
            });
        }
    }
}
