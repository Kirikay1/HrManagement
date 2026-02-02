using HrManagement.Model;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace HrManagement.ViewModel
{
    public class HrManagementPageViewModel : ViewModelBase
    {
        private const int OfficeMaxLength = 10;
        private static readonly Regex PhoneRegex = new Regex(@"^[0-9+()\-\s#]{0,20}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly List<EmployeeCardViewModel> allEmployeeCards = new List<EmployeeCardViewModel>();
        private ObservableCollection<EmployeeCardViewModel> employeeCards;
        private ObservableCollection<Department> departments;
        private ObservableCollection<Position> positions;
        private ObservableCollection<EmployeeLookupViewModel> departmentEmployees;
        private EmployeeCardViewModel selectedEmployee;
        private bool isEmployeeCardOpen;
        private bool isEditing;
        private readonly ObservableCollection<string> validationErrors = new ObservableCollection<string>();

        public HrManagementPageViewModel()
        {
            LoadEmployees();
            LoadReferenceData();
            FilterByDepartmentCommand = new RelayCommand(parameter => FilterEmployeesByDepartment(parameter as string));
            OpenEmployeeCardCommand = new RelayCommand(parameter => OpenEmployeeCard(parameter as EmployeeCardViewModel));
            CloseEmployeeCardCommand = new RelayCommand(_ => CloseEmployeeCard());
            StartEditEmployeeCommand = new RelayCommand(_ => StartEditEmployee(), _ => SelectedEmployee != null && !IsEditing);
            CancelEditEmployeeCommand = new RelayCommand(_ => CancelEditEmployee(), _ => SelectedEmployee != null && IsEditing);
            SaveEmployeeCommand = new RelayCommand(_ => SaveEmployee(), _ => SelectedEmployee != null && IsEditing);
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

        public ObservableCollection<Department> Departments
        {
            get => departments;
            private set
            {
                departments = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Position> Positions
        {
            get => positions;
            private set
            {
                positions = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<EmployeeLookupViewModel> DepartmentEmployees
        {
            get => departmentEmployees;
            private set
            {
                departmentEmployees = value;
                OnPropertyChanged();
            }
        }

        public EmployeeCardViewModel SelectedEmployee
        {
            get => selectedEmployee;
            private set
            {
                if (selectedEmployee != null)
                {
                    selectedEmployee.PropertyChanged -= SelectedEmployee_PropertyChanged;
                }

                selectedEmployee = value;
                OnPropertyChanged();

                if (selectedEmployee != null)
                {
                    selectedEmployee.PropertyChanged += SelectedEmployee_PropertyChanged;
                    UpdateDepartmentEmployees(selectedEmployee.IdEmployeeDepartment);
                }
                else
                {
                    DepartmentEmployees = new ObservableCollection<EmployeeLookupViewModel>();
                }
            }
        }

        public bool IsEmployeeCardOpen
        {
            get => isEmployeeCardOpen;
            private set
            {
                isEmployeeCardOpen = value;
                OnPropertyChanged();
            }
        }

        public bool IsEditing
        {
            get => isEditing;
            private set
            {
                isEditing = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> ValidationErrors => validationErrors;

        public ICommand FilterByDepartmentCommand { get; }
        public ICommand OpenEmployeeCardCommand { get; }
        public ICommand CloseEmployeeCardCommand { get; }
        public ICommand StartEditEmployeeCommand { get; }
        public ICommand CancelEditEmployeeCommand { get; }
        public ICommand SaveEmployeeCommand { get; }

        private void LoadEmployees()
        {
            var employees = AppData.db.Employee
                .Include(employee => employee.Department)
                .Include(employee => employee.Position)
                .ToList();

            allEmployeeCards.Clear();
            allEmployeeCards.AddRange(employees.Select(employee => new EmployeeCardViewModel
            {
                Id = employee.Id,
                IdEmployeeDepartment = employee.IdEmployeeDepartment,
                IdPosition = employee.IdPosition,
                DirectSupervisor = employee.DirectSupervisor,
                AssistantEmployee = employee.AssistantEmployee,
                DepartmentName = employee.Department?.NameDepartment,
                PositionName = employee.Position?.NamePosition,
                FullName = employee.FullName,
                PersonalPhone = employee.PersonalPhone,
                BirthDate = employee.BirthDate,
                WorkPhone = employee.WorkPhone,
                Email = employee.Email,
                EmployeeOffice = employee.EmployeeOffice,
                Other = employee.Other
            }));

            EmployeeCards = new ObservableCollection<EmployeeCardViewModel>(allEmployeeCards);
        }

        private void LoadReferenceData()
        {
            Departments = new ObservableCollection<Department>(AppData.db.Department.ToList());
            Positions = new ObservableCollection<Position>(AppData.db.Position.ToList());
            DepartmentEmployees = new ObservableCollection<EmployeeLookupViewModel>();
        }

        public void FilterEmployeesByDepartment(string departmentName)
        {
            if (string.IsNullOrWhiteSpace(departmentName))
            {
                EmployeeCards = new ObservableCollection<EmployeeCardViewModel>(allEmployeeCards);
                return;
            }

            var filtered = allEmployeeCards
                .Where(employee => string.Equals(employee.DepartmentName, departmentName, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            EmployeeCards = new ObservableCollection<EmployeeCardViewModel>(filtered);
        }

        private void OpenEmployeeCard(EmployeeCardViewModel employee)
        {
            if (employee == null)
            {
                return;
            }

            SelectedEmployee = employee;
            IsEmployeeCardOpen = true;
            IsEditing = false;
            ValidationErrors.Clear();
        }

        private void CloseEmployeeCard()
        {
            ValidationErrors.Clear();
            IsEditing = false;
            IsEmployeeCardOpen = false;
        }

        private void StartEditEmployee()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            SelectedEmployee.BeginEdit();
            IsEditing = true;
        }

        private void CancelEditEmployee()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            SelectedEmployee.CancelEdit();
            ValidationErrors.Clear();
            IsEditing = false;
            UpdateDepartmentEmployees(SelectedEmployee.IdEmployeeDepartment);
        }

        private void SaveEmployee()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            if (!ValidateSelectedEmployee())
            {
                return;
            }

            var employee = AppData.db.Employee.FirstOrDefault(item => item.Id == SelectedEmployee.Id);
            if (employee == null)
            {
                ValidationErrors.Clear();
                ValidationErrors.Add("Сотрудник не найден в базе данных.");
                return;
            }

            employee.FullName = SelectedEmployee.FullName;
            employee.PersonalPhone = SelectedEmployee.PersonalPhone;
            employee.BirthDate = SelectedEmployee.BirthDate;
            employee.IdEmployeeDepartment = SelectedEmployee.IdEmployeeDepartment;
            employee.IdPosition = SelectedEmployee.IdPosition;
            employee.DirectSupervisor = SelectedEmployee.DirectSupervisor;
            employee.AssistantEmployee = SelectedEmployee.AssistantEmployee;
            employee.WorkPhone = SelectedEmployee.WorkPhone;
            employee.Email = SelectedEmployee.Email;
            employee.EmployeeOffice = SelectedEmployee.EmployeeOffice;
            employee.Other = SelectedEmployee.Other;

            var department = Departments.FirstOrDefault(item => item.Id == SelectedEmployee.IdEmployeeDepartment);
            var position = Positions.FirstOrDefault(item => item.Id == SelectedEmployee.IdPosition);
            SelectedEmployee.DepartmentName = department?.NameDepartment;
            SelectedEmployee.PositionName = position?.NamePosition;

            AppData.db.SaveChanges();
            ValidationErrors.Clear();
            IsEditing = false;
        }

        private bool ValidateSelectedEmployee()
        {
            ValidationErrors.Clear();

            if (SelectedEmployee == null)
            {
                ValidationErrors.Add("Карточка сотрудника не выбрана.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(SelectedEmployee.FullName))
            {
                ValidationErrors.Add("ФИО является обязательным полем.");
            }

            if (SelectedEmployee.IdEmployeeDepartment <= 0)
            {
                ValidationErrors.Add("Структурное подразделение является обязательным полем.");
            }

            if (SelectedEmployee.IdPosition <= 0)
            {
                ValidationErrors.Add("Должность является обязательным полем.");
            }

            if (!string.IsNullOrWhiteSpace(SelectedEmployee.PersonalPhone)
                && !PhoneRegex.IsMatch(SelectedEmployee.PersonalPhone))
            {
                ValidationErrors.Add("Мобильный телефон содержит недопустимые символы или превышает 20 символов.");
            }

            if (string.IsNullOrWhiteSpace(SelectedEmployee.WorkPhone))
            {
                ValidationErrors.Add("Рабочий телефон является обязательным полем.");
            }
            else if (!PhoneRegex.IsMatch(SelectedEmployee.WorkPhone))
            {
                ValidationErrors.Add("Рабочий телефон содержит недопустимые символы или превышает 20 символов.");
            }

            if (string.IsNullOrWhiteSpace(SelectedEmployee.Email))
            {
                ValidationErrors.Add("Электронная почта является обязательным полем.");
            }
            else if (!EmailRegex.IsMatch(SelectedEmployee.Email))
            {
                ValidationErrors.Add("Электронная почта должна быть в формате x@x.x.");
            }

            if (string.IsNullOrWhiteSpace(SelectedEmployee.EmployeeOffice))
            {
                ValidationErrors.Add("Кабинет является обязательным полем.");
            }
            else if (SelectedEmployee.EmployeeOffice.Length > OfficeMaxLength)
            {
                ValidationErrors.Add($"Кабинет не должен превышать {OfficeMaxLength} символов.");
            }

            return ValidationErrors.Count == 0;
        }

        private void SelectedEmployee_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EmployeeCardViewModel.IdEmployeeDepartment))
            {
                UpdateDepartmentEmployees(SelectedEmployee.IdEmployeeDepartment);
            }
        }

        private void UpdateDepartmentEmployees(int departmentId)
        {
            var employees = allEmployeeCards
                .Where(employee => employee.IdEmployeeDepartment == departmentId && employee.Id != SelectedEmployee?.Id)
                .Select(employee => new EmployeeLookupViewModel(employee.Id, employee.FullName))
                .OrderBy(employee => employee.FullName)
                .ToList();

            DepartmentEmployees = new ObservableCollection<EmployeeLookupViewModel>(employees);

            if (SelectedEmployee == null)
            {
                return;
            }

            if (SelectedEmployee.DirectSupervisor.HasValue
                && !DepartmentEmployees.Any(employee => employee.Id == SelectedEmployee.DirectSupervisor.Value))
            {
                SelectedEmployee.DirectSupervisor = null;
            }

            if (SelectedEmployee.AssistantEmployee.HasValue
                && !DepartmentEmployees.Any(employee => employee.Id == SelectedEmployee.AssistantEmployee.Value))
            {
                SelectedEmployee.AssistantEmployee = null;
            }
        }

        public class EmployeeLookupViewModel
        {
            public EmployeeLookupViewModel(int id, string fullName)
            {
                Id = id;
                FullName = fullName;
            }

            public int Id { get; }
            public string FullName { get; }
        }
    }
}