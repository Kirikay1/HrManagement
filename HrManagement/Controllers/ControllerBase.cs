using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HrManagement.Controllers;

public abstract class ControllerBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
