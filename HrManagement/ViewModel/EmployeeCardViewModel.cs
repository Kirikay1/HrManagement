using HrManagement.Model;
using System.Data.Entity;
using System.Collections.ObjectModel;
using System.Linq;

namespace HrManagement.ViewModel
{
    public class HrManagementPageViewModel : ViewModelBase
    {
        private ObservableCollection<EmployeeCardViewModel> employeeCards;

        public HrManagementPageViewModel()
        {
            LoadEmployees();
        }

        public ObservableCollection<EmployeeCardViewModel> EmployeeCards
        {
            get => employeeCards;
            private set
            {
                employeeCards = value;
                OnPropertyChanged();
            }
        }

        private void LoadEmployees()
        {
            var employees = AppData.db.Employee
                .Include(employee => employee.Department)
                .Include(employee => employee.Position)
                .ToList();

            EmployeeCards = new ObservableCollection<EmployeeCardViewModel>(
                employees.Select(employee => new EmployeeCardViewModel
                {
                    DepartmentName = employee.Department?.NameDepartment,
                    PositionName = employee.Position?.NamePosition,
                    FullName = employee.FullName,
                    WorkPhone = employee.WorkPhone,
                    Email = employee.Email,
                    EmployeeOffice = employee.EmployeeOffice
                }));
        }
    }
}
