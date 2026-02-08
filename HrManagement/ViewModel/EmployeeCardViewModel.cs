namespace HrManagement.ViewModel
{
    public class EmployeeCardViewModel : ViewModelBase
    {
        private int id;
        private int idEmployeeDepartment;
        private int idPosition;
        private int? directSupervisor;
        private int? assistantEmployee;
        private string departmentName;
        private string positionName;
        private string fullName;
        private string personalPhone;
        private System.DateTime? birthDate;
        private string workPhone;
        private string email;
        private string employeeOffice;
        private string other;
        private System.DateTime? employmentEndDate;
        private EmployeeSnapshot backup;

        public int Id
        {
            get => id;
            set
            {
                id = value;
                OnPropertyChanged();
            }
        }

        public int IdEmployeeDepartment
        {
            get => idEmployeeDepartment;
            set
            {
                idEmployeeDepartment = value;
                OnPropertyChanged();
            }
        }

        public int IdPosition
        {
            get => idPosition;
            set
            {
                idPosition = value;
                OnPropertyChanged();
            }
        }

        public int? DirectSupervisor
        {
            get => directSupervisor;
            set
            {
                directSupervisor = value;
                OnPropertyChanged();
            }
        }

        public int? AssistantEmployee
        {
            get => assistantEmployee;
            set
            {
                assistantEmployee = value;
                OnPropertyChanged();
            }
        }

        public string DepartmentName
        {
            get => departmentName;
            set
            {
                departmentName = value;
                OnPropertyChanged();
            }
        }

        public string PositionName
        {
            get => positionName;
            set
            {
                positionName = value;
                OnPropertyChanged();
            }
        }

        public string FullName
        {
            get => fullName;
            set
            {
                fullName = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get => email;
            set
            {
                email = value;
                OnPropertyChanged();
            }
        }

        public string EmployeeOffice
        {
            get => employeeOffice;
            set
            {
                employeeOffice = value;
                OnPropertyChanged();
            }
        }

        public string PersonalPhone
        {
            get => personalPhone;
            set
            {
                personalPhone = value;
                OnPropertyChanged();
            }
        }

        public System.DateTime? BirthDate
        {
            get => birthDate;
            set
            {
                birthDate = value;
                OnPropertyChanged();
            }
        }

        public string WorkPhone
        {
            get => workPhone;
            set
            {
                workPhone = value;
                OnPropertyChanged();
            }
        }

        public string Other
        {
            get => other;
            set
            {
                other = value;
                OnPropertyChanged();
            }
        }

        public System.DateTime? EmploymentEndDate
        {
            get => employmentEndDate;
            set
            {
                employmentEndDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDismissed));
                OnPropertyChanged(nameof(IsDismissedRecently));
            }
        }

        public bool IsDismissed => EmploymentEndDate.HasValue;

        public bool IsDismissedRecently => EmploymentEndDate.HasValue
            && EmploymentEndDate.Value.Date >= System.DateTime.Today.AddDays(-30);

        public void BeginEdit()
        {
            backup = new EmployeeSnapshot
            {
                Id = Id,
                IdEmployeeDepartment = IdEmployeeDepartment,
                IdPosition = IdPosition,
                DirectSupervisor = DirectSupervisor,
                AssistantEmployee = AssistantEmployee,
                DepartmentName = DepartmentName,
                PositionName = PositionName,
                FullName = FullName,
                PersonalPhone = PersonalPhone,
                BirthDate = BirthDate,
                WorkPhone = WorkPhone,
                Email = Email,
                EmployeeOffice = EmployeeOffice,
                Other = Other,
                EmploymentEndDate = EmploymentEndDate
            };
        }

        public void CancelEdit()
        {
            if (backup == null)
            {
                return;
            }

            Id = backup.Id;
            IdEmployeeDepartment = backup.IdEmployeeDepartment;
            IdPosition = backup.IdPosition;
            DirectSupervisor = backup.DirectSupervisor;
            AssistantEmployee = backup.AssistantEmployee;
            DepartmentName = backup.DepartmentName;
            PositionName = backup.PositionName;
            FullName = backup.FullName;
            PersonalPhone = backup.PersonalPhone;
            BirthDate = backup.BirthDate;
            WorkPhone = backup.WorkPhone;
            Email = backup.Email;
            EmployeeOffice = backup.EmployeeOffice;
            Other = backup.Other;
            EmploymentEndDate = backup.EmploymentEndDate;
        }

        private class EmployeeSnapshot
        {
            public int Id { get; set; }
            public int IdEmployeeDepartment { get; set; }
            public int IdPosition { get; set; }
            public int? DirectSupervisor { get; set; }
            public int? AssistantEmployee { get; set; }
            public string DepartmentName { get; set; }
            public string PositionName { get; set; }
            public string FullName { get; set; }
            public string PersonalPhone { get; set; }
            public System.DateTime? BirthDate { get; set; }
            public string WorkPhone { get; set; }
            public string Email { get; set; }
            public string EmployeeOffice { get; set; }
            public string Other { get; set; }
            public System.DateTime? EmploymentEndDate { get; set; }
        }
    }
}