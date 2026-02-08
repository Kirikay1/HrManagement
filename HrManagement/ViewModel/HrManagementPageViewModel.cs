using HrManagement.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;

namespace HrManagement.ViewModel
{
    public class HrManagementPageViewModel : ViewModelBase
    {
        private const int OfficeMaxLength = 10;
        private const int NewEmployeeTemporaryKey = -1;
        private static readonly Regex PhoneRegex = new Regex(@"^[0-9+()\-\s#]{0,20}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly List<EmployeeCardViewModel> allEmployeeCards = new List<EmployeeCardViewModel>();
        private readonly Dictionary<int, ObservableCollection<EmployeeEventViewModel>> employeeEventsById = new Dictionary<int, ObservableCollection<EmployeeEventViewModel>>();
        private readonly ObservableCollection<string> eventTypeOptions = new ObservableCollection<string> { "Обучение", "Отгул", "Отпуск" };
        private ObservableCollection<EmployeeCardViewModel> employeeCards;
        private ObservableCollection<Department> departments;
        private ObservableCollection<Position> positions;
        private ObservableCollection<EmployeeLookupViewModel> departmentEmployees;
        private ObservableCollection<EmployeeEventViewModel> employeeEvents;
        private ICollectionView employeeEventsView;
        private EmployeeCardViewModel selectedEmployee;
        private bool isEmployeeCardOpen;
        private bool isEditing;
        private bool isNewEmployee;
        private string selectedDepartmentName;
        private readonly ObservableCollection<string> validationErrors = new ObservableCollection<string>();
        private readonly ObservableCollection<string> eventValidationErrors = new ObservableCollection<string>();
        private string selectedEventType;
        private DateTime? newEventStartDate = DateTime.Today;
        private DateTime? newEventEndDate = DateTime.Today;
        private string newEventReason;
        private bool showPastEvents;
        private bool showCurrentEvents = true;
        private bool showFutureEvents = true;
        private int currentEmployeeKey = NewEmployeeTemporaryKey;

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
            AddEmployeeCommand = new RelayCommand(_ => AddEmployee());
            AddEmployeeEventCommand = new RelayCommand(_ => AddEmployeeEvent(), _ => SelectedEmployee != null);
            DeleteEmployeeEventCommand = new RelayCommand(parameter => DeleteEmployeeEvent(parameter as EmployeeEventViewModel), _ => SelectedEmployee != null);
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

        public ObservableCollection<EmployeeEventViewModel> EmployeeEvents
        {
            get => employeeEvents;
            private set
            {
                employeeEvents = value;
                OnPropertyChanged();
            }
        }

        public ICollectionView EmployeeEventsView
        {
            get => employeeEventsView;
            private set
            {
                employeeEventsView = value;
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
                    LoadEmployeeEvents(selectedEmployee);
                }
                else
                {
                    DepartmentEmployees = new ObservableCollection<EmployeeLookupViewModel>();
                    LoadEmployeeEvents(null);
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

        public bool IsNewEmployee
        {
            get => isNewEmployee;
            private set
            {
                isNewEmployee = value;
                OnPropertyChanged();
            }
        }

        public string SelectedDepartmentName
        {
            get => selectedDepartmentName;
            private set
            {
                selectedDepartmentName = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> ValidationErrors => validationErrors;

        public ObservableCollection<string> EventValidationErrors => eventValidationErrors;
        public ObservableCollection<string> EventTypeOptions => eventTypeOptions;

        public string SelectedEventType
        {
            get => selectedEventType;
            set
            {
                selectedEventType = value;
                OnPropertyChanged();
            }
        }

        public DateTime? NewEventStartDate
        {
            get => newEventStartDate;
            set
            {
                newEventStartDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime? NewEventEndDate
        {
            get => newEventEndDate;
            set
            {
                newEventEndDate = value;
                OnPropertyChanged();
            }
        }

        public string NewEventReason
        {
            get => newEventReason;
            set
            {
                newEventReason = value;
                OnPropertyChanged();
            }
        }

        public bool ShowPastEvents
        {
            get => showPastEvents;
            set
            {
                showPastEvents = value;
                OnPropertyChanged();
                RefreshEmployeeEventsView();
            }
        }

        public bool ShowCurrentEvents
        {
            get => showCurrentEvents;
            set
            {
                showCurrentEvents = value;
                OnPropertyChanged();
                RefreshEmployeeEventsView();
            }
        }

        public bool ShowFutureEvents
        {
            get => showFutureEvents;
            set
            {
                showFutureEvents = value;
                OnPropertyChanged();
                RefreshEmployeeEventsView();
            }
        }

        public ICommand FilterByDepartmentCommand { get; }
        public ICommand OpenEmployeeCardCommand { get; }
        public ICommand CloseEmployeeCardCommand { get; }
        public ICommand StartEditEmployeeCommand { get; }
        public ICommand CancelEditEmployeeCommand { get; }
        public ICommand SaveEmployeeCommand { get; }
        public ICommand AddEmployeeCommand { get; }
        public ICommand AddEmployeeEventCommand { get; }
        public ICommand DeleteEmployeeEventCommand { get; }

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
            SelectedDepartmentName = string.IsNullOrWhiteSpace(departmentName) ? null : departmentName;
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
            IsNewEmployee = false;
            ValidationErrors.Clear();
            EventValidationErrors.Clear();
        }

        private void CloseEmployeeCard()
        {
            ValidationErrors.Clear();
            EventValidationErrors.Clear();
            IsEditing = false;
            IsEmployeeCardOpen = false;
            IsNewEmployee = false;
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
            if (IsNewEmployee)
            {
                IsEmployeeCardOpen = false;
                IsNewEmployee = false;
                SelectedEmployee = null;
                return;
            }
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

            var isCreating = IsNewEmployee || SelectedEmployee.Id <= 0;
            var employee = isCreating
                ? new Employee()
                : AppData.db.Employee.FirstOrDefault(item => item.Id == SelectedEmployee.Id);
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

            if (isCreating)
            {
                AppData.db.Employee.Add(employee);
            }

            AppData.db.SaveChanges();
            if (isCreating)
            {
                SelectedEmployee.Id = employee.Id;
                allEmployeeCards.Add(SelectedEmployee);
                ApplyDepartmentFilter();
                CommitNewEmployeeEvents(SelectedEmployee.Id);
            }
            IsNewEmployee = false;
            ValidationErrors.Clear();
            EventValidationErrors.Clear();
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

        private void AddEmployee()
        {
            var preselectedDepartment = Departments
                .FirstOrDefault(item => string.Equals(item.NameDepartment, SelectedDepartmentName, System.StringComparison.OrdinalIgnoreCase));

            SelectedEmployee = new EmployeeCardViewModel
            {
                Id = 0,
                DepartmentName = preselectedDepartment?.NameDepartment,
                IdEmployeeDepartment = preselectedDepartment?.Id ?? 0
            };

            IsEmployeeCardOpen = true;
            IsEditing = true;
            IsNewEmployee = true;
            ValidationErrors.Clear();
            EventValidationErrors.Clear();
        }

        private void ApplyDepartmentFilter()
        {
            if (string.IsNullOrWhiteSpace(SelectedDepartmentName))
            {
                EmployeeCards = new ObservableCollection<EmployeeCardViewModel>(allEmployeeCards);
                return;
            }

            var filtered = allEmployeeCards
                .Where(employee => string.Equals(employee.DepartmentName, SelectedDepartmentName, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            EmployeeCards = new ObservableCollection<EmployeeCardViewModel>(filtered);
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

        private void LoadEmployeeEvents(EmployeeCardViewModel employee)
        {
            EventValidationErrors.Clear();
            if (employee == null)
            {
                EmployeeEvents = new ObservableCollection<EmployeeEventViewModel>();
                EmployeeEventsView = null;
                currentEmployeeKey = NewEmployeeTemporaryKey;
                return;
            }

            currentEmployeeKey = employee.Id > 0 ? employee.Id : NewEmployeeTemporaryKey;
            if (!employeeEventsById.TryGetValue(currentEmployeeKey, out var events))
            {
                events = new ObservableCollection<EmployeeEventViewModel>();
                employeeEventsById[currentEmployeeKey] = events;
            }

            EmployeeEvents = events;
            BuildEmployeeEventsView();
        }

        private void BuildEmployeeEventsView()
        {
            if (EmployeeEvents == null)
            {
                EmployeeEventsView = null;
                return;
            }

            var view = new ListCollectionView(EmployeeEvents);
            view.SortDescriptions.Add(new SortDescription(nameof(EmployeeEventViewModel.StartDate), ListSortDirection.Ascending));
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(EmployeeEventViewModel.TypeName)));
            view.Filter = EventFilter;
            EmployeeEventsView = view;
        }

        private bool EventFilter(object item)
        {
            var employeeEvent = item as EmployeeEventViewModel;
            if (employeeEvent == null)
            {
                return false;
            }

            var today = DateTime.Today;
            var start = employeeEvent.StartDate.Date;
            var end = employeeEvent.EndDate.Date;

            var isPast = end < today;
            var isFuture = start > today;
            var isCurrent = !isPast && !isFuture;

            return (ShowPastEvents && isPast)
                || (ShowCurrentEvents && isCurrent)
                || (ShowFutureEvents && isFuture);
        }

        private void RefreshEmployeeEventsView()
        {
            if (EmployeeEventsView == null)
            {
                return;
            }

            EmployeeEventsView.Refresh();
        }

        private void AddEmployeeEvent()
        {
            EventValidationErrors.Clear();

            if (SelectedEmployee == null)
            {
                EventValidationErrors.Add("Сотрудник не выбран.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedEventType))
            {
                EventValidationErrors.Add("Тип события является обязательным полем.");
            }

            if (!NewEventStartDate.HasValue)
            {
                EventValidationErrors.Add("Дата начала является обязательным полем.");
            }

            if (!NewEventEndDate.HasValue)
            {
                EventValidationErrors.Add("Дата окончания является обязательным полем.");
            }

            if (NewEventStartDate.HasValue && NewEventEndDate.HasValue
                && NewEventEndDate.Value.Date < NewEventStartDate.Value.Date)
            {
                EventValidationErrors.Add("Дата окончания не может быть меньше даты начала.");
            }

            if (EventValidationErrors.Count > 0)
            {
                return;
            }

            var startDate = NewEventStartDate.Value.Date;
            var endDate = NewEventEndDate.Value.Date;

            ValidateEventOverlap(SelectedEventType, startDate, endDate);
            if (SelectedEventType == "Отгул")
            {
                ValidateTimeOffWorkingDays(startDate, endDate);
            }

            if (EventValidationErrors.Count > 0)
            {
                return;
            }

            EmployeeEvents.Add(new EmployeeEventViewModel
            {
                TypeName = SelectedEventType,
                StartDate = startDate,
                EndDate = endDate,
                Reason = NewEventReason
            });

            NewEventStartDate = DateTime.Today;
            NewEventEndDate = DateTime.Today;
            NewEventReason = string.Empty;
            RefreshEmployeeEventsView();
        }

        private void DeleteEmployeeEvent(EmployeeEventViewModel employeeEvent)
        {
            if (employeeEvent == null || EmployeeEvents == null)
            {
                return;
            }

            EmployeeEvents.Remove(employeeEvent);
            RefreshEmployeeEventsView();
        }

        private void ValidateEventOverlap(string newEventType, DateTime startDate, DateTime endDate)
        {
            foreach (var existing in EmployeeEvents)
            {
                if (!IsOverlapForbidden(existing.TypeName, newEventType))
                {
                    continue;
                }

                if (DatesOverlap(existing.StartDate, existing.EndDate, startDate, endDate))
                {
                    EventValidationErrors.Add($"Событие \"{newEventType}\" пересекается с \"{existing.TypeName}\".");
                    return;
                }
            }
        }

        private static bool IsOverlapForbidden(string existingType, string newType)
        {
            if (newType == "Отпуск")
            {
                return existingType == "Отгул";
            }

            if (newType == "Отгул")
            {
                return existingType == "Отпуск" || existingType == "Обучение";
            }

            if (newType == "Обучение")
            {
                return existingType == "Отгул";
            }

            return false;
        }

        private static bool DatesOverlap(DateTime firstStart, DateTime firstEnd, DateTime secondStart, DateTime secondEnd)
        {
            var start = firstStart <= secondStart ? secondStart : firstStart;
            var end = firstEnd <= secondEnd ? firstEnd : secondEnd;
            return start <= end;
        }

        private void ValidateTimeOffWorkingDays(DateTime startDate, DateTime endDate)
        {
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (IsNonWorkingDay(date))
                {
                    EventValidationErrors.Add("Отгул не может приходиться на выходной день по производственному календарю.");
                    return;
                }
            }
        }

        private bool IsNonWorkingDay(DateTime date)
        {
            var exception = AppData.db.WorkingCalendar
                .FirstOrDefault(item => item.ExceptionDate == date);

            if (exception != null)
            {
                return !exception.IsWorkingDay;
            }

            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        private void CommitNewEmployeeEvents(int newEmployeeId)
        {
            if (!employeeEventsById.TryGetValue(NewEmployeeTemporaryKey, out var events)
                || events == null)
            {
                return;
            }

            employeeEventsById.Remove(NewEmployeeTemporaryKey);
            employeeEventsById[newEmployeeId] = events;
            currentEmployeeKey = newEmployeeId;
        }

        public class EmployeeEventViewModel
        {
            public string TypeName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Reason { get; set; }

            public string DateRangeDisplay =>
                StartDate.Date == EndDate.Date
                    ? StartDate.ToString("dd.MM.yyyy")
                    : $"{StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}";
        }
    }
}