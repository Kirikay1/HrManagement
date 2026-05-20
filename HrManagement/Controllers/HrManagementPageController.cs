using HrManagement.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;

namespace HrManagement.Controllers
{
    public class HrManagementPageController : ControllerBase
    {
        private const int OfficeMaxLength = 10;
        private const int NewEmployeeTemporaryKey = -1;
        private static readonly Regex PhoneRegex = new Regex(@"^[0-9+()\-\s#]{0,20}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly List<EmployeeCardModel> allEmployeeCards = new List<EmployeeCardModel>();
        private readonly ObservableCollection<string> eventTypeOptions = new ObservableCollection<string> { "Обучение", "Отгул", "Отпуск" };
        private ObservableCollection<EmployeeCardModel> employeeCards;
        private ObservableCollection<Department> departments;
        private ObservableCollection<Position> positions;
        private ObservableCollection<EmployeeLookupModel> departmentEmployees;
        private ObservableCollection<EmployeeEventModel> employeeEvents;
        private ICollectionView employeeEventsView;
        private EmployeeCardModel selectedEmployee;
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

        private readonly HrManagementController controller;

        public HrManagementPageController(HrManagementController controller)
        {
            this.controller = controller;
            LoadEmployees();
            LoadReferenceData();
            FilterByDepartmentCommand = new UiCommand(parameter => FilterEmployeesByDepartment(parameter as string));
            OpenEmployeeCardCommand = new UiCommand(parameter => OpenEmployeeCard(parameter as EmployeeCardModel));
            CloseEmployeeCardCommand = new UiCommand(_ => CloseEmployeeCard());
            StartEditEmployeeCommand = new UiCommand(_ => StartEditEmployee(), _ => SelectedEmployee != null && !IsEditing);
            CancelEditEmployeeCommand = new UiCommand(_ => CancelEditEmployee(), _ => SelectedEmployee != null && IsEditing);
            SaveEmployeeCommand = new UiCommand(_ => SaveEmployee(), _ => SelectedEmployee != null && IsEditing);
            DismissEmployeeCommand = new UiCommand(_ => DismissEmployee(), _ => SelectedEmployee != null && SelectedEmployee.Id > 0);
            AddEmployeeCommand = new UiCommand(_ => AddEmployee());
            AddEmployeeEventCommand = new UiCommand(_ => AddEmployeeEvent(), _ => SelectedEmployee != null);
            DeleteEmployeeEventCommand = new UiCommand(parameter => DeleteEmployeeEvent(parameter as EmployeeEventModel), _ => SelectedEmployee != null);
        }

        public ObservableCollection<EmployeeCardModel> EmployeeCards
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

        public ObservableCollection<EmployeeLookupModel> DepartmentEmployees
        {
            get => departmentEmployees;
            private set
            {
                departmentEmployees = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<EmployeeEventModel> EmployeeEvents
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

        public EmployeeCardModel SelectedEmployee
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
                    DepartmentEmployees = new ObservableCollection<EmployeeLookupModel>();
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
        public ICommand DismissEmployeeCommand { get; }
        public ICommand AddEmployeeCommand { get; }
        public ICommand AddEmployeeEventCommand { get; }
        public ICommand DeleteEmployeeEventCommand { get; }

        private void LoadEmployees()
        {
            var employees = controller.GetEmployees();

            allEmployeeCards.Clear();
            allEmployeeCards.AddRange(employees.Select(employee => new EmployeeCardModel
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
                Other = employee.Other,
                EmploymentEndDate = employee.EmploymentEndDate
            }));

            EmployeeCards = new ObservableCollection<EmployeeCardModel>(allEmployeeCards.Where(IsVisibleInList));
        }

        private void LoadReferenceData()
        {
            Departments = new ObservableCollection<Department>(AppData.Db.Department.ToList());
            Positions = new ObservableCollection<Position>(AppData.Db.Position.ToList());
            DepartmentEmployees = new ObservableCollection<EmployeeLookupModel>();
        }

        public void FilterEmployeesByDepartment(string departmentName)
        {
            SelectedDepartmentName = string.IsNullOrWhiteSpace(departmentName) ? null : departmentName;
            if (string.IsNullOrWhiteSpace(departmentName))
            {
                EmployeeCards = new ObservableCollection<EmployeeCardModel>(allEmployeeCards.Where(IsVisibleInList));
                return;
            }

            var filtered = allEmployeeCards
                .Where(IsVisibleInList)
                .Where(employee => string.Equals(employee.DepartmentName, departmentName, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            EmployeeCards = new ObservableCollection<EmployeeCardModel>(filtered);
        }

        public void OpenEmployeeCard(EmployeeCardModel employee)
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

        public void CloseEmployeeCard()
        {
            ValidationErrors.Clear();
            EventValidationErrors.Clear();
            IsEditing = false;
            IsEmployeeCardOpen = false;
            IsNewEmployee = false;
        }

        public void StartEditEmployee()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            SelectedEmployee.BeginEdit();
            IsEditing = true;
        }

        public void CancelEditEmployee()
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

        public void SaveEmployee()
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
                : AppData.Db.Employee.FirstOrDefault(item => item.Id == SelectedEmployee.Id);
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
            employee.EmploymentEndDate = SelectedEmployee.EmploymentEndDate;

            var department = Departments.FirstOrDefault(item => item.Id == SelectedEmployee.IdEmployeeDepartment);
            var position = Positions.FirstOrDefault(item => item.Id == SelectedEmployee.IdPosition);
            SelectedEmployee.DepartmentName = department?.NameDepartment;
            SelectedEmployee.PositionName = position?.NamePosition;

            if (isCreating)
            {
                AppData.Db.Employee.Add(employee);
            }

            AppData.Db.SaveChanges();
            if (isCreating)
            {
                SelectedEmployee.Id = employee.Id;
                allEmployeeCards.Add(SelectedEmployee);
                ApplyDepartmentFilter();
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
            if (e.PropertyName == nameof(EmployeeCardModel.IdEmployeeDepartment))
            {
                UpdateDepartmentEmployees(SelectedEmployee.IdEmployeeDepartment);
            }
        }

        private void UpdateDepartmentEmployees(int departmentId)
        {
            var employees = allEmployeeCards
                .Where(employee => employee.IdEmployeeDepartment == departmentId && employee.Id != SelectedEmployee?.Id)
                .Select(employee => new EmployeeLookupModel(employee.Id, employee.FullName))
                .OrderBy(employee => employee.FullName)
                .ToList();

            DepartmentEmployees = new ObservableCollection<EmployeeLookupModel>(employees);

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

        private static bool IsVisibleInList(EmployeeCardModel employee)
        {
            return !employee.EmploymentEndDate.HasValue
                || employee.EmploymentEndDate.Value.Date >= DateTime.Today.AddDays(-30);
        }

        public void DismissEmployee()
        {
            ValidationErrors.Clear();

            if (SelectedEmployee == null || SelectedEmployee.Id <= 0)
            {
                return;
            }

            var dbEmployee = AppData.Db.Employee
                .Include(item => item.Calendar)
                .Include(item => item.Calendar.LearningCalendar)
                .Include(item => item.Calendar.VacationCalendar)
                .Include(item => item.Calendar.WorkingCalendar)
                .FirstOrDefault(item => item.Id == SelectedEmployee.Id);

            if (dbEmployee == null)
            {
                ValidationErrors.Add("Сотрудник не найден в базе данных.");
                return;
            }

            var today = DateTime.Today;
            var hasFutureLearning = dbEmployee.Calendar?.LearningCalendar != null
                && dbEmployee.Calendar.LearningCalendar.EndLearningn.Date >= today;

            if (hasFutureLearning)
            {
                ValidationErrors.Add("Нельзя уволить сотрудника: у него запланировано обучение.");
                return;
            }

            var confirmationResult = MessageBox.Show(
                "Подтвердите увольнение сотрудника. Будут удалены будущие отгулы и отпуска.",
                "Подтверждение увольнения",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmationResult != MessageBoxResult.Yes)
            {
                return;
            }

            var calendar = dbEmployee.Calendar;
            if (calendar != null)
            {
                if (calendar.VacationCalendar != null && calendar.VacationCalendar.EndVacation.Date >= today)
                {
                    AppData.Db.VacationCalendar.Remove(calendar.VacationCalendar);
                    calendar.IdVacationCalendar = null;
                }

                if (calendar.WorkingCalendar != null && calendar.WorkingCalendar.EndExceptionDate.Date >= today)
                {
                    AppData.Db.WorkingCalendar.Remove(calendar.WorkingCalendar);
                    calendar.IdWorkingCalendar = null;
                }
            }

            dbEmployee.EmploymentEndDate = today;
            SelectedEmployee.EmploymentEndDate = today;

            AppData.Db.SaveChanges();
            LoadEmployeeEvents(SelectedEmployee);
            ApplyDepartmentFilter();
        }

        public void AddEmployee()
        {
            var preselectedDepartment = Departments
                .FirstOrDefault(item => string.Equals(item.NameDepartment, SelectedDepartmentName, System.StringComparison.OrdinalIgnoreCase));

            SelectedEmployee = new EmployeeCardModel
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
                EmployeeCards = new ObservableCollection<EmployeeCardModel>(allEmployeeCards.Where(IsVisibleInList));
                return;
            }

            var filtered = allEmployeeCards
                .Where(IsVisibleInList)
                .Where(employee => string.Equals(employee.DepartmentName, SelectedDepartmentName, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            EmployeeCards = new ObservableCollection<EmployeeCardModel>(filtered);
        }

        public sealed class EmployeeLookupModel
        {
            public EmployeeLookupModel(int id, string fullName)
            {
                Id = id;
                FullName = fullName;
            }

            public int Id { get; }
            public string FullName { get; }
        }

        private void LoadEmployeeEvents(EmployeeCardModel employee)
        {
            EventValidationErrors.Clear();
            if (employee == null || employee.Id <= 0)
            {
                EmployeeEvents = new ObservableCollection<EmployeeEventModel>();
                EmployeeEventsView = null;
                currentEmployeeKey = NewEmployeeTemporaryKey;
                return;
            }

            var dbEmployee = AppData.Db.Employee
            .Include(item => item.Calendar)
            .Include(item => item.Calendar.VacationCalendar)
            .Include(item => item.Calendar.LearningCalendar)
            .Include(item => item.Calendar.WorkingCalendar)
            .FirstOrDefault(item => item.Id == employee.Id);

            currentEmployeeKey = employee.Id;
            var events = new ObservableCollection<EmployeeEventModel>();
            var calendar = dbEmployee?.Calendar;
            if (calendar != null)
            {
                if (calendar.VacationCalendar != null)
                {
                    events.Add(new EmployeeEventModel
                    {
                        TypeName = "Отпуск",
                        StartDate = calendar.VacationCalendar.BeginVacation,
                        EndDate = calendar.VacationCalendar.EndVacation,
                        Reason = calendar.VacationCalendar.reasonVacation,
                        CalendarId = calendar.Id,
                        VacationCalendarId = calendar.VacationCalendar.Id
                    });
                }

                if (calendar.WorkingCalendar != null)
                {
                    events.Add(new EmployeeEventModel
                    {
                        TypeName = "Отгул",
                        StartDate = calendar.WorkingCalendar.ExceptionDate,
                        EndDate = calendar.WorkingCalendar.EndExceptionDate,
                        Reason = calendar.WorkingCalendar.reasonWorking,
                        CalendarId = calendar.Id,
                        WorkingCalendarId = calendar.WorkingCalendar.Id
                    });
                }

                if (calendar.LearningCalendar != null)
                {
                    events.Add(new EmployeeEventModel
                    {
                        TypeName = "Обучение",
                        StartDate = calendar.LearningCalendar.BeginLearning,
                        EndDate = calendar.LearningCalendar.EndLearningn,
                        Reason = calendar.LearningCalendar.reasonLearning,
                        CalendarId = calendar.Id,
                        LearningCalendarId = calendar.LearningCalendar.Id
                    });
                }
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
            view.SortDescriptions.Add(new SortDescription(nameof(EmployeeEventModel.StartDate), ListSortDirection.Ascending));
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(EmployeeEventModel.TypeName)));
            view.Filter = EventFilter;
            EmployeeEventsView = view;
        }

        private bool EventFilter(object item)
        {
            var employeeEvent = item as EmployeeEventModel;
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

        public void AddEmployeeEvent()
        {
            EventValidationErrors.Clear();

            if (SelectedEmployee == null)
            {
                EventValidationErrors.Add("Сотрудник не выбран.");
                return;
            }

            if (SelectedEmployee.Id <= 0)
            {
                EventValidationErrors.Add("Сначала сохраните карточку сотрудника, затем добавляйте события.");
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
            var reason = string.IsNullOrWhiteSpace(NewEventReason) ? null : NewEventReason.Trim();

            ValidateEventOverlap(SelectedEventType, startDate, endDate);
            if (SelectedEventType == "Отгул")
            {
                ValidateTimeOffWorkingDays(startDate, endDate);
            }

            if (EventValidationErrors.Count > 0)
            {
                return;
            }

            if (EmployeeEvents.Any(item => item.TypeName == SelectedEventType))
            {
                EventValidationErrors.Add($"Событие типа \"{SelectedEventType}\" уже добавлено для сотрудника.");
                return;
            }

            var dbEmployee = AppData.Db.Employee
                .Include(item => item.Calendar)
                .FirstOrDefault(item => item.Id == SelectedEmployee.Id);

            if (dbEmployee == null)
            {
                EventValidationErrors.Add("Сотрудник не найден в базе данных.");
                return;
            }

            var calendar = EnsureEmployeeCalendar(dbEmployee);
            if (SelectedEventType == "Отпуск")
            {
                var vacation = new VacationCalendar
                {
                    BeginVacation = startDate,
                    EndVacation = endDate,
                    reasonVacation = reason
                };

                AppData.Db.VacationCalendar.Add(vacation);
                AppData.Db.SaveChanges();
                calendar.IdVacationCalendar = vacation.Id;
            }
            else if (SelectedEventType == "Обучение")
            {
                var learning = new LearningCalendar
                {
                    BeginLearning = startDate,
                    EndLearningn = endDate,
                    reasonLearning = reason
                };

                AppData.Db.LearningCalendar.Add(learning);
                AppData.Db.SaveChanges();
                calendar.IdLearningCalendar = learning.Id;
            }
            else if (SelectedEventType == "Отгул")
            {
                var timeOff = new WorkingCalendar
                {
                    ExceptionDate = startDate,
                    EndExceptionDate = endDate,
                    IsWorkingDay = false,
                    reasonWorking = reason
                };

                AppData.Db.WorkingCalendar.Add(timeOff);
                AppData.Db.SaveChanges();
                calendar.IdWorkingCalendar = timeOff.Id;
            }

            AppData.Db.SaveChanges();
            LoadEmployeeEvents(SelectedEmployee);

            NewEventStartDate = DateTime.Today;
            NewEventEndDate = DateTime.Today;
            NewEventReason = string.Empty;
            RefreshEmployeeEventsView();
        }

        public void DeleteEmployeeEvent(EmployeeEventModel employeeEvent)
        {
            if (employeeEvent == null || EmployeeEvents == null || SelectedEmployee == null || SelectedEmployee.Id <= 0)
            {
                return;
            }

            var dbEmployee = AppData.Db.Employee
    .Include(item => item.Calendar)
    .FirstOrDefault(item => item.Id == SelectedEmployee.Id);
            var calendar = dbEmployee?.Calendar;
            if (calendar == null)
            {
                return;
            }

            if (employeeEvent.VacationCalendarId.HasValue)
            {
                var vacation = AppData.Db.VacationCalendar.FirstOrDefault(item => item.Id == employeeEvent.VacationCalendarId.Value);
                if (vacation != null)
                {
                    AppData.Db.VacationCalendar.Remove(vacation);
                }

                if (calendar.IdVacationCalendar == employeeEvent.VacationCalendarId.Value)
                {
                    calendar.IdVacationCalendar = null;
                }
            }

            if (employeeEvent.LearningCalendarId.HasValue)
            {
                var learning = AppData.Db.LearningCalendar.FirstOrDefault(item => item.Id == employeeEvent.LearningCalendarId.Value);
                if (learning != null)
                {
                    AppData.Db.LearningCalendar.Remove(learning);
                }

                if (calendar.IdLearningCalendar == employeeEvent.LearningCalendarId.Value)
                {
                    calendar.IdLearningCalendar = null;
                }
            }

            if (employeeEvent.WorkingCalendarId.HasValue)
            {
                var timeOff = AppData.Db.WorkingCalendar.FirstOrDefault(item => item.Id == employeeEvent.WorkingCalendarId.Value);
                if (timeOff != null)
                {
                    AppData.Db.WorkingCalendar.Remove(timeOff);
                }

                if (calendar.IdWorkingCalendar == employeeEvent.WorkingCalendarId.Value)
                {
                    calendar.IdWorkingCalendar = null;
                }
            }

            AppData.Db.SaveChanges();
            LoadEmployeeEvents(SelectedEmployee);

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
            var exception = AppData.Db.WorkingCalendar
                .FirstOrDefault(item => item.ExceptionDate == date);


            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        private Calendar EnsureEmployeeCalendar(Employee employee)
        {
            if (employee.Calendar != null)
            {
                return employee.Calendar;
            }

            var calendar = new Calendar();
            AppData.Db.Calendar.Add(calendar);
            AppData.Db.SaveChanges();
            employee.CalendarEmployee = calendar.Id;
            AppData.Db.SaveChanges();
            return calendar;
        }

        public sealed class EmployeeEventModel
        {
            public string TypeName { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Reason { get; set; }
            public int? CalendarId { get; set; }
            public int? VacationCalendarId { get; set; }
            public int? LearningCalendarId { get; set; }
            public long? WorkingCalendarId { get; set; }

            public string DateRangeDisplay =>
                StartDate.Date == EndDate.Date
                    ? StartDate.ToString("dd.MM.yyyy")
                    : $"{StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}";
        }
    }
}