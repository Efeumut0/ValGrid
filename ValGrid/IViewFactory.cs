using System.Windows;

namespace ValGrid;

public interface IViewFactory
{
    FrameworkElement? ResolveView(object viewModel);
}

