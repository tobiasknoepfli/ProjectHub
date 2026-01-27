using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectHub.App.Models;
using ProjectHub.App.Services;

namespace ProjectHub.App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private List<Issue> _allProjectIssues = new();

        [ObservableProperty]
        private ObservableCollection<Project> _projects = new();

        [ObservableProperty]
        private Project? _selectedProject;

        [ObservableProperty]
        private string _selectedCategory = "Backlog"; 

        [ObservableProperty]
        private ObservableCollection<Sprint> _sprints = new();

        [ObservableProperty]
        private Sprint? _selectedSprint;

        [ObservableProperty]
        private ObservableCollection<Sprint> _archivedSprints = new();

        [ObservableProperty]
        private bool _isArchiveVisible;

        [ObservableProperty]
        private ObservableCollection<Issue> _openItems = new();

        [ObservableProperty]
        private ObservableCollection<Issue> _inProgressItems = new();

        [ObservableProperty]
        private ObservableCollection<Issue> _testingItems = new();

        [ObservableProperty]
        private ObservableCollection<Issue> _finishedItems = new();

        [ObservableProperty]
        private bool _isLoading;

        // Sprint Planning Overlay Properties
        [ObservableProperty]
        private bool _isSprintModalVisible;

        [ObservableProperty]
        private string _newSprintName = "";

        [ObservableProperty]
        private DateTime _newSprintStartDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _newSprintEndDate = DateTime.Today.AddDays(14);

        [ObservableProperty]
        private bool _isEditingSprint;

        [ObservableProperty]
        private string _sprintModalTitle = "Plan New Sprint";

        [ObservableProperty]
        private bool _isProjectSelectorVisible;

        // Project Modal Properties
        [ObservableProperty]
        private bool _isProjectModalVisible;
        [ObservableProperty]
        private string _newProjectName = "";
        [ObservableProperty]
        private string _newProjectDescription = "";
        [ObservableProperty]
        private string _newProjectLogoUrl = "";
        [ObservableProperty]
        private bool _isEditingProject;
        [ObservableProperty]
        private string _projectModalTitle = "Create New Project";

        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;
            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
            CreateProjectCommand = new RelayCommand(OpenProjectModal);
            CreateIssueCommand = new AsyncRelayCommand<string>(CreateIssueAsync);
            
            OpenSprintModalCommand = new RelayCommand(OpenSprintModal);
            EditSprintModalCommand = new RelayCommand(OpenEditSprintModal);
            SaveSprintCommand = new AsyncRelayCommand(SaveSprintAsync);
            CancelSprintModalCommand = new RelayCommand(() => IsSprintModalVisible = false);
            
            ShowLogsCommand = new AsyncRelayCommand<Issue>(ShowLogsAsync);
            CompleteSprintCommand = new AsyncRelayCommand(CompleteSprintAsync);
            AssignToSprintCommand = new AsyncRelayCommand<Issue>(AssignToSprintAsync);
            ToggleArchiveCommand = new RelayCommand(() => IsArchiveVisible = !IsArchiveVisible);
            DeleteSprintCommand = new AsyncRelayCommand(DeleteSprintAsync);
            ToggleProjectSelectorCommand = new RelayCommand(() => IsProjectSelectorVisible = !IsProjectSelectorVisible);
            SelectProjectCommand = new RelayCommand<Project>(p => {
                if (p != null) {
                    SelectedProject = p;
                    IsProjectSelectorVisible = false;
                }
            });

            OpenProjectModalCommand = new RelayCommand(OpenProjectModal);
            EditProjectModalCommand = new RelayCommand(OpenEditProjectModal);
            SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync);
            CancelProjectModalCommand = new RelayCommand(() => IsProjectModalVisible = false);
            BrowseLogoCommand = new RelayCommand(BrowseLogo);
            ClearLogoCommand = new RelayCommand(() => NewProjectLogoUrl = "");
        }

        public IAsyncRelayCommand LoadDataCommand { get; }
        public IRelayCommand CreateProjectCommand { get; }
        public IAsyncRelayCommand<string> CreateIssueCommand { get; }
        
        public IRelayCommand OpenSprintModalCommand { get; }
        public IRelayCommand EditSprintModalCommand { get; }
        public IAsyncRelayCommand SaveSprintCommand { get; }
        public IRelayCommand CancelSprintModalCommand { get; }
        
        public IAsyncRelayCommand<Issue> ShowLogsCommand { get; }
        public IAsyncRelayCommand CompleteSprintCommand { get; }
        public IAsyncRelayCommand<Issue> AssignToSprintCommand { get; }
        public IRelayCommand ToggleArchiveCommand { get; }
        public IAsyncRelayCommand DeleteSprintCommand { get; }
        public IRelayCommand ToggleProjectSelectorCommand { get; }
        public IRelayCommand<Project> SelectProjectCommand { get; }

        public IRelayCommand OpenProjectModalCommand { get; }
        public IRelayCommand EditProjectModalCommand { get; }
        public IAsyncRelayCommand SaveProjectCommand { get; }
        public IRelayCommand CancelProjectModalCommand { get; }
        public IRelayCommand BrowseLogoCommand { get; }
        public IRelayCommand ClearLogoCommand { get; }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var projects = await _dataService.GetProjectsAsync();
                Projects.Clear();
                foreach (var p in projects) Projects.Add(p);

                if (SelectedProject == null && Projects.Any())
                {
                    SelectedProject = Projects.First();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedProjectChanged(Project? value)
        {
            _ = LoadProjectDataAsync();
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            RefreshCategorizedIssues();
        }

        partial void OnSelectedSprintChanged(Sprint? value)
        {
            RefreshCategorizedIssues();
        }

        private async Task LoadProjectDataAsync()
        {
            if (SelectedProject == null) return;

            IsLoading = true;
            try
            {
                var sprintsTask = _dataService.GetSprintsAsync(SelectedProject.Id);
                var issuesTask = _dataService.GetIssuesAsync(SelectedProject.Id);

                await Task.WhenAll(sprintsTask, issuesTask);

                Sprints.Clear();
                ArchivedSprints.Clear();
                foreach (var s in sprintsTask.Result.OrderBy(x => x.StartDate)) 
                {
                    if (s.IsActive) Sprints.Add(s);
                    else ArchivedSprints.Add(s);
                }
                
                if (SelectedSprint == null)
                {
                    SelectedSprint = Sprints.FirstOrDefault(s => s.IsCurrent) ?? Sprints.FirstOrDefault(s => s.IsActive);
                }

                _allProjectIssues = issuesTask.Result;
                RefreshCategorizedIssues();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RefreshCategorizedIssues()
        {
            OpenItems.Clear();
            InProgressItems.Clear();
            TestingItems.Clear();
            FinishedItems.Clear();

            var filtered = _allProjectIssues
                .Where(i => i.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (SelectedSprint != null)
            {
                filtered = filtered.Where(i => i.SprintId == SelectedSprint.Id).ToList();
            }
            else
            {
                // Unplanned issues have no SprintId
                filtered = filtered.Where(i => i.SprintId == null).ToList();
            }

            foreach (var issue in filtered)
            {
                switch (issue.Status?.ToLower())
                {
                    case "open": OpenItems.Add(issue); break;
                    case "in progress": InProgressItems.Add(issue); break;
                    case "in testing": TestingItems.Add(issue); break;
                    case "finished": FinishedItems.Add(issue); break;
                    default: OpenItems.Add(issue); break;
                }
            }
        }

        private void OpenProjectModal()
        {
            IsEditingProject = false;
            ProjectModalTitle = "Create New Project";
            NewProjectName = "";
            NewProjectDescription = "";
            NewProjectLogoUrl = "";
            IsProjectModalVisible = true;
        }

        private void OpenEditProjectModal()
        {
            if (SelectedProject == null) return;
            IsEditingProject = true;
            ProjectModalTitle = "Edit Project";
            NewProjectName = SelectedProject.Name;
            NewProjectDescription = SelectedProject.Description;
            NewProjectLogoUrl = SelectedProject.LogoUrl ?? "";
            IsProjectModalVisible = true;
        }

        private async Task SaveProjectAsync()
        {
            if (string.IsNullOrWhiteSpace(NewProjectName)) return;

            IsLoading = true;
            try
            {
                if (IsEditingProject && SelectedProject != null)
                {
                    SelectedProject.Name = NewProjectName;
                    SelectedProject.Description = NewProjectDescription;
                    SelectedProject.LogoUrl = string.IsNullOrWhiteSpace(NewProjectLogoUrl) ? null : NewProjectLogoUrl;
                    await _dataService.UpdateProjectAsync(SelectedProject);
                    OnPropertyChanged(nameof(SelectedProject));
                }
                else
                {
                    var p = await _dataService.CreateProjectAsync(NewProjectName, NewProjectDescription, NewProjectLogoUrl);
                    Projects.Add(p);
                    SelectedProject = p;
                }
                IsProjectModalVisible = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void BrowseLogo()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files (*.*)|*.*",
                Title = "Select Project Logo"
            };
            if (dialog.ShowDialog() == true)
            {
                NewProjectLogoUrl = dialog.FileName;
            }
        }

        private void OpenSprintModal()
        {
            if (SelectedProject == null) return;
            IsEditingSprint = false;
            SprintModalTitle = "Plan New Sprint";
            NewSprintName = $"Sprint {Sprints.Count + 1}";
            NewSprintStartDate = DateTime.Today;
            NewSprintEndDate = DateTime.Today.AddDays(14);
            IsSprintModalVisible = true;
        }

        private void OpenEditSprintModal()
        {
            if (SelectedSprint == null) return;
            IsEditingSprint = true;
            SprintModalTitle = "Edit Sprint";
            NewSprintName = SelectedSprint.Name;
            NewSprintStartDate = SelectedSprint.StartDate;
            NewSprintEndDate = SelectedSprint.EndDate;
            IsSprintModalVisible = true;
        }

        private async Task SaveSprintAsync()
        {
            if (SelectedProject == null) return;

            if (IsEditingSprint)
            {
                if (SelectedSprint == null) return;
                SelectedSprint.Name = NewSprintName;
                SelectedSprint.StartDate = NewSprintStartDate;
                SelectedSprint.EndDate = NewSprintEndDate;
                await _dataService.UpdateSprintAsync(SelectedSprint);
            }
            else
            {
                // Deactivate existing active sprints
                var activeSprints = Sprints.Where(s => s.IsActive).ToList();
                foreach (var existingSprint in activeSprints)
                {
                    existingSprint.IsActive = false;
                    await _dataService.UpdateSprintAsync(existingSprint);
                }
                
                var s = new Sprint { 
                    ProjectId = SelectedProject.Id, 
                    Name = NewSprintName, 
                    StartDate = NewSprintStartDate, 
                    EndDate = NewSprintEndDate,
                    IsActive = true
                };
                
                await _dataService.CreateSprintAsync(s);
                SelectedSprint = s;
            }
            
            // Re-fetch sprints to ensure UI updates with the new state
            var updatedSprints = await _dataService.GetSprintsAsync(SelectedProject.Id);
            Sprints.Clear();
            foreach (var sprint in updatedSprints) Sprints.Add(sprint);
            
            // Re-select if we were editing or just created
            if (SelectedSprint != null)
            {
                var id = SelectedSprint.Id;
                SelectedSprint = Sprints.FirstOrDefault(sp => sp.Id == id);
            }

            IsSprintModalVisible = false;
        }

        private async Task DeleteSprintAsync()
        {
            if (SelectedSprint == null || SelectedProject == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete {SelectedSprint.Name}? All associated issues will be unassigned.", 
                "Confirm Delete", 
                System.Windows.MessageBoxButton.YesNo, 
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                // Unassign issues
                var affectedIssues = _allProjectIssues.Where(i => i.SprintId == SelectedSprint.Id).ToList();
                foreach (var issue in affectedIssues)
                {
                    issue.SprintId = null;
                    await _dataService.UpdateIssueAsync(issue);
                    await _dataService.AddLogAsync(new IssueLog { 
                        IssueId = issue.Id, 
                        Action = "Unassigned", 
                        Details = $"Removed from deleted sprint: {SelectedSprint.Name}" 
                    });
                }

                await _dataService.DeleteSprintAsync(SelectedSprint.Id);
                
                // Refresh data
                await LoadProjectDataAsync();
                SelectedSprint = null;
                IsSprintModalVisible = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CreateIssueAsync(string? status)
        {
            if (SelectedProject == null || string.IsNullOrEmpty(status)) return;

            var issue = new Issue
            {
                ProjectId = SelectedProject.Id,
                ProgramComponent = "New Component",
                Description = "Descriptive Title",
                Category = SelectedCategory,
                Status = status,
                Type = SelectedCategory == "Backlog" ? "Bug" : (SelectedCategory == "Pipeline" ? "Feature" : "Idea"),
                SprintId = SelectedSprint?.Id 
            };

            await _dataService.CreateIssueAsync(issue);
            _allProjectIssues.Add(issue);
            RefreshCategorizedIssues();
        }

        private async Task AssignToSprintAsync(Issue? issue)
        {
            if (issue == null || SelectedSprint == null) return;
            
            issue.SprintId = SelectedSprint.Id;
            await _dataService.UpdateIssueAsync(issue);
            await _dataService.AddLogAsync(new IssueLog { 
                IssueId = issue.Id, 
                Action = "Planned", 
                Details = $"Assigned to {SelectedSprint.Name}" 
            });
            RefreshCategorizedIssues();
        }

        private async Task CompleteSprintAsync()
        {
            if (SelectedSprint == null || SelectedProject == null) return;

            IsLoading = true;
            try
            {
                SelectedSprint.IsActive = false;
                await _dataService.UpdateSprintAsync(SelectedSprint);

                var unfinished = _allProjectIssues
                    .Where(i => i.SprintId == SelectedSprint.Id && i.Status != "Finished")
                    .ToList();

                // Find the next chronologically active sprint, or create one if none exist
                var nextSprint = Sprints.Where(s => s.IsActive && s.Id != SelectedSprint.Id && s.StartDate >= SelectedSprint.EndDate)
                                       .OrderBy(s => s.StartDate)
                                       .FirstOrDefault();

                if (nextSprint == null)
                {
                    nextSprint = new Sprint
                    {
                        ProjectId = SelectedProject.Id,
                        Name = $"Sprint {Sprints.Count + ArchivedSprints.Count + 1}",
                        StartDate = SelectedSprint.EndDate.AddDays(1),
                        EndDate = SelectedSprint.EndDate.AddDays(15),
                        IsActive = true
                    };
                    await _dataService.CreateSprintAsync(nextSprint);
                }

                foreach (var issue in unfinished)
                {
                    issue.SprintId = nextSprint.Id;
                    await _dataService.UpdateIssueAsync(issue);
                    await _dataService.AddLogAsync(new IssueLog { 
                        IssueId = issue.Id, 
                        Action = "Rollover", 
                        Details = $"Moved from {SelectedSprint.Name} to {nextSprint.Name} (Unfinished)" 
                    });
                }

                // Important: refresh both lists
                var updatedSprints = await _dataService.GetSprintsAsync(SelectedProject.Id);
                Sprints.Clear();
                ArchivedSprints.Clear();
                foreach (var s in updatedSprints.OrderBy(x => x.StartDate)) 
                {
                    if (s.IsActive) Sprints.Add(s);
                    else ArchivedSprints.Add(s);
                }

                SelectedSprint = Sprints.FirstOrDefault(sp => sp.Id == nextSprint.Id);
                RefreshCategorizedIssues();
                
                System.Windows.MessageBox.Show($"{unfinished.Count} unfinished issues moved to {nextSprint.Name}.", "Sprint Completed");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ShowLogsAsync(Issue? issue)
        {
            if (issue == null) return;
            var logs = await _dataService.GetLogsAsync(issue.Id);
            string logText = string.Join("\n", logs.Select(l => $"[{l.Timestamp:HH:mm}] {l.UserName}: {l.Action} ({l.Details})"));
            System.Windows.MessageBox.Show(logText, $"Timeline for: {issue.Description}");
        }
    }
}
